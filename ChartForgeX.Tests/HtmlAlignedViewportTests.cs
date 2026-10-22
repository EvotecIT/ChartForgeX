using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
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
        var requested = new ChartAxisWindow(x.FullMinimum, x.FullMinimum + (x.FullMaximum - x.FullMinimum) * .7);
        var window = renderer.PrepareSection(chart, new ChartCartesianViewport(x: requested), Configure);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AlignedAuthoredEdgesAndExplicitXYWindowsRetainRawDomainPolicy(bool includeNavigator) {
        var chart = Chart.Create().WithSize(800, 440).WithXAxisBounds(0, 12).WithYAxisBounds(0, 40)
            .AddScatter("Samples", new[] { new ChartPoint(0, 10), new ChartPoint(12, 30) });
        chart.Series[0].Markers.Radius = 18;
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart,
            new ChartCartesianViewport(new ChartAxisWindow(2, 10), new ChartAxisWindow(12, 24)), options => {
                Configure(options); options.XNavigation.IncludeNavigator = includeNavigator;
            });
        SameFullDomain(section.PreparedVisual, section.NarrowPreparedVisual!);
        Assert.Equal(0, section.PreparedVisual.XProjection!.FullMinimum); Assert.Equal(12, section.PreparedVisual.XProjection.FullMaximum);
        Assert.Equal(0, section.PreparedVisual.YProjection!.FullMinimum); Assert.Equal(40, section.PreparedVisual.YProjection.FullMaximum);
        Assert.Equal(12, section.NarrowPreparedVisual!.YProjection!.Minimum); Assert.Equal(24, section.NarrowPreparedVisual.YProjection.Maximum);
        Assert.Equal(0, chart.Options.YAxis.Minimum); Assert.Equal(40, chart.Options.YAxis.Maximum);
        Assert.Equal(includeNavigator ? 2 : 0, NavigatorImages(section.Html).Length);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("y")]
    [InlineData("xy")]
    public void AutomaticFullBoundsRemainExactAcrossNativeWindowsAndReset(string axes) {
        var chart = MetricSamples(1000);
        var renderer = new HtmlInteractiveChartRenderer();
        var initial = renderer.PrepareSection(chart, configure: ConfigureMetricDrawing);
        var requested = new ChartCartesianViewport(axes.Contains('x') ? new ChartAxisWindow(2, 10) : null,
            axes.Contains('y') ? new ChartAxisWindow(12, 24) : null);
        var window = renderer.PrepareSection(chart, requested, ConfigureMetricDrawing);
        var reset = renderer.PrepareSection(chart, configure: ConfigureMetricDrawing);
        SameFullDomain(initial.PreparedVisual, initial.NarrowPreparedVisual!);
        SameFullDomain(initial.PreparedVisual, window.PreparedVisual);
        SameFullDomain(window.PreparedVisual, window.NarrowPreparedVisual!);
        SameFullDomain(initial.PreparedVisual, reset.PreparedVisual);
        SameFullDomain(reset.PreparedVisual, reset.NarrowPreparedVisual!);
        foreach (var prepared in new[] { window.PreparedVisual, window.NarrowPreparedVisual! }) {
            var x = prepared.XProjection!; var y = prepared.YProjection!;
            Assert.Equal(requested.X?.Minimum ?? x.FullMinimum, x.Minimum);
            Assert.Equal(requested.X?.Maximum ?? x.FullMaximum, x.Maximum);
            Assert.Equal(requested.Y?.Minimum ?? y.FullMinimum, y.Minimum);
            Assert.Equal(requested.Y?.Maximum ?? y.FullMaximum, y.Maximum);
            var points = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node =>
                (string?)node.Attribute("data-cfx-role") == "point" && (string?)node.Attribute("data-cfx-series-key") == "samples").ToArray();
            Assert.Equal(1000, points.Length);
            for (var index = 0; index < points.Length; index++) {
                Assert.Equal("row-" + index.ToString(CultureInfo.InvariantCulture), (string?)points[index].Attribute("data-cfx-observation-id"));
                Assert.Equal(chart.Series[0].Points[index].X, double.Parse(points[index].Attribute("data-cfx-x")!.Value, CultureInfo.InvariantCulture));
                Assert.Equal(chart.Series[0].Points[index].Y, double.Parse(points[index].Attribute("data-cfx-y")!.Value, CultureInfo.InvariantCulture));
            }
        }
        Assert.Null(chart.Options.XAxis.Minimum); Assert.Null(chart.Options.XAxis.Maximum);
        Assert.Null(chart.Options.YAxis.Minimum); Assert.Null(chart.Options.YAxis.Maximum);
    }

    [Fact]
    public void AdaptiveSecondaryYFittingKeepsItsFullLabelsAndExactSharedXBounds() {
        var chart = MetricSamples(3).AddStepLine("Secondary", new[] { new ChartPoint(0, 1), new ChartPoint(6, 10), new ChartPoint(12, 1000) });
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        foreach (var series in chart.Series) series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 9; });
        var renderer = new HtmlInteractiveChartRenderer();
        var initial = renderer.PrepareSection(chart, configure: ConfigureMetricDrawing);
        var window = renderer.PrepareSection(chart, new ChartCartesianViewport(x: new ChartAxisWindow(2, 10)), ConfigureMetricDrawing);
        foreach (var pair in new[] { (Full: initial.PreparedVisual, Window: window.PreparedVisual),
            (Full: initial.NarrowPreparedVisual!, Window: window.NarrowPreparedVisual!) }) {
            Assert.NotNull(pair.Window.YProjection);
            Assert.Equal(pair.Full.SecondaryYProjection!.FullMinimum, pair.Window.SecondaryYProjection!.FullMinimum);
            Assert.Equal(pair.Full.SecondaryYProjection.FullMaximum, pair.Window.SecondaryYProjection.FullMaximum);
            Assert.Equal(initial.PreparedVisual.XProjection!.FullMinimum, pair.Window.XProjection!.FullMinimum);
            Assert.Equal(initial.PreparedVisual.XProjection.FullMaximum, pair.Window.XProjection.FullMaximum);
            Assert.Equal(2, pair.Window.XProjection.Minimum); Assert.Equal(10, pair.Window.XProjection.Maximum);
            foreach (var role in new[] { "axis-y-label", "axis-secondary-y-label" })
                Assert.Equal(pair.Full.Regions.Where(region => region.Role == role).Select(region => region.Label),
                    pair.Window.Regions.Where(region => region.Role == role).Select(region => region.Label));
        }
        Assert.Null(chart.Options.YAxis.Minimum); Assert.Null(chart.Options.YAxis.Maximum);
        Assert.Null(chart.Options.SecondaryYAxis.Minimum); Assert.Null(chart.Options.SecondaryYAxis.Maximum);
    }

    [Fact]
    public void AdaptiveNavigatorRetainsRealFullScenesAndCanBeOmittedWithoutChangingTheWindow() {
        var chart = MetricSamples(3);
        var renderer = new HtmlInteractiveChartRenderer();
        void WithNavigator(HtmlChartInteractionOptions options) { ConfigureMetricDrawing(options); options.XNavigation.Enabled = true; }
        var initial = renderer.PrepareSection(chart, configure: WithNavigator);
        var window = renderer.PrepareSection(chart, new ChartCartesianViewport(x: new ChartAxisWindow(2, 10)), WithNavigator);
        var images = NavigatorImages(window.Html);
        Assert.Equal(2, images.Length);
        Assert.Equal(NavigatorImages(initial.Html), images);
        foreach (var image in images) {
            var native = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(image));
            Assert.Contains("data-cfx-x=\"0\"", native); Assert.Contains("data-cfx-x=\"12\"", native);
            Assert.DoesNotContain("data-cfx-visible=\"false\"", native);
        }
        var omitted = renderer.PrepareSection(chart, new ChartCartesianViewport(x: new ChartAxisWindow(2, 10)), options => {
            WithNavigator(options); options.XNavigation.IncludeNavigator = false;
        });
        Assert.Empty(NavigatorImages(omitted.Html));
        Assert.Equal(window.PreparedVisual.ToSvg(), omitted.PreparedVisual.ToSvg());
        Assert.Equal(window.NarrowPreparedVisual!.ToSvg(), omitted.NarrowPreparedVisual!.ToSvg());
    }

    private static Chart MetricSamples(int count) {
        var family = BrowserProofFonts.Family;
        var theme = VisualTheme.Graphite().WithTypography(new VisualTypography(family, 22, 13, 11, 12, 11));
        var series = ChartSeries.CreateScatter(new ChartSeriesId("samples"), "Samples", Enumerable.Range(0, count)
            .Select(index => new ChartPoint(12d * index / (count - 1), 10 + 20d * index / (count - 1))))
            .WithObservationIds(Enumerable.Range(0, count).Select(index => new ChartObservationId("row-" + index.ToString(CultureInfo.InvariantCulture))));
        series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 4; });
        return Chart.Create().WithSize(800, 440).WithPadding(24, 24, 24, 24).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithTheme(theme, VisualThemeMode.Light).WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
            .AddSeries(series);
    }
    private static void ConfigureMetricDrawing(HtmlChartInteractionOptions options) {
        options.IdScope = "aligned-domain-proof"; options.Interaction.ChartId = "aligned-domain-proof";
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.NarrowWidth = 390; options.Adaptive.NarrowHeight = 440; options.Adaptive.Breakpoint = 640;
    }
    private static string[] NavigatorImages(string html) => Regex.Matches(html, "data:image/svg\\+xml;base64,([^\"]+)")
        .Select(match => match.Groups[1].Value).ToArray();
    private static void Configure(HtmlChartInteractionOptions options) { options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.XNavigation.Enabled = true; }
    private static void SameFullDomain(PreparedVisual first, PreparedVisual second) {
        Assert.Equal(first.XProjection!.FullMinimum, second.XProjection!.FullMinimum); Assert.Equal(first.XProjection.FullMaximum, second.XProjection.FullMaximum);
        Assert.Equal(first.YProjection!.FullMinimum, second.YProjection!.FullMinimum); Assert.Equal(first.YProjection.FullMaximum, second.YProjection.FullMaximum);
    }
}
