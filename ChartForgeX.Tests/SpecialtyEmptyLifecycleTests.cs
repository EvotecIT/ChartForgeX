using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects convenience factory and retained-handle consistency for source-driven progress and specialty families.</summary>
public sealed class SpecialtyEmptyLifecycleTests {
    [Theory]
    [InlineData(ChartSeriesKind.ProgressRing, "radial.no-data", "progress-ring-point")]
    [InlineData(ChartSeriesKind.LayeredRadial, "radial.no-data", "layered-radial-point")]
    [InlineData(ChartSeriesKind.ProgressBar, "progress.no-data", "progress-row")]
    [InlineData(ChartSeriesKind.Pictorial, "specialty.no-data", "pictorial-item")]
    [InlineData(ChartSeriesKind.WordCloud, "specialty.no-data", "word-cloud-term")]
    public void EmptyFactoriesPrepareWithoutInventedObservationsThenRefillTheSameHandle(ChartSeriesKind kind, string diagnostic, string role) {
        var chart = kind switch {
            ChartSeriesKind.ProgressRing => Chart.Create().AddProgressRing("Items", Array.Empty<ChartPoint>()),
            ChartSeriesKind.LayeredRadial => Chart.Create().AddLayeredRadial("Items", Array.Empty<ChartRadialLayer>()),
            ChartSeriesKind.ProgressBar => Chart.Create().AddProgressBars("Items", Array.Empty<ChartProgressItem>(), maximum: 200),
            ChartSeriesKind.Pictorial => Chart.Create().AddPictorial("Items", Array.Empty<ChartPictorialItem>(), ChartPictorialShape.Star),
            _ => Chart.Create().AddWordCloud("Items", Array.Empty<ChartWordCloudItem>())
        };
        chart.WithSize(400, 300).WithHeader(false).WithLegend(false);
        var series = Assert.Single(chart.Series); var id = series.Id;
        Assert.Equal(0, series.ObservationCount); Assert.Equal(0, series.SourcePointCount);
        var empty = chart.Prepare(Context()); var svg = empty.ToSvg(); var png = empty.ToPng(Raster());
        Assert.Contains(empty.Diagnostics, item => item.Code == diagnostic);
        Assert.DoesNotContain(empty.Scene.Nodes, node => node.Role == role);
        if (kind == ChartSeriesKind.LayeredRadial) series.ReplaceRadialLayers(new[] { new ChartRadialLayer("Arrived", 40) });
        else series.Points.Add(new ChartPoint(1, 40));
        series.WithObservationId(0, new ChartObservationId("arrived"));
        var filled = chart.Prepare(Context());
        var mark = Assert.Single(filled.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == role);
        Assert.Equal("arrived", mark.Metadata["data-cfx-observation-id"]);
        Assert.Equal("40", mark.Metadata["data-cfx-value"]);
        Assert.Same(series, Assert.Single(chart.Series)); Assert.Equal(id, series.Id);
        Assert.Equal(1, series.ObservationCount); Assert.Equal(1, series.SourcePointCount);
        if (kind == ChartSeriesKind.ProgressBar) Assert.Equal(200, chart.Options.ProgressMaximum);
        if (kind == ChartSeriesKind.Pictorial) Assert.Equal(ChartPictorialShape.Star, chart.Options.PictorialShape);
        Assert.NotEqual(svg, filled.ToSvg()); Assert.NotEmpty(filled.ToPng(Raster()));
        Assert.Equal(svg, empty.ToSvg()); Assert.Equal(png, empty.ToPng(Raster()));
        if (kind == ChartSeriesKind.LayeredRadial) series.ReplaceRadialLayers(Array.Empty<ChartRadialLayer>());
        else series.Points.Clear();
        var cleared = chart.Prepare(Context());
        Assert.Contains(cleared.Diagnostics, item => item.Code == diagnostic);
        Assert.DoesNotContain(cleared.Scene.Nodes, node => node.Role == role);
        Assert.Empty(series.ObservationIds);
        Capture(filled, "empty-factory-refilled-" + kind);
    }

    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(400, 300)), frame: new VisualFrame(showLegend: false));
    private static VisualRenderOptions Raster() => new(supersampling: 1);
    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + "-native.png"), prepared.ToPng(Raster()));
    }
}
