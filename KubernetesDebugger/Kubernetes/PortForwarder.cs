//-----------------------------------------------------------------------------
// FILE:	    AttachKubernetesCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2023 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using k8s;
using k8s.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Neon.Common;
using Neon.Net;

namespace KubernetesDebugger
{
    /// <summary>
    /// Implements a socket proxy that forwards traffic between the local machine and 
    /// a Kubernetes pod container listening on one or more ports.
    /// </summary>
    public sealed class PortForwarder : IDisposable
    {
        //---------------------------------------------------------------------
        // Local types.

        /// <summary>
        /// Specifies port forwarder connection modes.
        /// </summary>
        public enum ConnectionMode
        {
            /// <summary>
            /// Indicates that multiple connections to the pod is supported.
            /// </summary>
            Normal = 0,

            /// <summary>
            /// Indicates that only a single connection to the pod wis allowed.
            /// </summary>
            Single
        }

        /// <summary>
        /// Used to track co9nnections.
        /// </summary>
        private class Connection : IDisposable
        {
            private bool isDisposed;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="localSocket">Specifies the local socket.</param>
            /// <param name="webSocket">Specifies the websocket to the remote pod.</param>
            /// <param name="demuxer">Specifies the websocket demuxer.</param>
            public Connection(Socket localSocket, WebSocket webSocket, StreamDemuxer demuxer)
            {
                this.LocalSocket  = localSocket;
                this.WebSocket    = webSocket;
                this.Demuxer      = demuxer;
                this.RemoteStream = demuxer.GetStream((byte?)0, (byte?)0);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;

                    LocalSocket?.Dispose();
                    LocalSocket = null;

                    RemoteStream?.Dispose();
                    RemoteStream = null;

                    Demuxer?.Dispose();
                    Demuxer = null;

                    WebSocket?.Dispose();
                    WebSocket = null;

                    GC.SuppressFinalize(this);
                }
            }

            /// <summary>
            /// Returns the unique ID for the connection.
            /// </summary>
            public Guid Id { get; private set; } = Guid.NewGuid();

            /// <summary>
            /// Holds the local socket.
            /// </summary>
            public Socket LocalSocket;

            /// <summary>
            /// Holds the websocket to the remote pod.
            /// </summary>
            public WebSocket WebSocket;

            /// <summary>
            /// Holds the websocket demuxer.
            /// </summary>
            public StreamDemuxer Demuxer;

            /// <summary>
            /// Holds the stream to the remote pod.
            /// </summary>
            public Stream RemoteStream;
        }

        //---------------------------------------------------------------------
        // Static members.

        private const int BufferSize = 16384;

        /// <summary>
        /// <para>
        /// Starts a port forwarder that proxies TCP socket traffic between the local machine and
        /// a specific port within a pod.  You can specify a specific local port or let the method
        /// select an unused ephemeral port.  A <see cref="PortForwarder"/> instance will be returned
        /// that implements the proxy.  The local endpoint can be determined by <see cref="LocalEndpoint"/>.
        /// </para>
        /// <para>
        /// Stop the forwarder by calling <see cref="Dispose"/>.
        /// </para>
        /// </summary>
        /// <param name="k8s">Specifies the Kubernetes client.</param>
        /// <param name="name">Specifies the target pod name.</param>
        /// <param name="namespace">Specifies the target pod namespace.</param>
        /// <param name="podPort">Specifies the target pod network port.</param>
        /// <param name="mode">Specifes the connection mode (defaults to <see cref="ConnectionMode.Normal"/>).</param>
        /// <param name="localPort">Optionally specifies the local port (defaults to an unused ephemeral port).</param>
        /// <returns></returns>
        public static async Task<PortForwarder> StartAsync(IKubernetes k8s, string name, string @namespace, int podPort, ConnectionMode mode = ConnectionMode.Normal, int localPort = 0)
        {
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(@namespace), nameof(@namespace));

            // Create a temporary websocket to ensure that the pod exists and is ready for connections.

            using var webSocket = await k8s.WebSocketNamespacedPodPortForwardAsync(name, @namespace, new int[] { podPort }, WebSocketProtocol.ChannelWebSocketProtocol);

            return await Task.FromResult(new PortForwarder(k8s, name, @namespace, podPort, mode, localPort));
        }

        //---------------------------------------------------------------------
        // Instance members.

        private IKubernetes                     k8s;
        private string                          podName;
        private string                          podNamespace;
        private ConnectionMode                  mode;
        private bool                            isDisposed;
        private Socket                          listener;
        private Dictionary<Guid, Connection>    connections = new Dictionary<Guid, Connection>();
        private CancellationTokenSource         cts         = new CancellationTokenSource();
        private object                          syncLock    = new object();

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="k8s">Specifies the Kubernetes client.</param>
        /// <param name="name">Specifies the target pod name.</param>
        /// <param name="namespace">Specifies the target pod namespace.</param>
        /// <param name="podPort">Specifies the target pod port.</param>
        /// <param name="mode">Specifes the connection mode.</param>
        /// <param name="localPort">Specifies the local port or 0 to choose an unused ephemeral port.</param>
        internal PortForwarder(IKubernetes k8s, string name, string @namespace, int podPort, ConnectionMode mode, int localPort)
        {
            this.k8s          = k8s;
            this.podName      = name;
            this.podNamespace = @namespace;
            this.mode         = mode;
            this.PodPort      = podPort;
            this.listener     = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(new IPEndPoint(IPAddress.Loopback, localPort));
            listener.Listen(100);

            this.LocalEndpoint = (IPEndPoint)listener.LocalEndPoint;

            _ = ListenAsync();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (syncLock)
            {
                if (!isDisposed)
                {
                    isDisposed = true;

                    listener.Dispose();
                    listener = null;

                    lock (connections)
                    {
                        foreach (var connection in connections.Values)
                        {
                            connection.Dispose();
                        }

                        connections.Clear();
                    }

                    cts.Cancel();

                    GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>
        /// Returns the network port where network traffic will be forwarded in the target pad.
        /// </summary>
        public int PodPort { get; private set; }

        /// <summary>
        /// Returns the network endpoint listening on the local machine which can be used
        /// to establish connections through the proxy to the pod.
        /// </summary>
        public IPEndPoint LocalEndpoint { get; private set; }

        /// <summary>
        /// Listens for local socket connections and then starts proxying traffic to the pod.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task ListenAsync()
        {
            // We're going to spin up a proxy for every connection to the local
            // listening socket and let that run as an independent task until the
            // connection is closed.
            //
            // We're going to keep track of the connection related objects so we
            // can dispose them when the parent [PortForwarder] is disposed.

            try
            {
                while (!isDisposed)
                {
                    _ = ProxyAsync(await listener.AcceptAsync());

                    if (mode == ConnectionMode.Single)
                    {
                        break;
                    }
                }

                // Stop listening on the local port and then wait for the CTS 
                // to be cancelled.

                lock (syncLock)
                {
                    listener.Dispose();
                    listener = null;
                }

                await Task.Delay(-1, cts.Token);
            }
            catch
            {
                // Intentionally ignoring errors.
            }
        }

        /// <summary>
        /// Adds a connection to the connection dictionary.
        /// </summary>
        /// <param name="connection">Specifies the connection to be added.</param>
        private void AddConnection(Connection connection)
        {
            lock (syncLock)
            {
                connections.Add(connection.Id, connection);
            }
        }

        /// <summary>
        /// Removes a connection from the connection dictionary.
        /// </summary>
        /// <param name="connection">Specifies the connection to be removed.</param>
        private void RemoveConnection(Connection connection)
        {
            lock (syncLock)
            {
                connections.Remove(connection.Id);
            }
        }

        /// <summary>
        /// Proxies traffic between the local socket and the pod.
        /// </summary>
        /// <param name="localSocket">Specifies the local socket.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task ProxyAsync(Socket localSocket)
        {
            var webSocket  = await k8s.WebSocketNamespacedPodPortForwardAsync(podName, podNamespace, new int[] { PodPort }, WebSocketProtocol.ChannelWebSocketProtocol);
            var demuxer    = new StreamDemuxer(webSocket, StreamType.PortForward);
            var connection = new Connection(localSocket, webSocket, demuxer);

            AddConnection(connection);

            try
            {
                var tasks = new Task[]
                {
                    SendLoopAsync(connection),
                    ReceiveLoopAsync(connection)
                };

                await Task.WhenAll(tasks);
            }
            catch
            {
                // Intentionally ignoring errors.
            }
            finally
            {
                RemoveConnection(connection);
                connection.Dispose();
            }
        }

        /// <summary>
        /// Handles forwarding of traffic sent by the connection's local socket to the remote pod.
        /// </summary>
        /// <param name="connection">Specifies the connection.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task SendLoopAsync(Connection connection)
        {
            var localSocket = connection.LocalSocket;
            var buffer      = new byte[BufferSize];

            try
            {
                while (true)
                {
                    var cbRead = await localSocket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, BufferSize), SocketFlags.None);

                    if (cbRead == 0)
                    {
                        return; // EOF
                    }

                    await connection.RemoteStream.WriteAsync(buffer, 0, cbRead);
                }
            }
            catch
            {
                // Intentionally ignoring errors.
            }
        }

        /// <summary>
        /// Handles forwarding of traffic sent by the remote pod to the local socket.
        /// </summary>
        /// <param name="connection">Specifies the connection.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task ReceiveLoopAsync(Connection connection)
        {
            var buffer = new byte[BufferSize];

            try
            {
                while (true)
                {
                    var cbRead      = await connection.RemoteStream.ReadAsync(buffer,0, BufferSize);
                    var cbRemaining = cbRead;

                    while (cbRemaining > 0)
                    {
                        cbRemaining -= await connection.LocalSocket.SendAsync(new ArraySegment<byte>(buffer, cbRead - cbRemaining, cbRemaining), SocketFlags.None);
                    }
                }
            }
            catch
            {
                // Intentionally ignoring errors.
            }
        }
    }
}
