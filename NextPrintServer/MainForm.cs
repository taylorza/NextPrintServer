using System.Drawing.Printing;
using System.Net.Sockets;
using System.Net;

namespace NextPrintServer
{
    public partial class MainForm : BaseForm
    {
        private bool _hidden = false;
        private bool _forceClose = false;

        private Font _font = new("Courier New", 10);
        private PrintDocument _pd = new();
        private Bitmap? _previewImage;
        private Graphics? _previewGraphics;

        public MainForm()
        {
            InitializeComponent();
            Text = Resources.strTitle;
            lblPrinter.Text = Resources.strPrinter;
            lblFont.Text = Resources.strFont;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AddAboutMenu();

            if (string.IsNullOrEmpty(Properties.Settings.Default.Printer))
            {
                txtPrinter.Text = _pd.PrinterSettings.PrinterName;
            }
            else
            {
                txtPrinter.Text = Properties.Settings.Default.Printer;
                _pd.PrinterSettings.PrinterName = Properties.Settings.Default.Printer;
            }

            if (Properties.Settings.Default.PrintFont != null)
            {
                _font = Properties.Settings.Default.PrintFont;
            }
            UpdateFont(_font);
            
            
            new Thread(PrintListener)
            {
                IsBackground = true
            }.Start();
        }

        private void ShowForm()
        {
            if (_hidden)
            {
                txtFont.Select(0, 0);
                txtPrinter.Select(0, 0);
                ShowInTaskbar = true;
                Show();
                WindowState = FormWindowState.Normal;
                Focus();
                _hidden = false;
            }
        }

        private void SendToTray()
        {
            ShowInTaskbar = false;
            Hide();
            _hidden = true;
        }

        private void PrintListener(object? obj)
        {
            var server = new TcpListener(new IPEndPoint(IPAddress.Any, 9100));
            server.Start();

            while (true)
            {
                try
                {
                    Invoke(() => txtStatus.Text = Resources.strWaitingForConnection);
                    using var tcpClient = server.AcceptTcpClient();
                    Invoke(() => txtStatus.Text = string.Format(Resources.strConnectionFrom, tcpClient.Client.RemoteEndPoint));
                    new Client(_font, tcpClient, this).Print(_pd);                    
                }
                catch (Exception ex)
                {
                    Invoke(()=>txtStatus.Text = ex.Message);
                }
            }
        }

        private void UpdateFont(Font font)
        {
            txtFont.Font = font;
            txtFont.Text = $"{font.Name}";
        }

        private void btnFont_Click(object sender, EventArgs e)
        {
            fontDlg.Font = _font;
            if (fontDlg.ShowDialog() == DialogResult.OK)
            {
                _font?.Dispose();
                _font = fontDlg.Font;
                UpdateFont(_font);
                Properties.Settings.Default.PrintFont = _font;
                Properties.Settings.Default.Save();
            }
        }

        private void btnPrinter_Click(object sender, EventArgs e)
        {
            printDlg.Document = _pd;
            if (printDlg.ShowDialog() == DialogResult.OK)
            {
                _pd = printDlg.Document;
                txtPrinter.Text = _pd.PrinterSettings.PrinterName;
                Properties.Settings.Default.Printer = _pd.PrinterSettings.PrinterName;
                Properties.Settings.Default.Save();
            }
        }

        public void PreviewNewPage(float width, float height)
        {
            _previewGraphics?.Dispose();
            _previewImage?.Dispose();

            _previewImage = new Bitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height));
            _previewGraphics = Graphics.FromImage(_previewImage);

            _previewGraphics.FillRectangle(Brushes.White, 0, 0, _previewImage.Width, _previewImage.Height);
            pnlPreview.Refresh();
        }
        public void PreviewAddLine(string s, float x, float y)
        {
            _previewGraphics?.DrawString(s, _font, Brushes.Black, x, y);
            pnlPreview.Refresh();
        }

        private void pnlPreview_Paint(object sender, PaintEventArgs e)
        {
            using Graphics g = e.Graphics;
            if (_previewImage != null)
            {
                g.DrawImage(_previewImage, 0, 0, pnlPreview.ClientSize.Width, pnlPreview.ClientSize.Height);
            }
            else
            {
                g.FillRectangle(Brushes.White, 0, 0, pnlPreview.ClientSize.Width, pnlPreview.ClientSize.Height);
            }
        }

        private void pnlPreview_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                SendToTray();
            }
            else
            {
                pnlPreview.Refresh();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_forceClose)
            {
                SendToTray();
                e.Cancel = true;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm();
        }

        
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _forceClose = true;
            Application.Exit();
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void AddAboutMenu()
        {

        }
    }
}
