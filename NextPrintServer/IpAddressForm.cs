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

namespace NextPrintServer
{
    public partial class IpAddressForm : Form
    {
        public IpAddressForm()
        {
            InitializeComponent();
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

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in networkInterfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up || !ni.Supports(NetworkInterfaceComponent.IPv4)) continue;

                var properties = ni.GetIPProperties();
                foreach (var ua in properties.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var item = ipAddressListView.Items.Add(ua.Address.ToString());
                        item.SubItems.Add(ni.NetworkInterfaceType.ToString());
                        item.SubItems.Add(ni.Description);
                        if (localEndPoint != null && localEndPoint.Address.Equals(ua.Address))
                        {
                            item.Font = new Font(item.Font, FontStyle.Bold);
                        }
                    }
                }
            }
        }

        private void ipAddressListView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }
    }
}
