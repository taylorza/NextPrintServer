using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NextPrintServer
{
    public partial class PreviewPanel : Panel
    {
        public PreviewPanel()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (DesignMode)
            {
                base.OnPaintBackground(e);
            }
            // Do nothing            
        }
    }
}
