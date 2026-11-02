using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Named categories retain useful captions within a bounded horizontal chart gutter.</summary>
public sealed class HorizontalCategoryCaptionTests {
    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    public void CompactLongCategoriesKeepReadableCaptionsAndCompleteFacts(double fontSize) {
        var xs = new[] { 1, 2, 4, 5, 7, 8, 10, 11, 13, 14, 16, 17, 19, 20, 22, 23 };
        var names = Enumerable.Range(1, 24).Select(x => $"Region {x}: international distribution").ToArray();
        var chart = Chart.Create().WithSize(390, 420).WithTheme(VisualTheme.Graphite()).WithTitle("Regional net orders").WithLegend(false)
            .ConfigureTickLabelStyle(style => style.WithFontSize(fontSize)).WithXLabels(names)
            .AddHorizontalBar("Observed net orders", xs.Select((x, i) => new ChartPoint(x,
                i is 3 or 11 ? 0 : (i % 3 == 0 ? -1 : 1) * (7 + i % 7 * 4))));
        chart.Series[0].WithObservationIds(xs.Select(x => new ChartObservationId("region-" + x)));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "axis-y-label").ToArray();
        Assert.NotEmpty(captions);
        var axis = prepared.Scene.Nodes.OfType<VisualSceneLine>().Single(node => node.Role == "axis-y");
        foreach (var caption in captions) {
            var bounds = CartesianReversalLabelTests.Bounds(caption);
            Assert.Equal(fontSize, caption.Text.Size);
            Assert.True(bounds.Left >= 0 && bounds.Right <= axis.Start.X && bounds.Top >= 0 && bounds.Bottom <= 420);
        }
        Assert.Contains(prepared.Regions, region => region.Role == "axis-y-label" && region.Label!.Contains(names[22], StringComparison.Ordinal));
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "point").ToArray();
        Assert.Equal(xs.Length, points.Length);
        Assert.All(points, point => Assert.True(point.Metadata.ContainsKey("data-cfx-observation-id")));
        Assert.NotEmpty(prepared.ToPng());
    }
}
