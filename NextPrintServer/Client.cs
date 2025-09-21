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

        private FontManager _fontManager;
        private BrushManager _brushManager;
        private PenManager _penManager;

        private Font _currentFont;
        private Brush _currentBrush;
        private Pen _currentPen;
        private FontStyle _currentStyle = FontStyle.Regular;
        private bool _underline = false;

        enum State
        {
            Normal,
            Escape,
        }
        private State _currentState = State.Normal;


        static readonly Dictionary<int, char> _charMap = new Dictionary<int, char> {
            { 0x7f, '©' },
            { 0x60, '£' },
            { 0x5e, '↑' },
            { 0x80, ' ' },        // All white
            { 0x81, '▝' },
            { 0x82, '▘' },
            { 0x83, '▀' },
            { 0x84, '▗' },
            { 0x85, '▐' },
            { 0x86, '▚' },
            { 0x87, '▜' },
            { 0x88, '▖' },
            { 0x89, '▞' },
            { 0x8a, '▌' },
            { 0x8b, '▛' },
            { 0x8c, '▄' },
            { 0x8d, '▟' },
            { 0x8e, '▙' },
            { 0x8f, '█' },
         };

        internal Client(Font font, TcpClient tcpClient, MainForm form)
        {
            _font = font;
            _stream = tcpClient.GetStream();
            _form = form;
            _startch = 0;

            _fontManager = new FontManager(_font);
            _brushManager = new BrushManager();
            _penManager = new PenManager();

            _currentFont = _fontManager.GetFont(FontStyle.Regular);
            _currentBrush = _brushManager.GetBrush(Color.Black);
            _currentPen = _penManager.GetPen(Color.Black);            
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
                _fontManager.Dispose();
                _brushManager.Dispose();
                _penManager.Dispose();
                _stream.Close();
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (e.Graphics == null) return;

            float leftMargin = e.PageSettings.PrintableArea.Left;
            float topMargin = e.PageSettings.PrintableArea.Top;
            float rightMargin = e.PageSettings.PrintableArea.Right;
            float bottomMargin = e.PageSettings.PrintableArea.Bottom;

            var hasMorePages = true;
            float lineHeight = _font.GetHeight(e.Graphics);
            float ypos = topMargin;
            float xpos = leftMargin;
            bool ffSeen = false;

            CharacterRange[] ranges = { new CharacterRange(0, 1) };
            StringFormat stringFormat = new StringFormat();
            stringFormat.SetMeasurableCharacterRanges(ranges);
            stringFormat.FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap;

            _form.Invoke(new Action(() => _form.PreviewNewPage(rightMargin - leftMargin, bottomMargin - topMargin)));

            Region[]? regions = null;
            SizeF size;
            float lastCharWidth;

            regions = e.Graphics.MeasureCharacterRanges("i", _currentFont, RectangleF.Empty, stringFormat);
            size = regions[0].GetBounds(e.Graphics).Size;
            lastCharWidth = size.Width;
            
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
                    hasMorePages = false;
                    break;
                }

                if (ch == 27) // ESC
                {
                    _currentState = State.Escape;
                    continue;
                }

                switch (_currentState)
                {
                    case State.Normal:
                        if (ffSeen)
                        {
                            _startch = ch;
                            hasMorePages = true;
                            break;
                        }

                        if (ch == 12)
                        {
                            ffSeen = true;
                            continue;
                        }
                        else if (ch == 13)
                        {
                            ypos += lineHeight;
                            xpos = leftMargin;
                        }
                        else
                        {
                            switch (ch)
                            {
                                case 6:
                                    {
                                        // tab half the line width
                                        regions = e.Graphics.MeasureCharacterRanges("i", _currentFont, RectangleF.Empty, stringFormat);
                                        size = regions[0].GetBounds(e.Graphics).Size;

                                        int halfCharsPerLine = (int)((rightMargin - leftMargin) / size.Width) / 2;
                                        float tabX = leftMargin;
                                        while (halfCharsPerLine-- > 0) tabX += size.Width;

                                        if (xpos < tabX)
                                        {
                                            xpos = tabX;
                                        } 
                                        else 
                                        { 
                                            xpos = leftMargin;
                                            if ((ypos + lineHeight) > bottomMargin)
                                            {
                                                hasMorePages = true;
                                                ffSeen = false;
                                                break;
                                            }
                                            ypos += lineHeight;
                                        }
                                    }
                                    break;
                                case 8:
                                    if (xpos - lastCharWidth < leftMargin)
                                    {
                                        if (ypos == topMargin)
                                        {
                                            xpos = leftMargin;
                                        }
                                        else
                                        {
                                            xpos = rightMargin - lastCharWidth;
                                            ypos -= lineHeight;
                                        }
                                    }
                                    else
                                    {
                                        xpos -= lastCharWidth;
                                    }
                                    break;
                                case 9:
                                    if (xpos + lastCharWidth > rightMargin)
                                    {
                                        ypos += lineHeight;
                                        if ((ypos + lineHeight) > bottomMargin)
                                        {
                                            hasMorePages = true;
                                            ffSeen = false;
                                            break;
                                        }
                                        xpos = leftMargin;
                                    }
                                    else
                                    {
                                        xpos += lastCharWidth;
                                    }
                                    break;
                                case 0x16: // AT y,x
                                    int y = _stream.ReadByte();
                                    if (y == -1)
                                    {
                                        e.HasMorePages = false;
                                        return;
                                    }
                                    int x = _stream.ReadByte();
                                    if (x == -1)
                                    {
                                        e.HasMorePages = false;
                                        return;
                                    }

                                    float newYpos = topMargin + (y * lineHeight);
                                    if (newYpos < topMargin) newYpos = topMargin;
                                    if (newYpos > bottomMargin - lineHeight) newYpos = bottomMargin - lineHeight;
                                    ypos = newYpos;
                                    float newXpos = leftMargin + (x * lastCharWidth);
                                    if (newXpos < leftMargin) newXpos = leftMargin;
                                    if (newXpos > rightMargin - lastCharWidth) newXpos = rightMargin - lastCharWidth;
                                    xpos = newXpos;
                                    break;
                                case 0x10: // Ink color
                                    {
                                        int color = _stream.ReadByte();
                                        if (color == -1)
                                        {
                                            e.HasMorePages = false;
                                            return;
                                        }
                                        SetInkColor(color);
                                    }
                                    break;
                                case 23: // Tab control
                                    int a = _stream.ReadByte();
                                    if (a == -1)
                                    {
                                        e.HasMorePages = false;
                                        return;
                                    }
                                    int b = _stream.ReadByte();
                                    if (b == -1)
                                    {
                                        e.HasMorePages = false;
                                        return;
                                    }
                                    int spaces = a+(b*256);
                                    regions = e.Graphics.MeasureCharacterRanges('i'.ToString(), _currentFont, RectangleF.Empty, stringFormat);
                                    float spaceWidth = regions[0].GetBounds(e.Graphics).Size.Width;
                                    lastCharWidth = spaceWidth;
                                    float tabWidth = spaceWidth * spaces;
                                    xpos += tabWidth - ((xpos - leftMargin) % tabWidth);
                                    if (xpos > rightMargin) xpos = leftMargin + (xpos - rightMargin);
                                    break;
                                case 10:
                                    // Ignore LF
                                    break;
                                default:
                                    if (_charMap.TryGetValue(ch, out char newCh))
                                    {
                                        ch = newCh;
                                    }
                                    char c = (char)ch;
                                    char measureChar = c;
                                    if (measureChar == ' ') measureChar = 'i';

                                    regions = e.Graphics.MeasureCharacterRanges(measureChar.ToString(), _currentFont, RectangleF.Empty, stringFormat);
                                    size = regions[0].GetBounds(e.Graphics).Size;
                                    lastCharWidth = size.Width;
                                    if (xpos + size.Width > rightMargin)
                                    {
                                        ypos += lineHeight;
                                        if ((ypos + lineHeight) > bottomMargin)
                                        {
                                            _startch = ch;
                                            hasMorePages = true;
                                            ffSeen = false;
                                            break;
                                        }
                                        xpos = leftMargin;
                                    }
                                    
                                    e.Graphics.DrawString(c.ToString(), _currentFont, _currentBrush, xpos, ypos);
                                    if (_underline)
                                    {
                                        e.Graphics.DrawLine(_currentPen, xpos, ypos + lineHeight - 1, xpos + size.Width*1.3f, ypos + lineHeight - 1);
                                    }
                                    _form.Invoke(new Action(() => _form.PreviewAddChar(c, _currentFont, _underline, _currentBrush, _currentPen, xpos, ypos, size.Width*1.3f, lineHeight)));

                                    xpos += size.Width;
                                    break;
                            }
                        }
                        break;

                    case State.Escape:
                        switch (ch)
                        {
                            case (int)'@': // Reset
                                _currentStyle = FontStyle.Regular;
                                _currentFont = _fontManager.GetFont(_currentStyle);
                                _currentBrush = _brushManager.GetBrush(Color.Black);
                                _currentPen = _penManager.GetPen(Color.Black);
                                _underline = false;
                                break;
                            case (int)'E': // Bold on
                                _currentStyle |= FontStyle.Bold;
                                _currentFont = _fontManager.GetFont(_currentStyle);
                                break;
                            case (int)'F': // Bold off
                                _currentStyle &= ~FontStyle.Bold;
                                _currentFont = _fontManager.GetFont(_currentStyle);
                                break;
                            case (int)'4': // Italic on
                                _currentStyle |= FontStyle.Italic;
                                _currentFont = _fontManager.GetFont(_currentStyle);
                                break;
                            case (int)'5': // Italic off
                                _currentStyle &= ~FontStyle.Italic;
                                _currentFont = _fontManager.GetFont(_currentStyle);
                                break;
                            case (int)'-': // Underline on/off
                                ch = _stream.ReadByte();
                                if (ch == -1)
                                {
                                    e.HasMorePages = false;
                                    return;
                                }
                                if (ch >= '0') ch -= '0'; // Convert from ASCII
                                _underline = ch != 0;                    
                                break;
                            case (int)'r': // Color
                                ch = _stream.ReadByte();
                                if (ch == -1)
                                {
                                    e.HasMorePages = false;
                                    return;
                                }
                                if (ch >= '0') ch -= '0'; // Convert from ASCII
                                SetInkColor(ch);
                                break;

                            default:
                                // Unknown escape sequence - ignore it.
                                break;
                        }
                        _currentState = State.Normal;
                        break;
                }
            }
            e.HasMorePages = hasMorePages;
        }

        private Color MapToZXColor(int color)
        {
            return color switch
            {
                0 => Color.Black,
                1 => Color.Blue,
                2 => Color.Red,
                3 => Color.Magenta,
                4 => Color.Green,
                5 => Color.Cyan,
                6 => Color.Yellow,
                7 => Color.LightGray,
                _ => Color.Black,
            };
        }

        private void SetInkColor(int color)
        {
            Color c = MapToZXColor(color);
            _currentBrush = _brushManager.GetBrush(c);
            _currentPen = _penManager.GetPen(c);
        }
    }

    class PenManager : IDisposable
    {
        public PenManager()
        {
            _resourceManager = new ResourceManager<Color, Pen>(CreatePen);
        }
        public Pen GetPen(Color color)
        {
            return _resourceManager.GetResource(color);
        }
        private Pen CreatePen(Color color)
        {
            var pen = new Pen(color);
            _disposables.Add(pen);
            return pen;
        }
        public void Dispose()
        {
            foreach (var item in _disposables)
            {
                item.Dispose();
            }
            _disposables.Clear();
        }
        private readonly ResourceManager<Color, Pen> _resourceManager;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
    }

    class BrushManager : IDisposable
    {
        public BrushManager()
        {
            _resourceManager = new ResourceManager<Color, Brush>(CreateBrush);
        }
        public Brush GetBrush(Color color)
        {
            return _resourceManager.GetResource(color);
        }
        private Brush CreateBrush(Color color)
        {
            var brush = new SolidBrush(color);
            _disposables.Add(brush);
            return brush;
        }
        public void Dispose()
        {
            foreach (var item in _disposables)
            {
                item.Dispose();
            }
            _disposables.Clear();
        }
        private readonly ResourceManager<Color, Brush> _resourceManager;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
    }

    class FontManager : IDisposable
    {
        public FontManager(Font template)
        {
            _resourceManager = new ResourceManager<FontStyle, Font>(CreateFont);
            _template = template;
        }
        public Font GetFont(FontStyle style)
        {
            return _resourceManager.GetResource(style);
        }
        private Font CreateFont(FontStyle style)
        {
            return new Font(_template, style);
        }
        public void Dispose()
        {
            foreach (var item in _disposables)
            {
                item.Dispose();
            }
            _disposables.Clear();
        }
        private readonly ResourceManager<FontStyle, Font> _resourceManager;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly Font _template;
    }

    class ResourceManager<T, Resource>
    {
        public ResourceManager(Func<T, Resource> resourceFactory)
        {
            _resourceFactory = resourceFactory;
        }
        public Resource GetResource(T key)
        {
            foreach (var item in _items)
            {
                if (item.Key.Equals(key))
                {
                    return item.Resource;
                }
            }
            var resource = _resourceFactory(key);
            _items.Add(new ResourceItem(key, resource));
            return resource;
        }

        private readonly List<ResourceItem> _items = new List<ResourceItem>();
        private Func<T, Resource> _resourceFactory;

        private class ResourceItem(T key, Resource resource)
        {
            public T Key = key;
            public Resource Resource = resource;
        }
    }
}
