using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TimeAxisReviewClosureTests {
    private static readonly DateTime Start = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(false, VisualThemeMode.Light)]
    [InlineData(false, VisualThemeMode.Dark)]
    [InlineData(true, VisualThemeMode.Light)]
    [InlineData(true, VisualThemeMode.Dark)]
    public void HorizontalAxesPreserveAutomaticAndAuthoredTickDensityThroughProjection(bool category, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(category ? 360 : 1400, category ? 800 : 360).WithHeader(false).WithLegend(false)
            .WithThemeMode(mode).WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
            .AddLine("Dates", Enumerable.Range(0, 7).Select(index => category
                ? new ChartPoint(Start.AddDays(index), index) : new ChartPoint(index, Start.AddDays(index).ToOADate())));
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var axis = category ? chart.Options.YAxis : chart.Options.XAxis;
        axis.WithTimeScale();
        var role = category ? "axis-y-label" : "axis-x-label";
        PreparedVisual Prepare() => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var automatic = Prepare();
        var expected = Captions(automatic, role).Select(node => node.Text.Lines.Single().Text).ToArray();
        ScatterBubbleColorRenderingTests.Capture(automatic, $"time-horizontal-{category}-{mode}");
        Assert.True(expected.Length > 6, "Automatic physical-axis projection must retain its measured density rather than authoring the default six-tick ceiling.");
        axis.TickCount = 4;
        Assert.InRange(Captions(Prepare(), role).Length, 2, 4);
        axis.WithAutomaticTickCount();
        var restored = Prepare();
        Assert.Equal(expected, Captions(restored, role).Select(node => node.Text.Lines.Single().Text));
        Assert.Equal(automatic.ToPng(), restored.ToPng());
    }

    [Theory]
    [InlineData(3, VisualThemeMode.Light)]
    [InlineData(3, VisualThemeMode.Dark)]
    [InlineData(5, VisualThemeMode.Light)]
    [InlineData(5, VisualThemeMode.Dark)]
    public void UnlistedRegularBarCadencesFitTheGeneratedGridBeforePaintingCaptions(int days, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(360, 320).WithHeader(false).WithLegend(false).WithXAxisTimeScale().WithThemeMode(mode)
            .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
            .ConfigureGridStyle(style => style.ShowVerticalLines = true)
            .AddBar("Readings", Enumerable.Range(0, 15).Select(index => new ChartPoint(Start.AddDays(index * days), 20 + index)));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"time-cadence-{days}-{mode}");
        var labels = Captions(prepared, "axis-x-label");
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "grid-x").ToArray();
        var marks = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").ToArray();
        Assert.InRange(labels.Length, 2, 14);
        Assert.Equal(labels.Length, grid.Length);
        Assert.Equal(labels.Length, prepared.Regions.Count(region => region.Role == "axis-x-label"));
        for (var index = 0; index < labels.Length; index++) {
            var center = labels[index].X + labels[index].Text.Metrics.Width / 2;
            Assert.InRange(Math.Abs(center - grid[index].Start.X), 0, .5);
            Assert.Contains(marks, mark => Math.Abs(mark.Bounds.Left + mark.Bounds.Width / 2 - center) < .5);
            if (index > 1) Assert.Equal(grid[1].Start.X - grid[0].Start.X, grid[index].Start.X - grid[index - 1].Start.X, 7);
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.axis-label-overflow");
        Assert.NotEmpty(prepared.ToPng());
    }

    private static VisualSceneText[] Captions(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(node => node.Role == role).ToArray();
}
