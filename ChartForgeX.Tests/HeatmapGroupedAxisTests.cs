using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HeatmapGroupedAxisTests {
    [Theory]
    [InlineData(false, false, 390)] [InlineData(false, true, 1000)]
    [InlineData(true, true, 390)] [InlineData(true, false, 1000)]
    public void RectangularNumericAndStateMatricesKeepBothAxisPathsRawFactsMissingSlotsAndNativePaint(bool categorical, bool dark, int width) {
        var chart = Model(categorical, dark, width); var rows = chart.Series.ToArray();
        var points = rows.Select(row => row.Points.ToArray()).ToArray(); var sourceCounts = rows.Select(row => row.SourcePointCount).ToArray();
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var xml = XDocument.Parse(svg);
        var cells = Cells(xml); Assert.Equal(10, cells.Length);
        Assert.Equal(new[] { "west", "unknown", "east", "return" }, rows.Select(row => row.Id.Value));
        Assert.Equal(points.Select(row => row.Length), rows.Select(row => row.Points.Count));
        for (var index = 0; index < rows.Length; index++) Assert.Equal(points[index], rows[index].Points);
        Assert.Equal(sourceCounts, rows.Select(row => row.SourcePointCount));
        Assert.Equal(4, ByRole(xml, "heatmap-column-label").Length); Assert.Equal(4, ByRole(xml, "heatmap-row-label").Length);
        Assert.DoesNotContain(cells, cell => (string?)cell.Attribute("data-cfx-series-key") == "unknown");
        var first = cells.Single(cell => (string?)cell.Attribute("data-cfx-series-key") == "west" && (string?)cell.Attribute("data-cfx-point") == "0");
        Assert.Equal("west-a", (string?)first.Attribute("data-cfx-observation-id"));
        Assert.Contains("North / Core / West", (string?)first.Attribute("aria-label")); Assert.Contains("Morning / Work / A", (string?)first.Attribute("aria-label"));
        Assert.Equal("North", (string?)first.Attribute("data-cfx-row-category-group-0")); Assert.Equal("Morning", (string?)first.Attribute("data-cfx-column-category-group-0"));
        Assert.Equal(categorical ? "1" : "10", (string?)first.Attribute("data-cfx-x"));
        Assert.Equal((categorical ? chart.Options.StateCategories[0].Color : ChartColor.Black).ToCss(), (string?)first.RenderedAttribute("fill"));
        if (!categorical) { Assert.Equal("0", (string?)first.Attribute("data-cfx-value")); Assert.Equal(2, cells.Count(cell => (string?)cell.Attribute("data-cfx-value") == "0")); }
        else { Assert.Equal("pass", (string?)first.Attribute("data-cfx-status")); Assert.Equal("Pass", (string?)first.Attribute("data-cfx-state-label")); }
        Assert.DoesNotContain(cells, cell => (string?)cell.Attribute("data-cfx-series-key") == "west" && (string?)cell.Attribute("data-cfx-column") == "1");
        Assert.Equal(4, ByRole(xml, "axis-x-category-group").Length); Assert.Equal(6, ByRole(xml, "axis-y-category-group").Length);
        var northernRuns = ByRole(xml, "axis-y-category-group").Where(group => (string?)group.Attribute("data-cfx-group-depth") == "0" && (string?)group.Attribute("data-cfx-group-0") == "North").ToArray();
        Assert.Equal(2, northernRuns.Length); Assert.Equal(new[] { "0", "3" }, northernRuns.Select(group => (string?)group.Attribute("data-cfx-group-first-leaf")));
        Assert.All(ByRole(xml, "axis-x-category-group").Concat(ByRole(xml, "axis-y-category-group")), group => {
            Assert.Equal("source-slot", (string?)group.Attribute("data-cfx-group-coordinate-space"));
            Assert.Null(group.Attribute("data-cfx-series")); Assert.Null(group.Attribute("data-cfx-point")); Assert.Null(group.Attribute("tabindex"));
        });
        Assert.Contains(prepared.Regions, region => region.Role == "heatmap-cell" && region.Label!.Contains("North / Core / West", StringComparison.Ordinal));
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role is "axis-x-category-group-label" or "axis-y-category-group-label"), text => {
            var left = text.Text.Lines.Min(text.LineLeft); var right = text.Text.Lines.Max(line => text.LineLeft(line) + line.Width);
            var top = text.Baseline - text.Text.Ascent;
            Assert.InRange(left, -.001, prepared.Size.Width + .001); Assert.InRange(right, -.001, prepared.Size.Width + .001);
            Assert.InRange(top, -.001, prepared.Size.Height + .001); Assert.InRange(top + text.Text.Metrics.Height, -.001, prepared.Size.Height + .001);
        });
        Assert.NotEmpty(ByRole(xml, "axis-x-category-group-label")); Assert.NotEmpty(ByRole(xml, "axis-y-category-group-label"));
        var image = RasterImageDecoder.Decode(png); Assert.Equal(width, image.Width); Assert.Equal(560, image.Height);
        Save("grouped-heatmap-" + (categorical ? "states" : "numeric") + "-" + width + (dark ? "-dark" : "-light"), svg, png);
        chart.Options.XAxis.Labels.Clear(); chart.Options.YAxis.Labels.Clear(); chart.Series[0].WithPointColor(0, ChartColor.FromRgba(255, 0, 0, 255));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false, true, true)] [InlineData(true, true, false)] [InlineData(true, false, true)] [InlineData(true, true, true)]
    public void ReversalMirrorsRectangularGeometryWithoutReorderingSourceFactsOrObservationIdentity(bool grouped, bool reverseX, bool reverseY) {
        var chart = Model(false, false, 1000, grouped); var preparedBefore = Prepare(chart);
        var original = Cells(XDocument.Parse(preparedBefore.ToSvg()));
        var originalBounds = preparedBefore.Scene.Nodes.OfType<VisualSceneRectangle>().Where(cell => cell.Role == "heatmap-cell-shape").Select(cell => cell.Bounds).ToArray();
        chart.Options.XAxis.Reversed = reverseX; chart.Options.YAxis.Reversed = reverseY;
        var preparedAfter = Prepare(chart); var mirrored = Cells(XDocument.Parse(preparedAfter.ToSvg()));
        var mirroredBounds = preparedAfter.Scene.Nodes.OfType<VisualSceneRectangle>().Where(cell => cell.Role == "heatmap-cell-shape").Select(cell => cell.Bounds).ToArray();
        Assert.Equal(original.Select(Facts), mirrored.Select(Facts));
        Assert.Equal(original.Length, originalBounds.Length); Assert.Equal(original.Length, mirroredBounds.Length);
        var left = originalBounds.Min(bounds => bounds.Left); var right = originalBounds.Max(bounds => bounds.Right);
        var top = originalBounds.Min(bounds => bounds.Top); var bottom = originalBounds.Max(bounds => bounds.Bottom);
        for (var index = 0; index < original.Length; index++) {
            var initial = originalBounds[index]; var reflected = mirroredBounds[index];
            // Check native geometry before the SVG writer independently rounds each coordinate.
            Assert.Equal(reverseX ? left + right - initial.X - initial.Width : initial.X, reflected.X, 7);
            Assert.Equal(reverseY ? top + bottom - initial.Y - initial.Height : initial.Y, reflected.Y, 7);
            Assert.Equal(initial.Width, reflected.Width, 7); Assert.Equal(initial.Height, reflected.Height, 7);
            AssertSvgBounds(original[index], initial); AssertSvgBounds(mirrored[index], reflected);
            Assert.Equal((string?)original[index].RenderedAttribute("fill"), (string?)mirrored[index].RenderedAttribute("fill"));
        }
        Assert.Equal(new[] { "west", "unknown", "east", "return" }, chart.Series.Select(row => row.Id.Value));
        Assert.Equal(original.Select(cell => (string?)cell.Attribute("aria-label")), mirrored.Select(cell => (string?)cell.Attribute("aria-label")));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EmptyRefillAndRowReorderingKeepAuthoredSlotsSeparateFromRetainedSeriesIdentity(bool categorical) {
        var chart = Model(categorical, false, 1000); var rows = chart.Series.ToArray(); var svg = chart.ToSvg(); var png = chart.ToPng();
        chart.ClearSeries(); Assert.Contains(chart.Options.Labels.NoData, chart.ToSvg()); Assert.NotEmpty(chart.ToPng());
        foreach (var row in rows) chart.AddSeries(row);
        Assert.Equal(svg, chart.ToSvg()); Assert.Equal(png, chart.ToPng());
        chart.MoveSeries(rows[2], 0);
        var moved = Cells(XDocument.Parse(chart.ToSvg())).First(cell => (string?)cell.Attribute("data-cfx-series-key") == "east");
        Assert.Equal("0", (string?)moved.Attribute("data-cfx-row")); Assert.Equal("North", (string?)moved.Attribute("data-cfx-row-category-group-0"));
        Assert.Equal("east-a", (string?)moved.Attribute("data-cfx-observation-id")); Assert.Equal("east", rows[2].Id.Value);
    }

    internal static Chart Model(bool categorical, bool dark, int width, bool grouped = true) {
        var chart = Chart.Create().WithSize(width, 560).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Grouped matrix axes").WithXAxis("Work windows").WithYAxis("Service slots").WithDataLabels(false);
        var colors = chart.Options.Theme.Resolve(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        if (categorical) {
            chart.WithStateCategories(new ChartStateCategory("pass", "Pass", colors.Status.Pass.Fill), new ChartStateCategory("warning", "Review", colors.Status.Medium.Fill));
            chart.AddSeries(StateRow("west", "West source", new[] { "pass", null, "warning", "pass" }));
            chart.AddSeries(StateRow("unknown", "Unknown source", new string?[] { null, null, null, null }));
            chart.AddSeries(StateRow("east", "East source", new[] { "warning", "pass", "pass", "warning" }));
            chart.AddSeries(StateRow("return", "Return source", new[] { null, "pass", "warning", "pass" }));
        } else {
            chart.AddHeatmapRow("West source", new[] { new ChartPoint(10, 0), new ChartPoint(70, 10), new ChartPoint(90, 20) }, id: new ChartSeriesId("west"));
            chart.AddHeatmapRow("Unknown source", Array.Empty<ChartPoint>(), id: new ChartSeriesId("unknown"));
            chart.AddHeatmapRow("East source", new[] { new ChartPoint(10, 20), new ChartPoint(30, 10), new ChartPoint(70, 0), new ChartPoint(90, 5) }, id: new ChartSeriesId("east"));
            chart.AddHeatmapRow("Return source", new[] { new ChartPoint(30, 2), new ChartPoint(70, 3), new ChartPoint(90, 4) }, id: new ChartSeriesId("return"));
            foreach (var row in chart.Series) row.WithObservationIds(row.Points.Select(point => new ChartObservationId(row.Id.Value + "-" + (point.X == 10 ? "a" : point.X == 30 ? "b" : point.X == 70 ? "c" : "d"))));
            chart.WithHeatmapColorScale(ChartColorScale.Sequential(colors.Surface, colors.Accent).WithValueRange(0, 20));
        }
        chart.Series[0].WithPointColor(0, ChartColor.Black);
        var coordinates = categorical ? new[] { 1d, 2, 3, 4 } : new[] { 10d, 30, 70, 90 };
        chart.WithXLabels(coordinates.Select((value, index) => new ChartAxisLabel(value, index % 2 == 0 ? "A" : "B",
            grouped ? new[] { index < 2 ? "Morning" : "Evening", "Work" } : Array.Empty<string>())));
        chart.ConfigureYAxis(axis => axis.Labels.AddRange(new[] {
            new ChartAxisLabel(1, "West", grouped ? new[] { "North", "Core" } : Array.Empty<string>()),
            new ChartAxisLabel(2, "Unknown", grouped ? new[] { "North", "Core" } : Array.Empty<string>()),
            new ChartAxisLabel(3, "East", grouped ? new[] { "South", "Core" } : Array.Empty<string>()),
            new ChartAxisLabel(4, "Return", grouped ? new[] { "North", "Core" } : Array.Empty<string>())
        }));
        return chart;
    }
    private static ChartSeries StateRow(string id, string name, IEnumerable<string?> values) => ChartSeries.CreateHeatmapCategoryRow(new ChartSeriesId(id), name,
        values.Select(value => value == null ? (ChartHeatmapCell?)null : new ChartHeatmapCell(value)))
        .WithObservationIds(new[] { "a", "b", "c", "d" }.Select(value => new ChartObservationId(id + "-" + value)));
    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    internal static XElement[] ByRole(XDocument xml, string role) => xml.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    internal static XElement[] Cells(XDocument xml) => ByRole(xml, "heatmap-cell");
    private static object Facts(XElement cell) => new { Series = (string?)cell.Attribute("data-cfx-series-key"), Observation = (string?)cell.Attribute("data-cfx-observation-id"),
        X = (string?)cell.Attribute("data-cfx-x"), Y = (string?)cell.Attribute("data-cfx-y"), Value = (string?)cell.Attribute("data-cfx-value"),
        Row = (string?)cell.Attribute("data-cfx-row"), Column = (string?)cell.Attribute("data-cfx-column"), Source = (string?)cell.Attribute("data-cfx-source-point") };
    private static void AssertSvgBounds(XElement element, ChartRect bounds) {
        Assert.Equal(bounds.X.ToString("0.###", CultureInfo.InvariantCulture), element.RenderedAttribute("x")!.Value);
        Assert.Equal(bounds.Y.ToString("0.###", CultureInfo.InvariantCulture), element.RenderedAttribute("y")!.Value);
        Assert.Equal(bounds.Width.ToString("0.###", CultureInfo.InvariantCulture), element.RenderedAttribute("width")!.Value);
        Assert.Equal(bounds.Height.ToString("0.###", CultureInfo.InvariantCulture), element.RenderedAttribute("height")!.Value);
    }
    internal static void Save(string name, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(directory, name + ".png"), png);
    }
}
