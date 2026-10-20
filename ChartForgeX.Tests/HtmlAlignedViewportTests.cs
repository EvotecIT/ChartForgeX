using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlAlignedViewportTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdaptiveAutomaticMarkerDomainsStayAlignedAcrossWindowResetAndExports(bool reversed) {
        var chart = Chart.Create().WithSize(800, 440).AddScatter("Samples", new[] { new ChartPoint(0, 10), new ChartPoint(12, 30) });
        chart.Series[0].Markers.Radius = 18; chart.Options.XAxis.Reversed = reversed;
        var renderer = new HtmlInteractiveChartRenderer();
        var initial = renderer.PrepareSection(chart, configure: Configure);
        var wide = initial.PreparedVisual; var narrow = initial.NarrowPreparedVisual!;
        SameFullDomain(wide, narrow);
        Assert.True(narrow.XProjection!.FullMinimum < -.8); Assert.True(narrow.XProjection.FullMaximum > 12.8);
        var x = narrow.XProjection;
        var requested = new ChartXViewport(x.FullMinimum, x.FullMinimum + (x.FullMaximum - x.FullMinimum) * .7);
        var window = renderer.PrepareSection(chart, requested, Configure);
        Assert.Equal(requested.Minimum, window.PreparedVisual.XProjection!.Minimum);
        Assert.Equal(requested.Minimum, window.NarrowPreparedVisual!.XProjection!.Minimum);
        Assert.Equal(requested.Maximum, window.PreparedVisual.XProjection.Maximum);
        Assert.Equal(requested.Maximum, window.NarrowPreparedVisual.XProjection.Maximum);
        SameFullDomain(window.PreparedVisual, window.NarrowPreparedVisual);
        Assert.NotEmpty(window.PreparedVisual.ToPng()); Assert.NotEmpty(window.NarrowPreparedVisual.ToPng());
        Assert.Contains("<svg", window.PreparedVisual.ToSvg());
        var reset = renderer.PrepareSection(chart, configure: Configure);
        SameFullDomain(reset.PreparedVisual, reset.NarrowPreparedVisual!);
        Assert.Equal(initial.PreparedVisual.XProjection!.FullMinimum, reset.PreparedVisual.XProjection!.Minimum);
        Assert.Null(chart.Options.XAxis.Minimum); Assert.Null(chart.Options.XAxis.Maximum);
        Assert.Null(chart.Options.YAxis.Minimum); Assert.Null(chart.Options.YAxis.Maximum);
    }

    [Fact]
    public void AlignedAuthoredEdgesAndExplicitXYWindowsRetainRawDomainPolicy() {
        var chart = Chart.Create().WithSize(800, 440).WithXAxisBounds(0, 12).WithYAxisBounds(0, 40)
            .AddScatter("Samples", new[] { new ChartPoint(0, 10), new ChartPoint(12, 30) });
        chart.Series[0].Markers.Radius = 18;
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart,
            new ChartCartesianViewport(new ChartXViewport(2, 10), new ChartYViewport(12, 24)), Configure);
        SameFullDomain(section.PreparedVisual, section.NarrowPreparedVisual!);
        Assert.Equal(0, section.PreparedVisual.XProjection!.FullMinimum); Assert.Equal(12, section.PreparedVisual.XProjection.FullMaximum);
        Assert.Equal(0, section.PreparedVisual.YProjection!.FullMinimum); Assert.Equal(40, section.PreparedVisual.YProjection.FullMaximum);
        Assert.Equal(12, section.NarrowPreparedVisual!.YProjection!.Minimum); Assert.Equal(24, section.NarrowPreparedVisual.YProjection.Maximum);
        Assert.Equal(0, chart.Options.YAxis.Minimum); Assert.Equal(40, chart.Options.YAxis.Maximum);
    }
    private static void Configure(HtmlChartInteractionOptions options) { options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.XNavigation.Enabled = true; }
    private static void SameFullDomain(PreparedVisual first, PreparedVisual second) {
        Assert.Equal(first.XProjection!.FullMinimum, second.XProjection!.FullMinimum); Assert.Equal(first.XProjection.FullMaximum, second.XProjection.FullMaximum);
        Assert.Equal(first.YProjection!.FullMinimum, second.YProjection!.FullMinimum); Assert.Equal(first.YProjection.FullMaximum, second.YProjection.FullMaximum);
    }
}
