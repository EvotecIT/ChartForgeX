using System;
using System.Globalization;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgEscapesText() {
        var svg = SampleChart().ToSvg();
        Assert(svg.StartsWith("<svg", StringComparison.Ordinal), "SVG should start with the svg element.");
        Assert(svg.Contains("A &lt; B &amp; C", StringComparison.Ordinal), "SVG should escape text content.");
        Assert(svg.Contains("<clipPath", StringComparison.Ordinal), "SVG should clip plotted series to the plot area.");
    }

    private static void AreaBaselineDoesNotPolluteXAxis() {
        var svg = Chart.Create().WithSize(640, 360).AddArea("Passed", Points(100, 180, 260)).ToSvg();
        foreach (var line in svg.Split('\n')) {
            if (line.Contains("text-anchor=\"middle\"", StringComparison.Ordinal) && line.Contains(">0</text>", StringComparison.Ordinal)) {
                throw new InvalidOperationException("Area baseline created an unwanted x-axis zero tick.");
            }
        }
    }

    private static void XAxisLabelsRender() {
        var svg = SampleChart().ToSvg();
        Assert(svg.Contains(">Mon</text>", StringComparison.Ordinal), "SVG should render explicit x-axis labels.");
        Assert(svg.Contains(">Tue</text>", StringComparison.Ordinal), "SVG should render explicit x-axis labels.");
    }

    private static void SmoothSeriesRenderAsBezierPaths() {
        Assert(SampleChart().ToSvg().Contains(" C ", StringComparison.Ordinal), "Smooth series should render cubic Bezier path segments.");
    }

    private static void DataLabelsRenderWhenEnabled() {
        var svg = Chart.Create().WithSize(640, 360).WithDataLabels().AddBar("Values", Points(42, 84, 126)).ToSvg();
        Assert(svg.Contains(">42</text>", StringComparison.Ordinal), "Data labels should render numeric values.");
        Assert(svg.Contains(">126</text>", StringComparison.Ordinal), "Data labels should render numeric values.");
    }

    private static void DataLabelsUseReadableEdgeAwareStyling() {
        var svg = Chart.Create().WithSize(420, 280).WithDataLabels().AddLine("Values", Points(1000, 900, 1000)).ToSvg();
        Assert(svg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Data labels should be identifiable in SVG output.");
        Assert(svg.Contains("paint-order=\"stroke fill\"", StringComparison.Ordinal), "Data labels should render with a text halo for readability.");
        Assert(svg.Contains("dominant-baseline=\"middle\"", StringComparison.Ordinal), "Data labels should use stable vertical alignment.");
    }

    private static void CustomValueFormatterAffectsSvgValues() {
        var svg = Chart.Create()
            .WithSize(520, 320)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + " ms")
            .AddBar("Latency", Points(42, 84, 126))
            .ToSvg();
        Assert(svg.Contains(">42 ms</text>", StringComparison.Ordinal), "Custom value formatters should apply to data labels.");
        Assert(svg.Contains(">0 ms</text>", StringComparison.Ordinal), "Custom value formatters should apply to y-axis tick labels.");
    }

    private static void LongFormattedYAxisLabelsReservePlotSpace() {
        var svg = Chart.Create()
            .WithSize(420, 280)
            .WithValueFormatter(value => "$" + value.ToString("N0", CultureInfo.InvariantCulture) + " ms")
            .AddLine("Latency budget", Points(1000000, 1120000, 1080000))
            .ToSvg();
        Assert(GetAttribute(svg, "<clipPath", "x") > 76, "Long formatted y-axis labels should push the SVG plot area to the right.");
        Assert(svg.Contains("$1,000,000 ms", StringComparison.Ordinal) || svg.Contains("$1,200,000 ms", StringComparison.Ordinal), "Long formatted y-axis labels should render.");
    }

    private static void AnnotationsRenderInSvg() {
        var svg = Chart.Create()
            .WithSize(640, 360)
            .AddLine("Values", Points(42, 84, 126))
            .AddHorizontalLine(100, "target", ChartColor.FromRgb(251, 191, 36))
            .AddVerticalBand(1.5, 2.5, "window", ChartColor.FromRgb(96, 165, 250), 0.1)
            .ToSvg();
        Assert(svg.Contains(">target</text>", StringComparison.Ordinal), "Horizontal annotation label should render.");
        Assert(svg.Contains(">window</text>", StringComparison.Ordinal), "Band annotation label should render.");
        Assert(svg.Contains("stroke-dasharray=\"6 5\"", StringComparison.Ordinal), "Line annotations should render as dashed lines.");
    }

    private static void AnnotationLabelsStayInsidePlot() {
        var svg = Chart.Create().WithSize(420, 280).AddLine("Values", Points(10, 20, 30)).AddVerticalLine(3, "right edge marker", ChartColor.FromRgb(251, 191, 36)).ToSvg();
        Assert(svg.Contains("data-cfx-role=\"annotation-label\"", StringComparison.Ordinal), "Annotation label pills should be identifiable in SVG output.");
        Assert(svg.Contains(">right edge marker</text>", StringComparison.Ordinal), "Annotation label text should render.");
        Assert(svg.Contains("text-anchor=\"end\"", StringComparison.Ordinal), "Right-edge annotation labels should switch to end alignment.");
    }

    private static void SvgIncludesAccessibilityMetadata() {
        var svg = SampleChart().ToSvg();
        Assert(svg.Contains("role=\"img\"", StringComparison.Ordinal), "SVG should expose image semantics.");
        Assert(svg.Contains("aria-labelledby=\"", StringComparison.Ordinal), "SVG should reference title and description metadata.");
        Assert(svg.Contains("<title id=\"", StringComparison.Ordinal), "SVG should include a title element.");
        Assert(svg.Contains("<desc id=\"", StringComparison.Ordinal), "SVG should include a description element.");
    }

    private static void SvgUsesReportGradeStyling() {
        var svg = Chart.Create().WithTitle("Styled").WithTheme(ChartTheme.ReportDark()).WithSize(640, 360)
            .AddSmoothLine("Values", Points(10, 30, 20), ChartColor.FromRgb(96, 165, 250))
            .AddHorizontalLine(25, "target", ChartColor.FromRgb(251, 191, 36))
            .ToSvg();
        Assert(svg.Contains("-seriesFill0", StringComparison.Ordinal), "SVG should include series fill gradients.");
        Assert(svg.Contains("stroke-opacity=\"0.34\"", StringComparison.Ordinal), "Annotation labels should render as legible pills.");
        Assert(svg.Contains("font-weight=\"750\"", StringComparison.Ordinal), "SVG should use stronger title and label typography.");
    }

    private static void TypographyUsesNativeFontStackAndEscapesCustomFamilies() {
        Assert(SampleChart().ToSvg().Contains("-apple-system, BlinkMacSystemFont", StringComparison.Ordinal), "SVG should default to a native system font stack.");
        var svg = Chart.Create().WithFontFamily("A&B \"Display\"").AddLine("Values", Points(1, 2, 3)).ToSvg();
        Assert(svg.Contains("font-family=\"A&amp;B &quot;Display&quot;\"", StringComparison.Ordinal), "SVG font-family values should be attribute-escaped.");
        var html = Chart.Create().WithFontFamily("A;B{}").AddLine("Values", Points(1, 2, 3)).ToHtmlPage();
        Assert(!html.Contains("font-family:A;B{}", StringComparison.Ordinal), "HTML font-family values should not be able to break the style declaration.");
    }

    private static void RenderingUsesInvariantCulture() {
        var currentCulture = CultureInfo.CurrentCulture;
        var currentUiCulture = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
            CultureInfo.CurrentUICulture = new CultureInfo("pl-PL");
            var css = ChartColor.FromRgba(1, 2, 3, 128).ToCss();
            Assert(css == "rgba(1,2,3,0.502)", "CSS alpha values should use invariant decimal separators.");
            var png = Chart.Create().AddDonut("Checks", Points(70, 20, 10)).ToPng();
            Assert(png.Length > 64, "PNG rendering should stay valid under non-invariant cultures.");
        } finally {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUiCulture;
        }
    }

    private static void ReportThemesExposeVisualTokens() {
        var theme = ChartTheme.ReportDark();
        Assert(theme.CardBorder.A > 0, "Report themes should define card borders.");
        Assert(theme.PlotBorder.A > 0, "Report themes should define plot borders.");
        Assert(theme.PlotCornerRadius > 0, "Report themes should define plot corner radius.");
        Assert(theme.Positive.A > 0 && theme.Warning.A > 0 && theme.Negative.A > 0, "Report themes should define semantic status colors.");
        Assert(theme.TitleFontSize > theme.TickLabelFontSize, "Title text should be larger than tick labels.");
    }

    private static void StandaloneHtmlUsesVisibleBackground() {
        var html = SampleChart().ToHtmlPage();
        Assert(!html.Contains("background:transparent", StringComparison.Ordinal), "Standalone pages should use a visible page background.");
        Assert(html.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal), "Standalone pages should request browser font smoothing.");
    }

    private static void HtmlFragmentIsResponsive() {
        var html = SampleChart().ToHtmlFragment();
        Assert(html.Contains("style=\"width:100%;max-width:640px;box-sizing:border-box\"", StringComparison.Ordinal), "HTML fragment should carry responsive wrapper styles.");
        Assert(html.Contains("style=\"max-width:100%;height:auto;display:block\"", StringComparison.Ordinal), "SVG should carry responsive sizing styles.");
    }

    private static void DenseXAxisLabelsAreAutomaticallyReduced() {
        var labels = Enumerable.Range(1, 20).Select(value => "Checkpoint " + value.ToString("00", CultureInfo.InvariantCulture)).ToArray();
        var auto = Chart.Create().WithSize(420, 280).WithXLabels(labels).AddLine("Values", Points(Enumerable.Range(1, 20).Select(value => (double)value).ToArray())).ToSvg();
        Assert(auto.Contains(">Checkpoint 01</text>", StringComparison.Ordinal), "Automatic x-axis label thinning should preserve the first label.");
        Assert(auto.Contains(">Checkpoint 20</text>", StringComparison.Ordinal), "Automatic x-axis label thinning should preserve the last label.");
        Assert(!auto.Contains(">Checkpoint 02</text>", StringComparison.Ordinal), "Automatic x-axis label thinning should omit intermediate labels when space is tight.");
        var all = Chart.Create().WithSize(420, 280).WithXAxisLabelDensity(ChartLabelDensity.All).WithXLabels(labels).AddLine("Values", Points(Enumerable.Range(1, 20).Select(value => (double)value).ToArray())).ToSvg();
        Assert(all.Contains(">Checkpoint 02</text>", StringComparison.Ordinal), "All label density should preserve every explicit x-axis label.");
    }

    private static void EdgeXAxisLabelsStayInsidePlot() {
        var svg = Chart.Create().WithSize(420, 280).WithXAxisLabelDensity(ChartLabelDensity.All).WithXLabels("January", "February", "March", "April", "May", "December").AddLine("Values", Points(10, 20, 15, 30, 24, 35)).ToSvg();
        Assert(svg.Contains("text-anchor=\"start\"", StringComparison.Ordinal) && svg.Contains(">January</text>", StringComparison.Ordinal), "First x-axis label should be start-aligned.");
        Assert(svg.Contains("text-anchor=\"end\"", StringComparison.Ordinal) && svg.Contains(">December</text>", StringComparison.Ordinal), "Last x-axis label should be end-aligned.");
    }

    private static void XAxisLabelsCanBeRotated() {
        var svg = Chart.Create().WithSize(520, 340).WithXAxis("Month").WithXAxisLabelAngle(-35).WithXAxisLabelDensity(ChartLabelDensity.All).WithXLabels("January", "February", "March").AddLine("Values", Points(10, 20, 30)).ToSvg();
        Assert(svg.Contains("transform=\"rotate(-35", StringComparison.Ordinal), "SVG should rotate x-axis labels when requested.");
        Assert(svg.Contains("dominant-baseline=\"middle\"", StringComparison.Ordinal), "Rotated labels should use a stable text baseline.");
    }

    private static void LargeSvgValuesUseCompactUnits() {
        var svg = Chart.Create().WithSize(640, 360).AddLine("Values", Points(1200000, 2400000, 3600000)).ToSvg();
        Assert(svg.Contains(">1M</text>", StringComparison.Ordinal) || svg.Contains(">1.2M</text>", StringComparison.Ordinal), "Large SVG values should use M suffixes instead of thousands of k.");
    }

    private static void LegendRowsWrapWithRoleMarkers() {
        var svg = Chart.Create().WithSize(420, 320)
            .AddLine("Primary domain checks", Points(1, 2, 3))
            .AddLine("Certificate transparency drift", Points(2, 3, 4))
            .AddLine("Dnssec policy posture", Points(3, 4, 5))
            .AddLine("Mail authentication alignment", Points(4, 5, 6))
            .ToSvg();
        Assert(svg.Contains("data-cfx-role=\"legend\"", StringComparison.Ordinal), "SVG should expose a semantic legend group.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"legend-row\"") > 1, "Long legends should wrap into multiple rows.");
    }

    private static void SvgHasNoInvalidNumbers() {
        var svg = SampleChart().ToSvg();
        Assert(!svg.Contains("NaN", StringComparison.Ordinal), "SVG should not contain NaN values.");
        Assert(!svg.Contains("Infinity", StringComparison.Ordinal), "SVG should not contain infinity values.");
    }

    private static void DateXAxisLabelsRender() {
        var start = new DateTime(2026, 1, 1);
        var dates = new[] { start, start.AddDays(1), start.AddDays(2) };
        var svg = Chart.Create().WithSize(640, 360).WithXDateLabels(dates, "MMM dd").AddLine("Values", DatePoints(dates, 10, 20, 30)).ToSvg();
        Assert(svg.Contains(">Jan 01</text>", StringComparison.Ordinal), "Date x-axis labels should render.");
        Assert(svg.Contains(">Jan 03</text>", StringComparison.Ordinal), "Date x-axis labels should render.");
    }

    private static void SparklineHidesReportChrome() {
        var chart = Chart.Create().WithTitle("Tiny trend").WithSize(240, 64).WithSparkline().AddSmoothArea("Trend", Points(10, 14, 13, 19, 24, 22));
        var svg = chart.ToSvg();
        Assert(chart.Options.IsSparkline, "Sparkline option should be enabled.");
        Assert(!svg.Contains(">Tiny trend</text>", StringComparison.Ordinal), "Sparkline should not render visible title text.");
        Assert(!svg.Contains("font-size=\"11\">0</text>", StringComparison.Ordinal), "Sparkline should not render axis tick labels.");
    }

    private static void PublicApiRejectsInvalidInputs() {
        AssertThrows<ArgumentNullException>(() => Chart.Create().WithTitle(null!), "Chart titles should reject null values.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().Title = null!, "Chart title property should reject null values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithSize(0, 360), "Chart sizes should reject non-positive dimensions.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPadding(1, double.NaN, 1, 1), "Chart padding should reject non-finite values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTickCount(1), "Tick counts should reject values below two.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().Options.Theme = null!, "Chart options should reject null themes.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.TickCount = 1, "Chart options should reject invalid tick counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.XAxisLabelDensity = (ChartLabelDensity)999, "Chart options should reject unknown label density values.");
        AssertThrows<ArgumentException>(() => Chart.Create().WithPngFont(" "), "PNG font paths should reject empty values.");
        AssertThrows<ArgumentException>(() => Chart.Create().WithPngFont("font.ttc", faceName: " "), "PNG font face names should reject empty values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPngFont("font.ttc", -1), "PNG font collection indexes should reject negative values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartSize(-1, 100), "ChartSize should reject non-positive dimensions.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartPadding.All(double.NegativeInfinity), "ChartPadding should reject non-finite values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartPoint(1, double.NaN), "ChartPoint should reject non-finite values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartRect(0, 0, -1, 10), "ChartRect should reject negative dimensions.");
        AssertThrows<ArgumentNullException>(() => new ChartAxisLabel(1, null!), "Axis labels should reject null text.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartAxisLabel(double.NaN, "bad"), "Axis labels should reject non-finite values.");
        AssertThrows<ArgumentNullException>(() => ChartTheme.Light().Palette = null!, "Themes should reject null palettes.");
        AssertThrows<ArgumentException>(() => ChartTheme.Light().Palette = Array.Empty<ChartColor>(), "Themes should reject empty palettes.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartTheme.Light().TitleFontSize = 0, "Themes should reject non-positive font sizes.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartTheme.Light().ShadowOpacity = 2, "Themes should reject opacity values outside zero to one.");
        AssertThrows<ArgumentNullException>(() => ChartTheme.Light().FontFamily = null!, "Themes should reject null font families.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddLine("Values", new[] { new ChartPoint(1, double.PositiveInfinity) }), "Series should reject non-finite point values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddGauge("Score", 80, 100, 0), "Gauges should reject inverted scales.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddTimelineRange("Task", 10, 2), "Timelines should reject inverted ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHorizontalBand(1, 2, opacity: 1.5), "Band opacity should reject values outside zero to one.");
        AssertThrows<ArgumentNullException>(() => ChartExtensions.GetPngFontInfo(null!), "PNG font diagnostics should reject null charts.");
    }

    private static void SpecializedChartsRejectMixedSeries() {
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddGauge("Score", 87).AddLine("Trend", Points(1, 2, 3)).ToSvg(), "SVG rendering should reject mixed specialized and cartesian series.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddDonut("Checks", Points(70, 20)).AddPie("Other", Points(1, 2)).ToPng(), "PNG rendering should reject multiple pie-like series.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddGauge("Score", 87).AddGauge("Other", 72).ToSvg(), "Single-panel specialized charts should reject multiple series.");
    }
}
