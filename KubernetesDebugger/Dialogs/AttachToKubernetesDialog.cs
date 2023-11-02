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

#pragma warning disable VSTHRD100   // Avoid "async void" methods

namespace KubernetesDebugger.Dialogs
{
    /// <summary>
    /// Implements the pod/process attachment dialog.
    /// </summary>
    public partial class AttachToKubernetesDialog : Form
    {
        //---------------------------------------------------------------------
        // Private types

        /// <summary>
        /// Holds information about a process.
        /// </summary>
        private class ProcessInfo
        {
            /// <summary>
            /// Returns the process ID.
            /// </summary>
            public string Id { get; private set; }

            /// <summary>
            /// Returns the process (program) name.
            /// </summary>
            public string Process { get; private set; }

            /// <summary>
            /// Returns the command line for the process.
            /// </summary>
            public string Command { get; private set; }
        }

        //---------------------------------------------------------------------
        // Implementation

        // Control layout information required for dialog resizing.

        private int     contextComboBoxRightMargin;
        private int     namespaceComboBoxRightMargin;
        private int     podComboBoxRightMargin;
        private int     containerComboBoxRightMargin;
        private int     instructionsGroupBoxRightMargin;
        private int     instructionsTextBoxRightMargin;
        private int     processesGroupBoxRightMargin;
        private int     processesGroupBoxBottomMargin;
        private int     processesGridRightMargin;
        private int     processesGridBottomMargin;
        private int     attachButtonRightMargin;
        private int     attachButtonBottomMargin;
        private int     cancelButtonRightMargin;
        private int     cancelButtonBottomMargin;

        // Other state

        private bool                ignoreComboBoxEvents;
        private List<V1Pod>         currentPods       = new List<V1Pod>();
        private List<V1Container>   currentContainers = new List<V1Container>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public AttachToKubernetesDialog()
        {
            InitializeComponent();

            this.Load              += AttachToKubernetesDialog_Load;
            this.ClientSizeChanged += AttachToKubernetesDialog_ClientSizeChanged;
        }

        /// <summary>
        /// Set to the selected Kubernetes configuration after the user clicks Attach.
        /// </summary>
        public K8SConfiguration KubeConfig { get; private set; }

        /// <summary>
        /// Set to the selected cluster pod after the user clicks Attach.
        /// </summary>
        public V1Pod CurrentPod { get; private set; }

        /// <summary>
        /// Set to the selected cluster container after the user clicks Attach.
        /// </summary>
        public V1Container CurrentContainer { get; private set; }

        /// <summary>
        /// Handles initial form loading.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private async void AttachToKubernetesDialog_Load(object sender, EventArgs args)
        {
            // Replace any "\r\n" sequences in the instructions with actual line endings.

            instructionsTextBox.Text = instructionsTextBox.Text.Replace(@"\r\n", Environment.NewLine);

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
            attachButtonRightMargin         = GetRightMargin(attachButton);
            cancelButtonRightMargin         = GetRightMargin(cancelButton);

            processesGroupBoxBottomMargin   = GetBottomMargin(processesGroupBox);
            processesGridBottomMargin       = GetBottomMargin(processesGrid);
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

            attachButton.Left          = ClientSize.Width - (attachButtonRightMargin + attachButton.Width);
            cancelButton.Left          = ClientSize.Width - (cancelButtonRightMargin + cancelButton.Width);

            attachButton.Top           = ClientSize.Height - (attachButtonBottomMargin + attachButton.Height);
            cancelButton.Top           = ClientSize.Height - (cancelButtonBottomMargin + cancelButton.Height);

            processesGroupBox.Height   = ClientSize.Height - (processesGroupBoxBottomMargin + processesGroupBox.Top);
            processesGrid.Height       = ClientSize.Height - (processesGridBottomMargin + processesGrid.Top);
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
            }
            finally
            {
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
            }
            finally
            {
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

                this.CurrentPod = currentPods.FirstOrDefault(pod => pod.Name() == (string)podComboBox.SelectedItem);

                await LoadContainersAsync();
                await LoadProcessesAsync();
            }
            finally
            {
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

                this.CurrentContainer = currentContainers.FirstOrDefault(container => container.Name == (string)containerComboBox.SelectedItem);

                await LoadProcessesAsync();
            }
            finally
            {
                ignoreComboBoxEvents = false;
            }
        }

        /// <summary>
        /// Handles attach button clicks.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private void attachButton_Click(object sender, EventArgs args)
        {
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Handles cancel button clicks.
        /// </summary>
        /// <param name="sender">Specifies the event source.</param>
        /// <param name="args">Specfies the event args.</param>
        private void cancelButton_Click(object sender, EventArgs args)
        {
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

                        if (context.Name == KubeConfig.CurrentContext)
                        {
                            contextComboBox.SelectedItem = context.Name;
                        }
                    }
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

            if (!string.IsNullOrEmpty(currentContext))
            {
                var k8s = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: currentContext));

                foreach (var @namespace in (await k8s.CoreV1.ListNamespaceAsync()).Items
                    .OrderBy(@namespace => @namespace.Name(), StringComparer.CurrentCultureIgnoreCase))
                {
                    namespaces.Add(@namespace.Name());
                }
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

                        if (namespaces.Contains("default"))
                        {
                            namespaceComboBox.SelectedItem = "default";
                        }
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
            var pods             = new List<string>();
            var currentContext   = (string)null;
            var currentNamespace = (string)null;

            this.InvokeOnUiThread(
                () =>
                {
                    currentContext   = (string)contextComboBox.SelectedItem;
                    currentNamespace = (string)namespaceComboBox.SelectedItem;
                });

            this.CurrentPod = null;

            if (currentContext != null && currentNamespace != null)
            {
                var k8s = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: currentContext));

                currentPods = (await k8s.CoreV1.ListNamespacedPodAsync(currentNamespace)).Items.ToList();

                foreach (var pod in currentPods
                    .OrderBy(pod => pod.Name(), StringComparer.CurrentCultureIgnoreCase))
                {
                    if (this.CurrentPod == null)
                    {
                        this.CurrentPod = pod;
                    }

                    pods.Add(pod.Name());
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
            this.CurrentContainer = null;

            this.InvokeOnUiThread(
                () =>
                {
                    containerComboBox.Items.Clear();

                    if (CurrentPod != null && CurrentPod.Spec.Containers.Count > 0)
                    {
                        currentContainers = CurrentPod.Spec.Containers.ToList();

                        foreach (var container in currentContainers)
                        {
                            containerComboBox.Items.Add(container.Name);

                            if (this.CurrentContainer == null)
                            {
                                this.CurrentContainer = container;
                            }
                        }

                        containerComboBox.SelectedItem = CurrentPod.Spec.Containers.First().Name;
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
            if (CurrentPod == null || CurrentContainer == null)
            {
                this.InvokeOnUiThread(() => processesGrid.Rows.Clear());
                return;
            }

            var k8s      = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: KubeConfig.CurrentContext));
            var response = await Helper.ExecAsync(k8s, CurrentPod, CurrentContainer.Name, "/bin/ps");

            // Exec the [/usr/bin/ps -eo pid,cmd,args] command within the current pod container.
            // This will result in command output that looks something like:
            //
            // 
        }
    }
}
