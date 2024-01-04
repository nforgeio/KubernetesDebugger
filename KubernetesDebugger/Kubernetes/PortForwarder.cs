//-----------------------------------------------------------------------------
// FILE:	    PortForwarder.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2024 by neonFORGE, LLC.  All rights reserved.
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

// $todo(jefflill):
//
// I wasn't able to get this to work.  It does seem to be able to transmit data
// between a local socket and the remote pod, but the SSH protocol negotiation
// looks like it's crapping out.
//
// I'm going to recode this by using the [neon/kubectl port-forward] command and
// see whether that works.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using k8s;
using k8s.Models;
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
            /// <param name="kubectlProcess">Specifies the neon/kubectl process handling the port forwarding.</param>
            public Connection(Process kubectlProcess)
            {
                Covenant.Requires<ArgumentNullException>(kubectlProcess != null, nameof(kubectlProcess));

                this.KubectlProcess = kubectlProcess;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;

                    KubectlProcess?.Kill();
                    KubectlProcess = null;

                    GC.SuppressFinalize(this);
                }
            }

            /// <summary>
            /// Returns the unique ID for the connection.
            /// </summary>
            public Guid Id { get; private set; } = Guid.NewGuid();

            /// <summary>
            /// Holds the neon/kubectl process handling the port forwarding.
            /// </summary>
            public Process KubectlProcess;
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
        /// <returns></returns>
        public static async Task<PortForwarder> StartAsync(IKubernetes k8s, string name, string @namespace, int podPort, ConnectionMode mode = ConnectionMode.Normal)
        {
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(@namespace), nameof(@namespace));

            // Create a temporary websocket to ensure that the pod exists and is ready for connections.

            using var webSocket = await k8s.WebSocketNamespacedPodPortForwardAsync(name, @namespace, new int[] { podPort });

            return new PortForwarder(k8s, name, @namespace, podPort, mode);
        }

        //---------------------------------------------------------------------
        // Instance members.

        private IKubernetes                     k8s;
        private string                          podName;
        private string                          podNamespace;
        private ConnectionMode                  mode;
        private bool                            isDisposed;
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
        internal PortForwarder(IKubernetes k8s, string name, string @namespace, int podPort, ConnectionMode mode)
        {
            this.k8s           = k8s;
            this.podName       = name;
            this.podNamespace  = @namespace;
            this.mode          = mode;
            this.PodPort       = podPort;
            this.LocalEndpoint = new IPEndPoint(IPAddress.Loopback, NetHelper.GetUnusedTcpPort());

            try
            {
                var startInfo = new ProcessStartInfo(AttachKubernetesCommand.KubectlPath, $"port-forward -n {podNamespace} pod/{podName} {LocalEndpoint.Port}:{NetworkPorts.SSH}")
                {
                    CreateNoWindow = true
                };

                var kubectlProcess = Process.Start(startInfo);

                AddConnection(new Connection(kubectlProcess));
            }
            catch
            {
                VsShellUtilities.ShowMessageBox(
                    KubernetesDebuggerPackage.Instance,
                    "Cannot launch the [neon.exe] or [kubectl.exe] client.",
                    "ERROR: Attach Kubernetes",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                return;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (syncLock)
            {
                if (!isDisposed)
                {
                    isDisposed = true;

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
    }
}
