namespace NextPrintServer.Core
{
    [Flags]
    public enum TextStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
    }

    public readonly record struct InkColor(byte R, byte G, byte B)
    {
        public static readonly InkColor Black = new(0, 0, 0);

        /// <summary>Maps a ZX Spectrum / ESC r colour number to an ink colour.</summary>
        public static InkColor FromZX(int color) => color switch
        {
            0 => Black,
            1 => new(0, 0, 255),        // Blue
            2 => new(255, 0, 0),        // Red
            3 => new(255, 0, 255),      // Magenta
            4 => new(0, 128, 0),        // Green
            5 => new(0, 255, 255),      // Cyan
            6 => new(255, 255, 0),      // Yellow
            7 => new(211, 211, 211),    // Light gray
            _ => Black,
        };
    }

    /// <summary>
    /// The page a print job is rendered onto. All coordinates are in hundredths of an inch,
    /// the same unit the Windows version gets from System.Drawing.Printing.
    /// </summary>
    public interface IPrintSurface
    {
        // Printable area
        float Left { get; }
        float Top { get; }
        float Right { get; }
        float Bottom { get; }

        float LineHeight { get; }

        float MeasureChar(char c, TextStyle style);

        /// <summary>Finishes the current page, if any, and starts a new one.</summary>
        void NewPage();

        void DrawChar(char c, TextStyle style, InkColor color, float x, float y);
        void DrawLine(InkColor color, float x1, float y1, float x2, float y2);

        /// <summary>Draws a bitmap of 3 bytes per pixel (RGB) at one pixel per unit.</summary>
        void DrawBitmap(byte[] rgb, int width, int height, float x, float y);
    }
}
