using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeInitialFadeBeforeControls {
    [Theory]
    [InlineData(ChartSeriesKind.Line)] [InlineData(ChartSeriesKind.Pie)] [InlineData(ChartSeriesKind.Gauge)]
    [InlineData(ChartSeriesKind.ProgressRing)] [InlineData(ChartSeriesKind.RadialBar)] [InlineData(ChartSeriesKind.Radar)]
    [InlineData(ChartSeriesKind.Circle)] [InlineData(ChartSeriesKind.Heatmap)] [InlineData(ChartSeriesKind.Gantt)]
    [InlineData(ChartSeriesKind.RegionMap)] [InlineData(ChartSeriesKind.Sunburst)] [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)] [InlineData(ChartSeriesKind.WordCloud)]
    public void OrdinaryNativeFamiliesKeepDefaultSvgPngAndHtmlBytes(ChartSeriesKind kind) {
        var renderer = new HtmlInteractiveChartRenderer();
        var first = renderer.PrepareSection(Assert.IsType<Chart>(ExampleRecipes.ForKind(kind).Create()));
        var repeated = renderer.PrepareSection(Assert.IsType<Chart>(ExampleRecipes.ForKind(kind).Create()));
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "initial-fade-ordinary-" + kind);
            File.WriteAllText(path + ".html", first.Html); File.WriteAllText(path + ".svg", first.PreparedVisual.ToSvg()); File.WriteAllBytes(path + ".native.png", first.PreparedVisual.ToPng());
        }
        Assert.Equal(first.Html, repeated.Html); Assert.Equal(first.PreparedVisual.ToSvg(), repeated.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), repeated.PreparedVisual.ToPng());
    }
}
