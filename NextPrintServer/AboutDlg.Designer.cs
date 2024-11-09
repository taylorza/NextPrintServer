namespace NextPrintServer
{
    partial class AboutDlg
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutDlg));
            panelAboutIcon = new Panel();
            btnOK = new Button();
            lblMessage = new Label();
            SuspendLayout();
            // 
            // panelAboutIcon
            // 
            panelAboutIcon.Location = new Point(12, 12);
            panelAboutIcon.Name = "panelAboutIcon";
            panelAboutIcon.Size = new Size(32, 32);
            panelAboutIcon.TabIndex = 0;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(243, 56);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 23);
            btnOK.TabIndex = 1;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            // 
            // lblMessage
            // 
            lblMessage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblMessage.Location = new Point(50, 12);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(268, 35);
            lblMessage.TabIndex = 2;
            lblMessage.Text = "Message";
            // 
            // AboutDlg
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnOK;
            ClientSize = new Size(330, 91);
            Controls.Add(lblMessage);
            Controls.Add(btnOK);
            Controls.Add(panelAboutIcon);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutDlg";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "About";
            Load += AboutDlg_Load;
            ResumeLayout(false);
        }

        #endregion

        private Panel panelAboutIcon;
        private Button btnOK;
        private Label lblMessage;
    }
}