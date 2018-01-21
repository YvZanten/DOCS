namespace DOCS
{
    partial class FormProgress
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
            this.folderDialogSource = new System.Windows.Forms.FolderBrowserDialog();
            this.folderDialogDestination = new System.Windows.Forms.FolderBrowserDialog();
            this.panelContainer = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // folderDialogSource
            // 
            this.folderDialogSource.Description = "Select a source";
            // 
            // panelContainer
            // 
            this.panelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContainer.Location = new System.Drawing.Point(0, 0);
            this.panelContainer.Margin = new System.Windows.Forms.Padding(0);
            this.panelContainer.Name = "panelContainer";
            this.panelContainer.Size = new System.Drawing.Size(484, 161);
            this.panelContainer.TabIndex = 0;
            // 
            // FormProgress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 161);
            this.Controls.Add(this.panelContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FormProgress";
            this.Text = "Document Sorter";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog folderDialogSource;
        private System.Windows.Forms.FolderBrowserDialog folderDialogDestination;
        private System.Windows.Forms.Panel panelContainer;
    }
}

