using ChartForgeX.Rendering;
using ChartForgeX.Primitives;
using System.Globalization;
using System.Xml.Linq;

public static partial class XZoomBenchmarkCases {
    private static void ValidateBank(ZoomInput input, PreparedChartXZoom bank) {
        Require(bank.Levels.SequenceEqual(input.Levels), "Prepared levels changed.");
        Require(bank.FullViewport.Minimum == 0 && bank.FullViewport.Maximum == input.Count - 1, "Authored full X domain changed.");
        Require(bank.PlotBounds.Width > 0 && bank.PlotBounds.Height > 0, "Bank plot is empty.");
        var full = bank.At(0);
        var y = full.Regions.Where(region => region.Role == "point").ToDictionary(region => region.Id, region => region.Bounds.Top + region.Bounds.Height / 2);
        for (var level = 0; level < input.Levels.Count; level++)
        foreach (var pan in new[] { 0d, input.Pan, 1d }) {
            var view = bank.At(level, pan);
            ValidateView(input, bank, view, level, pan);
            foreach (var point in view.Regions.Where(region => region.Role == "point")) Near(y[point.Id], point.Bounds.Top + point.Bounds.Height / 2, "Y domain moved during pan/zoom.");
            if (input.Fixture.EndsWith("references", StringComparison.Ordinal)) {
                Require(view.Regions.Count(region => region.Role == "annotation") == 4, "Reference source facts were lost.");
                Require(view.Regions.Count(region => region.Role == "plot-region") == 2, "Plot-region source facts were lost.");
                Require(view.Regions.Single(region => region.Id == "annotation-0").Bounds.Equals(full.Regions.Single(region => region.Id == "annotation-0").Bounds), "Fixed Y reference moved.");
                Require(view.Regions.Single(region => region.Id == "plot-region-1").Bounds.Equals(full.Regions.Single(region => region.Id == "plot-region-1").Bounds), "Fixed Y region moved.");
            }
        }
    }

    private static void ValidateView(ZoomInput input, PreparedChartXZoom bank, PreparedVisual view, int level, double pan) {
        var projection = view.XProjection ?? throw new InvalidOperationException("At lost its detached projection.");
        var span = (input.Count - 1) / input.Levels[level]; var minimum = pan * (input.Count - 1 - span);
        Near(minimum, projection.Minimum, "Pan minimum changed."); Near(minimum + span, projection.Maximum, "Pan maximum changed.");
        Require(projection.FullMinimum == bank.FullViewport.Minimum && projection.FullMaximum == bank.FullViewport.Maximum, "Full domain changed.");
        Require(projection.PlotBounds.Equals(bank.PlotBounds) && projection.Reversed == input.Reversed, "Projection layout or reversal changed.");
        Near(input.Reversed ? projection.Maximum : projection.Minimum, projection.Unproject(bank.PlotBounds.Left), "Left projection inverse changed.");
        Require(view.Regions.Single(region => region.Role == "cartesian-plot").Bounds.Equals(bank.PlotBounds), "Semantic plot bounds changed.");
        Require(view.Regions.Count(region => region.Role == "point") == input.Chart.Series.Sum(series => series.Points.Count), "Source point count changed.");
        var facts = view.Regions.Where(region => region.Role == "point").ToDictionary(region => region.Id);
        for (var series = 0; series < input.Chart.Series.Count; series++)
        for (var index = 0; index < input.Chart.Series[series].Points.Count; index++) {
            var source = input.Chart.Series[series].Points[index]; var bounds = facts[$"series-{series}-point-{index}"].Bounds;
            Near(projection.Project(source.X), bounds.Left + bounds.Width / 2, "Source anchor geometry changed.");
            Require(double.IsFinite(bounds.Top) && double.IsFinite(bounds.Left), "Source fact geometry is not finite.");
        }
    }

    private static void ValidateSvg(ZoomInput input, PreparedVisual view, string svg) {
        var root = XDocument.Parse(svg).Root ?? throw new InvalidOperationException("SVG root is absent.");
        Require((double?)root.Attribute("width") == input.Context.Layout.Size.Width && (double?)root.Attribute("height") == input.Context.Layout.Size.Height, "SVG dimensions changed.");
        Require(!root.Descendants().Any(node => node.Name.LocalName == "script"), "Static SVG gained script.");
        var facts = root.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Require(facts.Length == input.Chart.Series.Sum(series => series.Points.Count), "SVG source facts changed.");
        var ids = facts.Select(node => ((string?)node.Attribute("data-cfx-series"), (string?)node.Attribute("data-cfx-source-point"))).ToHashSet();
        for (var series = 0; series < input.Chart.Series.Count; series++)
        for (var point = 0; point < input.Chart.Series[series].Points.Count; point++)
            Require(ids.Contains((series.ToString(CultureInfo.InvariantCulture), point.ToString(CultureInfo.InvariantCulture))), "SVG lost a source identity.");
        var lines = root.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "line").ToArray();
        Require(lines.Length == input.Chart.Series.Count, "Native line geometry is absent.");
        for (var series = 0; series < lines.Length; series++) {
            var data = (string?)lines[series].Attribute("d") ?? "";
            Require(data.Count(character => character == 'M') == input.Chart.Series[series].Points.Count(point => point.BreakBefore) + 1, "Native path gap boundaries changed.");
        }
        var projection = view.XProjection!;
        foreach (var caption in root.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "x-zoom-caption")) {
            var association = (string?)caption.Attribute("data-cfx-label-for");
            if (association == null || !association.StartsWith("series-", StringComparison.Ordinal)) continue;
            var parts = association.Split('-');
            var series = int.Parse(parts[1], CultureInfo.InvariantCulture); var point = int.Parse(parts[3], CultureInfo.InvariantCulture);
            var source = input.Chart.Series[series].Points[point].X;
            if (source < projection.Minimum - 1e-9 || source > projection.Maximum + 1e-9)
                Require((string?)caption.Attribute("data-cfx-label-visibility") != "visible", "An off-window source caption painted.");
        }
    }

    private static void ValidateNativeExport(ZoomInput input, PreparedVisual view) {
        var options = new VisualRenderOptions(scale: 1, supersampling: 2);
        var png = view.ToPng(options);
        Require(png.SequenceEqual(view.ToPng(options)), "Native PNG is not deterministic within the lane.");
        Require(png.Length >= 24 && png[0] == 137 && png[1] == 80, "PNG signature is absent.");
        Require(BigEndian(png, 16) == view.Size.Width && BigEndian(png, 20) == view.Size.Height, "PNG dimensions changed.");
        var rgba = view.ToRgba(new VisualRenderOptions(scale: 1, supersampling: 1));
        var blue = 0;
        for (var index = 0; index < rgba.Pixels.Length; index += 4)
            if (rgba.Pixels[index + 2] > rgba.Pixels[index] + 50 && rgba.Pixels[index + 2] > rgba.Pixels[index + 1] + 20) blue++;
        Require(blue > 0, "Native raster lost the explicitly blue series ink.");
    }

    private static int BigEndian(byte[] bytes, int offset) => bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
    private static void Near(double expected, double actual, string reason) => Require(Math.Abs(expected - actual) <= 1e-8 * Math.Max(1, Math.Abs(expected)), reason);
}
