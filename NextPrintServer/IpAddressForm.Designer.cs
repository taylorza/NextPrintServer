namespace NextPrintServer
{
    partial class IpAddressForm
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
            ipAddressListView = new ListView();
            ipAddressColumn = new ColumnHeader();
            ipTypeColumn = new ColumnHeader();
            ipDescriptionColumn = new ColumnHeader();
            btnOK = new Button();
            SuspendLayout();
            // 
            // ipAddressListView
            // 
            ipAddressListView.Columns.AddRange(new ColumnHeader[] { ipAddressColumn, ipTypeColumn, ipDescriptionColumn });
            ipAddressListView.Location = new Point(12, 12);
            ipAddressListView.Name = "ipAddressListView";
            ipAddressListView.Size = new Size(486, 193);
            ipAddressListView.TabIndex = 1;
            ipAddressListView.UseCompatibleStateImageBehavior = false;
            ipAddressListView.View = View.Details;
            ipAddressListView.SelectedIndexChanged += ipAddressListView_SelectedIndexChanged;
            // 
            // ipAddressColumn
            // 
            ipAddressColumn.Text = "IP Address";
            ipAddressColumn.Width = 120;
            // 
            // ipTypeColumn
            // 
            ipTypeColumn.Text = "Type";
            ipTypeColumn.Width = 120;
            // 
            // ipDescriptionColumn
            // 
            ipDescriptionColumn.Text = "Description";
            ipDescriptionColumn.Width = 240;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(424, 216);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 23);
            btnOK.TabIndex = 0;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // IpAddressForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnOK;
            ClientSize = new Size(511, 251);
            Controls.Add(btnOK);
            Controls.Add(ipAddressListView);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "IpAddressForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "IP Address";
            Load += IpAddressForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private ListView ipAddressListView;
        private ColumnHeader ipAddressColumn;
        private ColumnHeader ipDescriptionColumn;
        private ColumnHeader ipTypeColumn;
        private Button btnOK;
    }
}