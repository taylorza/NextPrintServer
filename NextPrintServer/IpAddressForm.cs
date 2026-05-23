using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NextPrintServer
{
    public partial class IpAddressForm : Form
    {
        private Font? _fontHighlight;
        public IpAddressForm()
        {
            InitializeComponent();
            _fontHighlight = new Font(ipAddressListView.Font, FontStyle.Bold);
        }

        private void IpAddressForm_Load(object sender, EventArgs e)
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            IPEndPoint? localEndPoint = null;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                localEndPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
            }

            int charWidth = (int)(TextRenderer.MeasureText("W", ipAddressListView.Font).Width + 1);
            int[] columnWidths = new int[ipAddressListView.Columns.Count];

            foreach (ColumnHeader column in ipAddressListView.Columns)
            {
                columnWidths[column.Index] = (int)(TextRenderer.MeasureText(column.Text, ipAddressListView.Font).Width);
            }

            ipAddressListView.BeginUpdate();
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in networkInterfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up || !ni.Supports(NetworkInterfaceComponent.IPv4)) continue;

                var properties = ni.GetIPProperties();
                foreach (var ua in properties.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ipAddress = ua.Address.ToString();
                        var interfaceType = ni.NetworkInterfaceType.ToString();
                        var description = ni.Description;

                        var items = new string[] { ipAddress, interfaceType, description };
                        for (int i = 0; i < items.Length; i++)
                        {
                            int w = TextRenderer.MeasureText(items[i], ipAddressListView.Font).Width + charWidth;
                            if (w > columnWidths[i]) columnWidths[i] = w;
                        }

                        var item = ipAddressListView.Items.Add(new ListViewItem(items));
                        if (localEndPoint != null && localEndPoint.Address.Equals(ua.Address))
                        {
                            item.Font = _fontHighlight;
                        }
                    }
                }
            }

            for (int i = 0; i < columnWidths.Length; i++)
            {
                ipAddressListView.Columns[i].Width = columnWidths[i];
            }
            ipAddressListView.EndUpdate();
        }

        private void ipAddressListView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void IpAddressForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _fontHighlight?.Dispose();
            _fontHighlight = null;
        }
    }
}
