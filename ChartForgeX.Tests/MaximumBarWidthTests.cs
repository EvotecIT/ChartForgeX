using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MaximumBarWidthTests {
    [Fact]
    public void ExistingCompiledThemeConstructorRemainsAvailable() {
        var parameterTypes = new[] { typeof(VisualDesignTokens), typeof(VisualDesignTokens), typeof(VisualTypography),
            typeof(double), typeof(double), typeof(double), typeof(double), typeof(double), typeof(double), typeof(double),
            typeof(double), typeof(double), typeof(ChartColor?), typeof(double), typeof(double), typeof(double) };
        Assert.NotNull(typeof(VisualTheme).GetConstructor(parameterTypes));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void FractionalCapsRemainActualMaxima(bool horizontal, bool fromTheme) {
        var chart = Chart.Create().WithBarStyle(ChartBarStyle.Flat);
        if (fromTheme) chart.WithTheme(VisualTheme.Graphite().Configure(builder => builder.MaximumBarWidth = .05));
        else chart.WithMaximumBarWidth(.05);
        if (horizontal) chart.AddHorizontalBar("A", new[] { new ChartPoint(0, 2) });
        else chart.AddBar("A", new[] { new ChartPoint(0, 2) });
        var bar = Assert.Single(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneRectangle>(), n => n.Role == (horizontal ? "horizontal-bar" : "bar"));
        Assert.Equal(.05, horizontal ? bar.Bounds.Height : bar.Bounds.Width, 6);
    }

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, false, 2)]
    [InlineData(false, true, 1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 2)]
    public void SparseRegularBarsKeepTheirCapAndCentredSeriesSlots(bool horizontal, bool stacked, int categories) {
        var chart = Chart.Create().WithSize(1120, 360).WithLegend(false).WithHeader(false).WithBarStyle(ChartBarStyle.Flat).WithMaximumBarWidth(42);
        var points = Enumerable.Range(0, categories).Select(i => new ChartPoint(i, 2)).ToArray();
        if (horizontal) chart.AddHorizontalBar("A", points).AddHorizontalBar("B", points);
        else chart.AddBar("A", points).AddBar("B", points);
        if (stacked) chart.WithStackedBars();
        var scene = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene;
        var bars = scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == (horizontal ? "horizontal-bar" : "bar")).Select(node => node.Bounds).ToArray();
        Assert.Equal(categories * 2, bars.Length);
        Assert.All(bars, bounds => Assert.Equal(42, horizontal ? bounds.Height : bounds.Width, 6));
        for (var i = 0; i < categories; i++) {
            var a = bars[i]; var b = bars[categories + i];
            var centreA = horizontal ? a.Top + a.Height / 2 : a.Left + a.Width / 2;
            var centreB = horizontal ? b.Top + b.Height / 2 : b.Left + b.Width / 2;
            if (stacked) Assert.Equal(centreA, centreB, 6);
            else Assert.InRange(centreB - centreA, 42, 55);
        }
    }

    [Fact]
    public void ThemeCapSurvivesConfigurationTypographyAndJsonRoundTrips() {
        var theme = VisualTheme.Graphite().Configure(builder => builder.MaximumBarWidth = 48).WithFontFamily("Arial");
        Assert.Equal(48, theme.MaximumBarWidth);
        Assert.Equal(48, VisualTheme.FromJson(theme.ToJson()).MaximumBarWidth);
        var chart = Chart.Create().WithTheme(theme).WithBarStyle(ChartBarStyle.Flat).AddBar("Count", new[] { new ChartPoint(0, 2) });
        var bars = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneRectangle>().Where(n => n.Role == "bar").ToArray();
        Assert.Equal(48, Assert.Single(bars).Bounds.Width, 6);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithMaximumBarWidth(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithMaximumBarWidth(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => theme.Configure(builder => builder.MaximumBarWidth = 0));
    }
}
