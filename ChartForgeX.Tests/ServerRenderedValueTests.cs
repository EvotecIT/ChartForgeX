using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class ServerRenderedValueTests {
    [Theory]
    [InlineData(ChartSeriesKind.Line, false, false)]
    [InlineData(ChartSeriesKind.Bar, false, false)]
    [InlineData(ChartSeriesKind.Scatter, false, false)]
    [InlineData(ChartSeriesKind.Line, true, false)]
    [InlineData(ChartSeriesKind.Bar, true, false)]
    [InlineData(ChartSeriesKind.Scatter, true, false)]
    [InlineData(ChartSeriesKind.Bar, true, true)]
    public void ValueFramesKeepHostPoliciesAndMatchIndependentlyPreparedGeometry(ChartSeriesKind kind, bool linked, bool labels) {
        var options = new ChartMarkOptions(href: linked ? "/reading" : null, tooltip: "Reading details",
            accessibleLabel: "Open the current reading", cssClass: "host-mark",
            dataAttributes: new Dictionary<string, string> { ["data-record"] = "current" }, current: linked, selected: linked);
        Chart Model(double value) {
            var series = new ChartSeries(new ChartSeriesId("metrics"), "Metrics", kind, new[] {
                new ChartPoint(1, 10), new ChartPoint(2, value).WithMarkOptions(options), new ChartPoint(3, 30)
            }).WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("current"), new ChartObservationId("last") })
                .WithMarkOptions(new ChartMarkOptions(cssClass: "host-series"));
            return Chart.Create().WithSize(600, 320).WithFontFamily(BrowserProofFonts.Family).WithLegend(false)
                .WithXAxisBounds(0, 4).WithYAxisBounds(0, 100).WithDataLabels(labels).AddSeries(series);
        }
        var renderer = new HtmlInteractiveChartRenderer();
        var from = renderer.PrepareSection(Model(20)); var to = renderer.PrepareSection(Model(80));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        var expected = renderer.PrepareSection(Model(50)).PreparedVisual;
        Assert.Equal(expected.ToSvg("middle"), frames[1].ToSvg("middle"));
        Assert.Equal(expected.ToPng(), frames[1].ToPng());
        var mark = Assert.Single(XDocument.Parse(frames[1].ToSvg()).Descendants(), element => (string?)element.Attribute("class") == "host-mark");
        Assert.Equal("current", (string?)mark.Attribute("data-record"));
        Assert.Equal("Open the current reading", (string?)mark.Attribute("aria-label"));
        Assert.Equal(linked ? "/reading" : null, (string?)mark.Attribute("href"));
        Assert.Equal(linked ? "true" : null, (string?)mark.Attribute("aria-current"));
    }
}
