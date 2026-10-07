using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

public static partial class GalleryWriter {
    // A host-framed chart deliberately has no edge inset. Allow only fitted y labels and thin horizontal
    // guides declared by the chart; unrelated marks and text must still trigger the edge-pressure check.
    private static PngHostAllowance? ReadPngHostAllowance(string pngPath, AssetDimensions png) {
        var svgPath = Path.ChangeExtension(pngPath, ".svg");
        if (!File.Exists(svgPath)) return null;
        try {
            var root = XDocument.Load(svgPath).Root;
            if ((string?)root?.Attribute("data-cfx-look") != "graphite" ||
                (string?)root.Attribute("data-cfx-host-frame") != "true" ||
                root.Descendants().Any(e => (string?)e.Attribute("data-cfx-role") == "card-surface")) return null;
            var svg = ReadSvgDimensions(svgPath);
            if (svg.Width <= 0 || svg.Height <= 0) return null;
            var scale = (double)png.Width / svg.Width;
            if (Math.Abs(scale - (double)png.Height / svg.Height) > .001) return null;
            var light = ChartTheme.GraphiteLight();
            var dark = ChartTheme.GraphiteDark();
            var regions = new List<PngHostRegion>();
            foreach (var element in root.Descendants()) {
                if (element.AncestorsAndSelf().Any(e => e.Attribute("transform") != null) ||
                    element.Ancestors().Any(e => e != root && e.Name.LocalName == "svg")) continue;
                if (element.Name.LocalName == "line" &&
                    ((string?)element.Attribute("class") ?? "").Split(' ').Contains("cfx-guide-stroke")) {
                    var x1 = Number(element, "x1"); var x2 = Number(element, "x2");
                    var y1 = Number(element, "y1"); var y2 = Number(element, "y2");
                    var width = Number(element, "stroke-width");
                    if (width <= 0 || width > 2 || Math.Abs(y1 - y2) > .01 ||
                        x1 < 0 || x2 > svg.Width || x2 <= x1 || y1 < 0 || y1 > svg.Height ||
                        !ChartColor.TryParse((string?)element.Attribute("stroke") ?? "", out var color) ||
                        !(color.Equals(light.Grid) || color.Equals(dark.Grid) || color.Equals(light.Axis) || color.Equals(dark.Axis))) continue;
                    regions.Add(new PngHostRegion(x1 * scale, (y1 - width / 2) * scale, (x2 - x1) * scale, width * scale, color));
                } else if (element.Name.LocalName == "text" &&
                    (string?)element.Attribute("data-cfx-role") == "y-axis-label" &&
                    (string?)element.Attribute("data-cfx-label-status") == "placed") {
                    var x = Number(element, "data-cfx-label-x"); var y = Number(element, "data-cfx-label-y");
                    var width = Number(element, "data-cfx-label-width"); var height = Number(element, "data-cfx-label-height");
                    if (x < -.01 || y < -.01 || width <= 0 || height <= 0 || x + width > svg.Width + .01 || y + height > svg.Height + .01 ||
                        !ChartColor.TryParse((string?)element.Attribute("fill") ?? "", out var color) ||
                        !(color.Equals(light.MutedText) || color.Equals(dark.MutedText))) continue;
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
        internal bool Includes(int x, int y, int rgba) => regions.Any(region => region.Includes(x, y, rgba));
    }

    private readonly struct PngHostRegion(double x, double y, double width, double height, ChartColor color) {
        internal bool Includes(int px, int py, int rgba) =>
            px + .5 >= x - 1.25 && px + .5 <= x + width + 1.25 && py + .5 >= y - 1.25 && py + .5 <= y + height + 1.25 &&
            Math.Abs(((rgba >> 24) & 255) - color.R) <= 2 &&
            Math.Abs(((rgba >> 16) & 255) - color.G) <= 2 &&
            Math.Abs(((rgba >> 8) & 255) - color.B) <= 2;
    }
}
