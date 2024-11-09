
namespace NextPrintServer
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            lblFont = new Label();
            txtFont = new TextBox();
            btnFont = new Button();
            fontDlg = new FontDialog();
            printDlg = new PrintDialog();
            lblPrinter = new Label();
            btnPrinter = new Button();
            txtPrinter = new TextBox();
            statusStrip1 = new StatusStrip();
            txtStatus = new ToolStripStatusLabel();
            pnlPreview = new PreviewPanel();
            notifyIcon = new NotifyIcon(components);
            contextMenuStrip1 = new ContextMenuStrip(components);
            openToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // lblFont
            // 
            lblFont.AutoSize = true;
            lblFont.Location = new Point(12, 38);
            lblFont.Name = "lblFont";
            lblFont.Size = new Size(31, 15);
            lblFont.TabIndex = 4;
            lblFont.Text = "Font";
            // 
            // txtFont
            // 
            txtFont.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFont.Location = new Point(60, 35);
            txtFont.Name = "txtFont";
            txtFont.ReadOnly = true;
            txtFont.Size = new Size(206, 23);
            txtFont.TabIndex = 5;
            // 
            // btnFont
            // 
            btnFont.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFont.Location = new Point(265, 35);
            btnFont.Name = "btnFont";
            btnFont.Size = new Size(30, 23);
            btnFont.TabIndex = 6;
            btnFont.Text = "...";
            btnFont.UseVisualStyleBackColor = true;
            btnFont.Click += btnFont_Click;
            // 
            // lblPrinter
            // 
            lblPrinter.AutoSize = true;
            lblPrinter.Location = new Point(12, 13);
            lblPrinter.Name = "lblPrinter";
            lblPrinter.Size = new Size(42, 15);
            lblPrinter.TabIndex = 1;
            lblPrinter.Text = "Printer";
            // 
            // btnPrinter
            // 
            btnPrinter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrinter.Location = new Point(265, 9);
            btnPrinter.Name = "btnPrinter";
            btnPrinter.Size = new Size(30, 23);
            btnPrinter.TabIndex = 3;
            btnPrinter.Text = "...";
            btnPrinter.UseVisualStyleBackColor = true;
            btnPrinter.Click += btnPrinter_Click;
            // 
            // txtPrinter
            // 
            txtPrinter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPrinter.Location = new Point(60, 9);
            txtPrinter.Name = "txtPrinter";
            txtPrinter.ReadOnly = true;
            txtPrinter.Size = new Size(206, 23);
            txtPrinter.TabIndex = 2;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { txtStatus });
            statusStrip1.Location = new Point(0, 428);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(307, 22);
            statusStrip1.TabIndex = 8;
            statusStrip1.Text = "statusStrip1";
            // 
            // txtStatus
            // 
            txtStatus.Name = "txtStatus";
            txtStatus.Size = new Size(0, 17);
            // 
            // pnlPreview
            // 
            pnlPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlPreview.BackColor = Color.White;
            pnlPreview.BorderStyle = BorderStyle.FixedSingle;
            pnlPreview.Location = new Point(12, 75);
            pnlPreview.Name = "pnlPreview";
            pnlPreview.Size = new Size(283, 337);
            pnlPreview.TabIndex = 99;
            pnlPreview.Paint += pnlPreview_Paint;
            pnlPreview.Resize += pnlPreview_Resize;
            // 
            // notifyIcon
            // 
            notifyIcon.ContextMenuStrip = contextMenuStrip1;
            notifyIcon.Icon = (Icon)resources.GetObject("notifyIcon.Icon");
            notifyIcon.Text = "Next Print Server";
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += notifyIcon_DoubleClick;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { openToolStripMenuItem, toolStripMenuItem1, exitToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(104, 54);
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(103, 22);
            openToolStripMenuItem.Text = "&Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(100, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(103, 22);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(307, 450);
            Controls.Add(pnlPreview);
            Controls.Add(statusStrip1);
            Controls.Add(txtPrinter);
            Controls.Add(btnPrinter);
            Controls.Add(lblPrinter);
            Controls.Add(btnFont);
            Controls.Add(txtFont);
            Controls.Add(lblFont);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "ZX Spectrum Next Print Server";
            FormClosing += MainForm_FormClosing;
            Load += Form1_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblFont;
        private TextBox txtFont;
        private Button btnFont;
        private FontDialog fontDlg;
        private PrintDialog printDlg;
        private Label lblPrinter;
        private Button btnPrinter;
        private TextBox txtPrinter;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel txtStatus;
        private PreviewPanel pnlPreview;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem exitToolStripMenuItem;
    }
}
