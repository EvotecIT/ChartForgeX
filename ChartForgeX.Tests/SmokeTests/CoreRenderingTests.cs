using System;
using System.Globalization;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Html;
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
        var svg = Chart.Create()
            .WithSize(420, 260)
            .AddSmoothLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235))
            .ToSvg();
        Assert(svg.Contains(" C ", StringComparison.Ordinal), "Smooth series should render cubic Bezier path segments.");
        Assert(svg.Contains("data-cfx-role=\"line-ambient-halo\"", StringComparison.Ordinal), "SVG line series should render the shared premium ambient halo.");
        Assert(svg.Contains("data-cfx-role=\"line-highlight\"", StringComparison.Ordinal), "SVG line series should render the shared premium highlight sheen.");
    }

    private static void LineVisualStyleIsReusableAndConfigurable() {
        var style = ChartLineVisualStyle.Premium()
            .WithAmbientHalo(0.07, 12)
            .WithHalo(0.22, 6)
            .WithHighlight(0.31, 0.4);
        var chart = Chart.Create()
            .WithSize(420, 260)
            .WithLineVisualStyle(style)
            .AddLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235));
        style.WithHighlight(0.02);

        var svg = chart.ToSvg();
        Assert(chart.Options.LineVisualStyle.HighlightOpacity == 0.31, "Charts should clone reusable line style instances so later caller changes do not mutate chart output.");
        Assert(svg.Contains("data-cfx-role=\"line-ambient-halo\"", StringComparison.Ordinal) && svg.Contains("opacity=\"0.07\"", StringComparison.Ordinal), "Reusable line styles should control ambient halo opacity.");
        Assert(svg.Contains("data-cfx-role=\"line-highlight\"", StringComparison.Ordinal) && svg.Contains("opacity=\"0.31\"", StringComparison.Ordinal), "Reusable line styles should control highlight opacity.");

        var classicSvg = Chart.Create()
            .WithSize(420, 260)
            .WithLineVisualStyle(ChartLineVisualStyle.Classic())
            .AddLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235))
            .ToSvg();
        Assert(!classicSvg.Contains("line-ambient-halo", StringComparison.Ordinal) && !classicSvg.Contains("line-highlight", StringComparison.Ordinal), "Classic line style should suppress premium-only line lighting layers.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartLineVisualStyle.Premium().WithHighlight(0.5, 0), "Line highlight stroke ratio should reject zero.");
    }

    private static void PngScatterDoesNotConnectPoints() {
        var chart = Chart.Create()
            .WithSize(220, 80)
            .WithSparkline()
            .AddScatter("Only points", new[] { new ChartPoint(1, 1), new ChartPoint(4, 1) }, ChartColor.FromRgb(37, 99, 235));
        var pixels = ReadPngRgba(chart.ToPng(), out var width, out var height);
        var markerPixels = CountNearColor(pixels, 37, 99, 235, 24);
        var centerLinePixels = CountNearColorInRect(pixels, width, width / 2 - 24, height / 2 - 10, 48, 20, 37, 99, 235, 24);
        Assert(markerPixels > 20, "PNG scatter charts should render visible markers.");
        Assert(centerLinePixels == 0, $"PNG scatter charts should not connect independent points. Center line pixels: {centerLinePixels}.");
    }

    private static void StepLineSeriesRenderAsStairSteps() {
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 30), new ChartPoint(3, 18), new ChartPoint(4, 42) };
        var line = Chart.Create().WithSize(420, 260).AddLine("Values", points);
        var stepLine = Chart.Create().WithSize(420, 260).AddStepLine("Values", points);
        var lineSvg = line.ToSvg();
        var stepSvg = stepLine.ToSvg();
        Assert(CountOccurrences(stepSvg, " L ") > CountOccurrences(lineSvg, " L "), "SVG step lines should add horizontal and vertical stair-step segments.");
        Assert(!line.ToPng().SequenceEqual(stepLine.ToPng()), "PNG step lines should rasterize differently from straight line series.");
    }

    private static void StepAreaSeriesRenderAsStairStepAreas() {
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 30), new ChartPoint(3, 18), new ChartPoint(4, 42) };
        var area = Chart.Create().WithSize(420, 260).AddArea("Values", points);
        var stepArea = Chart.Create().WithSize(420, 260).WithDataLabels().AddStepArea("Values", points, ChartColor.FromRgb(37, 99, 235));
        var areaSvg = area.ToSvg();
        var stepAreaSvg = stepArea.ToSvg();
        Assert(stepArea.Series[0].Kind == ChartSeriesKind.StepArea, "Step areas should use their own series kind.");
        Assert(stepAreaSvg.Contains("data-cfx-role=\"step-area\"", StringComparison.Ordinal), "SVG step areas should expose a filled step-area role.");
        Assert(stepAreaSvg.Contains("data-cfx-role=\"step-area-line\"", StringComparison.Ordinal), "SVG step areas should expose a readable boundary role.");
        Assert(stepAreaSvg.Contains("data-cfx-role=\"step-area-line-highlight\"", StringComparison.Ordinal), "SVG step-area boundaries should use the shared premium line highlight.");
        Assert(CountOccurrences(stepAreaSvg, " L ") > CountOccurrences(areaSvg, " L "), "SVG step areas should add horizontal and vertical stair-step area segments.");
        Assert(stepAreaSvg.Contains(">42</text>", StringComparison.Ordinal), "Step-area data labels should render values when enabled.");
        Assert(!area.ToPng().SequenceEqual(stepArea.ToPng()), "PNG step areas should rasterize differently from straight area series.");
    }

    private static void DataLabelsRenderWhenEnabled() {
        var svg = Chart.Create().WithSize(640, 360).WithDataLabels().AddBar("Values", Points(42, 84, 126)).ToSvg();
        Assert(svg.Contains(">42</text>", StringComparison.Ordinal), "Data labels should render numeric values.");
        Assert(svg.Contains(">126</text>", StringComparison.Ordinal), "Data labels should render numeric values.");
    }

    private static void PointCalloutsUseCustomPointLabels() {
        var chart = Chart.Create()
            .WithSize(480, 300)
            .AddLine("MRR", Points(100, 112, 119), ChartColor.FromRgb(37, 99, 235))
            .AddPointCallout("$119,000 MRR", 3, 119, ChartColor.FromRgb(37, 99, 235));

        var svg = chart.ToSvg();
        Assert(svg.Contains(">$119,000 MRR</text>", StringComparison.Ordinal), "Point callouts should render custom data-label text.");
        Assert(svg.Contains("data-cfx-role=\"point-callout\"", StringComparison.Ordinal), "Point callouts should expose a stable semantic SVG role.");
        Assert(!svg.Contains(">119</text>", StringComparison.Ordinal), "Point callouts should replace the formatted point value with the custom label.");
        Assert(!svg.Contains("data-cfx-role=\"legend-item\" data-cfx-series=\"1\"", StringComparison.Ordinal), "Point callouts should not add dashboard-only highlights to the legend.");
        Assert(svg.Contains("with 1 data series: MRR.", StringComparison.Ordinal), "Point callouts should not inflate generic SVG data-series descriptions.");
        Assert(chart.ToPng().Length > 64, "Point callouts should render PNG output.");

        chart.Series[1].UseFormattedPointLabel(0);
        Assert(chart.ToSvg().Contains(">119</text>", StringComparison.Ordinal), "Clearing a point label should restore formatted values.");
    }

    private static void SeriesDataLabelOverridesAreHonored() {
        var chart = Chart.Create()
            .WithSize(420, 280)
            .WithDataLabels()
            .AddBar("Visible", Points(42), ChartColor.FromRgb(37, 99, 235))
            .AddLine("Hidden", Points(84), ChartColor.FromRgb(245, 158, 11));
        chart.Series[1].WithDataLabels(false);

        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"data-label\"") == 1, "Series data-label overrides should hide labels for one series while preserving chart-level labels for others.");

        chart.Series[1].UseChartDataLabels();
        Assert(CountOccurrences(chart.ToSvg(), "data-cfx-role=\"data-label\"") == 2, "Clearing a series data-label override should restore chart-level label behavior.");
    }

    private static void DensePointLabelsAvoidCollisions() {
        var points = new[] {
            new ChartPoint(1, 50),
            new ChartPoint(1, 50.1),
            new ChartPoint(1, 50.2),
            new ChartPoint(1, 50.3),
            new ChartPoint(1, 50.4)
        };
        var svg = Chart.Create().WithSize(320, 220).WithDataLabels().AddScatter("Dense", points).ToSvg();
        var labelCount = CountOccurrences(svg, "data-cfx-role=\"data-label\"");
        Assert(labelCount > 0 && labelCount < points.Length, "Dense point labels should render a useful subset instead of overlapping every label.");
    }

    private static void DataLabelsUseReadableEdgeAwareStyling() {
        var svg = Chart.Create().WithSize(420, 280).WithDataLabels().AddLine("Values", Points(1000, 900, 1000)).ToSvg();
        Assert(svg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Data labels should be identifiable in SVG output.");
        Assert(svg.Contains("paint-order=\"stroke fill\"", StringComparison.Ordinal), "Data labels should render with a text halo for readability.");
        Assert(svg.Contains("dominant-baseline=\"middle\"", StringComparison.Ordinal), "Data labels should use stable vertical alignment.");

        var longSvg = Chart.Create()
            .WithSize(220, 140)
            .WithDataLabels()
            .WithValueFormatter(_ => "Extremely long remediation status label that must fit")
            .AddBar("Values", Points(72))
            .ToSvg();
        Assert(longSvg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Long SVG data labels should still render as identifiable data labels.");
        Assert(longSvg.Contains("...</text>", StringComparison.Ordinal), "SVG data labels should shorten formatter output that cannot fit inside the plot.");
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

    private static void CustomXAxisFormatterAffectsSvgAndPngTicks() {
        var chart = Chart.Create()
            .WithSize(520, 320)
            .WithXAxisBounds(0, 4)
            .WithTickCount(5)
            .WithXAxisValueFormatter(value => "D+" + value.ToString("0", CultureInfo.InvariantCulture))
            .AddLine("Incidents", new[] { new ChartPoint(0, 12), new ChartPoint(2, 18), new ChartPoint(4, 16) });
        var svg = chart.ToSvg();
        Assert(svg.Contains(">D+0</text>", StringComparison.Ordinal), "Custom x-axis formatters should apply to generated SVG x-axis tick labels.");
        Assert(svg.Contains(">D+4</text>", StringComparison.Ordinal), "Custom x-axis formatters should apply to generated SVG x-axis tick labels.");
        Assert(chart.ToPng().Length > 64, "Custom x-axis formatters should render valid PNG output.");

        var labeled = Chart.Create()
            .WithSize(420, 280)
            .WithXAxisValueFormatter(_ => "formatted")
            .WithXLabels("Alpha", "Beta")
            .AddBar("Values", Points(10, 20))
            .ToSvg();
        Assert(labeled.Contains(">Alpha</text>", StringComparison.Ordinal), "Explicit x-axis labels should take precedence over generated x-axis formatters.");
        Assert(!labeled.Contains(">formatted</text>", StringComparison.Ordinal), "Explicit x-axis labels should not be replaced by generated x-axis formatters.");

        var horizontal = Chart.Create()
            .WithSize(520, 320)
            .WithXAxisBounds(0, 100)
            .WithTickCount(6)
            .WithXAxisValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .WithXLabels("Critical", "High")
            .AddHorizontalBar("Risk", Points(70, 40))
            .ToSvg();
        Assert(horizontal.Contains(">100%</text>", StringComparison.Ordinal), "Horizontal bar value axes should use x-axis formatters.");
        Assert(horizontal.Contains(">Critical</text>", StringComparison.Ordinal), "Horizontal bar categories should still use explicit category labels.");

        var plainRotated = Chart.Create()
            .WithSize(420, 280)
            .WithLegend(false)
            .WithXAxis("Elapsed")
            .WithXAxisBounds(0, 4)
            .WithTickCount(5)
            .WithXAxisLabelAngle(-55)
            .AddLine("Incidents", new[] { new ChartPoint(0, 12), new ChartPoint(2, 18), new ChartPoint(4, 16) })
            .ToSvg();
        var longRotatedChart = Chart.Create()
            .WithSize(420, 280)
            .WithLegend(false)
            .WithXAxis("Elapsed")
            .WithXAxisBounds(0, 4)
            .WithTickCount(5)
            .WithXAxisLabelAngle(-55)
            .WithXAxisValueFormatter(value => "Checkpoint " + value.ToString("0", CultureInfo.InvariantCulture))
            .AddLine("Incidents", new[] { new ChartPoint(0, 12), new ChartPoint(2, 18), new ChartPoint(4, 16) });
        var longRotated = longRotatedChart.ToSvg();
        Assert(GetAttribute(longRotated, "<clipPath", "height") < GetAttribute(plainRotated, "<clipPath", "height"), "Rotated generated x-axis labels should reserve more SVG bottom space when formatting makes them longer.");
        Assert(longRotatedChart.ToPng().Length > 64, "Rotated generated x-axis labels should render valid PNG output when formatting makes them longer.");
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

    private static void LongAxisTitlesFitAvailableSpace() {
        const string longTitle = "Extremely long remediation status axis title that must fit inside the chart";
        var svg = Chart.Create()
            .WithSize(240, 180)
            .WithXAxis(longTitle)
            .WithYAxis(longTitle)
            .AddLine("Values", Points(10, 20, 30))
            .ToSvg();
        Assert(svg.Contains("...</text>", StringComparison.Ordinal), "SVG axis titles should shorten when the chart cannot fit the full title.");
        Assert(Chart.Create().WithSize(240, 180).WithXAxis(longTitle).WithYAxis(longTitle).AddLine("Values", Points(10, 20, 30)).ToPng().Length > 64, "PNG axis titles should render valid output when long titles require fitting.");
    }

    private static void LongHeaderTextFitsAvailableSpace() {
        const string longTitle = "Extremely long remediation posture report title that should fit inside the chart header";
        const string longSubtitle = "Detailed subtitle with enough operational context to exceed a compact chart width";
        var chart = Chart.Create()
            .WithSize(260, 180)
            .WithTitle(longTitle)
            .WithSubtitle(longSubtitle)
            .AddLine("Values", Points(10, 20, 30));
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"chart-title\"", StringComparison.Ordinal), "SVG header titles should expose a stable role marker.");
        Assert(svg.Contains("data-cfx-role=\"chart-subtitle\"", StringComparison.Ordinal), "SVG header subtitles should expose a stable role marker.");
        Assert(svg.Contains("...</text>", StringComparison.Ordinal), "SVG header text should shorten when it cannot fit in the chart width.");
        Assert(chart.ToPng().Length > 64, "PNG header text should render valid output when long title and subtitle fitting is required.");
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

    private static void StatisticalOverlaysRenderComputedAnnotations() {
        var points = Points(10, 20, 30, 40);
        var chart = Chart.Create()
            .WithSize(640, 360)
            .AddLine("Values", points)
            .AddMeanLine("mean", points, ChartColor.FromRgb(245, 158, 11))
            .AddMedianLine("median", points, ChartColor.FromRgb(14, 165, 233))
            .AddStandardDeviationBand("1 sigma", points, 1, ChartColor.FromRgb(96, 165, 250), 0.16);
        var svg = chart.ToSvg();
        Assert(svg.Contains(">mean</text>", StringComparison.Ordinal), "Mean overlays should render line labels.");
        Assert(svg.Contains(">median</text>", StringComparison.Ordinal), "Median overlays should render line labels.");
        Assert(svg.Contains(">1 sigma</text>", StringComparison.Ordinal), "Standard deviation bands should render band labels.");
        Assert(svg.Contains("opacity=\"0.16\"", StringComparison.Ordinal), "Standard deviation bands should use the requested opacity.");
        Assert(CountOccurrences(svg, "stroke-dasharray=\"6 5\"") >= 2, "Mean and median overlays should render as annotation lines.");
        Assert(chart.ToPng().Length > 64, "Statistical overlays should render PNG output.");
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

        var hiddenLegendSvg = Chart.Create()
            .WithTitle("Hidden legend data")
            .AddLine("Revenue", Points(10, 20, 30));
        hiddenLegendSvg.Series[0].WithLegendEntry(false);
        Assert(hiddenLegendSvg.ToSvg().Contains("Hidden legend data with 1 data series: Revenue.", StringComparison.Ordinal), "SVG descriptions should describe data series independently from legend visibility.");
    }

    private static void SvgUsesReportGradeStyling() {
        var svg = Chart.Create().WithTitle("Styled").WithTheme(ChartTheme.ReportDark()).WithSize(640, 360)
            .AddSmoothLine("Values", Points(10, 30, 20), ChartColor.FromRgb(96, 165, 250))
            .AddHorizontalLine(25, "target", ChartColor.FromRgb(251, 191, 36))
            .ToSvg();
        Assert(svg.Contains("-seriesFill0", StringComparison.Ordinal), "SVG should include series fill gradients.");
        Assert(svg.Contains("stroke-opacity=\"0.36\"", StringComparison.Ordinal), "Annotation labels should render as legible pills.");
        Assert(svg.Contains("font-weight=\"750\"", StringComparison.Ordinal), "SVG should use stronger title and label typography.");
    }

    private static void TypographyUsesNativeFontStackAndEscapesCustomFamilies() {
        Assert(SampleChart().ToSvg().Contains(ChartFontStacks.SystemSans, StringComparison.Ordinal), "SVG should default to a native system font stack.");
        var svg = Chart.Create().WithFontFamily("A&B \"Display\"").AddLine("Values", Points(1, 2, 3)).ToSvg();
        Assert(svg.Contains("font-family=\"A&amp;B &quot;Display&quot;\"", StringComparison.Ordinal), "SVG font-family values should be attribute-escaped.");
        var editorial = Chart.Create().WithTheme(ChartTheme.Editorial()).AddLine("Values", Points(1, 2, 3)).ToSvg();
        Assert(editorial.Contains(ChartFontStacks.Serif, StringComparison.Ordinal), "Editorial themes should use the built-in serif font stack.");
        var dashboard = Chart.Create().WithTheme(ChartTheme.DashboardLight()).AddBar("KPI", Points(8, 9, 7)).ToSvg();
        Assert(dashboard.Contains("#DDFB20", StringComparison.Ordinal) && dashboard.Contains("rx=\"24\"", StringComparison.Ordinal), "Dashboard themes should expose the reusable KPI-card visual language.");
        var saas = Chart.Create().WithTheme(ChartTheme.SaasDashboardLight()).AddSmoothLine("MRR", Points(104, 112, 126)).ToSvg();
        Assert(saas.Contains("#356AF4", StringComparison.Ordinal) && saas.Contains("r=\"4.2\"", StringComparison.Ordinal), "SaaS dashboard themes should expose recurring-revenue line-card tokens.");
        var customized = Chart.Create()
            .WithTitle("Custom typography")
            .WithTheme(theme => theme
                .WithSurfaceColors(ChartColor.FromRgb(250, 250, 250), ChartColor.FromRgb(1, 1, 1), ChartColor.FromRgb(2, 2, 2), ChartColor.FromRgb(3, 3, 3), ChartColor.FromRgb(4, 4, 4))
                .WithTextColors(ChartColor.FromRgb(5, 5, 5), ChartColor.FromRgb(6, 6, 6))
                .WithGuideColors(ChartColor.FromRgb(7, 7, 7), ChartColor.FromRgb(8, 8, 8))
                .WithSemanticColors(ChartColor.FromRgb(9, 9, 9), ChartColor.FromRgb(10, 10, 10), ChartColor.FromRgb(11, 11, 11))
                .WithFontFamily(ChartFontStacks.Mono)
                .WithTypography(24, 12, 11, 10, 11, 10)
                .WithCornerRadius(4, 2)
                .WithStrokeWidth(2)
                .WithMarkerRadius(5)
                .WithShadowOpacity(0.2)
                .WithSurfaceStyle(ChartSurfaceStyle.Floating))
            .WithPalette(ChartPalettes.Pastel)
            .AddLine("Values", Points(1, 2, 3))
            .ToSvg();
        Assert(customized.Contains(ChartFontStacks.Mono, StringComparison.Ordinal), "Theme callbacks should let users customize font stacks fluently.");
        Assert(customized.Contains("font-size=\"24\"", StringComparison.Ordinal), "Theme callbacks should let users customize typography fluently.");
        Assert(customized.Contains("#60A5FA", StringComparison.Ordinal), "Chart palette helpers should accept reusable palette presets.");
        var hexPalette = Chart.Create()
            .WithPalette("#123456", "#0ea5e9")
            .AddLine("First", Points(1, 2, 3))
            .AddLine("Second", Points(3, 2, 1))
            .ToSvg();
        Assert(hexPalette.Contains("#123456", StringComparison.Ordinal) && hexPalette.Contains("#0EA5E9", StringComparison.Ordinal), "Chart palette helpers should accept pasted hex colors.");
        var hexTheme = ChartTheme.Light().WithPalette("#ABC", "#0EA5E980");
        Assert(hexTheme.Palette[0].ToHex() == "#AABBCC" && hexTheme.Palette[1].ToCss() == "rgba(14,165,233,0.502)", "Theme palette helpers should parse short and alpha hex colors.");
        Assert(customized.Contains("#010101", StringComparison.Ordinal) && customized.Contains("#020202", StringComparison.Ordinal), "Theme callbacks should let users customize surface colors fluently.");
        Assert(customized.Contains("#050505", StringComparison.Ordinal), "Theme callbacks should let users customize text colors fluently.");
        Assert(customized.Contains("#070707", StringComparison.Ordinal) && customized.Contains("#080808", StringComparison.Ordinal), "Theme callbacks should let users customize guide colors fluently.");
        Assert(customized.Contains("rx=\"24\"", StringComparison.Ordinal), "Theme callbacks should let users apply reusable surface styles fluently.");
        var bare = Chart.Create()
            .WithTheme(theme => theme.WithSurfaceStyle(ChartSurfaceStyle.Bare))
            .AddLine("Values", Points(1, 2, 3))
            .ToSvg();
        Assert(!bare.Contains("x=\"14\" y=\"14\"", StringComparison.Ordinal), "Bare surface style should suppress the outer card surface.");
        var glass = ChartTheme.Light().WithSurfaceStyle(ChartSurfaceStyle.Glass);
        Assert(glass.CardBackground.A < 255 && glass.PlotBackground.A < 255, "Glass surface style should make card and plot surfaces translucent.");
        var compact = ChartTheme.Light().WithSurfaceStyle(ChartSurfaceStyle.Compact);
        Assert(compact.TitleFontSize == 22 && compact.MarkerRadius < ChartTheme.Light().MarkerRadius, "Compact surface style should tighten typography and markers.");
        var grid = ChartGrid.Create()
            .WithTitle("Custom grid")
            .WithTheme(theme => theme.WithFontFamily(ChartFontStacks.Mono).WithTypography(22, 12, 11, 10, 11, 10))
            .Add(Chart.Create().AddLine("Values", Points(1, 2, 3)))
            .ToSvg();
        Assert(grid.Contains(ChartFontStacks.Mono, StringComparison.Ordinal), "Grid theme callbacks should let users customize grid typography fluently.");
        Assert(grid.Contains("font-size=\"22\"", StringComparison.Ordinal), "Grid theme callbacks should apply customized heading typography.");
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
        var palette = new[] { ChartColor.Black };
        theme.Palette = palette;
        palette[0] = ChartColor.White;
        Assert(theme.Palette[0].R == ChartColor.Black.R && theme.Palette[0].G == ChartColor.Black.G && theme.Palette[0].B == ChartColor.Black.B, "Themes should snapshot assigned palettes instead of retaining caller-owned arrays.");
        var pastel = ChartPalettes.Pastel;
        pastel[0] = ChartColor.Black;
        Assert(ChartPalettes.Pastel[0].R != ChartColor.Black.R || ChartPalettes.Pastel[0].G != ChartColor.Black.G || ChartPalettes.Pastel[0].B != ChartColor.Black.B, "Palette presets should return fresh arrays so callers can mutate local copies safely.");
        var overlayTheme = ChartTheme.TransparentOverlayDark();
        Assert(overlayTheme.Background.A == 0 && overlayTheme.CardBackground.A > 0 && overlayTheme.CardBackground.A < 255, "Transparent overlay themes should keep the chart background clear and use a translucent card.");
        Assert(overlayTheme.Palette.Length >= 8, "Transparent overlay themes should expose a broad operational palette.");
        foreach (var namedTheme in new[] { ChartTheme.Colorblind(), ChartTheme.Aurora(), ChartTheme.Editorial(), ChartTheme.Candy(), ChartTheme.Terminal(), ChartTheme.TransparentOverlayDark(), ChartTheme.Minimal() }) {
            Assert(namedTheme.Palette.Length >= 8, "Built-in style themes should provide broad qualitative palettes.");
            Assert(namedTheme.FontFamily.Length > 0, "Built-in style themes should define a font stack.");
            Assert(namedTheme.Text.A > 0 && namedTheme.MutedText.A > 0 && namedTheme.Grid.A > 0, "Built-in style themes should define core visual tokens.");
        }
        foreach (var preset in new[] { ChartPalettes.Report, ChartPalettes.Colorblind, ChartPalettes.Vivid, ChartPalettes.Pastel, ChartPalettes.Editorial, ChartPalettes.Jewel, ChartPalettes.Terminal, ChartPalettes.CommandCenter }) {
            Assert(preset.Length >= 8, "Reusable palette presets should provide enough colors for multi-series charts.");
        }
    }

    private static void StandaloneHtmlUsesVisibleBackground() {
        var html = SampleChart().ToHtmlPage();
        Assert(!html.Contains("place-items:center;background:transparent", StringComparison.Ordinal), "Standalone pages should use a visible screen background.");
        Assert(html.Contains("linear-gradient(180deg", StringComparison.Ordinal), "Standalone pages should render a polished page surface gradient.");
        Assert(html.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal), "Standalone pages should request browser font smoothing.");
        Assert(html.Contains("place-items:center", StringComparison.Ordinal) && html.Contains("padding:clamp(16px,4vmin,52px)", StringComparison.Ordinal), "Standalone pages should center previews with responsive padding.");
        Assert(html.Contains("@media(max-width:680px){body{padding:16px;place-items:start center}}", StringComparison.Ordinal), "Standalone pages should reduce padding and keep charts immediately visible on narrow viewports.");
        Assert(html.Contains("@media print", StringComparison.Ordinal), "Standalone pages should include print-friendly framing.");
        var untitled = Chart.Create().AddLine("Values", Points(1, 2, 3)).ToHtmlPage();
        Assert(untitled.Contains("<title>ChartForgeX chart</title>", StringComparison.Ordinal), "Untitled standalone pages should provide a useful browser title.");
    }

    private static void HtmlFragmentIsResponsive() {
        var html = SampleChart().ToHtmlFragment();
        Assert(html.Contains("style=\"width:100%;max-width:640px;box-sizing:border-box;overflow:visible\"", StringComparison.Ordinal), "HTML fragment should carry responsive wrapper styles.");
        Assert(html.Contains("style=\"max-width:100%;height:auto;display:block\"", StringComparison.Ordinal), "SVG should carry responsive sizing styles.");
        var repeated = Chart.Create().WithTitle("Repeated fragment").WithSize(320, 220).AddLine("Values", Points(10, 20, 30));
        var combined = repeated.ToHtmlFragment() + repeated.ToHtmlFragment();
        var titleIds = ExtractAttributeValues(combined, "<title id=\"");
        Assert(titleIds.Length == 2 && titleIds.Distinct(StringComparer.Ordinal).Count() == 2, "Concatenated HTML fragments should give repeated charts unique SVG title IDs.");
        AssertNoDuplicateIds(combined, "Concatenated HTML fragments");
        var scopedRawSvg = repeated.ToSvg("embed-a") + repeated.ToSvg("embed-b");
        AssertNoDuplicateIds(scopedRawSvg, "Scoped raw SVG charts");
        Assert(repeated.ToSvg("stable-scope") == repeated.ToSvg("stable-scope"), "Explicit SVG ID scopes should keep raw SVG output deterministic.");
        var boundaryA = Chart.Create().WithTitle("ab").WithSubtitle("c").WithSize(320, 220).AddLine("Values", Points(10, 20, 30));
        var boundaryB = Chart.Create().WithTitle("a").WithSubtitle("bc").WithSize(320, 220).AddLine("Values", Points(10, 20, 30));
        AssertNoDuplicateIds(boundaryA.ToSvg("embed") + boundaryB.ToSvg("embed"), "Boundary-distinct scoped raw SVG charts");
    }

    private static void ExplicitAxisBoundsAffectSvgTicks() {
        var chart = Chart.Create().WithSize(420, 280).WithXAxisBounds(0, 10).WithYAxisBounds(0, 100).AddLine("Values", Points(20, 40, 80));
        var svg = chart.ToSvg();
        Assert(svg.Contains(">10</text>", StringComparison.Ordinal), "Explicit x-axis bounds should affect SVG tick labels.");
        Assert(svg.Contains(">100</text>", StringComparison.Ordinal), "Explicit y-axis bounds should affect SVG tick labels.");
        Assert(chart.Options.XAxisMinimum == 0 && chart.Options.XAxisMaximum == 10, "Explicit x-axis bounds should be stored on chart options.");
        Assert(chart.Options.YAxisMinimum == 0 && chart.Options.YAxisMaximum == 100, "Explicit y-axis bounds should be stored on chart options.");
        chart.WithAutomaticXAxisBounds().WithAutomaticYAxisBounds();
        Assert(!chart.Options.XAxisMinimum.HasValue && !chart.Options.XAxisMaximum.HasValue, "Automatic x-axis bounds should clear explicit bounds.");
        Assert(!chart.Options.YAxisMinimum.HasValue && !chart.Options.YAxisMaximum.HasValue, "Automatic y-axis bounds should clear explicit bounds.");
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

    private static void DefaultNumericCompactFormatterIsShared() {
        var svg = Chart.Create().WithSize(640, 360).AddLine("Values", Points(1200000, 2400000, 3600000)).ToSvg();
        Assert(svg.Contains(">1M</text>", StringComparison.Ordinal) || svg.Contains(">1.2M</text>", StringComparison.Ordinal), "Default compact values should use million suffixes.");
        var root = FindRepositoryRoot();
        var svgHelpers = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Helpers.cs"));
        var pngRenderer = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.cs"));
        Assert(svgHelpers.Contains("ChartNumericFormatter.FormatCompact", StringComparison.Ordinal), "SVG default numeric labels should use the shared compact formatter.");
        Assert(pngRenderer.Contains("ChartNumericFormatter.FormatCompact", StringComparison.Ordinal), "PNG default numeric labels should use the shared compact formatter.");
    }

    private static void LegendRowsWrapWithRoleMarkers() {
        var svg = Chart.Create().WithSize(420, 320).AddLine("Primary domain checks", Points(1, 2, 3)).AddLine("Certificate transparency drift", Points(2, 3, 4)).AddLine("Dnssec policy posture", Points(3, 4, 5)).AddLine("Mail authentication alignment", Points(4, 5, 6)).ToSvg();
        Assert(svg.Contains("data-cfx-role=\"legend\"", StringComparison.Ordinal), "SVG should expose a semantic legend group.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"legend-row\"") > 1, "Long legends should wrap into multiple rows.");

        var rightLegend = Chart.Create().WithSize(520, 320).WithLegendPosition(ChartLegendPosition.Right).AddLine("Primary domain checks with a realistic name", Points(1, 2, 3)).AddLine("Certificate transparency drift with another realistic name", Points(2, 3, 4));
        var rightLegendSvg = rightLegend.ToSvg();
        Assert(rightLegendSvg.Contains("data-cfx-role=\"legend\" data-cfx-position=\"Right\"", StringComparison.Ordinal), "SVG legends should expose configurable legend placement.");
        Assert(rightLegendSvg.Contains("...</text>", StringComparison.Ordinal), "Side legends should shorten long series names inside their reserved lane.");
        Assert(rightLegend.ToPng().Length > 64, "Configured legend positions should render PNG output.");

        var longSvg = Chart.Create().WithSize(320, 220).AddLine("Extremely long certificate transparency drift monitor", Points(1, 2, 3)).AddLine("Extremely long DNSSEC posture remediation backlog", Points(2, 3, 4)).ToSvg();
        Assert(longSvg.Contains("...</text>", StringComparison.Ordinal), "SVG legends should shorten series names that exceed the bounded legend lane.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithLegendPosition((ChartLegendPosition)999), "Legend positions should reject undefined enum values.");

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
        AssertThrows<ArgumentException>(() => Chart.Create().WithXLabels(new[] { default(ChartAxisLabel) }), "Explicit axis labels should reject default labels with null text.");
        AssertThrows<ArgumentNullException>(() => ChartTheme.Light().Palette = null!, "Themes should reject null palettes.");
        AssertThrows<ArgumentException>(() => ChartTheme.Light().Palette = Array.Empty<ChartColor>(), "Themes should reject empty palettes.");
        AssertThrows<ArgumentException>(() => ChartColor.FromHex("#12"), "Hex colors should reject invalid lengths.");
        AssertThrows<ArgumentException>(() => ChartColor.FromHex("#ggg"), "Hex colors should reject invalid characters.");
        AssertThrows<ArgumentException>(() => ChartPalettes.FromHex(), "Hex palette helpers should reject empty palettes.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartTheme.Light().TitleFontSize = 0, "Themes should reject non-positive font sizes.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartTheme.Light().ShadowOpacity = 2, "Themes should reject opacity values outside zero to one.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartTheme.Light().WithSurfaceStyle((ChartSurfaceStyle)999), "Themes should reject unknown surface styles.");
        AssertThrows<ArgumentNullException>(() => ChartTheme.Light().FontFamily = null!, "Themes should reject null font families.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddLine("Values", new[] { new ChartPoint(1, double.PositiveInfinity) }), "Series should reject non-finite point values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartSeries("Values", (ChartSeriesKind)999, Points(1, 2, 3)), "Series should reject unknown series kinds.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddLine("Values", Points(1, 2, 3)).Series[0].StrokeWidth = 0, "Series stroke widths should reject non-positive values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddLine("Values", Points(1, 2, 3)).Series[0].StrokeWidth = double.NaN, "Series stroke widths should reject non-finite values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddLine("Values", Points(1, 2, 3)).Series[0].YAxis = (ChartAxisSide)99, "Series y-axis side should reject unknown enum values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartAnnotation((ChartAnnotationKind)999, 1, null, "bad", ChartColor.Black, 1), "Annotations should reject unknown annotation kinds.");
        AssertThrows<ArgumentException>(() => new ChartAnnotation(ChartAnnotationKind.HorizontalBand, 1, null, "bad", ChartColor.Black, 0.2), "Band annotations should require an end value.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartAnnotation(ChartAnnotationKind.VerticalBand, 1, 1, "bad", ChartColor.Black, 0.2), "Band annotations should reject empty ranges.");
        AssertThrows<ArgumentException>(() => new ChartAnnotation(ChartAnnotationKind.HorizontalLine, 1, 2, "bad", ChartColor.Black, 1), "Line annotations should reject unused end values.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().WithSecondaryYAxis(null!), "Secondary y-axis titles should reject null.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithSecondaryYAxisBounds(10, 10), "Secondary y-axis bounds should reject empty numeric domains.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddHistogram("Empty", Array.Empty<double>()), "Histograms should reject empty raw value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHistogram("Bad", new[] { 1d, 2d }, 0), "Histograms should reject bin counts below one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHistogram("Bad", new[] { 1d, double.NaN }), "Histograms should reject non-finite raw values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTrendLine("Empty", Array.Empty<ChartPoint>()), "Trend lines should reject empty source points.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTrendLine("Vertical", new[] { new ChartPoint(1, 2), new ChartPoint(1, 4) }), "Trend lines should reject source points with no x variation.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddCircle("Bad", 50, 100, 100), "Circle charts should reject empty numeric domains.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddCircle("Bad", double.NaN), "Circle charts should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRangeBar("Empty", Array.Empty<ChartInterval>()), "Range bars should reject empty interval sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartInterval(1, 2, double.NaN), "Chart intervals should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddBubble("Empty", Array.Empty<ChartBubble>()), "Bubble charts should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartBubble(1, 2, 0), "Bubble charts should reject non-positive sizes.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartBubble(1, 2, double.NaN), "Bubble charts should reject non-finite sizes.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddErrorBar("Empty", Array.Empty<ChartErrorBar>()), "Error bars should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartErrorBar(1, 5, 6, 8), "Error bars should reject lower bounds above the point estimate.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartErrorBar(1, 5, 2, 4), "Error bars should reject upper bounds below the point estimate.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartErrorBar(1, 5, 2, double.NaN), "Error bars should reject non-finite bounds.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddCandlestick("Empty", Array.Empty<ChartCandlestick>()), "Candlestick charts should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartCandlestick(1, 5, 4, 2, 3), "Candlestick charts should reject high values below open or close values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartCandlestick(1, 5, 8, 6, 7), "Candlestick charts should reject low values above open or close values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartCandlestick(1, 5, 8, double.NaN, 7), "Candlestick charts should reject non-finite OHLC values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRangeBand("Empty", Array.Empty<ChartRangeBand>()), "Range bands should reject empty value sets.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRangeArea("Empty", Array.Empty<ChartRangeBand>()), "Range areas should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartRangeBand(1, 8, 4), "Range bands should reject lower values above upper values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartRangeBand(1, double.NaN, 4), "Range bands should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddDumbbell("Empty", Array.Empty<ChartDumbbell>()), "Dumbbell charts should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartDumbbell(1, double.NaN, 4), "Dumbbell charts should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddHeatmapRow("Empty", Array.Empty<ChartPoint>()), "Heatmap rows should reject empty value sets.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddPareto("Empty", Array.Empty<ChartParetoItem>()), "Pareto charts should reject empty item sets.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddPareto("Zero", new[] { new ChartParetoItem("A", 0), new ChartParetoItem("B", 0) }), "Pareto charts should reject all-zero item sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartParetoItem("Bad", -1), "Pareto items should reject negative values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartParetoItem("Bad", double.NaN), "Pareto items should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTreemap("Empty", Array.Empty<ChartTreemapItem>()), "Treemaps should reject empty item sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTreemapItem("Bad", -1), "Treemap items should reject negative values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTreemapItem("Bad", double.NaN), "Treemap items should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddPictorial("Empty", Array.Empty<ChartPictorialItem>()), "Pictorial charts should reject empty item sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartPictorialItem("Bad", -1), "Pictorial items should reject negative values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartPictorialItem("Bad", double.NaN), "Pictorial items should reject non-finite values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddPictorial("Bad", new[] { new ChartPictorialItem("A", 1) }, (ChartPictorialShape)999), "Pictorial charts should reject unknown shapes.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPictorialShape((ChartPictorialShape)999), "Pictorial shape customization should reject unknown shapes.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPictorialColumns(0), "Pictorial charts should reject column counts below one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.PictorialColumns = 101, "Pictorial chart options should reject excessive column counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPictorialMaximum(0), "Pictorial charts should reject zero scale maximums.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.PictorialMaximum = double.NaN, "Pictorial chart options should reject non-finite scale maximums.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPictorialValuePerSymbol(0), "Pictorial charts should reject zero value-per-symbol scales.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.PictorialValuePerSymbol = double.NaN, "Pictorial chart options should reject non-finite value-per-symbol scales.");
        AssertThrows<ArgumentException>(() => Chart.Create().WithPictorialSvgPath(""), "Pictorial charts should reject empty SVG path data.");
        AssertThrows<ArgumentException>(() => Chart.Create().WithPictorialSvgPath("M0 0 <script>"), "Pictorial charts should reject unsafe SVG path data.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPictorialSvgPath("M0 0 L1 1 Z", new ChartRect(0, 0, 0, 24)), "Pictorial charts should reject empty SVG path viewBoxes.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPictorialSvgPath("M0 0 L1 1 Z", (ChartPictorialShape)999), "Pictorial charts should reject unknown PNG fallback shapes.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddWordCloud("Empty", Array.Empty<ChartWordCloudItem>()), "Word clouds should reject empty item sets.");
        AssertThrows<ArgumentException>(() => new ChartWordCloudItem("", 1), "Word cloud terms should reject empty text.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartWordCloudItem("Bad", -1), "Word cloud terms should reject negative weights.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartWordCloudItem("Bad", double.NaN), "Word cloud terms should reject non-finite weights.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithWordCloudFontRange(0, 40), "Word clouds should reject non-positive minimum font sizes.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithWordCloudFontRange(40, 40), "Word clouds should reject inverted font ranges.");
        AssertThrows<ArgumentException>(() => Chart.Create().WithWordCloudAngles(), "Word clouds should reject empty angle patterns.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithWordCloudAngles(-91), "Word clouds should reject excessive term rotation.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithWordCloudMaximumTerms(0), "Word clouds should reject non-positive term limits.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithWordCloudDensity(0.49), "Word clouds should reject overly loose layout density.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithWordCloudDensity(2.01), "Word clouds should reject overly dense layout density.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddBoxPlot("Empty", Array.Empty<ChartBoxPlot>()), "Box plots should reject empty summary sets.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().AddBoxPlot("Raw", 1, null!), "Raw box plots should reject null sample sets.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddBoxPlot("Raw", 1, Array.Empty<double>()), "Raw box plots should reject empty sample sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddBoxPlot("Raw", double.PositiveInfinity, new[] { 1d, 2d }), "Raw box plots should reject non-finite x values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddBoxPlot("Raw", 1, new[] { 1d, double.NaN }), "Raw box plots should reject non-finite sample values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartBoxPlot(1, 5, 4, 3, 2, 1), "Box plots should reject unordered summary values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddGauge("Score", 80, 100, 0), "Gauges should reject inverted scales.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRadialBar("Empty", Array.Empty<ChartPoint>()), "Radial bars should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddRadialBar("Bad", Points(101)), "Radial bars should reject values above 100.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddRadialBar("Bad", Points(-1)), "Radial bars should reject negative values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddTimelineRange("Task", 10, 2), "Timelines should reject inverted ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddGanttTask("Task", 10, 2), "Gantt tasks should reject inverted ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddGanttTask("Task", 1, 2, 1.1), "Gantt tasks should reject progress above one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddGanttTask("Task", 1, 2, dependsOn: 0), "Gantt tasks should reject dependencies that do not reference earlier tasks.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithGanttToday(double.NaN), "Gantt today markers should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddSankey("Flow", Array.Empty<ChartSankeyLink>()), "Sankey charts should reject empty link sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartSankeyLink("A", "B", 0), "Sankey links should reject non-positive values.");
        AssertThrows<ArgumentException>(() => new ChartSankeyLink("", "B", 1), "Sankey links should reject empty source labels.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddSankey("Flow", new[] { new ChartSankeyLink("A", "A", 1) }), "Sankey charts should reject self links.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddSankey("Flow", new[] { new ChartSankeyLink("A", "B", 1), new ChartSankeyLink("B", "A", 1) }), "Sankey charts should reject cyclic link sets.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTree("Tree", Array.Empty<ChartTreeLink>()), "Tree charts should reject empty link sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTreeLink("A", "B", 0), "Tree links should reject non-positive values.");
        AssertThrows<ArgumentException>(() => new ChartTreeLink("", "B"), "Tree links should reject empty parent labels.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTree("Tree", new[] { new ChartTreeLink("A", "A") }), "Tree charts should reject self links.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTree("Tree", new[] { new ChartTreeLink("A", "C"), new ChartTreeLink("B", "C") }), "Tree charts should reject multiple parents for a child.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTree("Tree", new[] { new ChartTreeLink("A", "B"), new ChartTreeLink("C", "D") }), "Tree charts should reject multiple roots.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddSunburst("Sunburst", Array.Empty<ChartTreeLink>()), "Sunburst charts should reject empty link sets.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddSunburst("Sunburst", new[] { new ChartTreeLink("A", "A") }), "Sunburst charts should reject self links.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddSunburst("Sunburst", new[] { new ChartTreeLink("A", "C"), new ChartTreeLink("B", "C") }), "Sunburst charts should reject multiple parents for a child.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddSunburst("Sunburst", new[] { new ChartTreeLink("A", "B"), new ChartTreeLink("C", "D") }), "Sunburst charts should reject multiple roots.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHorizontalBand(1, 2, opacity: 1.5), "Band opacity should reject values outside zero to one.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddMeanLine("Mean", Array.Empty<ChartPoint>()), "Mean overlays should reject empty source points.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddStandardDeviationBand("Sigma", Points(10)), "Standard deviation bands should reject single-point source data.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddStandardDeviationBand("Sigma", Points(10, 20), 0), "Standard deviation bands should reject non-positive deviation counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithXAxisBounds(10, 10), "X-axis bounds should reject empty ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithXAxisBounds(double.NaN, 10), "X-axis bounds should reject non-finite minimum values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.XAxisMaximum = double.PositiveInfinity, "X-axis option bounds should reject non-finite maximum values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithYAxisBounds(10, 10), "Y-axis bounds should reject empty ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithYAxisBounds(double.NaN, 10), "Y-axis bounds should reject non-finite minimum values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.YAxisMaximum = double.PositiveInfinity, "Y-axis option bounds should reject non-finite maximum values.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().WithTitle(null!), "Chart grids should reject null titles.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().Add(null!), "Chart grids should reject null charts.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithColumns(0), "Chart grids should reject non-positive column counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithGap(-1), "Chart grids should reject negative gaps.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithPadding(-1), "Chart grids should reject negative padding.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithPanelSize(0, 200), "Chart grids should reject non-positive panel widths.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().PanelSize = default(ChartSize), "Chart grid panel size setters should reject non-positive dimensions.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithPanelFit((ChartGridPanelFit)999), "Chart grids should reject unknown panel fit values.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().WithTheme(null!), "Chart grids should reject null themes.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().WithSharedXAxis(), "Shared x-axis grids should reject empty grids.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().Add(Chart.Create().AddGauge("Score", 87)).WithSharedXAxis(), "Shared x-axis grids should require cartesian charts.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().WithSharedAxes(), "Shared-axis grids should reject empty grids.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().WithSharedYAxis(), "Shared y-axis grids should reject empty grids.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().Add(Chart.Create().AddGauge("Score", 87)).WithSharedYAxis(), "Shared y-axis grids should require cartesian charts.");
        AssertThrows<ArgumentNullException>(() => ChartExtensions.ToSvg((ChartGrid)null!), "Chart grid SVG export should reject null grids.");
        AssertThrows<ArgumentNullException>(() => ChartExtensions.ToPng((ChartGrid)null!), "Chart grid PNG export should reject null grids.");
        AssertThrows<ArgumentNullException>(() => ChartExtensions.GetPngFontInfo(null!), "PNG font diagnostics should reject null charts.");
        AssertThrows<ArgumentNullException>(() => new HtmlChartRenderer().RenderFragment(null!), "HTML fragment rendering should reject null charts.");
        AssertThrows<ArgumentNullException>(() => new HtmlChartRenderer().RenderPage(null!), "HTML page rendering should reject null charts.");
        AssertThrows<ArgumentNullException>(() => new HtmlChartGridRenderer().RenderPage(null!), "HTML grid rendering should reject null grids.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().ToHtmlPage(), "HTML grid rendering should reject empty grids.");
        var chartWithNullSeries = Chart.Create();
        chartWithNullSeries.Series.Add(null!);
        AssertThrows<InvalidOperationException>(() => chartWithNullSeries.ToSvg(), "Rendering should reject null entries in the public series collection.");
        var chartWithNullAnnotation = Chart.Create().AddLine("Values", Points(1, 2, 3));
        chartWithNullAnnotation.Annotations.Add(null!);
        AssertThrows<InvalidOperationException>(() => chartWithNullAnnotation.ToPng(), "Rendering should reject null entries in the public annotation collection.");
        var chartWithDefaultAxisLabel = Chart.Create().AddLine("Values", Points(1, 2, 3));
        chartWithDefaultAxisLabel.Options.XAxisLabels.Add(default);
        AssertThrows<InvalidOperationException>(() => chartWithDefaultAxisLabel.ToSvg(), "Rendering should reject default axis labels added through the mutable options collection.");
        var malformedBubble = Chart.Create();
        malformedBubble.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Bubble, new[] { new ChartPoint(1, 2) }));
        AssertThrows<InvalidOperationException>(() => malformedBubble.ToSvg(), "Bubble renderers should reject incomplete public series tuples.");
        var malformedErrorBar = Chart.Create();
        malformedErrorBar.Series.Add(new ChartSeries("Bad", ChartSeriesKind.ErrorBar, new[] { new ChartPoint(1, 5), new ChartPoint(1, 6), new ChartPoint(1, 8) }));
        AssertThrows<InvalidOperationException>(() => malformedErrorBar.ToPng(), "Error-bar renderers should reject inverted public lower bounds.");
        var malformedCandlestick = Chart.Create();
        malformedCandlestick.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Candlestick, new[] { new ChartPoint(1, 5), new ChartPoint(1, 4), new ChartPoint(1, 2), new ChartPoint(1, 3) }));
        AssertThrows<InvalidOperationException>(() => malformedCandlestick.ToSvg(), "Candlestick renderers should reject malformed public OHLC tuples.");
        var malformedBoxPlot = Chart.Create();
        malformedBoxPlot.Series.Add(new ChartSeries("Bad", ChartSeriesKind.BoxPlot, new[] { new ChartPoint(1, 1), new ChartPoint(1, 4), new ChartPoint(1, 3), new ChartPoint(1, 5), new ChartPoint(1, 6) }));
        AssertThrows<InvalidOperationException>(() => malformedBoxPlot.ToPng(), "Box plot renderers should reject unordered public summary tuples.");
        var fractionalGanttDependency = Chart.Create();
        fractionalGanttDependency.Series.Add(new ChartSeries("A", ChartSeriesKind.Gantt, new[] { new ChartPoint(1, 2), new ChartPoint(1, -1), new ChartPoint(0, 0) }));
        fractionalGanttDependency.Series.Add(new ChartSeries("B", ChartSeriesKind.Gantt, new[] { new ChartPoint(2, 3), new ChartPoint(1, 0.4), new ChartPoint(0, 0) }));
        AssertThrows<InvalidOperationException>(() => fractionalGanttDependency.ToSvg(), "Gantt renderers should reject fractional dependency indexes instead of rounding them.");
    }

    private static void SpecializedChartsRejectMixedSeries() {
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddGauge("Score", 87).AddLine("Trend", Points(1, 2, 3)).ToSvg(), "SVG rendering should reject mixed specialized and cartesian series.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddDonut("Checks", Points(70, 20)).AddPie("Other", Points(1, 2)).ToPng(), "PNG rendering should reject multiple pie-like series.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddGauge("Score", 87).AddGauge("Other", 72).ToSvg(), "Single-panel specialized charts should reject multiple series.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddPie("Empty", Points(0, 0, 0)).ToSvg(), "Pie charts should reject data with no positive slice values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddDonut("Empty", Points(-3, 0, -2)).ToPng(), "Donut charts should reject data with no positive slice values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddPie("Bad", Points(20, -1, 30)).ToSvg(), "Pie charts should reject negative slice values instead of silently filtering them.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddFunnel("Empty", Points(0, -1, 0)).ToSvg(), "Funnel charts should reject data with no positive stage values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddFunnel("Bad", Points(100, -10, 50)).ToSvg(), "Funnel charts should reject negative stage values instead of silently filtering them.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddTreemap("Empty", new[] { new ChartTreemapItem("A", 0), new ChartTreemapItem("B", 0) }).ToPng(), "Treemaps should reject data with no positive tile values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddPictorial("Empty", new[] { new ChartPictorialItem("A", 0), new ChartPictorialItem("B", 0) }).ToSvg(), "Pictorial charts should reject data with no positive values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddWordCloud("Empty", new[] { new ChartWordCloudItem("A", 0), new ChartWordCloudItem("B", 0) }).ToSvg(), "Word clouds should reject data with no positive weights.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddPolarArea("Empty", Points(0, -1, 0)).ToSvg(), "Polar-area charts should reject data with no positive segment values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddPolarArea("Bad", Points(30, -1, 40)).ToPng(), "Polar-area charts should reject negative segment values instead of silently filtering them.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddWaterfall("Empty", Array.Empty<ChartPoint>()).ToSvg(), "Waterfall charts should reject empty data instead of rendering a blank chart.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddRadar("Too small", Points(72, 84)).ToPng(), "Radar charts should reject fewer than three categories instead of rendering a blank chart.");
        var malformedHeatmap = Chart.Create();
        malformedHeatmap.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Heatmap, Array.Empty<ChartPoint>()));
        AssertThrows<InvalidOperationException>(() => malformedHeatmap.ToSvg(), "Heatmap renderers should reject empty public rows instead of rendering a blank matrix.");
        var malformedBullet = Chart.Create();
        malformedBullet.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Bullet, Points(72)));
        AssertThrows<InvalidOperationException>(() => malformedBullet.ToSvg(), "Bullet renderers should reject malformed public series instead of rendering a blank chart.");
        var malformedGantt = Chart.Create();
        malformedGantt.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Gantt, Points(1, 2)));
        AssertThrows<InvalidOperationException>(() => malformedGantt.ToPng(), "Gantt renderers should reject malformed public series instead of rendering a blank chart.");
        var cyclicSankey = Chart.Create();
        cyclicSankey.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Sankey, new[] { new ChartPoint(0, 1), new ChartPoint(1, 1), new ChartPoint(1, 0), new ChartPoint(1, 1) }));
        AssertThrows<InvalidOperationException>(() => cyclicSankey.ToSvg(), "Sankey renderers should reject cyclic public series instead of laying out backward links.");
        var fractionalSankey = Chart.Create();
        fractionalSankey.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Sankey, new[] { new ChartPoint(0.4, 1), new ChartPoint(1, 1) }));
        AssertThrows<InvalidOperationException>(() => fractionalSankey.ToPng(), "Sankey renderers should reject fractional node indexes instead of rounding them.");
        var disconnectedTree = Chart.Create();
        disconnectedTree.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Tree, new[] { new ChartPoint(0, 1), new ChartPoint(1, 1), new ChartPoint(2, 3), new ChartPoint(1, 1) }));
        AssertThrows<InvalidOperationException>(() => disconnectedTree.ToPng(), "Tree renderers should reject disconnected public series instead of rendering an arbitrary root.");
        var fractionalTree = Chart.Create();
        fractionalTree.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Tree, new[] { new ChartPoint(0, 1.4), new ChartPoint(1, 1) }));
        AssertThrows<InvalidOperationException>(() => fractionalTree.ToSvg(), "Tree renderers should reject fractional node indexes instead of rounding them.");
        var disconnectedSunburst = Chart.Create();
        disconnectedSunburst.Series.Add(new ChartSeries("Bad", ChartSeriesKind.Sunburst, new[] { new ChartPoint(0, 1), new ChartPoint(1, 1), new ChartPoint(2, 3), new ChartPoint(1, 1) }));
        AssertThrows<InvalidOperationException>(() => disconnectedSunburst.ToPng(), "Sunburst renderers should reject disconnected public series instead of rendering an arbitrary root.");
    }
}
