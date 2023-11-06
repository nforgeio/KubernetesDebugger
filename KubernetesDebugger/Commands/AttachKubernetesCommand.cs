//-----------------------------------------------------------------------------
// FILE:	    AttachKubernetesCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2023 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.Ffile.cop
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#pragma warning disable VSTHRD100   // Avoid "async void" methods

using System;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using EnvDTE80;

using k8s;
using k8s.Models;

using Neon.Common;
using Neon.IO;
using Neon.Net;

using Newtonsoft.Json.Linq;

using DialogResult = System.Windows.Forms.DialogResult;
using Task         = System.Threading.Tasks.Task;

namespace KubernetesDebugger
{
    /// <summary>
    /// Implements the <b>Attach Kubernetes</b> command handler.
    /// </summary>
    internal sealed class AttachKubernetesCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x8000;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("417f9fa6-cc1f-47ed-96ee-d42a8f5dbb95");

        /// <summary>
        /// Provides access to Visual Studio commands, etc.
        /// </summary>
        private readonly DTE2 dte;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachKubernetesCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private AttachKubernetesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.dte     = (DTE2)Package.GetGlobalService(typeof(SDTE));
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem      = new MenuCommand(this.Execute, menuCommandID);

            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AttachKubernetesCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in AttachKubernetesCommand's constructor requires
            // the UI thread.

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            Instance = new AttachKubernetesCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private async void Execute(object sender, EventArgs args)
        {
            // $todo(jefflill): Add SHELL button to dialog. 
            //
            // neon exec -it -n=neon-system neon-cluster-operator-2p22s -c vs-debug.neon-cluster-operator -- /bin/bash

            try
            {
                var dialog = new AttachToKubernetesDialog();

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var kubeConfig         = dialog.KubeConfig;
                    var k8s                = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: dialog.TargetContext));
                    var targetPod          = dialog.TargetPod;
                    var targetContainer    = dialog.TargetContainer;
                    var targetProcessId    = dialog.TargetProcessId;
                    var debugContainerName = dialog.DebugContainerName;

                    switch (dialog.Operation)
                    {
                        case AttachToKubernetesDialog.RequestedOperation.Attach:

                            await AttachDebugContainerAsync(k8s, targetPod, targetContainer, targetProcessId, debugContainerName);
                            break;

                        case AttachToKubernetesDialog.RequestedOperation.Trace:

                            // $todo(jefflill): Implement this.

                            throw new NotImplementedException();

                        default:

                            throw new NotImplementedException();
                    }
                }
            }
            catch (Exception e)
            {
                VsShellUtilities.ShowMessageBox(
                    KubernetesDebuggerPackage.Instance,
                    NeonHelper.ExceptionError(e),
                    "ERROR",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Attaches a new ephemeral debug container to the target container in the target pod if an ephemeral
        /// container is not already attached and then waits for the debug container to report being ready since
        /// it may take some time to load the debug container image and start it.
        /// </summary>
        /// <param name="k8s">Specifies the cluster Kubernetes client.</param>
        /// <param name="targetPod">Specifies the target pod.</param>
        /// <param name="targetContainer">Specifies the target container whose process namespace will be shared.</param>
        /// <param name="targetProcessId">Specifies the ID of the target process in the target container.</param>
        /// <param name="debugContainerName">Specifies the name for the ephemeral debug container.</param>
        /// <param name="timeout">Optionally specifies the maximum time to wait for the debug cointainer (defaults to 120 seconds).</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        /// <exception cref="TimeoutException">Thrown when the debug container didn't start in time.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a terminated debug container with the same name is already attached to the pod.</exception>
        private async Task AttachDebugContainerAsync(Kubernetes k8s, V1Pod targetPod, V1Container targetContainer, int targetProcessId, string debugContainerName, TimeSpan timeout = default)
        {
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));
            Covenant.Requires<ArgumentNullException>(targetPod != null, nameof(targetPod));
            Covenant.Requires<ArgumentNullException>(targetContainer != null, nameof(targetContainer));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(debugContainerName), nameof(debugContainerName));

Log.Info("AttachDebugContainerAsync: ENTER");
            if (timeout == default(TimeSpan))
            {
                timeout = TimeSpan.FromSeconds(120);
            }

            var debugContainer = targetPod.Spec.EphemeralContainers?.SingleOrDefault(container => container.Name == debugContainerName);

            // Attach an ephemeral debug container to the target container, if one isn't
            // already attached.  Note that this container starts a SSHD server.

            if (debugContainer == null)
            {
                var patchJson =
$@"
{{
    ""spec"":
    {{
        ""ephemeralContainers"":
        [
            {{
                ""name"": ""{debugContainerName}"",
                ""image"": ""ghcr.io/neonkube-stage/vs-debug:latest"",
                ""targetContainerName"": ""{targetContainer.Name}"",
                ""stdin"": false,
                ""tty"": false
            }}
        ]
    }}
}}
";
                var patchContent = new StringContent(patchJson, Encoding.UTF8, "application/strategic-merge-patch+json");

                await k8s.PatchSafeAsync(new Uri($"api/v1/namespaces/{targetPod.Namespace()}/pods/{targetPod.Name()}/ephemeralcontainers", UriKind.Relative), patchContent);
            }

            // Wait for the new debug container to report being ready.

            try
            {
                await NeonHelper.WaitForAsync(
                    async () =>
                    {
                        targetPod = await k8s.CoreV1.ReadNamespacedPodAsync(targetPod.Name(), targetPod.Namespace());

                        if (targetPod.Status.EphemeralContainerStatuses == null || targetPod.Status.EphemeralContainerStatuses.Count == 0)
                        {
                            return false;
                        }

                        var debugContainerStatus = targetPod.Status.EphemeralContainerStatuses.FirstOrDefault(status => status.Name == debugContainerName);

                        if (debugContainerStatus == null)
                        {
                            return false;
                        }

                        if (debugContainerStatus.State.Terminated != null)
                        {
                            // $todo(jefflill):
                            //
                            // I think we could relax this constraint by attaching another ephemeral container with
                            // a different name like "vs-debug.1.target", "vs-debug.2.target",...
                            //
                            // I'm not sure whether this wild be very useful though, since the user currently has to
                            // manually terminate the debug container by stopping the SSHD server running there.

                            throw new InvalidOperationException($"A terminated debug container named [{debugContainerName}] is already attached to pod [{targetPod.Namespace()}/{targetPod.Name()}].  You'll need to restart the pod to debug it.");
                        }

                        return debugContainerStatus.State.Running != null;
                    },
                    timeout:      timeout,
                    pollInterval: TimeSpan.FromSeconds(1));
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"Debug container [{debugContainerName}] attached to pod [{targetPod.Namespace()}/{targetPod.Name()}] did not start within [{timeout}].");
            }

            // Start a port forwarder for the debugger SSH connection to the pod and then spin up the debugger.

            var portForwarder = (PortForwarder)null;

            try
            {
                // Establish network proxy between a local loopback ephemeral port and the SSHD server
                // running in the new ephemeral container.

Log.Info("------------------------------------------------------------");
Log.Info("AttachDebugContainerAsync: 1");
                portForwarder = await PortForwarder.StartAsync(k8s, targetPod.Name(), targetPod.Namespace(), NetworkPorts.SSH, PortForwarder.ConnectionMode.Single);
Log.Info("AttachDebugContainerAsync: 2");

                // Generate a temporary [launch.json] file and launch the VS debugger.

                using (var tempFile = await CreateLaunchSettingsAsync(portForwarder, targetProcessId))
                {
                    //###############################
                    // $debug(jefflill): DELETE THIS!

                    var tempFilePath = @"C:\Temp\launch.json";

                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }

                    File.Copy(tempFile.Path, tempFilePath);

                    //await Task.Delay(-1);
                    //###############################

                    try
                    {
Log.Info("AttachDebugContainerAsync: 3");
                        dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{tempFile.Path}\"");
Log.Info("AttachDebugContainerAsync: 4");
                    }
                    catch (Exception e)
                    {
Log.Error($"AttachDebugContainerAsync: 5 : {NeonHelper.ExceptionError(e)}");
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
Log.Info("AttachDebugContainerAsync: 6");
                portForwarder?.Dispose();

                VsShellUtilities.ShowMessageBox(
                    KubernetesDebuggerPackage.Instance,
                    NeonHelper.ExceptionError(e),
                    "ERROR",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
Log.Info("AttachDebugContainerAsync: EXIT");
        }

        /// <summary>
        /// Creates the temporary launch settings file we'll use for launching <b>vsdbg</b> in
        /// the ephemeral container.
        /// </summary>
        /// <param name="portForwarder">Specifies the port forwarder being used.</param>
        /// <param name="targetProcessId">Specifies the ID of the target process in the target container.</param>
        /// <returns>The <see cref="TempFile"/> referencing the created launch file.</returns>
        private async Task<TempFile> CreateLaunchSettingsAsync(PortForwarder portForwarder, int targetProcessId)
        {
            Covenant.Requires<ArgumentNullException>(portForwarder != null, nameof(portForwarder));

            // Here's information about how this works:
            //
            //      https://github.com/Microsoft/MIEngine/wiki/Offroad-Debugging-of-.NET-Core-on-Linux---OSX-from-Visual-Studio
            //      https://github.com/Microsoft/MIEngine/wiki/Offroad-Debugging-of-.NET-Core-on-Linux---OSX-from-Visual-Studio#attaching

            var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            var settings =
                new JObject
                (
                    new JProperty("version", "0.2.1"),
                    new JProperty("adapter", Path.Combine(systemRoot, "System32", "OpenSSH", "ssh.exe")),
                    new JProperty("adapterArgs", $"-o \"StrictHostKeyChecking no\" root@127.0.0.1 -p {portForwarder.LocalEndpoint.Port} /vsdbg/vsdbg --interpreter=vscode"),
                    new JProperty("configurations",
                        new JArray
                        (
                            new JObject
                            (
                                new JProperty("name", $"Attach to process: {targetProcessId}"),
                                new JProperty("type", "coreclr"),
                                new JProperty("request", "attach"),
                                new JProperty("processId", targetProcessId)
                            )
                        )
                    )
                );

            var tempFile = new TempFile(".launch.json", folder: @"C:\Temp");    // $debug(jefflill): REMOVE THE FOLDER ARG!

            using (var stream = new FileStream(tempFile.Path, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes(settings.ToString()));
            }

            return tempFile;
        }
    }
}
