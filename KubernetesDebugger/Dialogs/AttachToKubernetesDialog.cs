using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KubernetesDebugger.Dialogs
{
    /// <summary>
    /// Implements the pod/process attachment dialog.
    /// </summary>
    public partial class AttachToKubernetesDialog : Form
    {
        private int clusterComboBoxRightMargin;
        private int namespaceComboBoxRightMargin;
        private int podComboBoxRightMargin;
        private int instructionsGroupBoxRightMargin;
        private int instructionsTextBoxRightMargin;
        private int processesGroupBoxRightMargin;
        private int processesGroupBoxBottomMargin;
        private int processesGridRightMargin;
        private int processesGridBottomMargin;
        private int attachButtonRightMargin;
        private int attachButtonBottomMargin;
        private int cancelButtonRightMargin;
        private int cancelButtonBottomMargin;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AttachToKubernetesDialog()
        {
            InitializeComponent();

            this.Load += (s, a) =>
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
            };

            this.ClientSizeChanged += (s, a) =>
            {
                // Relocate the controls when the user resizes the dialog.

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
                processesGrid.Height       = ClientSize.Height - (processesGridBottomMargin +processesGrid.Top);
            };
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
            var margin = ClientSize.Height - (control.Top + control.Height);

            return ClientSize.Height - (control.Top + control.Height);
        }
    }
}
