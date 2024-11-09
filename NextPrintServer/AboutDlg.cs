using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NextPrintServer
{
    public partial class AboutDlg : Form
    {
        public AboutDlg()
        {
            InitializeComponent();
        }

        private void AboutDlg_Load(object sender, EventArgs e)
        {
            var versionAttribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            Text = Resources.strAboutTitle; 
            lblMessage.Text = string.Format(Resources.strAbout, versionAttribute?.InformationalVersion, DateTime.Now.Year);
            panelAboutIcon.BackgroundImage = Resources.AppIcon.ToBitmap();
        }
    }
}
