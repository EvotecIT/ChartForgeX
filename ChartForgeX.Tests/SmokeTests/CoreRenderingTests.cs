using System;
using System.Globalization;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
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
        var svg = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 260)
            .WithLineVisualStyle(ChartLineVisualStyle.Premium())
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
        var layers = PreparedFamily(chart).Scene.Nodes.OfType<ChartForgeX.Rendering.VisualScenePath>().ToArray();
        Assert(layers.Single(layer => layer.Role == "line-ambient-halo").Stroke!.Value.A == (byte)Math.Round(.07 * 255), "Reusable line styles should control ambient halo opacity.");
        Assert(layers.Single(layer => layer.Role == "line-highlight").Stroke!.Value.A == (byte)Math.Round(.31 * 255), "Reusable line styles should control highlight opacity.");

        var classicSvg = Chart.Create()
            .WithSize(420, 260)
            .WithLineVisualStyle(ChartLineVisualStyle.Classic())
            .AddLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235))
            .ToSvg();
        Assert(!classicSvg.Contains("line-ambient-halo", StringComparison.Ordinal) && !classicSvg.Contains("line-highlight", StringComparison.Ordinal), "Classic line style should suppress premium-only line lighting layers.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartLineVisualStyle.Premium().WithHighlight(0.5, 0), "Line highlight stroke ratio should reject zero.");
    }

    private static void PngScatterDoesNotConnectPoints() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
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
        var area = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(420, 260).AddArea("Values", points);
        var stepArea = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(420, 260).WithDataLabels().WithLineVisualStyle(ChartLineVisualStyle.Premium()).AddStepArea("Values", points, ChartColor.FromRgb(37, 99, 235));
        var areaSvg = area.ToSvg();
        var stepAreaSvg = stepArea.ToSvg();
        Assert(stepArea.Series[0].Kind == ChartSeriesKind.StepArea, "Step areas should use their own series kind.");
        Assert(stepAreaSvg.Contains("data-cfx-role=\"area\"", StringComparison.Ordinal), "SVG step areas should expose their filled geometry.");
        Assert(stepAreaSvg.Contains("data-cfx-role=\"line\"", StringComparison.Ordinal), "SVG step areas should expose a readable boundary.");
        Assert(stepAreaSvg.Contains("data-cfx-role=\"line-highlight\"", StringComparison.Ordinal), "SVG step-area boundaries should honor explicitly enabled line highlights.");
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
        Assert(svg.Contains("data-cfx-semantic-role=\"point-callout\"", StringComparison.Ordinal), "Point callouts should retain their source semantic role.");
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
        Assert(CountVisibleDataLabels(svg) == 1, "Series data-label overrides should hide labels for one series while preserving chart-level labels for others.");

        chart.Series[1].UseChartDataLabels();
        Assert(CountVisibleDataLabels(chart.ToSvg()) == 2, "Clearing a series data-label override should restore chart-level label behavior.");
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
        var labelCount = CountVisibleDataLabels(svg);
        AssertUsefulSubset(svg, points.Length, "Dense point labels should remain readable in their available lanes.");
    }

    private static void DataLabelsUseReadableEdgeAwareStyling() {
        var svg = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(420, 280).WithDataLabels().AddLine("Values", Points(1000, 900, 1000)).ToSvg();
        Assert(svg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Data labels should be identifiable in SVG output.");
        Assert(System.Xml.Linq.XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text" && element.Ancestors().Any(parent => (string?)parent.Attribute("data-cfx-role") == "data-label"))
            .All(element => double.TryParse((string?)element.Attribute("y"), NumberStyles.Float, CultureInfo.InvariantCulture, out _)), "Data labels should export their measured numeric baselines.");

        var longSvg = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(220, 140)
            .WithDataLabels()
            .WithValueFormatter(_ => "Extremely long remediation status label that must fit")
            .AddBar("Values", Points(72))
            .ToSvg();
        Assert(longSvg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Long SVG data labels should still render as identifiable data labels.");
        Assert(longSvg.Contains("…</text>", StringComparison.Ordinal), "SVG data labels should shorten formatter output that cannot fit inside the plot.");
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
        Assert(CartesianPlot(longRotated).Height < CartesianPlot(plainRotated).Height, "Rotated generated x-axis labels should reserve more SVG bottom space when formatting makes them longer.");
        Assert(longRotatedChart.ToPng().Length > 64, "Rotated generated x-axis labels should render valid PNG output when formatting makes them longer.");
    }

    private static void LongFormattedYAxisLabelsReservePlotSpace() {
        var svg = Chart.Create()
            .WithSize(420, 280)
            .WithValueFormatter(value => "$" + value.ToString("N0", CultureInfo.InvariantCulture) + " ms")
            .AddLine("Latency budget", Points(1000000, 1120000, 1080000))
            .ToSvg();
        Assert(CartesianPlot(svg).Left > 76, "Long formatted y-axis labels should push the SVG plot area to the right.");
        Assert(svg.Contains(" ms</text>", StringComparison.Ordinal), "Long formatted y-axis labels should render after density-aware selection.");
    }

    private static void LongAxisTitlesFitAvailableSpace() {
        const string longTitle = "Extremely long remediation status axis title that must fit inside the chart";
        var svg = Chart.Create()
            .WithSize(240, 180)
            .WithXAxis(longTitle)
            .WithYAxis(longTitle)
            .AddLine("Values", Points(10, 20, 30))
            .ToSvg();
        Assert(svg.Contains("…</text>", StringComparison.Ordinal), "SVG axis titles should shorten when the chart cannot fit the full title.");
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
        var request = VisualExportRequest.ForChart(chart);
        var prepared = chart.Prepare(request.Context);
        var headings = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "frame-heading"
            || node.Role == "frame-heading-continuation").ToArray();
        Assert(headings.Count(node => node.Role == "frame-heading") == 2,
            "The shared frame should retain visible title and subtitle lines in a compact viewport.");
        var padding = request.Context.Layout.PaddingEdges;
        Assert(headings.All(node => node.Text.Lines.All(line => node.LineLeft(line) >= padding.Left - .000001
            && node.LineLeft(line) + line.Width <= prepared.Size.Width - padding.Right + .000001)),
            "Shared header fitting should keep every measured line inside the available horizontal bounds.");
        Assert(headings.All(node => node.Baseline - node.Text.Ascent >= padding.Top - .000001
            && node.Baseline <= prepared.Size.Height - padding.Bottom + .000001), "Header baselines should remain inside the viewport.");
        Assert(headings.SelectMany(node => node.Text.Lines).Any(line => line.Text.EndsWith("…", StringComparison.Ordinal)),
            "The shared frame should visibly mark truncated compact headings.");
        Assert(prepared.Accessibility.Name == longTitle, "Visual fitting should retain the complete title in the text alternative.");
        Assert(request.Context.Frame.Subtitle == longSubtitle, "Fitting should preserve the complete source subtitle.");
        Assert(prepared.ToSvg().Contains("data-cfx-role=\"frame-heading\"", StringComparison.Ordinal), "SVG should expose the canonical shared header role.");
        Assert(prepared.ToPng(request.RasterOptions).Length > 64, "The same fitted header scene should render native PNG output.");
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

        var annotationOnly = Chart.Create()
            .WithSize(640, 360)
            .AddHorizontalLine(100, "standalone target", ChartColor.FromRgb(251, 191, 36));
        Assert(annotationOnly.ToSvg().Contains("data-cfx-role=\"annotation-line\"", StringComparison.Ordinal), "Annotation-only charts should remain on the cartesian SVG rendering path.");
        Assert(annotationOnly.ToPng().Length > 64, "Annotation-only charts should preserve SVG and PNG rendering parity.");
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
        var band = PreparedFamily(chart).Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneRectangle>().Single(mark => mark.Role == "annotation-band");
        Assert(band.Fill!.Value.A == (byte)Math.Round(.16 * 255), "Standard deviation bands should use the requested opacity.");
        Assert(CountOccurrences(svg, "stroke-dasharray=\"6 5\"") >= 2, "Mean and median overlays should render as annotation lines.");
        Assert(chart.ToPng().Length > 64, "Statistical overlays should render PNG output.");
    }

    private static void AnnotationLabelsStayInsidePlot() {
        var chart = Chart.Create().WithSize(420, 280).AddLine("Values", Points(10, 20, 30)).AddVerticalLine(3, "right edge marker", ChartColor.FromRgb(251, 191, 36));
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"annotation-label\"", StringComparison.Ordinal), "Annotation labels should be identifiable in SVG output.");
        Assert(svg.Contains(">right edge marker</text>", StringComparison.Ordinal), "Annotation label text should render.");
        var label = FamilyLabels(PreparedFamily(chart), "annotation-label").Single();
        var line = label.Text.Lines.Single();
        var plot = CartesianPlot(svg);
        Assert(label.LineLeft(line) >= plot.Left && label.LineLeft(line) + line.Width <= plot.Right, "Right-edge annotation labels should fit their entire measured text in the plot.");
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
        var chart = Chart.Create().WithTitle("Styled").WithTheme(ChartTheme.ReportDark()).WithSize(640, 360)
            .WithLineVisualStyle(ChartLineVisualStyle.Premium())
            .AddSmoothLine("Values", Points(10, 30, 20), ChartColor.FromRgb(96, 165, 250))
            .AddHorizontalLine(25, "target", ChartColor.FromRgb(251, 191, 36));
        var prepared = PreparedFamily(chart);
        Assert(prepared.Scene.Nodes.OfType<VisualScenePath>().Any(mark => mark.Role == "line-highlight")
            && prepared.Scene.Nodes.OfType<VisualScenePath>().Any(mark => mark.Role == "line-halo"),
            "Explicit premium styling should retain its shared highlight and halo layers.");
        Assert(FamilyLabels(prepared, "annotation-label").Any(label => label.Text.Lines.Any(line => line.Text == "target")),
            "Annotations should retain their readable visible captions.");
        Assert(FamilyLabels(prepared, "frame-heading").Single().Text.Style.Font.Weight == 600,
            "Report titles should use the shared heading weight.");
        Assert(chart.ToSvg().Contains("data-cfx-role=\"line-highlight\"", StringComparison.Ordinal) && chart.ToPng().Length > 64,
            "The same explicit styling scene should render SVG and native PNG.");
    }

    private static void TypographyUsesNativeFontStackAndEscapesCustomFamilies() {
        var system = Chart.Create().WithFontFamily(ChartFontStacks.SystemSans).AddLine("Values", Points(1, 2, 3));
        Assert(system.ToSvg().Contains(ChartFontStacks.SystemSans, StringComparison.Ordinal), "SVG should retain a configured native system font stack.");
        var svg = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithLegend(true).WithFontFamily("A&B \"Display\"").AddLine("Values", Points(1, 2, 3)).ToSvg();
        Assert(svg.Contains("font-family=\"A&amp;B &quot;Display&quot;\"", StringComparison.Ordinal), "SVG font-family values should be attribute-escaped.");
        var editorial = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithTheme(ChartTheme.Editorial()).AddLine("Values", Points(1, 2, 3)).ToSvg();
        Assert(editorial.Contains(ChartFontStacks.Serif, StringComparison.Ordinal), "Editorial themes should use the built-in serif font stack.");
        var dashboard = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithTheme(ChartTheme.DashboardLight()).AddBar("KPI", Points(8, 9, 7)).ToSvg();
        Assert(dashboard.Contains("#DDFB20", StringComparison.Ordinal) && dashboard.Contains("rx=\"14\"", StringComparison.Ordinal), "Dashboard themes should preserve their palette and shared frame corner geometry.");
        var saasChart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithTheme(ChartTheme.SaasDashboardLight()).WithLineMarkers(ChartLineMarkerMode.All).AddSmoothLine("MRR", Points(104, 112, 126));
        var saas = saasChart.ToSvg();
        Assert(saas.Contains("#356AF4", StringComparison.Ordinal) && PreparedFamily(saasChart).Scene.Nodes.OfType<VisualSceneEllipse>().Where(mark => mark.Role == "marker").All(mark => mark.Rx == 4.2), "SaaS dashboard themes should retain their series palette and explicit endpoint marker radius.");
        var customized = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithTitle("Custom typography")
            .WithTheme(theme => theme
                .WithSurfaceColors(ChartColor.FromRgb(250, 250, 250), ChartColor.FromRgb(1, 1, 1), ChartColor.FromRgb(2, 2, 2), ChartColor.FromRgb(3, 3, 3), ChartColor.FromRgb(4, 4, 4))
                .WithTextColors(ChartColor.FromRgb(5, 5, 5), ChartColor.FromRgb(6, 6, 6))
                .WithGuideColors(ChartColor.FromRgb(7, 7, 7), ChartColor.FromRgb(8, 8, 8))
                .WithSemanticColors(ChartColor.FromRgb(9, 9, 9), ChartColor.FromRgb(10, 10, 10), ChartColor.FromRgb(11, 11, 11))
                .WithFontFamily(ChartFontStacks.Mono)
                .WithTypography(24, 12, 11, 10, 11, 10)
                .WithStrokeWidth(2)
                .WithMarkerRadius(5)
                .WithShadowOpacity(0.2)
                .WithSurfaceStyle(ChartSurfaceStyle.Floating)
                .WithCornerRadius(73, 2))
            .WithPalette(ChartPalettes.Pastel)
            .AddLine("Values", Points(1, 2, 3))
            .ToSvg();
        Assert(customized.Contains(ChartFontStacks.Mono, StringComparison.Ordinal), "Theme callbacks should let users customize font stacks fluently.");
        Assert(customized.Contains("font-size=\"24\"", StringComparison.Ordinal), "Theme callbacks should let users customize typography fluently.");
        Assert(customized.Contains("#60A5FA", StringComparison.Ordinal), "Chart palette helpers should accept reusable palette presets.");
        var hexPalette = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
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
        Assert(SvgHasAttributes(customized, "data-cfx-role=\"frame-card\" rx=\"73\"")
            && SvgHasAttributes(customized, "data-cfx-role=\"content-surface\" rx=\"2\""), "Theme callbacks should preserve independent outer card and plot corner geometry.");
        var bare = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithTheme(theme => theme.WithSurfaceStyle(ChartSurfaceStyle.Bare))
            .AddLine("Values", Points(1, 2, 3))
            .ToSvg();
        Assert(!SvgHasAttributes(bare, "data-cfx-role=\"frame-card\""), "Bare surface style should suppress the outer card surface.");
        var glass = ChartTheme.Light().WithSurfaceStyle(ChartSurfaceStyle.Glass);
        Assert(glass.CardBackground.A < 255 && glass.PlotBackground.A < 255, "Glass surface style should make card and plot surfaces translucent.");
        var compact = ChartTheme.Light().WithSurfaceStyle(ChartSurfaceStyle.Compact);
        Assert(compact.TitleFontSize == 22 && compact.MarkerRadius < ChartTheme.Light().MarkerRadius, "Compact surface style should tighten typography and markers.");
        var grid = ChartGrid.Create()
            .WithTitle("Custom grid")
            .WithTheme(theme => theme.WithFontFamily(ChartFontStacks.Mono).WithTypography(22, 12, 11, 10, 11, 10))
            .Add(Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).AddLine("Values", Points(1, 2, 3)))
            .ToSvg();
        Assert(grid.Contains(ChartFontStacks.Mono, StringComparison.Ordinal), "Grid theme callbacks should let users customize grid typography fluently.");
        Assert(grid.Contains("font-size=\"22\"", StringComparison.Ordinal), "Grid theme callbacks should apply customized heading typography.");
        var html = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithFontFamily("A;B{}").AddLine("Values", Points(1, 2, 3)).ToHtmlPage();
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
        var repeated = Chart.Create().WithTitle("Repeated fragment").WithSize(320, 220).WithLegend(true).AddLine("Values", Points(10, 20, 30));
        Assert(repeated.ToHtmlFragment() == repeated.ToHtmlFragment(), "Default HTML chart fragments should be deterministic.");
        var combined = repeated.ToHtmlFragment("embed-a") + repeated.ToHtmlFragment("embed-b");
        var titleIds = ExtractAttributeValues(combined, "<title id=\"");
        Assert(titleIds.Length == 2 && titleIds.Distinct(StringComparer.Ordinal).Count() == 2, "Explicitly scoped HTML fragments should give equivalent charts unique SVG title IDs.");
        AssertNoDuplicateIds(combined, "Explicitly scoped HTML fragments");
        var scopedRawSvg = repeated.ToSvg("embed-a") + repeated.ToSvg("embed-b");
        AssertNoDuplicateIds(scopedRawSvg, "Scoped raw SVG charts");
        Assert(repeated.ToSvg("stable-scope") == repeated.ToSvg("stable-scope"), "Explicit SVG ID scopes should keep raw SVG output deterministic.");
        var boundaryA = Chart.Create().WithTitle("ab").WithSubtitle("c").WithSize(320, 220).AddLine("Values", Points(10, 20, 30));
        var boundaryB = Chart.Create().WithTitle("a").WithSubtitle("bc").WithSize(320, 220).AddLine("Values", Points(10, 20, 30));
        AssertNoDuplicateIds(boundaryA.ToSvg() + boundaryB.ToSvg(), "Boundary-distinct content namespaces");
        var lightVariant = Chart.Create().WithTitle("Visual identity").WithSize(320, 220).WithTheme(ChartTheme.Light()).AddLine("Values", Points(10, 20, 30));
        var darkVariant = Chart.Create().WithTitle("Visual identity").WithSize(320, 220).WithTheme(ChartTheme.Dark()).AddLine("Values", Points(10, 20, 30));
        AssertNoDuplicateIds(lightVariant.ToHtmlFragment() + darkVariant.ToHtmlFragment(), "Unscoped fragments with distinct visual identities");
        var withoutLegend = Chart.Create().WithTitle("Repeated fragment").WithSize(320, 220).WithLegend(false).AddLine("Values", Points(10, 20, 30));
        AssertNoDuplicateIds(repeated.ToHtmlFragment() + withoutLegend.ToHtmlFragment(), "Unscoped fragments with distinct layout options");
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
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(420, 280).WithXAxisLabelDensity(ChartLabelDensity.All).WithXLabels("January", "February", "March", "April", "May", "December").AddLine("Values", Points(10, 20, 15, 30, 24, 35));
        var plot = CartesianPlot(chart.ToSvg());
        var labels = FamilyLabels(PreparedFamily(chart), "axis-x-label");
        Assert(labels.Any(label => label.Text.Lines.Single().Text == "January") && labels.Any(label => label.Text.Lines.Single().Text == "December"),
            "Edge x-axis labels should preserve their complete visible category text.");
        Assert(labels.All(label => label.Text.Lines.All(line => label.LineLeft(line) >= plot.Left - .000001
            && label.LineLeft(line) + line.Width <= plot.Right + .000001)), "Every measured x-axis label should remain inside the plot's horizontal bounds.");
    }

    private static void XAxisLabelsCanBeRotated() {
        var svg = Chart.Create().WithSize(520, 340).WithXAxis("Month").WithXAxisLabelAngle(-35).WithXAxisLabelDensity(ChartLabelDensity.All).WithXLabels("January", "February", "March").AddLine("Values", Points(10, 20, 30)).ToSvg();
        Assert(svg.Contains("transform=\"rotate(-35", StringComparison.Ordinal), "SVG should rotate x-axis labels when requested.");
        Assert(System.Xml.Linq.XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text" && element.Ancestors().Any(parent => (string?)parent.Attribute("data-cfx-role") == "axis-x-label"))
            .All(element => double.TryParse((string?)element.Attribute("y"), NumberStyles.Float, CultureInfo.InvariantCulture, out _)), "Rotated labels should retain explicitly measured text baselines.");
    }

    private static void LargeSvgValuesUseCompactUnits() {
        var svg = Chart.Create().WithSize(640, 360).AddLine("Values", Points(1200000, 2400000, 3600000)).ToSvg();
        Assert(svg.Contains(">1M</text>", StringComparison.Ordinal) || svg.Contains(">1.2M</text>", StringComparison.Ordinal), "Large SVG values should use M suffixes instead of thousands of k.");
    }

    private static void DefaultNumericCompactFormatterIsShared() {
        var svg = Chart.Create().WithSize(640, 360).AddLine("Values", Points(1200000, 2400000, 3600000)).ToSvg();
        Assert(svg.Contains(">1M</text>", StringComparison.Ordinal) || svg.Contains(">1.2M</text>", StringComparison.Ordinal), "Default compact values should use million suffixes.");
    }

    private static void LegendRowsWrapWithRoleMarkers() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(420, 320).AddLine("Primary domain checks", Points(1, 2, 3)).AddLine("Certificate transparency drift", Points(2, 3, 4)).AddLine("Dnssec policy posture", Points(3, 4, 5)).AddLine("Mail authentication alignment", Points(4, 5, 6));
        var prepared = PreparedFamily(chart);
        var svg = chart.ToSvg();
        Assert(SvgHasAttributes(svg, "data-cfx-role=\"legend-entry\""), "SVG should expose semantic legend entries.");
        Assert(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "legend-label")
            .Select(text => text.Baseline).Distinct().Count() > 1, "Long legends should wrap into multiple measured rows.");

        var rightLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(520, 320).WithLegendPosition(ChartLegendPosition.Right).AddLine("Primary domain checks with a realistic name", Points(1, 2, 3)).AddLine("Certificate transparency drift with another realistic name", Points(2, 3, 4));
        var rightLegendSvg = rightLegend.ToSvg();
        var rightRegions = PreparedFamily(rightLegend).Regions.Where(region => region.Role == "legend" && region.Bounds.Width > 0).ToArray();
        Assert(rightRegions.Length > 0 && rightRegions.All(region => region.Bounds.Left >= rightLegend.Options.Size.Width * .6 && region.Bounds.Right <= rightLegend.Options.Size.Width),
            "Right legends should remain inside their reserved right-side lane.");
        Assert(rightLegendSvg.Contains("…</text>", StringComparison.Ordinal), "Side legends should shorten long series names inside their reserved lane.");
        Assert(rightLegend.ToPng().Length > 64, "Configured legend positions should render PNG output.");

        var longSvg = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(320, 220).AddLine("Extremely long certificate transparency drift monitor", Points(1, 2, 3)).AddLine("Extremely long DNSSEC posture remediation backlog", Points(2, 3, 4)).ToSvg();
        Assert(longSvg.Contains("…</text>", StringComparison.Ordinal), "SVG legends should shorten series names that exceed the bounded legend lane.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithLegendPosition((ChartLegendPosition)999), "Legend positions should reject undefined enum values.");

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

}
