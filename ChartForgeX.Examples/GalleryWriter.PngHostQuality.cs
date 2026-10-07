using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

public static partial class GalleryWriter {
    // Transparent charts without a card can put fitted y labels and thin axes or guides at the edge.
    // Allow only their declared paint and measured geometry; unrelated marks still trigger edge pressure.
    private static PngHostAllowance? ReadPngHostAllowance(string pngPath, AssetDimensions png) {
        var svgPath = Path.ChangeExtension(pngPath, ".svg");
        if (!File.Exists(svgPath)) return null;
        try {
            var root = XDocument.Load(svgPath).Root;
            if (root == null || root.Descendants().Any(e => (string?)e.Attribute("data-cfx-role") is "frame-card" or "background") ||
                !root.Descendants().Any(e => (string?)e.Attribute("data-cfx-role") is "axis-x" or "axis-y")) return null;
            var svg = ReadSvgDimensions(svgPath);
            if (svg.Width <= 0 || svg.Height <= 0) return null;
            var scale = (double)png.Width / svg.Width;
            if (Math.Abs(scale - (double)png.Height / svg.Height) > .001) return null;
            var regions = new List<PngHostRegion>();
            foreach (var element in root.Descendants()) {
                if (element.AncestorsAndSelf().Any(e => e.Attribute("transform") != null) ||
                    element.Ancestors().Any(e => e != root && e.Name.LocalName == "svg")) continue;
                if (element.Name.LocalName == "line" &&
                    (string?)element.Attribute("data-cfx-role") is "grid-x" or "grid-y" or "axis-x" or "axis-y") {
                    var x1 = Number(element, "x1"); var x2 = Number(element, "x2");
                    var y1 = Number(element, "y1"); var y2 = Number(element, "y2");
                    var width = Number(element, "stroke-width");
                    if (width <= 0 || width > 2 || Math.Abs(y1 - y2) > .01 && Math.Abs(x1 - x2) > .01 ||
                        Math.Min(x1, x2) < 0 || Math.Max(x1, x2) > svg.Width || Math.Min(y1, y2) < 0 || Math.Max(y1, y2) > svg.Height ||
                        !ChartColor.TryParse((string?)element.Attribute("stroke") ?? "", out var color)) continue;
                    regions.Add(new PngHostRegion((Math.Min(x1, x2) - width / 2) * scale, (Math.Min(y1, y2) - width / 2) * scale,
                        (Math.Abs(x2 - x1) + width) * scale, (Math.Abs(y2 - y1) + width) * scale, color, true));
                } else if (element.Name.LocalName == "text" &&
                    element.Ancestors().Any(e => (string?)e.Attribute("data-cfx-role") == "axis-y-label")) {
                    var font = new FontSpec { Family = (string?)element.Attribute("font-family") ?? "sans-serif",
                        Weight = TypographyFontResolver.ParseCssWeight((string?)element.Attribute("font-weight"), 400),
                        Italic = (string?)element.Attribute("font-style") == "italic" };
                    var size = Number(element, "font-size");
                    if (!double.IsFinite(size) || size <= 0) continue;
                    var metrics = TextLayoutEngine.Measure(element.Value, new TextStyle { Font = font, FontSize = size, LineHeight = 1 });
                    var ascent = TypographyFontResolver.ResolveFace(font).Font?.Ascent(size) ?? size * .82;
                    var x = Number(element, "x"); var y = Number(element, "y") - ascent;
                    var width = metrics.Width; var height = metrics.Height;
                    if (x < -.01 || y < -.01 || width <= 0 || height <= 0 || x + width > svg.Width + .01 || y + height > svg.Height + .01 ||
                        !ChartColor.TryParse((string?)element.Attribute("fill") ?? "", out var color)) continue;
                    regions.Add(new PngHostRegion(x * scale, y * scale, width * scale, height * scale, color));
                }
            }
            return new PngHostAllowance(regions);
        } catch (System.Xml.XmlException) {
            return null;
        }
        static double Number(XElement element, string name) =>
            double.TryParse((string?)element.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : double.NaN;
    }

    private sealed class PngHostAllowance(List<PngHostRegion> regions) {
        internal bool Includes(int x, int y, int rgba) {
            for (var i = 0; i < regions.Count; i++) {
                var first = regions[i];
                if (!first.Contains(x, y)) continue;
                if (first.Matches(rgba)) return true;
                if (!first.IsLine) continue;
                // Supersampling can combine a grid line and an axis on the same boundary.
                // Restrict mixed paints to the actual intersection of two declared thin lines.
                for (var j = i + 1; j < regions.Count; j++) {
                    var second = regions[j];
                    if (second.IsLine && second.Contains(x, y) && first.MatchesBlend(second, rgba)) return true;
                }
            }
            return false;
        }
    }

    private readonly struct PngHostRegion(double x, double y, double width, double height, ChartColor color, bool isLine = false) {
        internal bool IsLine => isLine;
        internal ChartColor Color => color;
        internal bool Contains(int px, int py) =>
            px + .5 >= x - 1.25 && px + .5 <= x + width + 1.25 && py + .5 >= y - 1.25 && py + .5 <= y + height + 1.25;
        internal bool Matches(int rgba) =>
            Math.Abs(((rgba >> 24) & 255) - color.R) <= 2 &&
            Math.Abs(((rgba >> 16) & 255) - color.G) <= 2 &&
            Math.Abs(((rgba >> 8) & 255) - color.B) <= 2;
        internal bool MatchesBlend(PngHostRegion other, int rgba) {
            var dr = other.Color.R - color.R; var dg = other.Color.G - color.G; var db = other.Color.B - color.B;
            var r = (rgba >> 24) & 255; var g = (rgba >> 16) & 255; var b = (rgba >> 8) & 255;
            var denominator = dr * dr + dg * dg + db * db;
            if (denominator == 0) return false;
            var fraction = ((r - color.R) * dr + (g - color.G) * dg + (b - color.B) * db) / (double)denominator;
            return fraction >= 0 && fraction <= 1 && Math.Abs(r - color.R - fraction * dr) <= 2 &&
                Math.Abs(g - color.G - fraction * dg) <= 2 && Math.Abs(b - color.B - fraction * db) <= 2;
        }
    }
}
