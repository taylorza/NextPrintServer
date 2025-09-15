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

            Font font = _font;
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

                                    var regions = e.Graphics.MeasureCharacterRanges(measureChar.ToString(), font, RectangleF.Empty, stringFormat);
                                    SizeF size = regions[0].GetBounds(e.Graphics).Size;

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
                                switch (ch)
                                {
                                    case 0: // Black
                                        _currentBrush = _brushManager.GetBrush(Color.Black);
                                        _currentPen = _penManager.GetPen(Color.Black);
                                        break;
                                    case 1: // Blue
                                        _currentBrush = _brushManager.GetBrush(Color.Blue);
                                        _currentPen = _penManager.GetPen(Color.Blue);
                                        break;
                                    case 2: // Red
                                        _currentBrush = _brushManager.GetBrush(Color.Red);
                                        _currentPen = _penManager.GetPen(Color.Red);
                                        break;
                                    case 3: // Magenta
                                        _currentBrush = _brushManager.GetBrush(Color.Magenta);
                                        _currentPen = _penManager.GetPen(Color.Magenta);
                                        break;
                                    case 4: // Green
                                        _currentBrush = _brushManager.GetBrush(Color.Green);
                                        _currentPen = _penManager.GetPen(Color.Green);
                                        break;
                                    case 5: // Cyan
                                        _currentBrush = _brushManager.GetBrush(Color.Cyan);
                                        _currentPen = _penManager.GetPen(Color.Cyan);
                                        break;
                                    case 6: // Yellow
                                        _currentBrush = _brushManager.GetBrush(Color.Yellow);
                                        _currentPen = _penManager.GetPen(Color.Yellow);
                                        break;
                                    case 7: // White
                                        _currentBrush = _brushManager.GetBrush(Color.LightGray);
                                        _currentPen = _penManager.GetPen(Color.LightGray);
                                        break;
                                    default:
                                        // Unknown color - ignore it.
                                        break;
                                }
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
