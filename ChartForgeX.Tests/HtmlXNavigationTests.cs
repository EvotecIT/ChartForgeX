using ChartForgeX.Core;
using ChartForgeX.Interactivity;
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
        var chart = Chart.Create().XAxis(x => x.Bounds(0, 10)).AddLine("Readings", new[] { new ChartPoint(0, 2), new ChartPoint(5, 8), new ChartPoint(10, 3) });
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

    private static Chart DailyReadings(Action<ChartAxis>? x = null) {
        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return Chart.Create().Size(800, 440).XAxis(axis => { axis.TimeScale(); x?.Invoke(axis); })
            .AddLine("Readings", Enumerable.Range(0, 60).Select(day => new ChartPoint(first.AddDays(day), 40 + day % 7)));
    }

    [Fact]
    public void SavedNavigatorSupportsAutomaticCalendarSeriesAndNamesEachDeclinedPolicy() {
        Assert.Empty(HtmlChartXNavigationSupport.DiagnoseStandalone(DailyReadings()));
        Assert.Empty(HtmlChartXNavigationSupport.DiagnoseStandalone(Chart.Create().AddBar("Counts", new[] { new ChartPoint(0, 2), new ChartPoint(1, 3) })));

        var pie = Assert.Single(HtmlChartXNavigationSupport.DiagnoseStandalone(Chart.Create().AddPie("Share", new[] { new ChartPoint(0, 2), new ChartPoint(1, 3) })));
        Assert.Equal(HtmlChartXNavigationIssue.UnsupportedSeriesKind, pie.Issue);
        Assert.Equal("Share", pie.SeriesName);

        var formatted = HtmlChartXNavigationSupport.DiagnoseStandalone(DailyReadings(axis => axis.LabelBy(value => "Day " + value)));
        Assert.Equal(HtmlChartXNavigationIssue.UnsupportedTimeAxis, Assert.Single(formatted).Issue);
        Assert.Contains("x time axis", formatted[0].Message);

        var banded = HtmlChartXNavigationSupport.DiagnoseStandalone(DailyReadings(axis => axis.Grid(grid => grid.ShowBands = true)));
        Assert.Contains(banded, issue => issue.Issue == HtmlChartXNavigationIssue.StaticTicks);

        var extra = DailyReadings();
        extra.AddValueAxis(ChartAxisPlacement.End, "Pressure", axis => axis.Bounds(0, 100));
        Assert.Contains(HtmlChartXNavigationSupport.DiagnoseStandalone(extra), issue => issue.Issue == HtmlChartXNavigationIssue.AdditionalValueAxes);
        Assert.Throws<ArgumentNullException>(() => HtmlChartXNavigationSupport.DiagnoseStandalone(null!));
    }

    [Fact]
    public void SavedNavigatorSelectsTheStandaloneReaderAndMarksDeclinedCharts() {
        var renderer = new HtmlInteractiveChartRenderer();
        var page = DailyReadings().ToInteractiveHtmlPage(options => {
            options.Runtime = HtmlChartRuntime.Standalone;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.KeyboardNavigation;
            Configure(options);
        });
        Assert.Contains(HtmlInteractiveAssetFiles.ChartScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.ReportReview).Content, page);
        Assert.DoesNotContain("data-cfx-x-standalone-issues", page);
        // Without the navigator the same tooltip page keeps the smaller core reader.
        var reader = DailyReadings().ToInteractiveHtmlPage(options => {
            options.Runtime = HtmlChartRuntime.Standalone; options.Interaction.Features = ChartInteractionFeatures.Tooltips;
        });
        Assert.Contains(HtmlInteractiveAssetFiles.ChartScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.Tooltips).Content, reader);

        var declined = renderer.PrepareSection(DailyReadings(axis => axis.LabelBy(value => "Day " + value)), configure: Configure).Html;
        Assert.Contains("data-cfx-x-standalone-issues=\"UnsupportedTimeAxis\"", declined);
        Assert.Contains("data-cfx-x-navigation=\"true\"", declined);
    }

    [Fact]
    public void SavedNavigatorExampleCarriesSupportedControlsAndPresets() {
        var page = HtmlSavedNavigatorExamples.CreatePage(dark: true);
        Assert.Contains("data-cfx-x-navigation=\"true\"", page);
        Assert.DoesNotContain("data-cfx-x-standalone-issues", page);
        Assert.Contains(">1D</button>", page); Assert.Contains(">1M</button>", page);
        Assert.Contains(HtmlInteractiveAssetFiles.ChartScriptFor(HtmlChartRuntime.Standalone, ChartInteractionFeatures.ReportReview).Content, page);
    }

    private static void Configure(HtmlChartInteractionOptions options) {
        options.XNavigation.Enabled = true;
        options.XNavigation.RangePresets.Add(new HtmlChartXRangePreset("Latest four", 4));
    }
    private static string Image(string html) => html.Split("data:image/svg+xml;base64,", StringSplitOptions.None)[1].Split('"')[0];
}
