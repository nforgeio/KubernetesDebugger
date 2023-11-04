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

#pragma warning disable VSTHRD100   // Avoid "async void" methods

using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using k8s;
using k8s.KubeConfigModels;
using k8s.Models;

using Neon.Common;

using DialogResult = System.Windows.Forms.DialogResult;
using Task         = System.Threading.Tasks.Task;

namespace KubernetesDebugger
{
    /// <summary>
    /// Command handler
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
        /// <param name="sender">Specifies the sender.</param>
        /// <param name="args">specifies the arguments.</param>
        private async void Execute(object sender, EventArgs args)
        {
            try
            {
                var dialog = new AttachToKubernetesDialog();

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var kubeConfig         = dialog.KubeConfig;
                    var k8s                = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: dialog.TargetContext));
                    var targetPod          = dialog.TargetPod;
                    var targetContainer    = dialog.TargetContainer;
                    var debugContainerName = $"vs-debug-{targetContainer.Name}";
                    var debugContainer     = targetPod.Spec.EphemeralContainers?.SingleOrDefault(container => container.Name == debugContainerName);

                    // Attach an ephemeral debug container to the target container, if one isn't
                    // already attached.  Note that this container starts a SSHD server.

                    if (debugContainer == null)
                    {
                        debugContainer = new V1EphemeralContainer(
                            name:                debugContainerName,
                            image:               "ghcr.io/neonkube-stage/vs-debug:latest",
                            imagePullPolicy:     "IfNotPresent",
                            targetContainerName: targetContainer.Name);

                        // $todo(jefflill):

                        throw new NotImplementedException();

                        // await k8s.HttpClient.PatchAsync(uri, content);
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
    }
}
