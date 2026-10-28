using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Short authored point captions stay complete in feasible mixed-chart lanes.</summary>
public sealed class CartesianAuthoredAutoLabelTests {
    private static readonly string[] Captions = { "Opened", "Reviewed, ready", "Released" };

    [Theory]
    [InlineData(false, 390, false)] [InlineData(false, 390, true)]
    [InlineData(false, 1100, false)] [InlineData(false, 1100, true)]
    [InlineData(true, 390, false)] [InlineData(true, 390, true)]
    [InlineData(true, 1100, false)] [InlineData(true, 1100, true)]
    public void FullMixedCaptionsAvoidMappedBarsContoursAndEachOther(bool horizontal, int width, bool dark) {
        var chart = Create(horizontal, width, dark);
        var source = chart.Series.Select(series => series.Points.ToArray()).ToArray();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        AssertComplete(prepared, 1);
        for (var index = 0; index < source.Length; index++) Assert.Equal(source[index], chart.Series[index].Points);
        Capture(prepared, "mixed-" + (horizontal ? "horizontal-" : "vertical-") + width + (dark ? "-dark" : "-light"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CompactLineOnlyEndpointKeepsOpenedVisibleInsteadOfAcceptingAnEllipsis(bool dark) {
        var source = MermaidXYChartSourceDepthTests.Source(true).Replace("bar \"Intake\" [18, 70, 44, 85]\n", "", StringComparison.Ordinal);
        var chart = new MermaidParser().ParseXYChart(source).Document!.ToChart(MermaidXYChartSourceDepthTests.Options(390, dark).XYChart);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        AssertComplete(prepared, 0);
        Capture(prepared, "line-only-horizontal-390" + (dark ? "-dark" : "-light"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReversedLogarithmicSecondaryValuesUseTheSameMappedObstacleLanes(bool horizontal) {
        var chart = Create(horizontal, 1100, false);
        foreach (var series in chart.Series) series.UseSecondaryValueAxis();
        var valueAxis = ChartCartesianOrientation.ValueAxis(chart, ChartAxisSide.Secondary);
        valueAxis.WithBounds(1, 100).WithScale(ChartScaleKind.Logarithmic).WithReversal();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        AssertComplete(prepared, 1);
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(8, points.Length);
        Assert.All(points, point => Assert.Equal("secondary", point.Metadata["data-cfx-axis"]));
        Capture(prepared, "mixed-secondary-log-reversed-" + (horizontal ? "horizontal" : "vertical"));
    }

    [Fact]
    public void AlreadyCompleteCandidatesStayExactAndOrdinaryRequestsKeepTheirFallbackPolicy() {
        var style = new TextStyle { FontSize = 12 };
        var candidates = new[] { new LabelCandidate(12, -8, 0, 1) };
        var full = new LabelPlacementRequest("Opened", new ChartPoint(100, 100), style, candidates) {
            AutoPointMarkBounds = new ChartRect(97, 97, 6, 6), AutoPointSpacing = 8
        };
        var ordinary = new LabelPlacementRequest(full.Text, full.Anchor, style, candidates);
        var placement = new LabelPlacementService();
        var bounds = new ChartRect(0, 0, 400, 200);
        var expected = placement.Place(new[] { ordinary }, bounds)[0];
        var actual = placement.Place(new[] { full }, bounds)[0];
        Assert.Equal(0, actual.CandidateIndex); Assert.Equal(expected.Text, actual.Text); Assert.Equal(expected.Bounds, actual.Bounds);
        // An ordinary request keeps the original shortening rule. No new Auto candidates
        // are enabled for numeric captions or explicit-placement chart requests.
        var blocked = new[] { new LabelObstacle("bar", new ChartRect(105, 70, 80, 30)) };
        Assert.True(placement.Place(new[] { ordinary }, bounds, blocked)[0].IsDropped);
        Assert.False(placement.Place(new[] { full }, bounds, blocked)[0].IsDropped);
    }

    private static Chart Create(bool horizontal, int width, bool dark) {
        var chart = new MermaidParser().ParseXYChart(MermaidXYChartSourceDepthTests.Source(horizontal)).Document!
            .ToChart(MermaidXYChartSourceDepthTests.Options(width, dark).XYChart);
        return chart.WithLineMarkers(ChartLineMarkerMode.All);
    }

    private static void AssertComplete(PreparedVisual prepared, int lineIndex) {
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label").ToArray();
        Assert.Equal(Captions, labels.Select(text => Normalize(string.Join(" ", text.Text.Lines.Select(line => line.Text)))));
        var groups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "data-label-source").ToArray();
        Assert.Equal(Captions, groups.Select(group => group.Metadata["data-cfx-full-label"]));
        Assert.Equal(new[] { 0, 2, 3 }.Select(index => "series-" + lineIndex + "-point-" + index),
            groups.Select(group => group.Metadata["data-cfx-label-for"]));
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        var bars = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role is "bar" or "horizontal-bar").ToArray();
        var lines = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(mark => mark.Role == "line")
            .Select(mark => new LabelMarkShape(VisualSceneGeometry.Flatten(mark, 12), false, mark.StrokeWidth, plot)).ToArray();
        for (var index = 0; index < labels.Length; index++) {
            var bounds = CartesianReversalLabelTests.Bounds(labels[index]);
            Assert.True(LabelPlacementService.Contains(plot, bounds), Captions[index] + " escaped the native plot.");
            Assert.All(bars, bar => Assert.False(Overlaps(bounds, bar.Bounds), Captions[index] + " covers a mapped companion bar."));
            Assert.All(lines, line => Assert.False(line.Intersects(bounds), Captions[index] + " covers the actual line contour."));
            for (var other = index + 1; other < labels.Length; other++)
                Assert.False(Overlaps(bounds, CartesianReversalLabelTests.Bounds(labels[other])));
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        AssertVisibleSvgCaptions(XDocument.Parse(prepared.ToSvg()));
        Assert.NotEmpty(prepared.ToPng());
    }

    internal static void AssertVisibleSvgCaptions(XDocument svg) {
        var sources = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "data-label-source").ToArray();
        Assert.Equal(Captions.OrderBy(text => text), sources.Select(source => Normalize(string.Join(" ", source.Descendants()
            .Where(node => node.Name.LocalName == "text").SelectMany(text => {
                var rows = text.Descendants().Where(node => node.Name.LocalName == "tspan").ToArray();
                return rows.Length == 0 ? new[] { text.Value } : rows.Select(row => row.Value);
            })))).OrderBy(text => text));
        Assert.All(sources, source => Assert.NotNull(source.Attribute("data-cfx-label-for")));
    }

    private static string Normalize(string text) => string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    private static bool Overlaps(ChartRect first, ChartRect second) => first.Left < second.Right && first.Right > second.Left
        && first.Top < second.Bottom && first.Bottom > second.Top;
    private static void Capture(PreparedVisual prepared, string pose) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "authored-auto-caption-" + pose);
        File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + ".native.png", prepared.ToPng());
    }
}
