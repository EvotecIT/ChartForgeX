using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Authored axis text fits the actual shared scene without changing numeric source identity.</summary>
public sealed class NumericRadialAxisTextTests {
    [Theory]
    [InlineData(false, 360)]
    [InlineData(false, 800)]
    [InlineData(true, 360)]
    [InlineData(true, 800)]
    public void IndependentFixedAnglesAndTitlesFitBesideTheSameSourceMarks(bool bars, int width) {
        var chart = Configured(bars, width);
        var prepared = Prepare(chart); var scene = prepared.Scene;
        var texts = Footprints(scene);
        var titles = texts.Where(item => item.Text.Role is "radial-category-axis-title" or "radial-value-axis-title").ToArray();
        Assert.Equal(3, titles.Length);
        Assert.Equal(new[] { "Requests", "Resolved (%)", "Region" }, titles.Select(item => item.Text.Text.Lines.Single().Text));
        foreach (var title in titles) {
            Assert.Equal(0, title.Angle);
            var strip = scene.Regions.Single(region => region.Id == title.Text.Id).Bounds;
            Assert.True(LabelPlacementService.Contains(strip, title.Bounds));
            Assert.All(texts.Where(item => item.Text != title.Text), neighbor => Assert.False(Intersects(title.Bounds, neighbor.Bounds), title.Text.Id + " overlaps " + neighbor.Text.Id));
        }
        var ticks = texts.Where(item => item.Text.Role is "radial-category-label" or "radial-value-label").ToArray();
        Assert.Contains(ticks, item => item.Text.Role == "radial-category-label" && Math.Abs(item.Angle + 30) < .001);
        Assert.Contains(ticks, item => item.Text.Id!.Contains("Primary", StringComparison.Ordinal) && Math.Abs(item.Angle - 30) < .001);
        Assert.Contains(ticks, item => item.Text.Id!.Contains("Secondary", StringComparison.Ordinal) && Math.Abs(item.Angle - 90) < .001);
        var marks = NumericRadialSeriesTests.Marks(scene);
        foreach (var tick in ticks) {
            Assert.True(LabelPlacementService.Contains(new ChartRect(0, 0, width, width == 360 ? 360 : 440), tick.Bounds));
            Assert.All(marks, mark => Assert.False(new LabelMarkShape(VisualSceneGeometry.Flatten(mark, 8), true, 0).Intersects(tick.Bounds), tick.Text.Id + " crosses a painted sector"));
        }
        for (var first = 0; first < ticks.Length; first++) for (var second = first + 1; second < ticks.Length; second++)
            Assert.False(Intersects(ticks[first].Bounds, ticks[second].Bounds));
        Assert.Equal("1200", Point(scene, 0, 0)["data-cfx-y"]);
        Assert.Equal("85", Point(scene, 1, 0)["data-cfx-y"]);
        var image = prepared.ToRgba();
        foreach (var title in titles) Assert.True(Ink(image, title.Bounds, title.Text.Color) > 3, title.Text.Id + " has no native title ink");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongMultilineStyledTitlesStayInsideTheirStripsAndRetainCompleteDescriptions(bool bars) {
        var chart = LongTitles(bars);
        var scene = Prepare(chart).Scene;
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "numeric-radial.axis-title-overflow");
        foreach (var title in Footprints(scene).Where(item => item.Text.Role is "radial-category-axis-title" or "radial-value-axis-title"))
            Assert.True(LabelPlacementService.Contains(scene.Regions.Single(region => region.Id == title.Text.Id).Bounds, title.Bounds));
        Assert.Contains(scene.Regions, region => region.Role == "radial-category-axis-title" && region.Label == chart.XAxisTitle.ToUpperInvariant());
        Assert.Contains(scene.Regions, region => region.Role == "radial-value-axis-title" && region.Label == chart.YAxisTitle.ToUpperInvariant());
        Assert.Contains(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "radial-value-axis-title-source" && group.Metadata["data-cfx-full-label"] == chart.SecondaryYAxisTitle);
        Assert.Equal("1200", Point(scene, 0, 0)["data-cfx-y"]);
        if (bars) {
            Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "numeric-radial.insufficient-space");
            Assert.Empty(NumericRadialSeriesTests.Marks(scene));
            Assert.DoesNotContain(scene.Nodes, node => node.Role is "radial-value-grid" or "radial-value-label" or "radial-category-label");
            var points = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
            Assert.Equal(6, points.Length);
            Assert.All(points, point => Assert.Equal("layout-collapse", point.Metadata["data-cfx-geometry-status"]));
            Assert.All(scene.Regions.Where(region => region.Role == "point"), region => Assert.Equal(0, region.Bounds.Width + region.Bounds.Height));
        }
    }

    [Theory]
    [InlineData(false, "category")]
    [InlineData(true, "primary")]
    [InlineData(false, "secondary")]
    [InlineData(true, "all")]
    public void HiddenTitlesAndAnglesReserveNoSpaceWhileShowLineOnlyControlsRules(bool bars, string hidden) {
        var chart = Configured(bars, 800);
        if (hidden == "all") chart.WithAxes(false);
        else Axis(chart, hidden).Visible = false;
        var initial = Prepare(chart).ToSvg();
        if (hidden is "category" or "all") { chart.WithXAxis(new string('W', 100)); chart.Options.XAxis.LabelAngle = 90; }
        if (hidden is "primary" or "all") { chart.WithYAxis(new string('W', 100)); chart.Options.YAxis.LabelAngle = -90; }
        if (hidden is "secondary" or "all") { chart.WithSecondaryYAxis(new string('W', 100)); chart.Options.SecondaryYAxis.LabelAngle = 180; }
        Assert.Equal(initial, Prepare(chart).ToSvg());
        chart = Configured(bars, 800);
        var visible = Prepare(chart).Scene;
        chart.Options.XAxis.ShowLine = chart.Options.YAxis.ShowLine = chart.Options.SecondaryYAxis.ShowLine = false;
        var withoutRules = Prepare(chart).Scene;
        Assert.Equal(3, withoutRules.Nodes.OfType<VisualSceneText>().Count(text => text.Role is "radial-category-axis-title" or "radial-value-axis-title"));
        Assert.DoesNotContain(withoutRules.Nodes, node => node.Role == "radial-value-rule");
        Assert.Equal(NumericRadialSeriesTests.Marks(visible).Select(mark => mark.Outer), NumericRadialSeriesTests.Marks(withoutRules).Select(mark => mark.Outer));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnlyParticipatingValueAxesReserveTitleStripsIncludingCompleteEmptyDomains(bool bars) {
        var chart = Configured(bars, 800);
        chart.Series.RemoveAt(1);
        var primaryPrepared = Prepare(chart); var primary = primaryPrepared.Scene;
        Assert.DoesNotContain(primary.Nodes.OfType<VisualSceneText>(), text => text.Id == "radial-value-axis-title-secondary");
        chart.WithSecondaryYAxis("Unused caption that must not shrink the plot"); chart.Options.SecondaryYAxis.LabelAngle = -90;
        Assert.Equal(primaryPrepared.ToSvg(), Prepare(chart).ToSvg());
        NumericRadialSeriesTests.Add(chart, bars, "Missing rate", Array.Empty<ChartPoint>()).Series[1].UseSecondaryYAxis();
        chart.Options.SecondaryYAxis.WithAutomaticBounds();
        Assert.DoesNotContain(Prepare(chart).Scene.Nodes.OfType<VisualSceneText>(), text => text.Id == "radial-value-axis-title-secondary");
        chart.WithSecondaryYAxis("Empty rate (%)").WithSecondaryYAxisBounds(0, 100);
        Assert.Contains(Prepare(chart).Scene.Nodes.OfType<VisualSceneText>(), text => text.Id == "radial-value-axis-title-secondary");
        chart.Series[0].Points.Clear();
        var empty = Prepare(chart).Scene;
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code == "numeric-radial.no-data");
        Assert.DoesNotContain(empty.Nodes.OfType<VisualSceneText>(), text => text.Role is "radial-value-axis-title" or "radial-category-axis-title");
    }

    [Fact]
    public void PreparedAxisTextIsDetachedAndFormattersRunOncePerTick() {
        var calls = new Dictionary<double, int>();
        var chart = Configured(true, 800).ConfigureYAxis(axis => axis.WithLabelFormatter(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return value.ToString("0", CultureInfo.InvariantCulture) + " requests";
        }));
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        chart.WithXAxis("Changed region").WithYAxis("Changed requests").WithSecondaryYAxis("Changed rate");
        chart.Options.XAxis.LabelAngle = 90; chart.Options.YAxis.LabelAngle = -90; chart.Options.AxisTitleStyle.WithFontSize(30);
        chart.Options.YAxis.WithLabelFormatter(_ => "Changed tick"); chart.Series[0].Points[0] = new ChartPoint(1, 1);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
    }

    internal static Chart Configured(bool bars, int width, bool dark = false) {
        var chart = Chart.Create().WithSize(width, width == 360 ? 360 : 440).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithFontFamily("Carlito").WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXLabels("North", "South", "East").WithRadialGeometry(new(-75, 195, .2, .25, .1))
            .WithXAxis("Region").WithYAxis("Requests").WithSecondaryYAxis("Resolved (%)")
            .WithYAxisBounds(0, 1500).WithSecondaryYAxisBounds(0, 100)
            .ConfigureXAxis(axis => axis.LabelAngle = -30).ConfigureYAxis(axis => { axis.LabelAngle = 30; axis.TickCount = 3; })
            .ConfigureSecondaryYAxis(axis => { axis.LabelAngle = 90; axis.TickCount = 3; });
        NumericRadialSeriesTests.Add(chart, bars, "Requests", new(1, 1200), new(2, 900), new(3, 700));
        NumericRadialSeriesTests.Add(chart, bars, "Resolved (%)", new(1, 85), new(2, 70), new(3, 60)).Series[1].UseSecondaryYAxis();
        chart.Options.PngFontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        return chart;
    }

    internal static Chart LongTitles(bool bars) {
        var chart = Configured(bars, 360).WithXAxis("Region names across the entire service area\nQuarterly reporting categories")
            .WithYAxis("Requests received across all channels\nSource count, including follow-up work")
            .WithSecondaryYAxis("Resolved requests as a percentage of received source counts\nIndependent percentage scale");
        chart.Options.AxisTitleStyle.WithFontSize(28).WithWeight("650").TextCase = TextCaseTransform.Uppercase;
        return chart;
    }

    internal static List<(VisualSceneText Text, ChartRect Bounds, double Angle)> Footprints(VisualScene scene) {
        var transform = VisualSceneTransform.Identity; var stack = new Stack<VisualSceneTransform>();
        var result = new List<(VisualSceneText, ChartRect, double)>();
        foreach (var node in scene.Nodes) {
            if (node is VisualSceneGroup group) {
                stack.Push(transform);
                if (group.Translation.HasValue) transform = transform.Translate(group.Translation.Value);
                if (group.Rotation.HasValue) transform = transform.Rotate(group.Rotation.Value);
            } else if (node is VisualSceneEndGroup) transform = stack.Pop();
            else if (node is VisualSceneText text) {
                var bounds = CartesianReversalLabelTests.Bounds(text);
                var corners = new[] { new ChartPoint(bounds.Left, bounds.Top), new(bounds.Right, bounds.Top), new(bounds.Right, bounds.Bottom), new(bounds.Left, bounds.Bottom) }.Select(transform.Apply).ToArray();
                result.Add((text, new ChartRect(corners.Min(point => point.X), corners.Min(point => point.Y), corners.Max(point => point.X) - corners.Min(point => point.X), corners.Max(point => point.Y) - corners.Min(point => point.Y)), Math.Atan2(transform.B, transform.A) * 180 / Math.PI));
            }
        }
        return result;
    }
    private static bool Intersects(ChartRect first, ChartRect second) => first.Left < second.Right - .001 && second.Left < first.Right - .001 && first.Top < second.Bottom - .001 && second.Top < first.Bottom - .001;
    private static ChartAxis Axis(Chart chart, string name) => name == "category" ? chart.Options.XAxis : name == "primary" ? chart.Options.YAxis : chart.Options.SecondaryYAxis;
    private static IReadOnlyDictionary<string, string> Point(VisualScene scene, int series, int point) => scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == $"series-{series}-point-{point}").Metadata;
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static int Ink(RgbaImage image, ChartRect bounds, ChartColor color) {
        var count = 0;
        for (var y = Math.Max(0, (int)Math.Floor(bounds.Top)); y < Math.Min(image.Height, (int)Math.Ceiling(bounds.Bottom)); y++)
            for (var x = Math.Max(0, (int)Math.Floor(bounds.Left)); x < Math.Min(image.Width, (int)Math.Ceiling(bounds.Right)); x++) {
                var offset = (y * image.Width + x) * 4;
                if (Math.Abs(image.Pixels[offset] - color.R) < 40 && Math.Abs(image.Pixels[offset + 1] - color.G) < 40 && Math.Abs(image.Pixels[offset + 2] - color.B) < 40) count++;
            }
        return count;
    }
}
