//-----------------------------------------------------------------------------
// FILE:	    AttachToKubernetesDialog.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using k8s;
using k8s.KubeConfigModels;
using k8s.Models;

using Neon.Common;

using EnvDTE;
using System.Diagnostics.Contracts;

namespace KubernetesDebugger
{
    /// <summary>
    /// Implements the pod/process attachment dialog.
    /// </summary>
    public partial class AttachToKubernetesDialog : Form
    {
        //---------------------------------------------------------------------
        // Static members

        // We're going to remember the context and namespace from previous runs.


        private static string   previousContext;
        private static string   previousNamespace;
        private static string   previousPod;

        //---------------------------------------------------------------------
        // Instance members

        // Control layout information required for dialog resizing.

        private int     contextComboBoxRightMargin;
        private int     namespaceComboBoxRightMargin;
        private int     podComboBoxRightMargin;
        private int     containerComboBoxRightMargin;
        private int     instructionsGroupBoxRightMargin;
        private int     instructionsTextBoxRightMargin;
        private int     processesGroupBoxRightMargin;
        private int     processesGroupBoxBottomMargin;
        private int     processErrorLabelRightMargin;
        private int     processErrorLabelBottomMargin;
        private int     processesGridRightMargin;
        private int     processesGridBottomMargin;
        private int     attachButtonRightMargin;
        private int     attachButtonBottomMargin;
        private int     cancelButtonRightMargin;
        private int     cancelButtonBottomMargin;

        // Other state

        private bool                ignoreComboBoxEvents;
        private bool                comError;
        private List<V1Pod>         currentPods       = new List<V1Pod>();
        private List<V1Container>   currentContainers = new List<V1Container>();
        private string              psError           = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AttachToKubernetesDialog()
        {
            InitializeComponent();

            Load              += AttachToKubernetesDialog_Load;
            ClientSizeChanged += AttachToKubernetesDialog_ClientSizeChanged;
        }

        /// <summary>
        /// Set to the selected Kubernetes configuration after the user clicks Attach.
        /// </summary>
        public K8SConfiguration KubeConfig { get; private set; }

        /// <summary>
        /// Set to the selected Kubernetes context name after the user clicks Attach.
        /// </summary>
        public string TargetContext { get; private set; }

        /// <summary>
        /// Set to the selected cluster pod after the user clicks Attach.
        /// </summary>
        public V1Pod TargetPod { get; private set; }

        /// <summary>
        /// Set to the selected cluster container after the user clicks Attach.
        /// </summary>
        public V1Container TargetContainer { get; private set; }

        /// <summary>
        /// Set to the target process ID after the user clicks attach.
        /// </summary>
        public int Pid { get; private set; }

        /// <summary>
        /// Handles initial form loading.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private async void AttachToKubernetesDialog_Load(object sender, EventArgs args)
        {
            // Replace any "\r\n" sequences in the instructions with actual line endings.

            instructionsTextBox.Text = instructionsTextBox.Text.Replace(@"\r\n", Environment.NewLine);

            // Set the position of the [noProcessesLabel] to be the same as
            // the processes grid and then hide it.

            processErrorLabel.Left    = processesGrid.Left;
            processErrorLabel.Top     = processesGrid.Top;
            processErrorLabel.Width   = processesGrid.Width;
            processErrorLabel.Height  = processesGrid.Height;

            ClearProcessError();

            // Compute the control right and bottom margins so we'll be able to
            // relocate the controls when the user resizes the dialog.

            contextComboBoxRightMargin      = GetRightMargin(contextComboBox);
            namespaceComboBoxRightMargin    = GetRightMargin(namespaceComboBox);
            podComboBoxRightMargin          = GetRightMargin(podComboBox);
            containerComboBoxRightMargin    = GetRightMargin(containerComboBox);
            instructionsGroupBoxRightMargin = GetRightMargin(instructionsGroupBox);
            instructionsTextBoxRightMargin  = GetRightMargin(instructionsTextBox);
            processesGroupBoxRightMargin    = GetRightMargin(processesGroupBox);
            processesGridRightMargin        = GetRightMargin(processesGrid);
            processErrorLabelRightMargin    = GetRightMargin(processErrorLabel);
            attachButtonRightMargin         = GetRightMargin(attachButton);
            cancelButtonRightMargin         = GetRightMargin(cancelButton);

            processesGroupBoxBottomMargin   = GetBottomMargin(processesGroupBox);
            processesGridBottomMargin       = GetBottomMargin(processesGrid);
            processErrorLabelBottomMargin   = GetBottomMargin(processErrorLabel);
            attachButtonBottomMargin        = GetBottomMargin(attachButton);
            cancelButtonBottomMargin        = GetBottomMargin(cancelButton);

            try
            {
                // Load the default kubeconfig file and then initialize the dialog comboboxes.

                try
                {
                    // Disable handling of combobox selection events while we're
                    // initializing things for better performance.

                    ignoreComboBoxEvents = true;

                    await LoadKubeConfigAsync();
                    await LoadNamespacesAsync();
                    await LoadPodsAsync();
                    await LoadContainersAsync();
                    await LoadProcessesAsync();

                    SetProcessErrorState();
                    SetAttachButtonEnabledState();
                }
                catch (Exception e)
                {
                    SetProcessComError(e);
                    SetAttachButtonEnabledState();
                }
                finally
                {
                    ignoreComboBoxEvents = false;
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
        /// Relocates the controls when the user resizes the dialog.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private void AttachToKubernetesDialog_ClientSizeChanged(object sender, EventArgs args)
        {
            contextComboBox.Width      = ClientSize.Width - (contextComboBoxRightMargin + contextComboBox.Left);
            namespaceComboBox.Width    = ClientSize.Width - (namespaceComboBoxRightMargin + namespaceComboBox.Left);
            podComboBox.Width          = ClientSize.Width - (podComboBoxRightMargin + podComboBox.Left);
            containerComboBox.Width    = ClientSize.Width - (containerComboBoxRightMargin + containerComboBox.Left);
            instructionsGroupBox.Width = ClientSize.Width - (instructionsGroupBoxRightMargin + instructionsGroupBox.Left);
            instructionsTextBox.Width  = ClientSize.Width - (instructionsTextBoxRightMargin + instructionsTextBox.Left);
            processesGroupBox.Width    = ClientSize.Width - (processesGroupBoxRightMargin + processesGroupBox.Left);
            processesGrid.Width        = ClientSize.Width - (processesGridRightMargin + processesGrid.Left);
            processErrorLabel.Width    = ClientSize.Width - (processErrorLabelRightMargin + processErrorLabel.Left);

            attachButton.Left          = ClientSize.Width - (attachButtonRightMargin + attachButton.Width);
            cancelButton.Left          = ClientSize.Width - (cancelButtonRightMargin + cancelButton.Width);

            attachButton.Top           = ClientSize.Height - (attachButtonBottomMargin + attachButton.Height);
            cancelButton.Top           = ClientSize.Height - (cancelButtonBottomMargin + cancelButton.Height);

            processesGroupBox.Height   = ClientSize.Height - (processesGroupBoxBottomMargin + processesGroupBox.Top);
            processesGrid.Height       = ClientSize.Height - (processesGridBottomMargin + processesGrid.Top);
            processErrorLabel.Height   = ClientSize.Height - (processErrorLabelBottomMargin + processErrorLabel.Top);
        }

        /// <summary>
        /// Computes the distance between the right side of a control and the right side of the dialog window.
        /// </summary>
        /// <param name="control">Specifies the control.</param>
        /// <returns>The right margin.</returns>
        private int GetRightMargin(Control control)
        {
            return ClientSize.Width - (control.Left + control.Width);
        }

        /// <summary>
        /// Computes the distance between the bottom side of a control and the bottom side of the dialog window.
        /// </summary>
        /// <param name="control">Specifies the control.</param>
        /// <returns>The bottom margin.</returns>
        private int GetBottomMargin(Control control)
        {
            return ClientSize.Height - (control.Top + control.Height);
        }

        /// <summary>
        /// Handles context combobox selection changes.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private async void contextComboBox_SelectedValueChanged(object sender, EventArgs args)
        {
            if (ignoreComboBoxEvents)
            {
                return;
            }

            try
            {
                ignoreComboBoxEvents = true;

                await LoadNamespacesAsync();
                await LoadPodsAsync();
                await LoadContainersAsync();
                await LoadProcessesAsync();

                SetProcessErrorState();
                SetAttachButtonEnabledState();
            }
            finally
            {
                comError             = false;
                ignoreComboBoxEvents = false;
            }
        }

        /// <summary>
        /// Handles namespace combobox selection changes.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private async void namespaceComboBox_SelectedValueChanged(object sender, EventArgs args)
        {
            if (ignoreComboBoxEvents)
            {
                return;
            }

            try
            {
                ignoreComboBoxEvents = true;

                await LoadPodsAsync();
                await LoadContainersAsync();
                await LoadProcessesAsync();

                SetProcessErrorState();
                SetAttachButtonEnabledState();
            }
            finally
            {
                comError             = false;
                ignoreComboBoxEvents = false;
            }
        }

        /// <summary>
        /// Handles pod combobox selection changes.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private async void podComboBox_SelectedValueChanged(object sender, EventArgs args)
        {
            if (ignoreComboBoxEvents)
            {
                return;
            }

            try
            {
                ignoreComboBoxEvents = true;
                TargetPod           = currentPods.FirstOrDefault(pod => pod.Name() == (string)podComboBox.SelectedItem);

                await LoadContainersAsync();
                await LoadProcessesAsync();

                SetProcessErrorState();
                SetAttachButtonEnabledState();
            }
            finally
            {
                comError             = false;
                ignoreComboBoxEvents = false;
            }
        }

        /// <summary>
        /// Handles container combobox selection changes.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private async void containerComboBox_SelectedValueChanged(object sender, EventArgs args)
        {
            if (ignoreComboBoxEvents)
            {
                return;
            }

            try
            {
                ignoreComboBoxEvents = true;
                TargetContainer     = currentContainers.FirstOrDefault(container => container.Name == (string)containerComboBox.SelectedItem);

                await LoadProcessesAsync();
            }
            finally
            {
                comError             = false;
                ignoreComboBoxEvents = false;
            }
        }

        /// <summary>
        /// Handles process grid selection changes.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private void processesGrid_SelectionChanged(object sender, EventArgs args)
        {
            SetAttachButtonEnabledState();
        }

        /// <summary>
        /// Handles process grid double-clicks.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private void processesGrid_DoubleClick(object sender, EventArgs args)
        {
            if (attachButton.Enabled)
            {
                attachButton_Click(sender, args);
            }
        }

        /// <summary>
        /// Handles attach button clicks.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private void attachButton_Click(object sender, EventArgs args)
        {
            Pid            = (int)processesGrid.SelectedRows[0].Cells[0].Value;
            TargetContext = (string)contextComboBox.SelectedItem;

            previousContext   = (string)contextComboBox.SelectedItem;
            previousNamespace = (string)namespaceComboBox.SelectedItem;

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Handles cancel button clicks.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private void cancelButton_Click(object sender, EventArgs args)
        {
            previousContext   = (string)contextComboBox.SelectedItem;
            previousNamespace = (string)namespaceComboBox.SelectedItem;

            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Loads the standard kubeconfig file into <see cref="KubeConfig"/> and also loads
        /// the context names into the contexts combobox, selecting the current one.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task LoadKubeConfigAsync()
        {
            KubeConfig = await k8s.KubernetesClientConfiguration.LoadKubeConfigAsync();

            this.InvokeOnUiThread(
                () =>
                {
                    contextComboBox.Items.Clear();

                    foreach (var context in KubeConfig.Contexts
                        .OrderBy(context => context.Name, StringComparer.CurrentCultureIgnoreCase))
                    {
                        contextComboBox.Items.Add(context.Name);
                    }

                    // When we saved the context from a previous run of the dialog,
                    // try to select that context, otherwise select the current context.

                    if (previousContext != null && KubeConfig.Contexts.Any(context => context.Name == previousContext))
                    {
                        contextComboBox.SelectedItem = previousContext;
                    }
                    else if (!string.IsNullOrEmpty(KubeConfig.CurrentContext))
                    {
                        contextComboBox.SelectedItem = KubeConfig.CurrentContext;

                        previousNamespace = null;   // $hack(jefflill): Don't try to restore the namespace when the context doesn't exist.
                        previousPod       = null;   // $hack(jefflill): Don't try to restore the pod when the context doesn't exist.
                    }

                    previousContext = null;
                });
        }

        /// <summary>
        /// Loads the namespaces for the current cluster config (if any) and then selects
        /// the <b>default</b> namespace if it exists (which should be the case).
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task LoadNamespacesAsync()
        {
            var namespaces     = new List<string>();
            var currentContext = (string)null;

            this.InvokeOnUiThread(
                () =>
                {
                    currentContext = (string)contextComboBox.SelectedItem;
                });

            try
            {
                if (!string.IsNullOrEmpty(currentContext))
                {
                    var k8s = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: currentContext));

                    foreach (var @namespace in (await k8s.CoreV1.ListNamespaceAsync()).Items
                        .OrderBy(@namespace => @namespace.Name(), StringComparer.CurrentCultureIgnoreCase))
                    {
                        namespaces.Add(@namespace.Name());
                    }
                }
            }
            catch (Exception e)
            {
                this.InvokeOnUiThread(
                    () =>
                    {
                        namespaceComboBox.Items.Clear();
                        SetProcessComError(e);
                    });

                return;
            }

            this.InvokeOnUiThread(
                () =>
                {
                    namespaceComboBox.Items.Clear();

                    if (namespaces.Count > 0)
                    {
                        foreach (var @namespace in namespaces)
                        {
                            namespaceComboBox.Items.Add(@namespace);
                        }

                        if (previousNamespace != null && namespaces.Contains(previousNamespace))
                        {
                            namespaceComboBox.SelectedItem = previousNamespace;
                        }
                        else if (namespaces.Contains("default"))
                        {
                            namespaceComboBox.SelectedItem = "default";

                            previousPod = null;   // $hack(jefflill): Don't try to restore the pod when the namespace doesn't exist.
                        }

                        previousNamespace = null;
                    }
                });
        }

        /// <summary>
        /// Loads the pod information for the current cluster and namespace into the namespaces
        /// combobox and selects the first pod (by name).
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task LoadPodsAsync()
        {
            if (comError)
            {
                TargetPod = null;
                Invoke(() => podComboBox.Items.Clear());
                return;
            }

            var pods             = new List<string>();
            var currentContext   = (string)null;
            var currentNamespace = (string)null;

            this.InvokeOnUiThread(
                () =>
                {
                    currentContext   = (string)contextComboBox.SelectedItem;
                    currentNamespace = (string)namespaceComboBox.SelectedItem;
                });

            TargetPod = null;

            if (currentContext != null && currentNamespace != null)
            {
                try
                {
                    var k8s = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: currentContext));

                    currentPods = (await k8s.CoreV1.ListNamespacedPodAsync(currentNamespace)).Items.ToList();

                    foreach (var pod in currentPods
                        .OrderBy(pod => pod.Name(), StringComparer.CurrentCultureIgnoreCase))
                    {
                        if (TargetPod == null)
                        {
                            TargetPod = pod;
                        }

                        pods.Add(pod.Name());
                    }

                    if (previousPod != null && currentPods.Any(pod => pod.Name() == previousPod))
                    {
                        TargetPod = currentPods.Single(pod => pod.Name() == previousPod);
                    }
                }
                catch (Exception e)
                {
                    SetProcessComError(e);
                    return;
                }
                finally
                {
                    previousPod = null;
                }
            }
            else
            {
                currentPods.Clear();
            }

            this.InvokeOnUiThread(
                () =>
                {
                    podComboBox.Items.Clear();

                    if (pods.Count > 0)
                    {
                        foreach (var pod in pods)
                        {
                            podComboBox.Items.Add(pod);
                        }

                        podComboBox.SelectedItem = pods.First();
                    }
                });
        }

        /// <summary>
        /// Loads the containers for the current pod into the containers combobox.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task LoadContainersAsync()
        {
            TargetContainer = null;

            if (comError)
            {
                this.InvokeOnUiThread(() => containerComboBox.Items.Clear());
                return;
            }

            this.InvokeOnUiThread(
                () =>
                {
                    containerComboBox.Items.Clear();

                    if (TargetPod != null && TargetPod.Spec.Containers.Count > 0)
                    {
                        // We're going to ignore any of our debug ephermeral containers.

                        currentContainers = TargetPod.Spec.Containers
                            .Where(container => !container.Name.StartsWith("vs-debug-"))
                            .ToList();

                        foreach (var container in currentContainers)
                        {
                            containerComboBox.Items.Add(container.Name);

                            if (TargetContainer == null)
                            {
                                TargetContainer = container;
                            }
                        }

                        containerComboBox.SelectedItem = TargetPod.Spec.Containers.First().Name;
                    }
                    else
                    {
                        currentContainers.Clear();
                    }
                });

            await Task.CompletedTask;
        }

        /// <summary>
        /// Fetches information about the processes running in the current pod and then 
        /// loads this into the data grid.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task LoadProcessesAsync()
        {
            if (TargetPod == null || TargetContainer == null || comError)
            {
                this.InvokeOnUiThread(() => processesGrid.Rows.Clear());
                return;
            }

            // Exec the [ps -eo pid,cmd] command within the current pod container.
            // This will result in command output that looks something like:
            // 
            //      PID COMMAND
            //        1 /init
            //      140 /init
            //      141 /init
            //      142 /mnt/wsl/docker-desktop/docker-desktop-user-distro proxy --distro-name neon-kubebuilder --docker-desktop-root /mnt/wsl/docker-desktop C:\Progr
            //      159 /init
            //      160 docker serve --address unix:///root/.docker/run/docker-cli-api.sock
            //      195 /init
            //      196 /init
            //      197 -bash
            //      513 ps -eo pid,args

            try
            {
                var k8s      = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: KubeConfig.CurrentContext));
                var response = await Helper.ExecAsync(k8s, TargetPod, TargetContainer.Name, "ps", "-eo", "pid,cmd");

                if (response.ExitCode == 0)
                {
                    psError = null;

                    // Extract the process information from the [ps] command output.

                    var processes = new List<ProcessInfo>();

                    foreach (var rawLine in response.OutputText
                        .ToLines()
                        .Skip(1))
                    {
                        var line     = rawLine.Trim();
                        var spacePos = line.IndexOf(' ');
                        var pid      = int.Parse(line.Substring(0, spacePos));
                        var command  = line.Substring(spacePos + 1);

                        spacePos = command.IndexOf(' ');

                        var name = spacePos == -1 ? command : command.Substring(0, spacePos);

                        if (name != "ps")
                        {
                            processes.Add(new ProcessInfo(pid, name, command));
                        }
                    }

                    processesGrid.DataSource = processes.OrderBy(process => process.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                }
                else
                {
                    psError = "Cannot list container processes because the [ps] command is not on the path.";
                }
            }
            catch (Exception e)
            {
                SetProcessComError(e);
            }
        }

        /// <summary>
        /// Manages the processes grid and process error label based on the current state of the dialog.
        /// </summary>
        private void SetProcessErrorState()
        {
            if (comError)
            {
                return;
            }

            var currentContext   = (string)contextComboBox.SelectedItem;
            var currentNamespace = (string)namespaceComboBox.SelectedItem;
            var currentPod       = (string)podComboBox.SelectedItem;
            var currentContainer = (string)containerComboBox.SelectedItem;

            if (string.IsNullOrEmpty(currentContext))
            {
                SetProcessError("No cluster/context is selected.");
            }
            else if (string.IsNullOrEmpty(currentNamespace))
            {
                SetProcessError("No namespace is selected.");
            }
            else if (podComboBox.Items.Count == 0)
            {
                SetProcessError($"[{currentNamespace}] namespace has no pods.");
            }
            else if (string.IsNullOrEmpty(currentPod))
            {
                SetProcessError("No pod is selected.");
            }
            else if (string.IsNullOrEmpty(currentContainer))
            {
                SetProcessError("No container is selected.");
            }
            else if (psError != null)
            {
                SetProcessError(psError);
            }
            else
            {
                ClearProcessError();
            }
        }

        /// <summary>
        /// Hides the processes grid and displays the process error label with the message passed.
        /// </summary>
        /// <param name="message">Specifies the error message.</param>
        private void SetProcessError(string message)
        {
            this.InvokeOnUiThread(
                () =>
                {
                    processErrorLabel.Text = message;
                    processErrorLabel.Visible = true;
                    processesGrid.Visible = false;
                });
        }

        /// <summary>
        /// Hides the processes grid and displays the process error label with communication error details.
        /// </summary>
        /// <param name="e">Specifies the error.</param>
        private void SetProcessComError(Exception e)
        {
            this.InvokeOnUiThread(
                () =>
                {
                    comError = true;

                    SetProcessError($"Error communicating with the Kubernetes cluster:\r\n\r\n{NeonHelper.ExceptionError(e)}");
                });
        }

        /// <summary>
        /// Hides the process error label and displays the processes grid.
        /// </summary>
        private void ClearProcessError()
        {
            this.EnsureOnUiThread();

            comError                  = false;
            processErrorLabel.Text    = string.Empty;
            processErrorLabel.Visible = false;
            processesGrid.Visible     = true;
        }

        /// <summary>
        /// Sets the enabled state of the <b>Attach</b> button based on the current state of the dialog.
        /// </summary>
        private void SetAttachButtonEnabledState()
        {
            this.EnsureOnUiThread();

            attachButton.Enabled = processesGrid.Visible && processesGrid.SelectedRows.Count > 0;
        }
    }
}
