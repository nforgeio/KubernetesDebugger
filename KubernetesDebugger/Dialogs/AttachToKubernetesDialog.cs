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
            /// Returns the process name.
            /// </summary>
            public string Process { get; private set; }

            /// <summary>
            /// Returns the process ID.
            /// </summary>
            public string Id { get; private set; }

            /// <summary>
            /// Returns the command line for the process.
            /// </summary>
            public string Command { get; private set; }
        }

        //---------------------------------------------------------------------
        // Implementation

        // Control layout information required for dialog resizing.

        private int     clusterComboBoxRightMargin;
        private int     namespaceComboBoxRightMargin;
        private int     podComboBoxRightMargin;
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

        // Current Kubernetes information.

        private K8SConfiguration    kubeConfig;
        private List<string>        namespaces = new List<string>();
        private string              currentNamespace;
        private string              currentContext;
        private List<string>        pods = new List<string>();
        private string              currentPod;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AttachToKubernetesDialog()
        {
            InitializeComponent();

            this.Load += AttachToKubernetesDialog_Load;
            this.ClientSizeChanged += AttachToKubernetesDialog_ClientSizeChanged;
        }

        /// <summary>
        /// Handles initial form loading.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void AttachToKubernetesDialog_Load(object sender, EventArgs args)
        {
            // Replace any "\r\n" sequences in the instructions with actual line endings.

            instructionsTextBox.Text = instructionsTextBox.Text.Replace(@"\r\n", Environment.NewLine);

            // Compute the control right and bottom margins so we'll be able to
            // relocate the controls when the user resizes the dialog.

            clusterComboBoxRightMargin      = GetRightMargin(clusterComboBox);
            namespaceComboBoxRightMargin    = GetRightMargin(namespaceComboBox);
            podComboBoxRightMargin          = GetRightMargin(podComboBox);
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
                // Load the default kubeconfig file and then initialize the dialog configs.

                await LoadKubeConfigAsync();
                clusterComboBox.Items.Clear();

                foreach (var context in kubeConfig.Contexts
                    .OrderBy(context => context.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    clusterComboBox.Items.Add(context.Name);

                    if (context.Name == currentContext)
                    {
                        clusterComboBox.SelectedItem = context.Name;
                    }
                }

                if (clusterComboBox.SelectedItem == null)
                {
                    return;
                }

                // Obtain the namespaces for the cluster and add them to the namespaces
                // combobox, initializing the selected namespace to "default".

                await LoadNamespacesAsync();

                namespaceComboBox.Items.Clear();
                currentNamespace = null;

                if (namespaces.Count > 0)
                {
                    foreach (var @namespace in namespaces)
                    {
                        namespaceComboBox.Items.Add(@namespace);
                    }

                    if (namespaces.Contains("default"))
                    {
                        currentNamespace               = "default";
                        namespaceComboBox.SelectedItem = "default";
                    }
                }

                // Obtain the pods for the current namespace and add them to the combobox.
                // Note that we're not going to select a pod.

                await LoadPodsAsync();

                podComboBox.Items.Clear();
                currentPod = null;

                foreach (var pod in pods
                    .OrderBy(pod => pod, StringComparer.CurrentCultureIgnoreCase))
                {
                    podComboBox.Items.Add(pod);
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
        /// <param name="sender"></param>
        /// <param name="args></param>
        private void AttachToKubernetesDialog_ClientSizeChanged(object sender, EventArgs args)
        {
            clusterComboBox.Width      = ClientSize.Width - (clusterComboBoxRightMargin + clusterComboBox.Left);
            namespaceComboBox.Width    = ClientSize.Width - (namespaceComboBoxRightMargin + namespaceComboBox.Left);
            podComboBox.Width          = ClientSize.Width - (podComboBoxRightMargin + podComboBox.Left);
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
        /// Loads the standard kubeconfig file into <see cref="kubeConfig"/>.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task LoadKubeConfigAsync()
        {
            kubeConfig     = await k8s.KubernetesClientConfiguration.LoadKubeConfigAsync();
            currentContext = kubeConfig.CurrentContext;
        }

        /// <summary>
        /// Loads the namespaces for the current cluster config.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task LoadNamespacesAsync()
        {
            namespaces.Clear();

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

        /// <summary>
        /// Loads the pod information for the current cluster and namespace.
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        private async Task LoadPodsAsync()
        {
            pods.Clear();

            if (!string.IsNullOrEmpty(currentContext) && !string.IsNullOrEmpty(currentNamespace))
            {
                var k8s = new Kubernetes(KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: currentContext));

                foreach (var pod in (await k8s.CoreV1.ListNamespacedPodAsync(currentNamespace)).Items
                    .OrderBy(pod => pod.Name(), StringComparer.CurrentCultureIgnoreCase))
                {
                    pods.Add(pod.Name());
                }
            }
        }
    }
}
