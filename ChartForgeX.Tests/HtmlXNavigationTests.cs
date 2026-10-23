using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlXNavigationTests {
    [Fact]
    public void SourceAndFixedDurationPresetsValidateTheirPublicUnits() {
        Assert.Equal(2.5, new HtmlChartXRangePreset("Recent", 2.5).Span);
        Assert.Equal(.25, HtmlChartXRangePreset.FromDuration("6 hours", TimeSpan.FromHours(6)).Span);
        Assert.Throws<ArgumentException>(() => new HtmlChartXRangePreset(" ", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlChartXRangePreset("Invalid", double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlChartXRangePreset("Invalid", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlChartXRangePreset.FromDuration("Invalid", TimeSpan.FromHours(-1)));
    }

    [Fact]
    public void NavigatorIsOptInAndContainsAnIsolatedNativeFullDomainImage() {
        var chart = Chart.Create().WithXAxisBounds(0, 10).AddLine("Readings", new[] { new ChartPoint(0, 2), new ChartPoint(5, 8), new ChartPoint(10, 3) });
        var renderer = new HtmlInteractiveChartRenderer();
        Assert.DoesNotContain("data-cfx-x-navigation", renderer.PrepareSection(chart).Html);
        var full = renderer.PrepareSection(chart, configure: Configure);
        var detail = renderer.PrepareSection(chart, new ChartCartesianViewport(x: new ChartAxisWindow(4, 6)), Configure);
        Assert.Contains("data-cfx-x-navigation=\"true\"", detail.Html);
        Assert.Contains("data-cfx-x-range=\"start\"", detail.Html);
        Assert.Contains("data-cfx-x-range=\"end\"", detail.Html);
        Assert.Contains("data-cfx-x-range=\"position\"", detail.Html);
        Assert.Equal(Image(full.Html), Image(detail.Html));
        var native = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Image(detail.Html)));
        Assert.Contains("data-cfx-x=\"0\"", native); Assert.Contains("data-cfx-x=\"10\"", native);
        Assert.DoesNotContain("data-cfx-visible=\"false\"", native);
        Assert.Equal(4, detail.PreparedVisual.XProjection!.Minimum); Assert.Equal(6, detail.PreparedVisual.XProjection.Maximum);
        var controlsOnly = renderer.PrepareSection(chart, new ChartCartesianViewport(x: new ChartAxisWindow(4, 6)), options => {
            Configure(options); options.XNavigation.IncludeNavigator = false; options.XNavigation.IncludeScrollbar = false;
        }).Html;
        Assert.DoesNotContain("data:image/svg+xml", controlsOnly);
        Assert.DoesNotContain("data-cfx-x-range=\"position\"", controlsOnly);
    }

    [Fact]
    public void UnsupportedChartsOmitNativeNavigationAndControlMessagesAreLocalizable() {
        var chart = Chart.Create().AddPie("Categories", new[] { new ChartPoint(0, 2), new ChartPoint(1, 3) });
        var renderer = new HtmlInteractiveChartRenderer();
        var unsupported = renderer.PrepareSection(chart, configure: Configure);
        Assert.Null(unsupported.PreparedVisual.XProjection);
        Assert.DoesNotContain("data-cfx-x-navigation", unsupported.Html);
        var supported = Chart.Create().AddLine("Readings", new[] { new ChartPoint(0, 2), new ChartPoint(1, 3) });
        var html = renderer.PrepareSection(supported, configure: options => {
            Configure(options); options.Localization[HtmlChartText.XNavigation] = "Zakres danych";
            options.Localization[HtmlChartText.XRangeStart] = "Początek";
        }).Html;
        Assert.Contains("Zakres danych", html); Assert.Contains("Początek", html);
    }

    private static void Configure(HtmlChartInteractionOptions options) {
        options.XNavigation.Enabled = true;
        options.XNavigation.RangePresets.Add(new HtmlChartXRangePreset("Latest four", 4));
    }
    private static string Image(string html) => html.Split("data:image/svg+xml;base64,", StringSplitOptions.None)[1].Split('"')[0];
}
