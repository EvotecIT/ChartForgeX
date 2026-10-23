using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianPhysicalAxesTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HorizontalMixedValueAxesUseIndependentPhysicalRangesReadoutsAndNativeExport(bool reversed) {
        var chart = Horizontal();
        chart.Options.XAxis.Reversed = reversed; chart.Options.SecondaryXAxis.Reversed = !reversed;
        chart.WithXAxisBounds(0, 40).WithYAxisBounds(0, 4).WithSecondaryXAxisBounds(100, 500);
        chart.Options.XAxis.LabelFormatter = value => "Primary=" + value.ToString(CultureInfo.InvariantCulture);
        chart.Options.SecondaryXAxis.LabelFormatter = value => "Secondary=" + value.ToString(CultureInfo.InvariantCulture);
        var prepared = chart.Prepare(Context());
        var secondary = Assert.IsType<ChartAxisProjection>(prepared.SecondaryXProjection);
        Assert.Equal(ChartAxisDimension.X, secondary.Dimension); Assert.Equal(ChartAxisSide.Secondary, secondary.Side);
        Assert.Null(prepared.SecondaryYProjection);
        Assert.Equal(reversed ? secondary.PlotBounds.Left : secondary.PlotBounds.Right, secondary.Project(100), 8);
        Assert.Equal(300, secondary.Unproject(secondary.Project(300)), 8);
        var svg = XDocument.Parse(prepared.ToSvg());
        var second = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-observation-id") == "secondary-1");
        Assert.Equal("300", (string?)second.Attribute("data-cfx-y"));
        Assert.Equal("Secondary=300", (string?)second.Attribute("data-cfx-formatted-value"));
        Assert.Contains(prepared.Regions, region => region.Role == "axis-secondary-x-label" && region.Label!.StartsWith("Secondary=", StringComparison.Ordinal));
        var view = chart.Prepare(Context(), new ChartCartesianViewport(y: new ChartAxisWindow(1.25, 2.75), secondaryX: new ChartAxisWindow(200, 400)));
        Assert.Equal(1.25, view.YProjection!.Minimum); Assert.Equal(200, view.SecondaryXProjection!.Minimum);
        Assert.Equal(0, view.XProjection!.Minimum); Assert.Equal(40, view.XProjection.Maximum);
        Assert.NotEmpty(view.ToPng()); Assert.Equal(3, chart.Series[1].SourcePointCount);
        var outside = XDocument.Parse(view.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-observation-id") == "secondary-0");
        Assert.Equal("false", (string?)outside.Attribute("data-cfx-visible"));
        var saved = view.ToSvg(); chart.Options.SecondaryXAxis.Reversed = reversed; chart.Series[1].UsePrimaryValueAxis();
        Assert.Equal(saved, view.ToSvg()); Assert.Equal(300, view.SecondaryXProjection.Unproject(view.SecondaryXProjection.Project(300)), 8);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecondaryValueWindowsAndAlignedAlternateLayoutsKeepEveryFullAxisDetached(bool horizontal) {
        var chart = horizontal ? Horizontal() : Vertical();
        var renderer = new HtmlInteractiveChartRenderer();
        void Configure(HtmlChartInteractionOptions options) { options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.Breakpoint = 500; options.Adaptive.NarrowWidth = 390; options.Adaptive.NarrowHeight = 440; }
        var full = renderer.PrepareSection(chart, configure: Configure);
        var secondary = horizontal ? full.PreparedVisual.SecondaryXProjection! : full.PreparedVisual.SecondaryYProjection!;
        var middle = secondary.FullMinimum / 2 + secondary.FullMaximum / 2;
        var window = new ChartAxisWindow(secondary.FullMinimum + (middle - secondary.FullMinimum) / 2, middle);
        var request = horizontal ? new ChartCartesianViewport(secondaryX: window) : new ChartCartesianViewport(secondaryY: window);
        var section = renderer.PrepareSection(chart, request, Configure);
        foreach (var prepared in new[] { section.PreparedVisual, section.NarrowPreparedVisual! }) {
            var projected = horizontal ? prepared.SecondaryXProjection! : prepared.SecondaryYProjection!;
            Assert.Equal(secondary.FullMinimum, projected.FullMinimum); Assert.Equal(secondary.FullMaximum, projected.FullMaximum);
            Assert.Equal(window.Minimum, projected.Minimum); Assert.Equal(window.Maximum, projected.Maximum);
            Assert.NotEmpty(prepared.ToPng());
        }
        using var metadata = Metadata(section.Html);
        var key = horizontal ? "secondaryXProjection" : "secondaryYProjection";
        Assert.Equal(window.Minimum, metadata.RootElement.GetProperty(key).GetProperty("minimum").GetDouble());
        Assert.Equal(2, chart.Series.Count); Assert.Null(ChartCartesianOrientation.ValueAxis(chart, ChartAxisSide.Secondary).Minimum);
        Assert.Throws<NotSupportedException>(() => chart.Prepare(Context(), horizontal
            ? new ChartCartesianViewport(secondaryY: window) : new ChartCartesianViewport(secondaryX: window)));
    }

    [Fact]
    public void VisibleCategoriesFitCrossingSegmentsAndIndependentSecondaryDomainsWithoutDroppingSourceFacts() {
        var chart = Vertical();
        foreach (var series in chart.Series) series.Markers.Enabled = false;
        var full = chart.Prepare(Context());
        var request = new ChartCartesianViewport(x: new ChartAxisWindow(4, 6), valueDomain: ChartValueDomain.VisibleCategories);
        var view = chart.Prepare(Context(), request);
        Assert.InRange(view.YProjection!.Minimum, 30, 40); Assert.InRange(view.YProjection.Maximum, 60, 70);
        Assert.InRange(view.SecondaryYProjection!.Minimum, 130, 140); Assert.InRange(view.SecondaryYProjection.Maximum, 160, 170);
        Assert.Equal(full.YProjection!.FullMinimum, view.YProjection.FullMinimum);
        var facts = XDocument.Parse(view.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(4, facts.Length); Assert.All(facts, node => Assert.Equal("false", (string?)node.Attribute("data-cfx-visible")));
        Assert.Equal(new[] { "0", "10", "0", "10" }, facts.Select(node => (string?)node.Attribute("data-cfx-x")));
        Assert.Null(chart.Options.YAxis.Minimum); Assert.Null(chart.Options.SecondaryYAxis.Maximum);
        Assert.NotEmpty(view.ToPng());
        var manual = chart.Prepare(Context(), new ChartCartesianViewport(x: request.X, y: new ChartAxisWindow(20, 80), valueDomain: request.ValueDomain));
        Assert.Equal(20, manual.YProjection!.Minimum); Assert.Equal(80, manual.YProjection.Maximum);
        Assert.Equal(view.SecondaryYProjection.Minimum, manual.SecondaryYProjection!.Minimum);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VisibleCategoryPolicyKeepsDisconnectedEmptyWindowsAndAuthoredAxisBounds(bool horizontal) {
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithXAxisBounds(0, 100).WithYAxisBounds(0, 100)
            .AddLine("Gaps", new[] { new ChartPoint(0, 10), new ChartPoint(10, 20), new ChartPoint(90, 80, true), new ChartPoint(100, 90) });
        if (horizontal) chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var full = chart.Prepare(Context());
        var request = horizontal ? new ChartCartesianViewport(y: new ChartAxisWindow(40, 60), valueDomain: ChartValueDomain.VisibleCategories)
            : new ChartCartesianViewport(x: new ChartAxisWindow(40, 60), valueDomain: ChartValueDomain.VisibleCategories);
        var view = chart.Prepare(Context(), request);
        var value = horizontal ? view.XProjection! : view.YProjection!;
        Assert.Equal(0, value.Minimum); Assert.Equal(100, value.Maximum);
        Assert.Equal(full.ToSvg(), chart.Prepare(Context(), null).ToSvg());
        Assert.Equal(4, chart.Series[0].SourcePointCount);
    }

    [Fact]
    public void SecondaryTimeFactsUseTheirOwnCapturedAxisEvenWhenTheGenericYBrushIsAmbiguous() {
        var date = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithLegend(false).AddLine("Counts", new[] { new ChartPoint(0, 10), new ChartPoint(1, 20) })
            .AddLine("Instants", new[] { new ChartPoint(0, date.ToOADate()), new ChartPoint(1, date.AddDays(1).ToOADate()) });
        chart.Series[1].UseSecondaryValueAxis(); chart.ConfigureSecondaryYAxis(axis => axis.WithTimeScale());
        var prepared = chart.Prepare(Context());
        Assert.Null(prepared.Scene.DataProjection!.Y); Assert.Equal(ChartScaleKind.Time, prepared.SecondaryYProjection!.Scale);
        var facts = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point"
            && (string?)node.Attribute("data-cfx-series") == "1").ToArray();
        Assert.Equal(date.ToString("O", CultureInfo.InvariantCulture), (string?)facts[0].Attribute("data-cfx-value-instant"));
        Assert.Equal(date.AddDays(1).ToString("O", CultureInfo.InvariantCulture), (string?)facts[1].Attribute("data-cfx-value-instant"));
    }

    [Fact]
    public void WindowPolicyValidationAndSecondaryAxisConfigurationAreAtomic() {
        Assert.Throws<ArgumentException>(() => new ChartCartesianViewport());
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartCartesianViewport(x: new ChartAxisWindow(0, 1), valueDomain: (ChartValueDomain)99));
        var chart = Horizontal(); var original = chart.Options.SecondaryXAxis;
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ConfigureSecondaryXAxis(axis => { axis.Reversed = true; axis.WithBounds(10, 0); }));
        Assert.Same(original, chart.Options.SecondaryXAxis); Assert.False(original.Reversed);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Prepare(Context(), new ChartCartesianViewport(secondaryX: new ChartAxisWindow(-1, 10000))));
    }

    private static Chart Horizontal() {
        var chart = Chart.Create().WithSize(800, 440).WithHeader(false).WithLegend(false)
            .AddHorizontalBar("Counts", new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) })
            .AddLine("Duration", new[] { new ChartPoint(1, 100), new ChartPoint(2, 300), new ChartPoint(3, 500) });
        chart.Options.LineOrientation = ChartOrientation.Horizontal; chart.Series[1].UseSecondaryValueAxis();
        for (var index = 0; index < 3; index++) chart.Series[1].WithObservationId(index, new ChartObservationId("secondary-" + index));
        return chart;
    }
    private static Chart Vertical() {
        var chart = Chart.Create().WithSize(800, 440).WithHeader(false).WithLegend(false)
            .AddLine("Counts", new[] { new ChartPoint(0, 0), new ChartPoint(10, 100) })
            .AddLine("Duration", new[] { new ChartPoint(0, 100), new ChartPoint(10, 200) });
        chart.Series[1].UseSecondaryValueAxis(); return chart;
    }
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(800, 440)));
    private static JsonDocument Metadata(string html) {
        var attribute = System.Text.RegularExpressions.Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]*)\"").Groups[1].Value;
        return JsonDocument.Parse(System.Net.WebUtility.HtmlDecode(attribute));
    }
}
