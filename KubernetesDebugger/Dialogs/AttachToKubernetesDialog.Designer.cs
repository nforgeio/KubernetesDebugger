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
            this.contextComboBox = new System.Windows.Forms.ComboBox();
            this.namespaceLabel = new System.Windows.Forms.Label();
            this.namespaceComboBox = new System.Windows.Forms.ComboBox();
            this.podLabel = new System.Windows.Forms.Label();
            this.podComboBox = new System.Windows.Forms.ComboBox();
            this.instructionsGroupBox = new System.Windows.Forms.GroupBox();
            this.instructionsTextBox = new System.Windows.Forms.TextBox();
            this.processesGrid = new System.Windows.Forms.DataGridView();
            this.attachButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.processesGroupBox = new System.Windows.Forms.GroupBox();
            this.processErrorLabel = new System.Windows.Forms.Label();
            this.containerLabel = new System.Windows.Forms.Label();
            this.containerComboBox = new System.Windows.Forms.ComboBox();
            this.Process = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CommandLine = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this.clusterLabel.Size = new System.Drawing.Size(80, 13);
            this.clusterLabel.TabIndex = 0;
            this.clusterLabel.Text = "Cluster context:";
            // 
            // contextComboBox
            // 
            this.contextComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.contextComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.contextComboBox.FormattingEnabled = true;
            this.contextComboBox.Location = new System.Drawing.Point(99, 10);
            this.contextComboBox.Name = "contextComboBox";
            this.contextComboBox.Size = new System.Drawing.Size(1003, 21);
            this.contextComboBox.TabIndex = 1;
            this.contextComboBox.SelectedValueChanged += new System.EventHandler(this.contextComboBox_SelectedValueChanged);
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
            this.namespaceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.namespaceComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.namespaceComboBox.FormattingEnabled = true;
            this.namespaceComboBox.Location = new System.Drawing.Point(99, 37);
            this.namespaceComboBox.Name = "namespaceComboBox";
            this.namespaceComboBox.Size = new System.Drawing.Size(1003, 21);
            this.namespaceComboBox.TabIndex = 3;
            this.namespaceComboBox.SelectedValueChanged += new System.EventHandler(this.namespaceComboBox_SelectedValueChanged);
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
            this.podComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.podComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.podComboBox.FormattingEnabled = true;
            this.podComboBox.Location = new System.Drawing.Point(99, 64);
            this.podComboBox.Name = "podComboBox";
            this.podComboBox.Size = new System.Drawing.Size(1003, 21);
            this.podComboBox.TabIndex = 5;
            this.podComboBox.SelectedValueChanged += new System.EventHandler(this.podComboBox_SelectedValueChanged);
            // 
            // instructionsGroupBox
            // 
            this.instructionsGroupBox.Controls.Add(this.instructionsTextBox);
            this.instructionsGroupBox.Location = new System.Drawing.Point(17, 125);
            this.instructionsGroupBox.Name = "instructionsGroupBox";
            this.instructionsGroupBox.Size = new System.Drawing.Size(1085, 70);
            this.instructionsGroupBox.TabIndex = 6;
            this.instructionsGroupBox.TabStop = false;
            this.instructionsGroupBox.Text = "Instructions";
            // 
            // instructionsTextBox
            // 
            this.instructionsTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.instructionsTextBox.Location = new System.Drawing.Point(11, 19);
            this.instructionsTextBox.Multiline = true;
            this.instructionsTextBox.Name = "instructionsTextBox";
            this.instructionsTextBox.ReadOnly = true;
            this.instructionsTextBox.Size = new System.Drawing.Size(1068, 40);
            this.instructionsTextBox.TabIndex = 0;
            this.instructionsTextBox.Text = "Use this to attach the debugger to a process running in a remote cluster pod.\\r\\n" +
    "\\r\\nChoose the target cluster, namespace and pod above, select the target proces" +
    "s below and then click Attach.";
            // 
            // processesGrid
            // 
            this.processesGrid.AllowUserToAddRows = false;
            this.processesGrid.AllowUserToDeleteRows = false;
            this.processesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.processesGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Process,
            this.ID,
            this.CommandLine});
            this.processesGrid.Location = new System.Drawing.Point(16, 28);
            this.processesGrid.MultiSelect = false;
            this.processesGrid.Name = "processesGrid";
            this.processesGrid.ReadOnly = true;
            this.processesGrid.RowHeadersVisible = false;
            this.processesGrid.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.processesGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.processesGrid.ShowCellErrors = false;
            this.processesGrid.ShowCellToolTips = false;
            this.processesGrid.ShowEditingIcon = false;
            this.processesGrid.ShowRowErrors = false;
            this.processesGrid.Size = new System.Drawing.Size(1053, 404);
            this.processesGrid.TabIndex = 9;
            this.processesGrid.SelectionChanged += new System.EventHandler(this.processesGrid_SelectionChanged);
            this.processesGrid.DoubleClick += new System.EventHandler(this.processesGrid_DoubleClick);
            // 
            // attachButton
            // 
            this.attachButton.Location = new System.Drawing.Point(862, 658);
            this.attachButton.Name = "attachButton";
            this.attachButton.Size = new System.Drawing.Size(117, 23);
            this.attachButton.TabIndex = 10;
            this.attachButton.Text = "Attach";
            this.attachButton.UseVisualStyleBackColor = true;
            this.attachButton.Click += new System.EventHandler(this.attachButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(985, 658);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(117, 23);
            this.cancelButton.TabIndex = 11;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // processesGroupBox
            // 
            this.processesGroupBox.Controls.Add(this.processErrorLabel);
            this.processesGroupBox.Controls.Add(this.processesGrid);
            this.processesGroupBox.Location = new System.Drawing.Point(17, 201);
            this.processesGroupBox.Name = "processesGroupBox";
            this.processesGroupBox.Size = new System.Drawing.Size(1085, 451);
            this.processesGroupBox.TabIndex = 8;
            this.processesGroupBox.TabStop = false;
            this.processesGroupBox.Text = "Available processes";
            // 
            // processErrorLabel
            // 
            this.processErrorLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.processErrorLabel.Location = new System.Drawing.Point(269, 205);
            this.processErrorLabel.Name = "processErrorLabel";
            this.processErrorLabel.Size = new System.Drawing.Size(570, 23);
            this.processErrorLabel.TabIndex = 12;
            this.processErrorLabel.Text = "Cannot list container processes because the \"ps\" command is not on the path.";
            this.processErrorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // containerLabel
            // 
            this.containerLabel.AutoSize = true;
            this.containerLabel.Location = new System.Drawing.Point(12, 94);
            this.containerLabel.Name = "containerLabel";
            this.containerLabel.Size = new System.Drawing.Size(55, 13);
            this.containerLabel.TabIndex = 6;
            this.containerLabel.Text = "Container:";
            // 
            // containerComboBox
            // 
            this.containerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.containerComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.containerComboBox.FormattingEnabled = true;
            this.containerComboBox.Location = new System.Drawing.Point(99, 91);
            this.containerComboBox.Name = "containerComboBox";
            this.containerComboBox.Size = new System.Drawing.Size(1003, 21);
            this.containerComboBox.TabIndex = 7;
            this.containerComboBox.SelectedValueChanged += new System.EventHandler(this.containerComboBox_SelectedValueChanged);
            // 
            // Process
            // 
            this.Process.DataPropertyName = "Name";
            this.Process.HeaderText = "Process";
            this.Process.Name = "Process";
            this.Process.ReadOnly = true;
            this.Process.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Process.Width = 150;
            // 
            // ID
            // 
            this.ID.DataPropertyName = "Pid";
            this.ID.HeaderText = "ID";
            this.ID.Name = "ID";
            this.ID.ReadOnly = true;
            this.ID.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.ID.Width = 75;
            // 
            // CommandLine
            // 
            this.CommandLine.DataPropertyName = "Command";
            this.CommandLine.HeaderText = "Command Line";
            this.CommandLine.Name = "CommandLine";
            this.CommandLine.ReadOnly = true;
            this.CommandLine.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.CommandLine.Width = 1000;
            // 
            // AttachToKubernetesDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1114, 693);
            this.Controls.Add(this.containerComboBox);
            this.Controls.Add(this.containerLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.attachButton);
            this.Controls.Add(this.instructionsGroupBox);
            this.Controls.Add(this.podComboBox);
            this.Controls.Add(this.podLabel);
            this.Controls.Add(this.namespaceComboBox);
            this.Controls.Add(this.namespaceLabel);
            this.Controls.Add(this.contextComboBox);
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
        private System.Windows.Forms.ComboBox contextComboBox;
        private System.Windows.Forms.Label namespaceLabel;
        private System.Windows.Forms.ComboBox namespaceComboBox;
        private System.Windows.Forms.Label podLabel;
        private System.Windows.Forms.ComboBox podComboBox;
        private System.Windows.Forms.GroupBox instructionsGroupBox;
        private System.Windows.Forms.TextBox instructionsTextBox;
        private System.Windows.Forms.DataGridView processesGrid;
        private System.Windows.Forms.Button attachButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.GroupBox processesGroupBox;
        private System.Windows.Forms.Label containerLabel;
        private System.Windows.Forms.ComboBox containerComboBox;
        private System.Windows.Forms.Label processErrorLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn Process;
        private System.Windows.Forms.DataGridViewTextBoxColumn ID;
        private System.Windows.Forms.DataGridViewTextBoxColumn CommandLine;
    }
}