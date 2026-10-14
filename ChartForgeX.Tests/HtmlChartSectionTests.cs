using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlChartSectionTests {
    [Fact]
    public void SectionAndExportShareOnePreparationAndRemainDetachedFromTheChartAndCallback() {
        var chart = Line();
        var calls = new Dictionary<double, int>();
        chart.Options.XAxis.LabelFormatter = value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return "Captured X " + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        };
        HtmlChartInteractionOptions? retained = null;
        var configureCalls = 0;
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart, new ChartXViewport(2, 6), options => {
            configureCalls++; retained = options; options.IdScope = "captured-section";
        });
        Assert.Equal(1, configureCalls);
        Assert.NotEmpty(calls); Assert.All(calls.Values, count => Assert.Equal(1, count));
        Assert.Equal(XElement.Parse(section.PreparedVisual.ToSvg(new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId("captured-section"), chart.Options.SvgColorVariables))).ToString(SaveOptions.DisableFormatting), EmbeddedSvg(section.Html).ToString(SaveOptions.DisableFormatting));
        var html = section.Html; var svg = section.PreparedVisual.ToSvg(); var png = section.PreparedVisual.ToPng();
        var countAfterPreparation = calls.Values.Sum();
        retained!.IdScope = "changed-section";
        chart.WithTitle("Changed").WithXAxisBounds(-100, 100).WithYAxisBounds(-100, 100);
        chart.Options.XAxis.Reversed = true; chart.Series[0].Points.Clear();
        Assert.Equal(html, section.Html); Assert.Equal(svg, section.PreparedVisual.ToSvg()); Assert.Equal(png, section.PreparedVisual.ToPng());
        Assert.Equal(countAfterPreparation, calls.Values.Sum());
        Assert.Equal(2, section.PreparedVisual.XProjection!.Minimum); Assert.False(section.PreparedVisual.XProjection.Reversed);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data-cfx-interactive-assets", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeSectionRecomputesXTicksKeepsFullDataYAndResetsThroughTheSameApi(bool reversed) {
        var chart = Line(); chart.Options.XAxis.Reversed = reversed;
        var renderer = new HtmlInteractiveChartRenderer();
        var full = renderer.PrepareSection(chart);
        var narrowed = renderer.PrepareSection(chart, new ChartXViewport(3, 7));
        var reset = renderer.PrepareSection(chart, viewport: null);
        Assert.Equal(AxisLabels(full.PreparedVisual, "axis-y-label"), AxisLabels(narrowed.PreparedVisual, "axis-y-label"));
        Assert.NotEqual(AxisLabels(full.PreparedVisual, "axis-x-label"), AxisLabels(narrowed.PreparedVisual, "axis-x-label"));
        Assert.Equal(full.Html, reset.Html); Assert.Equal(full.PreparedVisual.ToPng(), reset.PreparedVisual.ToPng());
        Assert.Equal(full.Html, renderer.RenderFragmentWithoutAssets(chart));
        Assert.Equal(0, chart.Options.XAxis.Minimum); Assert.Equal(10, chart.Options.XAxis.Maximum);
        using var metadata = Metadata(narrowed.Html);
        var facts = metadata.RootElement.GetProperty("xProjection"); var projection = narrowed.PreparedVisual.XProjection!;
        Assert.Equal(projection.FullMinimum, facts.GetProperty("fullMinimum").GetDouble());
        Assert.Equal(projection.FullMaximum, facts.GetProperty("fullMaximum").GetDouble());
        Assert.Equal(3, facts.GetProperty("minimum").GetDouble()); Assert.Equal(7, facts.GetProperty("maximum").GetDouble());
        Assert.Equal(reversed, facts.GetProperty("reversed").GetBoolean());
        var plot = facts.GetProperty("plotBounds");
        Assert.Equal(projection.PlotBounds.X, plot.GetProperty("x").GetDouble());
        Assert.Equal(projection.PlotBounds.Y, plot.GetProperty("y").GetDouble());
        Assert.Equal(projection.PlotBounds.Width, plot.GetProperty("width").GetDouble());
        Assert.Equal(projection.PlotBounds.Height, plot.GetProperty("height").GetDouble());
    }

    [Fact]
    public void OffWindowObservationsRetainStableSourceIdentityWithoutInventingVisibleMarkers() {
        var chart = Line();
        chart.Series[0].ConfigureMarkers(markers => markers.Enabled = true);
        chart.Series[0].WithObservationId(0, new ChartObservationId("before-window"))
            .WithObservationId(1, new ChartObservationId("inside-window"))
            .WithObservationId(2, new ChartObservationId("after-window"));
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart, new ChartXViewport(3, 7));
        var points = EmbeddedSvg(section.Html).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(new[] { "before-window", "inside-window", "after-window" }, points.Select(node => (string?)node.Attribute("data-cfx-observation-id")));
        Assert.Equal(new[] { "0", "1", "2" }, points.Select(node => (string?)node.Attribute("data-cfx-source-point")));
        Assert.All(points, point => Assert.Equal("observations", (string?)point.Attribute("data-cfx-series-key")));
        Assert.Empty(points[0].Elements()); Assert.NotEmpty(points[1].Elements()); Assert.Empty(points[2].Elements());
        Assert.Equal("false", (string?)points[0].Attribute("data-cfx-visible"));
        Assert.Null(points[1].Attribute("data-cfx-visible"));
        Assert.Equal("false", (string?)points[2].Attribute("data-cfx-visible"));
        Assert.Equal(3, section.PreparedVisual.Regions.Count(region => region.Role == "point"));
        using var metadata = Metadata(section.Html);
        var series = metadata.RootElement.GetProperty("series")[0];
        Assert.Equal("observations", series.GetProperty("key").GetString());
        Assert.Equal(chart.Series[0].SourcePointIndices, series.GetProperty("indices").EnumerateArray().Select(value => value.GetInt32()));
        chart.Series[0].ConfigureMarkers(markers => markers.Enabled = false);
        var markerFree = EmbeddedSvg(new HtmlInteractiveChartRenderer().PrepareSection(chart, new ChartXViewport(3, 7)).Html)
            .Descendants().Single(node => (string?)node.Attribute("data-cfx-observation-id") == "inside-window");
        Assert.Empty(markerFree.Elements());
        Assert.Null(markerFree.Attribute("data-cfx-visible"));
    }

    [Fact]
    public void UnsupportedAndEmptySectionsRemainRenderableWithoutAdvertisingANativeProjection() {
        var empty = Chart.Create().AddLine("Empty", Array.Empty<ChartPoint>());
        var bar = Chart.Create().AddBar("Bars", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) });
        var smooth = Line(); smooth.Series[0].WithSmooth();
        var sparkline = Line(); sparkline.Options.IsSparkline = true;
        var renderer = new HtmlInteractiveChartRenderer();
        foreach (var chart in new[] { empty, bar, smooth, sparkline }) {
            var section = renderer.PrepareSection(chart);
            Assert.Null(section.PreparedVisual.XProjection);
            using var metadata = Metadata(section.Html);
            Assert.False(metadata.RootElement.TryGetProperty("xProjection", out _));
            Assert.NotNull(EmbeddedSvg(section.Html));
        }
        Assert.Throws<InvalidOperationException>(() => renderer.PrepareSection(empty, new ChartXViewport(3, 7)));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareSection(bar, new ChartXViewport(3, 7)));
        Assert.Throws<ArgumentNullException>(() => renderer.PrepareSection(null!));
    }

    private static Chart Line() => Chart.Create().WithTitle("Captured observations").WithSize(520, 320).WithLegend(false)
        .WithXAxisBounds(0, 10)
        .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("observations"), "Observed", new[] { new ChartPoint(0, -100), new ChartPoint(5, 4), new ChartPoint(10, 100) }));

    private static string?[] AxisLabels(PreparedVisual visual, string role) => visual.Regions.Where(region => region.Role == role).Select(region => region.Label).ToArray();
    private static JsonDocument Metadata(string html) {
        var match = Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]+)\"");
        Assert.True(match.Success);
        return JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
    }
    private static XElement EmbeddedSvg(string html) {
        var start = html.IndexOf("<svg", StringComparison.Ordinal);
        var end = html.IndexOf("</svg>", start, StringComparison.Ordinal) + "</svg>".Length;
        return XElement.Parse(html.Substring(start, end - start));
    }
}
