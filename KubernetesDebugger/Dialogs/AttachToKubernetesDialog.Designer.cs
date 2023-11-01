namespace KubernetesDebugger.Dialogs
{
    partial class AttachToKubernetesDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AttachToKubernetesDialog));
            this.clusterLabel = new System.Windows.Forms.Label();
            this.clusterComboBox = new System.Windows.Forms.ComboBox();
            this.namespaceLabel = new System.Windows.Forms.Label();
            this.namespaceComboBox = new System.Windows.Forms.ComboBox();
            this.podLabel = new System.Windows.Forms.Label();
            this.podComboBox = new System.Windows.Forms.ComboBox();
            this.instructionsGroupBox = new System.Windows.Forms.GroupBox();
            this.instructionsTextBox = new System.Windows.Forms.TextBox();
            this.processesGrid = new System.Windows.Forms.DataGridView();
            this.Process = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Container = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CommandLine = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.attachButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.processesGroupBox = new System.Windows.Forms.GroupBox();
            this.instructionsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.processesGrid)).BeginInit();
            this.processesGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // clusterLabel
            // 
            this.clusterLabel.AutoSize = true;
            this.clusterLabel.Location = new System.Drawing.Point(12, 13);
            this.clusterLabel.Name = "clusterLabel";
            this.clusterLabel.Size = new System.Drawing.Size(42, 13);
            this.clusterLabel.TabIndex = 0;
            this.clusterLabel.Text = "Cluster:";
            // 
            // clusterComboBox
            // 
            this.clusterComboBox.FormattingEnabled = true;
            this.clusterComboBox.Location = new System.Drawing.Point(76, 10);
            this.clusterComboBox.Name = "clusterComboBox";
            this.clusterComboBox.Size = new System.Drawing.Size(1026, 21);
            this.clusterComboBox.TabIndex = 1;
            // 
            // namespaceLabel
            // 
            this.namespaceLabel.AutoSize = true;
            this.namespaceLabel.Location = new System.Drawing.Point(12, 40);
            this.namespaceLabel.Name = "namespaceLabel";
            this.namespaceLabel.Size = new System.Drawing.Size(67, 13);
            this.namespaceLabel.TabIndex = 2;
            this.namespaceLabel.Text = "Namespace:";
            // 
            // namespaceComboBox
            // 
            this.namespaceComboBox.FormattingEnabled = true;
            this.namespaceComboBox.Location = new System.Drawing.Point(76, 37);
            this.namespaceComboBox.Name = "namespaceComboBox";
            this.namespaceComboBox.Size = new System.Drawing.Size(1026, 21);
            this.namespaceComboBox.TabIndex = 3;
            // 
            // podLabel
            // 
            this.podLabel.AutoSize = true;
            this.podLabel.Location = new System.Drawing.Point(12, 67);
            this.podLabel.Name = "podLabel";
            this.podLabel.Size = new System.Drawing.Size(29, 13);
            this.podLabel.TabIndex = 4;
            this.podLabel.Text = "Pod:";
            // 
            // podComboBox
            // 
            this.podComboBox.FormattingEnabled = true;
            this.podComboBox.Location = new System.Drawing.Point(76, 64);
            this.podComboBox.Name = "podComboBox";
            this.podComboBox.Size = new System.Drawing.Size(1026, 21);
            this.podComboBox.TabIndex = 5;
            // 
            // instructionsGroupBox
            // 
            this.instructionsGroupBox.Controls.Add(this.instructionsTextBox);
            this.instructionsGroupBox.Location = new System.Drawing.Point(17, 91);
            this.instructionsGroupBox.Name = "instructionsGroupBox";
            this.instructionsGroupBox.Size = new System.Drawing.Size(1085, 69);
            this.instructionsGroupBox.TabIndex = 6;
            this.instructionsGroupBox.TabStop = false;
            this.instructionsGroupBox.Text = "Instructions";
            // 
            // instructionsTextBox
            // 
            this.instructionsTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.instructionsTextBox.Location = new System.Drawing.Point(6, 19);
            this.instructionsTextBox.Multiline = true;
            this.instructionsTextBox.Name = "instructionsTextBox";
            this.instructionsTextBox.ReadOnly = true;
            this.instructionsTextBox.Size = new System.Drawing.Size(1073, 44);
            this.instructionsTextBox.TabIndex = 0;
            this.instructionsTextBox.Text = "Use this to attach the debugger to a process running in a remote cluster pod.\\r\\n" +
    "\\r\\nChoose the target cluster, namespace and pod above, select the target proces" +
    "s below and then click ATTACH.";
            // 
            // processesGrid
            // 
            this.processesGrid.AllowUserToAddRows = false;
            this.processesGrid.AllowUserToDeleteRows = false;
            this.processesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.processesGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Process,
            this.ID,
            this.Container,
            this.CommandLine});
            this.processesGrid.Location = new System.Drawing.Point(16, 19);
            this.processesGrid.Name = "processesGrid";
            this.processesGrid.ReadOnly = true;
            this.processesGrid.RowHeadersVisible = false;
            this.processesGrid.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.processesGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.processesGrid.ShowCellToolTips = false;
            this.processesGrid.Size = new System.Drawing.Size(1053, 448);
            this.processesGrid.TabIndex = 7;
            // 
            // Process
            // 
            this.Process.HeaderText = "Process";
            this.Process.Name = "Process";
            this.Process.ReadOnly = true;
            this.Process.Width = 150;
            // 
            // ID
            // 
            this.ID.HeaderText = "ID";
            this.ID.Name = "ID";
            this.ID.ReadOnly = true;
            this.ID.Width = 75;
            // 
            // Container
            // 
            this.Container.HeaderText = "Container";
            this.Container.Name = "Container";
            this.Container.ReadOnly = true;
            this.Container.Width = 200;
            // 
            // CommandLine
            // 
            this.CommandLine.HeaderText = "Command Line";
            this.CommandLine.Name = "CommandLine";
            this.CommandLine.ReadOnly = true;
            this.CommandLine.Width = 800;
            // 
            // attachButton
            // 
            this.attachButton.Location = new System.Drawing.Point(862, 658);
            this.attachButton.Name = "attachButton";
            this.attachButton.Size = new System.Drawing.Size(117, 23);
            this.attachButton.TabIndex = 8;
            this.attachButton.Text = "Attach";
            this.attachButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(985, 658);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(117, 23);
            this.cancelButton.TabIndex = 9;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // processesGroupBox
            // 
            this.processesGroupBox.Controls.Add(this.processesGrid);
            this.processesGroupBox.Location = new System.Drawing.Point(17, 166);
            this.processesGroupBox.Name = "processesGroupBox";
            this.processesGroupBox.Size = new System.Drawing.Size(1085, 486);
            this.processesGroupBox.TabIndex = 10;
            this.processesGroupBox.TabStop = false;
            this.processesGroupBox.Text = "Available processes";
            // 
            // AttachToKubernetesDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1114, 693);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.attachButton);
            this.Controls.Add(this.instructionsGroupBox);
            this.Controls.Add(this.podComboBox);
            this.Controls.Add(this.podLabel);
            this.Controls.Add(this.namespaceComboBox);
            this.Controls.Add(this.namespaceLabel);
            this.Controls.Add(this.clusterComboBox);
            this.Controls.Add(this.clusterLabel);
            this.Controls.Add(this.processesGroupBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(847, 549);
            this.Name = "AttachToKubernetesDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Attach to Kubernetes";
            this.instructionsGroupBox.ResumeLayout(false);
            this.instructionsGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.processesGrid)).EndInit();
            this.processesGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label clusterLabel;
        private System.Windows.Forms.ComboBox clusterComboBox;
        private System.Windows.Forms.Label namespaceLabel;
        private System.Windows.Forms.ComboBox namespaceComboBox;
        private System.Windows.Forms.Label podLabel;
        private System.Windows.Forms.ComboBox podComboBox;
        private System.Windows.Forms.GroupBox instructionsGroupBox;
        private System.Windows.Forms.TextBox instructionsTextBox;
        private System.Windows.Forms.DataGridView processesGrid;
        private System.Windows.Forms.Button attachButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn Process;
        private System.Windows.Forms.DataGridViewTextBoxColumn ID;
        private System.Windows.Forms.DataGridViewTextBoxColumn Container;
        private System.Windows.Forms.DataGridViewTextBoxColumn CommandLine;
        private System.Windows.Forms.GroupBox processesGroupBox;
    }
}