using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RangeBarAutoBoundsTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("thumbnail", VisualThemeMode.Light)]
    [InlineData("reversed-value", VisualThemeMode.Light)]
    [InlineData("reversed-category", VisualThemeMode.Dark)]
    [InlineData("reversed-both-thumbnail", VisualThemeMode.Dark)]
    [InlineData("logarithmic", VisualThemeMode.Light)]
    [InlineData("reversed-logarithmic", VisualThemeMode.Dark)]
    [InlineData("flat", VisualThemeMode.Dark)]
    public void AutomaticValueBoundsContainActualIntervalPaintAfterEveryAncestorClip(string scenario, VisualThemeMode mode) {
        var thumbnail = scenario.Contains("thumbnail", StringComparison.Ordinal);
        var compact = scenario is "compact" or "reversed-category" or "reversed-logarithmic" or "flat";
        var width = thumbnail ? 400 : compact ? 360 : 800; var height = thumbnail ? 280 : compact ? 360 : 440;
        var chart = V2GalleryModels.Create(ChartSeriesKind.RangeBar, compact ? "compact-horizontal" : "horizontal");
        chart.Options.XAxis.Reversed = scenario is "reversed-value" or "reversed-both-thumbnail" or "reversed-logarithmic";
        chart.Options.YAxis.Reversed = scenario is "reversed-category" or "reversed-both-thumbnail" or "reversed-logarithmic";
        if (scenario is "logarithmic" or "reversed-logarithmic") chart.Options.XAxis.WithScale(ChartScaleKind.Logarithmic);
        if (scenario == "flat") chart.WithBarStyle(ChartBarStyle.Flat);
        // The raw-data domain remains exact; pixel gutters belong to preparation, not authored bounds.
        var raw = ChartRange.FromChart(chart);
        Assert.Equal(10, raw.MinX); Assert.Equal(55, raw.MaxX);
        Assert.Null(chart.Options.XAxis.Minimum); Assert.Null(chart.Options.XAxis.Maximum);
        var prepared = chart.Prepare(Context(width, height, mode, thumbnail));
        var groups = PointGroups(prepared);
        Assert.Equal(8, groups.Length);
        var paint = groups.SelectMany(Parts).Select(part => {
            var bounds = PaintBounds(part); var clip = EffectiveClip(part); var visible = Intersect(bounds, clip);
            AssertContained(bounds, visible);
            return new { role = (string?)part.Attribute("data-cfx-role"), bounds, clip, visible };
        }).ToArray();
        Assert.Equal("32", (string?)groups[5].Attribute("data-cfx-start"));
        Assert.Equal("12", (string?)groups[5].Attribute("data-cfx-end"));
        Assert.Equal("1", (string?)groups[5].Attribute("data-cfx-source-point"));
        Assert.Equal(new[] { "Standard", "Express", "Premium", "Bulk" }, prepared.Scene.Nodes.OfType<VisualSceneText>()
            .Where(text => text.Role == "axis-y-label").Select(text => string.Concat(text.Text.Lines.Select(line => line.Text))));
        Capture(prepared, scenario, paint);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitValueBoundsKeepEndpointsAtThePlotEdgesAndIntentionallyClipTheirCaps(bool reversed) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.RangeBar, "horizontal");
        chart.Options.XAxis.WithBounds(10, 55).WithReversal(reversed);
        var groups = PointGroups(chart.Prepare(Context(800, 440, VisualThemeMode.Light)));
        Assert.Equal(10, chart.Options.XAxis.Minimum); Assert.Equal(55, chart.Options.XAxis.Maximum);
        AssertPinned(MainCaps(groups[1])[0], maximum: false, reversed);
        AssertPinned(MainCaps(groups[7])[1], maximum: true, reversed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneExplicitValueEndClipsOnlyThatEndWhileTheAutomaticEndContainsItsLighting(bool explicitMaximum) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.RangeBar, "horizontal");
        if (explicitMaximum) chart.Options.XAxis.Maximum = 55;
        else chart.Options.XAxis.Minimum = 10;
        var groups = PointGroups(chart.Prepare(Context(800, 440, VisualThemeMode.Light)));
        AssertPinned(explicitMaximum ? MainCaps(groups[7])[1] : MainCaps(groups[1])[0], explicitMaximum, false);
        var automatic = Parts(explicitMaximum ? groups[1] : groups[7]).Where(part => part.Name.LocalName == "line")
            .Skip(explicitMaximum ? 0 : 4).Take(4).ToArray();
        Assert.Equal(4, automatic.Length);
        Assert.All(automatic, part => AssertContained(PaintBounds(part), Intersect(PaintBounds(part), EffectiveClip(part))));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, .000001)]
    public void ZeroOrTinyFlatIntervalsKeepTheirMinimumBodyInsideAutomaticBounds(bool reversed, double delta) {
        var chart = Chart.Create().ConfigureRangeBar(options => options.Orientation = ChartOrientation.Horizontal)
            .WithBarStyle(ChartBarStyle.Flat).WithXLabels("Minimum", "Maximum")
            .AddRangeBar("Range", new[] { new ChartInterval(1, 10, 10 + delta), new ChartInterval(2, 55, 55 + delta) });
        chart.Series[0].StrokeWidth = .1; chart.Options.XAxis.Reversed = reversed;
        var prepared = chart.Prepare(Context(360, 360, VisualThemeMode.Dark)); var groups = PointGroups(prepared);
        Assert.Equal(2, groups.Length);
        foreach (var group in groups) {
            var body = group.Descendants().Single(part => (string?)part.Attribute("data-cfx-role") == "range-bar");
            Assert.Equal(2, Number(body, "width"));
            AssertContained(PaintBounds(body), Intersect(PaintBounds(body), EffectiveClip(body)));
        }
        Assert.Equal(55 + delta, Number(groups[1], "data-cfx-end"));
        Capture(prepared, "minimum-body-" + reversed + "-" + delta.ToString("R", CultureInfo.InvariantCulture), new { delta, reversed });
    }

    private static VisualRenderContext Context(int width, int height, VisualThemeMode mode, bool thumbnail = false) {
        const string family = "CFX Range Bounds Carlito";
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(family, Path.Combine(directory, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(family, Path.Combine(directory, "Carlito-Bold.ttf"), 700);
        var theme = thumbnail ? VisualTheme.Graphite().WithTypography(new VisualTypography(axisSize: 13.5, legendSize: 13.5, dataLabelSize: 13.5)) : VisualTheme.Graphite();
        return new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), thumbnail ? 16 : 24), theme, mode,
            thumbnail ? new VisualFrame("", "", showLegend: false) : new VisualFrame(V2GalleryModels.Title(ChartSeriesKind.RangeBar), "Grouped horizontal intervals preserve both authored endpoints", showLegend: true),
            FontSpec.FromFamily(family));
    }

    private static XElement[] PointGroups(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();

    private static IEnumerable<XElement> Parts(XElement group) => group.Descendants().Where(node => node.Name.LocalName is "rect" or "line");
    private static XElement[] MainCaps(XElement group) => group.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "range-bar-cap").ToArray();

    private static void AssertPinned(XElement cap, bool maximum, bool reversed) {
        var clip = EffectiveClip(cap); var paint = PaintBounds(cap); var visible = Intersect(paint, clip);
        Assert.InRange(Math.Abs(Number(cap, "x1") - (maximum != reversed ? clip.Right : clip.Left)), 0, .002);
        Assert.InRange(visible.Width / paint.Width, .499, .501);
    }

    private static ChartRect EffectiveClip(XElement element) {
        ChartRect? result = null;
        foreach (var ancestor in element.Ancestors()) {
            var reference = (string?)ancestor.Attribute("clip-path");
            if (reference == null) continue;
            var id = reference.Substring(5, reference.Length - 6);
            var clip = element.Document!.Descendants().Single(node => node.Name.LocalName == "clipPath" && (string?)node.Attribute("id") == id);
            var rectangle = Assert.Single(clip.Elements()); Assert.Equal("rect", rectangle.Name.LocalName);
            var bounds = PaintBounds(rectangle); result = result.HasValue ? Intersect(result.Value, bounds) : bounds;
        }
        Assert.True(result.HasValue, "An interval must be checked against its actual plot clip.");
        return result!.Value;
    }

    private static ChartRect PaintBounds(XElement element) {
        if (element.Name.LocalName == "rect") return new ChartRect(Number(element, "x"), Number(element, "y"), Number(element, "width"), Number(element, "height"));
        var radius = Number(element, "stroke-width") / 2;
        var left = Math.Min(Number(element, "x1"), Number(element, "x2")) - radius; var top = Math.Min(Number(element, "y1"), Number(element, "y2")) - radius;
        return new ChartRect(left, top, Math.Abs(Number(element, "x2") - Number(element, "x1")) + radius * 2, Math.Abs(Number(element, "y2") - Number(element, "y1")) + radius * 2);
    }

    private static ChartRect Intersect(ChartRect a, ChartRect b) {
        var left = Math.Max(a.Left, b.Left); var top = Math.Max(a.Top, b.Top);
        return new ChartRect(left, top, Math.Max(0, Math.Min(a.Right, b.Right) - left), Math.Max(0, Math.Min(a.Bottom, b.Bottom) - top));
    }

    private static void AssertContained(ChartRect paint, ChartRect visible) {
        Assert.True(visible.Width > 0 && visible.Height > 0, "The plotted interval paint must survive clipping.");
        Assert.InRange(Math.Abs(paint.Width - visible.Width), 0, .003); Assert.InRange(Math.Abs(paint.Height - visible.Height), 0, .003);
    }

    private static void Capture(PreparedVisual prepared, string name, object proof) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, "auto-bounds-" + name);
        File.WriteAllText(path + ".svg", prepared.ToSvg()); File.WriteAllBytes(path + ".png", prepared.ToPng());
        File.WriteAllText(path + ".json", JsonSerializer.Serialize(proof, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
