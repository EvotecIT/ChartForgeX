using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianPlotRegionGalleryTests {
    [Theory]
    [InlineData(ChartSeriesKind.Scatter, "quadrants", false)]
    [InlineData(ChartSeriesKind.Scatter, "quadrants", true)]
    [InlineData(ChartSeriesKind.Scatter, "compact-quadrants", false)]
    [InlineData(ChartSeriesKind.Scatter, "compact-quadrants", true)]
    [InlineData(ChartSeriesKind.Bubble, "plot-regions", false)]
    [InlineData(ChartSeriesKind.Bubble, "plot-regions", true)]
    [InlineData(ChartSeriesKind.Bubble, "compact-plot-regions", false)]
    [InlineData(ChartSeriesKind.Bubble, "compact-plot-regions", true)]
    public void ExecutableGalleryModelsKeepBackgroundGuidanceAndObservationsIndependent(ChartSeriesKind kind, string variant, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var compact = variant.StartsWith("compact-", StringComparison.Ordinal);
        var chart = V2GalleryModels.Create(kind, variant, mode).WithSize(compact ? 360 : 800, compact ? 360 : 440)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle(kind == ChartSeriesKind.Scatter ? "Effort and impact" : "Capacity and delay");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(kind == ChartSeriesKind.Scatter ? 4 : 2, prepared.Regions.Count(r => r.Role == "plot-region"));
        Assert.Equal(kind == ChartSeriesKind.Scatter ? 6 : 5, prepared.Regions.Count(r => r.Role == "point"));
        var svg = prepared.ToSvg(); var document = XDocument.Parse(svg);
        var regions = document.Descendants().Where(n => (string?)n.Attribute("data-cfx-role") == "plot-region").ToArray();
        Assert.All(regions, r => Assert.Equal("true", (string?)r.Attribute("data-cfx-passive")));
        Assert.DoesNotContain(prepared.Diagnostics, d => d.Code == "cartesian.plot-region-label-overflow");
        var path = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(path)) return;
        Directory.CreateDirectory(path);
        var name = "family-" + kind.ToString().ToLowerInvariant() + "-" + variant + "-" + (dark ? "dark" : "light");
        File.WriteAllText(Path.Combine(path, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(path, name + ".png"), prepared.ToPng());
    }
}
