namespace NextPrintServer.Core
{
    /// <summary>
    /// Interprets the byte stream sent by the ZX Spectrum Next WIFI printer driver
    /// (ZX print control codes plus a subset of ESC/POS) and renders it onto an
    /// <see cref="IPrintSurface"/>. This is a port of Client.cs from the Windows version.
    /// </summary>
    public class PrintJob
    {
        private readonly Stream _stream;
        private readonly IPrintSurface _surface;

        private int _pendingByte = -1;
        private bool _pageOpen;

        private TextStyle _style = TextStyle.Regular;
        private InkColor _color = InkColor.Black;
        private bool _underline;

        private enum State
        {
            Normal,
            Escape,
        }
        private State _state = State.Normal;

        private static readonly Dictionary<int, char> _charMap = new()
        {
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

        public PrintJob(Stream stream, IPrintSurface surface)
        {
            _stream = stream;
            _surface = surface;
        }

        /// <summary>Number of pages produced. Zero if the job contained nothing printable.</summary>
        public int PageCount { get; private set; }

        /// <summary>Reads the stream to its end, rendering pages as it goes.</summary>
        public void Run()
        {
            while (RenderPage())
            {
                _pageOpen = false;
            }
        }

        // Pages are started lazily so that an empty connection, or a job that ends
        // exactly on a page boundary, does not produce a blank sheet.
        private void EnsurePage()
        {
            if (_pageOpen) return;
            _surface.NewPage();
            _pageOpen = true;
            PageCount++;
        }

        private int ReadByte()
        {
            if (_pendingByte != -1)
            {
                int ch = _pendingByte;
                _pendingByte = -1;
                return ch;
            }

            try
            {
                return _stream.ReadByte();
            }
            catch (IOException)
            {
                // Timeout or connection reset - treat as the end of the job
                return -1;
            }
        }

        /// <returns>true if there is more data to print on a following page</returns>
        private bool RenderPage()
        {
            float leftMargin = _surface.Left;
            float topMargin = _surface.Top;
            float rightMargin = _surface.Right;
            float bottomMargin = _surface.Bottom;

            float lineHeight = _surface.LineHeight;
            float ypos = topMargin;
            float xpos = leftMargin;
            bool ffSeen = false;

            float lastCharWidth = _surface.MeasureChar('i', _style);

            while ((ypos + lineHeight) < bottomMargin)
            {
                int ch = ReadByte();
                if (ch == -1) return false;

                if (ch == 27) // ESC
                {
                    _state = State.Escape;
                    continue;
                }

                switch (_state)
                {
                    case State.Normal:
                        if (ffSeen)
                        {
                            // More data after a form feed, it belongs on the next page
                            _pendingByte = ch;
                            return true;
                        }

                        switch (ch)
                        {
                            case 12: // Form feed
                                EnsurePage();
                                ffSeen = true;
                                break;
                            case 13:
                                ypos += lineHeight;
                                xpos = leftMargin;
                                break;
                            case 6:
                                {
                                    // tab half the line width
                                    float charWidth = _surface.MeasureChar('i', _style);
                                    int halfCharsPerLine = (int)((rightMargin - leftMargin) / charWidth) / 2;
                                    float tabX = leftMargin + (halfCharsPerLine * charWidth);

                                    if (xpos < tabX)
                                    {
                                        xpos = tabX;
                                    }
                                    else
                                    {
                                        xpos = leftMargin;
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
                                    xpos = leftMargin;
                                }
                                else
                                {
                                    xpos += lastCharWidth;
                                }
                                break;
                            case 0x16: // AT y,x
                                {
                                    int y = ReadByte();
                                    if (y == -1) return false;
                                    int x = ReadByte();
                                    if (x == -1) return false;

                                    ypos = Math.Clamp(topMargin + (y * lineHeight), topMargin, bottomMargin - lineHeight);
                                    xpos = Math.Clamp(leftMargin + (x * lastCharWidth), leftMargin, rightMargin - lastCharWidth);
                                }
                                break;
                            case 0x10: // Ink color
                                {
                                    int color = ReadByte();
                                    if (color == -1) return false;
                                    _color = InkColor.FromZX(color);
                                }
                                break;
                            case 23: // Tab control
                                {
                                    int a = ReadByte();
                                    if (a == -1) return false;
                                    int b = ReadByte();
                                    if (b == -1) return false;

                                    int spaces = a + (b * 256);
                                    float spaceWidth = _surface.MeasureChar('i', _style);
                                    lastCharWidth = spaceWidth;
                                    if (spaces > 0)
                                    {
                                        float tabWidth = spaceWidth * spaces;
                                        xpos += tabWidth - ((xpos - leftMargin) % tabWidth);
                                        if (xpos > rightMargin) xpos = leftMargin + (xpos - rightMargin);
                                    }
                                }
                                break;
                            default:
                                {
                                    if (ch < 32 && !_charMap.ContainsKey(ch)) break; // LF and other unsupported control codes

                                    char c = _charMap.TryGetValue(ch, out char mapped) ? mapped : (char)ch;
                                    float width = _surface.MeasureChar(c == ' ' ? 'i' : c, _style);
                                    lastCharWidth = width;
                                    if (xpos + width > rightMargin)
                                    {
                                        ypos += lineHeight;
                                        if ((ypos + lineHeight) > bottomMargin)
                                        {
                                            _pendingByte = ch;
                                            return true;
                                        }
                                        xpos = leftMargin;
                                    }

                                    EnsurePage();
                                    _surface.DrawChar(c, _style, _color, xpos, ypos);
                                    if (_underline)
                                    {
                                        _surface.DrawLine(_color, xpos, ypos + lineHeight - 1, xpos + width * 1.3f, ypos + lineHeight - 1);
                                    }

                                    xpos += width;
                                }
                                break;
                        }
                        break;

                    case State.Escape:
                        _state = State.Normal;
                        switch (ch)
                        {
                            case '@': // Reset
                                _style = TextStyle.Regular;
                                _color = InkColor.Black;
                                _underline = false;
                                break;
                            case 'E': // Bold on
                                _style |= TextStyle.Bold;
                                break;
                            case 'F': // Bold off
                                _style &= ~TextStyle.Bold;
                                break;
                            case '4': // Italic on
                                _style |= TextStyle.Italic;
                                break;
                            case '5': // Italic off
                                _style &= ~TextStyle.Italic;
                                break;
                            case '-': // Underline on/off
                                ch = ReadByte();
                                if (ch == -1) return false;
                                if (ch >= '0') ch -= '0'; // Convert from ASCII
                                _underline = ch != 0;
                                break;
                            case 'r': // Color
                                ch = ReadByte();
                                if (ch == -1) return false;
                                if (ch >= '0') ch -= '0'; // Convert from ASCII
                                _color = InkColor.FromZX(ch);
                                break;
                            case 'g': // Print RGB bitmap
                                {
                                    int widthLow = ReadByte();
                                    if (widthLow == -1) return false;
                                    int widthHigh = ReadByte();
                                    if (widthHigh == -1) return false;
                                    int width = widthLow + (widthHigh * 256);

                                    int heightLow = ReadByte();
                                    if (heightLow == -1) return false;
                                    int heightHigh = ReadByte();
                                    if (heightHigh == -1) return false;
                                    int height = heightLow + (heightHigh * 256);

                                    byte[] data = new byte[checked(width * 3 * height)]; // 3 bytes per pixel (RGB)
                                    for (int i = 0; i < data.Length; i++)
                                    {
                                        int b = ReadByte();
                                        if (b == -1) return false;
                                        data[i] = (byte)b;
                                    }

                                    if (width > 0 && height > 0)
                                    {
                                        EnsurePage();
                                        _surface.DrawBitmap(data, width, height, xpos, ypos);
                                    }

                                    xpos += width;
                                    if (xpos > rightMargin)
                                    {
                                        xpos = leftMargin;
                                        if ((ypos + height) <= bottomMargin)
                                        {
                                            ypos += height;
                                        }
                                    }
                                }
                                break;
                            default:
                                // Unknown escape sequence - ignore it.
                                break;
                        }
                        break;
                }
            }

            return true;
        }
    }
}
