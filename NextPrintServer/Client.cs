using System.Drawing;
using System.Drawing.Printing;
using System.Net.Sockets;
using System.Text;

namespace NextPrintServer
{
    internal class Client
    {
        private readonly Font _font;
        private readonly Stream _stream;
        private readonly MainForm _form;
        private int _startch;
        
        internal Client(Font font, TcpClient tcpClient, MainForm form)
        {
            _font = font;
            _stream = tcpClient.GetStream();
            _form = form;            
            _startch = 0;         
        }

        internal void Print(PrintDocument pd)
        {
            try
            {
                pd.PrintPage += Pd_PrintPage;
                pd.Print();                
            }
            finally
            {
                pd.PrintPage -= Pd_PrintPage;
            }            
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (e.Graphics == null) return;
           
            float leftMargin = e.PageSettings.PrintableArea.Left;
            float topMargin = e.PageSettings.PrintableArea.Top;
            float rightMargin = e.PageSettings.PrintableArea.Right;
            float bottomMargin = e.PageSettings.PrintableArea.Bottom;
            
            var line = new StringBuilder();
            var hasMorePages = true;
            float lineHeight = _font.GetHeight(e.Graphics);
            float ypos = topMargin; 
            bool ffSeen = false;
            
            _form.Invoke(new Action(() => _form.PreviewNewPage(rightMargin - leftMargin, bottomMargin - topMargin)));
            while ((ypos + lineHeight) < bottomMargin)
            {
                int ch;
                if (_startch != 0)
                {
                    ch = _startch;
                    _startch = 0;
                }
                else
                {
                    ch = _stream.ReadByte();
                }
                
                if (ch == -1)
                {
                    if (line.Length > 0)
                    {
                        PrintLine(e.Graphics, leftMargin, line, ypos);
                        line.Length = 0;
                    }
                    hasMorePages = false;
                    break;
                }
                
                if (ffSeen)
                {
                    _startch = ch;                    
                    hasMorePages = true;
                    break;
                }
                
                if (ch == 12)
                {
                    if (line.Length > 0)
                    {
                        PrintLine(e.Graphics, leftMargin, line, ypos);
                        line.Length = 0;
                    }
                    ffSeen = true;
                    continue;
                }
                else if (ch == 13)
                {
                    if (line.Length > 0)
                    {
                        PrintLine(e.Graphics, leftMargin, line, ypos);
                        line.Length = 0;
                    }
                    ypos += lineHeight;                    
                }
                else
                {
                    switch (ch)
                    {
                        case 10:
                            // Ignore LF
                            break;
                        default:
                            line.Append((char)ch);
                            var size = e.Graphics.MeasureString(line.ToString(), _font);

                            if (size.Width > rightMargin - leftMargin)
                            {
                                line.Length -= 1;
                                PrintLine(e.Graphics, leftMargin, line, ypos);
                                ypos += lineHeight;
                                line.Length = 0;
                                line.Append((char)ch);
                            }
                            break;
                    }
                }                
            }       
            e.HasMorePages = hasMorePages;
        }

        private void PrintLine(Graphics g, float leftMargin, StringBuilder line, float ypos)
        {
            var s = line.ToString();
            g.DrawString(s, _font, Brushes.Black, leftMargin, ypos);
            _form.Invoke(new Action(() => _form.PreviewAddLine(s, leftMargin, ypos)));
        }
    }
}
