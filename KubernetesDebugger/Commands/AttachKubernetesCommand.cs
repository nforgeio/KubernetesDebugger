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
using EnvDTE;

namespace KubernetesDebugger
{
    /// <summary>
    /// Implements the <b>Attach Kubernetes</b> command handler.
    /// </summary>
    internal sealed class AttachKubernetesCommand
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x8000;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("417f9fa6-cc1f-47ed-96ee-d42a8f5dbb95");

        /// <summary>
        /// Returns the path to the neon or kubectl executable or <c>null</c> when neither
        /// of these can be located.
        /// </summary>
        public static string KubectlPath { get; private set; }

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

            // We're currently using neon/kubectl to handle port forwarding from the
            // local machine to the remote pod for the Visual Studio SSH debug adapter.
            //
            // We need to locate one of these on the PATH.  We're going to favor the neon
            // client if present.  We're going to remember this for the current Visual Studio
            // session.  If we don't find a client, we'll report that to the user when the
            // debug command is executed.

            // Look for: neon.exe

            if (string.IsNullOrEmpty(KubectlPath))
            {
                foreach (var rawPath in Environment.GetEnvironmentVariable("PATH").Split(';'))
                {
                    var path = rawPath.Trim();

                    if (path == string.Empty)
                    {
                        continue;
                    }

                    if (Directory.Exists(path))
                    {
                        var cliPath = Path.Combine(path, "neon.exe");

                        if (File.Exists(cliPath))
                        {
                            KubectlPath = cliPath;
                            break;
                        }
                    }
                }
            }

            // Special-case maintainer build machines looking for: %NC_ROOT%\Build\neon-cli\neon.exe

            if (NeonHelper.IsMaintainer && string.IsNullOrEmpty(KubectlPath))
            {
                var ncRoot = Environment.GetEnvironmentVariable("NC_ROOT");

                if (ncRoot != null)
                {
                    var path = Path.Combine(ncRoot, "Build", "neon-cli", "neon.exe");

                    if (File.Exists(path))
                    {
                        KubectlPath = path;
                    }
                }
            }

            // Look for: kubectl.exe

            if (string.IsNullOrEmpty(KubectlPath))
            {
                foreach (var rawPath in Environment.GetEnvironmentVariable("PATH").Split(';'))
                {
                    var path = rawPath.Trim();

                    if (path == string.Empty)
                    {
                        continue;
                    }

                    if (Directory.Exists(path))
                    {
                        var cliPath = Path.Combine(path, "kubectl.exe");

                        if (File.Exists(cliPath))
                        {
                            KubectlPath = cliPath;
                            break;
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Instance members

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
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private async void Execute(object sender, EventArgs args)
        {
            if (string.IsNullOrEmpty(KubectlPath))
            {
                VsShellUtilities.ShowMessageBox(
                    KubernetesDebuggerPackage.Instance,
                    "Cannot locate the [neon.exe] or [kubectl.exe] Kubernetes client on the PATH.",
                    "ERROR: Attach Kubernetes",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                return;
            }

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
                    "ERROR: Attach Kubernetes",
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
        private async Task AttachDebugContainerAsync(
            Kubernetes      k8s, 
            V1Pod           targetPod, 
            V1Container     targetContainer, 
            int             targetProcessId, 
            string          debugContainerName, 
            TimeSpan        timeout = default)
        {
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));
            Covenant.Requires<ArgumentNullException>(targetPod != null, nameof(targetPod));
            Covenant.Requires<ArgumentNullException>(targetContainer != null, nameof(targetContainer));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(debugContainerName), nameof(debugContainerName));

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
                            // a different name like "vs-debug-1-target", "vs-debug-2-target",...
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

                portForwarder = await PortForwarder.StartAsync(k8s, targetPod.Name(), targetPod.Namespace(), NetworkPorts.SSH, PortForwarder.ConnectionMode.Single);

                // Generate a temporary [launch.json] file and launch the VS debugger.

                using (var launchInfo = await CreateLaunchSettingsAsync(portForwarder, targetProcessId))
                {
                    try
                    {
                        dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{launchInfo.SettingPath}\"");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                portForwarder?.Dispose();

                VsShellUtilities.ShowMessageBox(
                    KubernetesDebuggerPackage.Instance,
                    NeonHelper.ExceptionError(e),
                    "ERROR: Attach Kubernetes",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Holds debugger launch related information.
        /// </summary>
        public sealed class LaunchInfo : IDisposable
        {
            private TempFolder      folder;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="folder">Specifies the temporary folder holding the launch setting related files.</param>
            /// <param name="settingsPath">Specifies the path to the generated launch file.</param>
            public LaunchInfo(TempFolder folder, string settingsPath)
            {
                Covenant.Requires<ArgumentNullException>(folder != null, nameof(folder));
                Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(settingsPath), nameof(settingsPath));

                this.folder      = folder;
                this.SettingPath = settingsPath;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                folder?.Dispose();
                folder = null;
            }

            /// <summary>
            /// Returns the path to the generated launch settings file.
            /// </summary>
            public string SettingPath { get; private set; }
        }

        /// <summary>
        /// Creates the temporary launch settings and SSH private key files we'll use for launching <b>vsdbg</b> in
        /// the ephemeral container.
        /// </summary>
        /// <param name="portForwarder">Specifies the port forwarder being used.</param>
        /// <param name="targetProcessId">Specifies the ID of the target process in the target container.</param>
        /// <param name="launchPath">Returns as the path to the launch file.</param>
        /// <returns>The generated <see cref="LaunchInfo"/>.  Be sure to dispose this such that the folder holding the files is deleted.</returns>
        private async Task<LaunchInfo> CreateLaunchSettingsAsync(PortForwarder portForwarder, int targetProcessId)
        {
            Covenant.Requires<ArgumentNullException>(portForwarder != null, nameof(portForwarder));

            // Here's information about how this works:
            //
            //      https://github.com/Microsoft/MIEngine/wiki/Offroad-Debugging-of-.NET-Core-on-Linux---OSX-from-Visual-Studio
            //      https://github.com/Microsoft/MIEngine/wiki/Offroad-Debugging-of-.NET-Core-on-Linux---OSX-from-Visual-Studio#attaching

            var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var tempFolder = new TempFolder(folder: Path.Combine(userFolder, ".ssh"), prefix: "vs-debug-"); // This needs to be here for security
            var keyPath    = Path.Combine(tempFolder.Path, "key");

            var settings =
                new JObject
                (
                    new JProperty("version", "0.2.1"),
                    new JProperty("adapter", Path.Combine(systemRoot, "System32", "OpenSSH", "ssh.exe")),
                    new JProperty("adapterArgs", $"-o \"StrictHostKeyChecking no\" root@127.0.0.1 -i \"{keyPath}\" -p {portForwarder.LocalEndpoint.Port} /vsdbg/vsdbg --interpreter=vscode --engineLogging=/vsdbg.log"),
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

            var launchPath = Path.Combine(tempFolder.Path, "launch.json");

            File.WriteAllText(keyPath, KubernetesDebuggerPackage.PrivateSshKey);
            File.WriteAllText(launchPath, settings.ToString(Newtonsoft.Json.Formatting.Indented));

            return await Task.FromResult(new LaunchInfo(tempFolder, launchPath));
        }
    }
}
