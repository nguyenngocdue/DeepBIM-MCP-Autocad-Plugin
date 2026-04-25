using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace autocad_mcp_plugin.Utils
{
    /// <summary>
    /// Creates ribbon button images using Segoe MDL2 Assets glyphs.
    /// Same visual style as the Revit plugin: rounded rectangle with DeepBim accent #59DCCB.
    /// </summary>
    public static class RibbonIconHelper
    {
        private static readonly Color AccentColor = Color.FromRgb(0x59, 0xDC, 0xCB);
        private static readonly Color AccentDark  = Color.FromRgb(0x2F, 0xB3, 0xA5);
        private static readonly Color StartColor  = Color.FromRgb(0x43, 0xA0, 0x47); // green
        private static readonly Color StartDark   = Color.FromRgb(0x2E, 0x7D, 0x32);
        private static readonly Color StopColor   = Color.FromRgb(0xE5, 0x39, 0x35); // red
        private static readonly Color StopDark    = Color.FromRgb(0xB7, 0x1C, 0x1C);

        private const string SegoeMdl2 = "Segoe MDL2 Assets";

        // Segoe MDL2 glyphs
        private const string GlyphStart    = "\uE768"; // Play
        private const string GlyphStop     = "\uE71A"; // Stop
        private const string GlyphSettings = "\uE713"; // Settings gear
        private const string GlyphStatus   = "\uE946"; // Info / Status

        public static BitmapSource GetLargeImage(string kind) => CreateIcon(32, kind);
        public static BitmapSource GetSmallImage(string kind)  => CreateIcon(16, kind);

        private static BitmapSource CreateIcon(int size, string kind)
        {
            string glyph  = kind == "stop"     ? GlyphStop
                          : kind == "settings" ? GlyphSettings
                          : kind == "status"   ? GlyphStatus
                          : GlyphStart; // start / default

            Color bg   = kind == "stop"  ? StopColor
                       : kind == "start" ? StartColor
                       : AccentColor;

            Color dark = kind == "stop"  ? StopDark
                       : kind == "start" ? StartDark
                       : AccentDark;

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, size, size));

                var typeface = new Typeface(
                    new FontFamily(SegoeMdl2),
                    FontStyles.Normal,
                    FontWeights.Normal,
                    FontStretches.Normal);

                double fontSize = size * 0.65;
                const double dpi = 1.0;

                var text = new FormattedText(
                    glyph,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.White,
                    null,
                    dpi);

                double x = (size - text.Width)  / 2;
                double y = (size - text.Height) / 2;

                var bgRect = new Rect(0, 0, size, size);
                dc.DrawRoundedRectangle(
                    new SolidColorBrush(bg),
                    new Pen(new SolidColorBrush(dark), size > 20 ? 1 : 0.5),
                    bgRect, size * 0.2, size * 0.2);

                dc.DrawText(text, new Point(x, y));
            }

            var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);
            bmp.Freeze();
            return bmp;
        }
    }
}
