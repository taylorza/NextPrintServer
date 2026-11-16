using SkiaSharp;

namespace NextPrintServer.Core
{
    public record PageSetup(float WidthMm, float HeightMm, float MarginMm)
    {
        public static bool TryParsePaper(string name, out float widthMm, out float heightMm)
        {
            (widthMm, heightMm) = name.ToLowerInvariant() switch
            {
                "a3" => (297f, 420f),
                "a4" => (210f, 297f),
                "a5" => (148f, 210f),
                "letter" => (215.9f, 279.4f),
                "legal" => (215.9f, 355.6f),
                _ => (0f, 0f),
            };
            if (widthMm > 0) return true;

            // Custom size: WIDTHxHEIGHT in millimetres
            var parts = name.ToLowerInvariant().Split('x');
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            return parts.Length == 2
                && float.TryParse(parts[0], culture, out widthMm)
                && float.TryParse(parts[1], culture, out heightMm)
                && widthMm > 0 && heightMm > 0;
        }
    }

    /// <summary>Renders a print job to a PDF document using Skia.</summary>
    public sealed class PdfPrintSurface : IPrintSurface, IDisposable
    {
        private const float UnitsPerMm = 100f / 25.4f;      // surface units are 1/100 inch
        private const float PointsPerUnit = 72f / 100f;

        private readonly SKDocument _document;
        private readonly float _pageWidth;
        private readonly float _pageHeight;
        private readonly string _fontFamilyOrFile;
        private readonly float _fontSize;

        private readonly SKPaint _paint = new() { IsAntialias = true };
        private readonly Dictionary<TextStyle, SKFont> _fonts = new();
        private readonly Dictionary<(TextStyle, char), SKFont> _fallbackFonts = new();
        private readonly Dictionary<(TextStyle, char), float> _widths = new();
        private readonly List<IDisposable> _disposables = new();

        private SKCanvas? _canvas;

        /// <param name="output">Stream the PDF is written to</param>
        /// <param name="font">Font family name, or the path to a font file</param>
        /// <param name="fontSizePt">Font size in points</param>
        public PdfPrintSurface(Stream output, PageSetup page, string font, float fontSizePt)
        {
            _pageWidth = page.WidthMm * UnitsPerMm;
            _pageHeight = page.HeightMm * UnitsPerMm;
            float margin = page.MarginMm * UnitsPerMm;
            Left = margin;
            Top = margin;
            Right = _pageWidth - margin;
            Bottom = _pageHeight - margin;

            _fontFamilyOrFile = font;
            _fontSize = fontSizePt / PointsPerUnit;
            LineHeight = GetFont(TextStyle.Regular).Spacing;

            _document = SKDocument.CreatePdf(output);
        }

        public float Left { get; }
        public float Top { get; }
        public float Right { get; }
        public float Bottom { get; }
        public float LineHeight { get; }

        /// <summary>The family name of the font that was actually loaded.</summary>
        public string FontFamily => GetFont(TextStyle.Regular).Typeface.FamilyName;

        public float MeasureChar(char c, TextStyle style)
        {
            if (!_widths.TryGetValue((style, c), out float width))
            {
                width = GetFontFor(c, style).MeasureText(c.ToString());
                _widths[(style, c)] = width;
            }
            return width;
        }

        public void NewPage()
        {
            if (_canvas != null) _document.EndPage();
            _canvas = _document.BeginPage(_pageWidth * PointsPerUnit, _pageHeight * PointsPerUnit);
            _canvas.Scale(PointsPerUnit);
        }

        public void DrawChar(char c, TextStyle style, InkColor color, float x, float y)
        {
            if (_canvas == null) return;
            _paint.Color = new SKColor(color.R, color.G, color.B);
            // y is the top of the line, Skia draws from the baseline
            float baseline = y - GetFont(style).Metrics.Ascent;
            _canvas.DrawText(c.ToString(), x, baseline, SKTextAlign.Left, GetFontFor(c, style), _paint);
        }

        public void DrawLine(InkColor color, float x1, float y1, float x2, float y2)
        {
            if (_canvas == null) return;
            _paint.Color = new SKColor(color.R, color.G, color.B);
            _paint.StrokeWidth = 1;
            _canvas.DrawLine(x1, y1, x2, y2, _paint);
        }

        public void DrawBitmap(byte[] rgb, int width, int height, float x, float y)
        {
            if (_canvas == null) return;

            byte[] rgba = new byte[width * height * 4];
            for (int src = 0, dst = 0; dst < rgba.Length; src += 3, dst += 4)
            {
                rgba[dst] = rgb[src];
                rgba[dst + 1] = rgb[src + 1];
                rgba[dst + 2] = rgb[src + 2];
                rgba[dst + 3] = 255;
            }

            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var image = SKImage.FromPixelCopy(info, rgba);
            _canvas.DrawImage(image, SKRect.Create(x, y, width, height));
        }

        public void Dispose()
        {
            if (_canvas != null) _document.EndPage();
            _canvas = null;
            _document.Close();
            _document.Dispose();
            _paint.Dispose();
            foreach (var item in _disposables) item.Dispose();
            _disposables.Clear();
        }

        private SKFont GetFont(TextStyle style)
        {
            if (!_fonts.TryGetValue(style, out var font))
            {
                SKTypeface? typeface = File.Exists(_fontFamilyOrFile)
                    ? SKTypeface.FromFile(_fontFamilyOrFile)
                    : SKTypeface.FromFamilyName(_fontFamilyOrFile, ToSkStyle(style));
                font = CreateFont(typeface ?? SKTypeface.Default, style);
                _fonts[style] = font;
            }
            return font;
        }

        // The ZX block graphics are missing from many fonts, so fall back to
        // any installed font that has the character.
        private SKFont GetFontFor(char c, TextStyle style)
        {
            var font = GetFont(style);
            if (font.ContainsGlyph(c)) return font;

            if (!_fallbackFonts.TryGetValue((style, c), out var fallback))
            {
                var typeface = SKFontManager.Default.MatchCharacter(font.Typeface.FamilyName, ToSkStyle(style), null, c);
                fallback = typeface != null ? CreateFont(typeface, style) : font;
                _fallbackFonts[(style, c)] = fallback;
            }
            return fallback;
        }

        private SKFont CreateFont(SKTypeface typeface, TextStyle style)
        {
            var font = new SKFont(typeface, _fontSize)
            {
                Subpixel = true,
                // Synthesize styles the font family does not provide
                Embolden = style.HasFlag(TextStyle.Bold) && !typeface.IsBold,
                SkewX = style.HasFlag(TextStyle.Italic) && !typeface.IsItalic ? -0.25f : 0,
            };
            _disposables.Add(font);
            return font;
        }

        private static SKFontStyle ToSkStyle(TextStyle style) => new(
            style.HasFlag(TextStyle.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            style.HasFlag(TextStyle.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
    }
}
