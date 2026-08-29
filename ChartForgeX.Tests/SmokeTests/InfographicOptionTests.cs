using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TextStyleOverridesRenderAcrossRoles() {
        var chart = Chart.Create()
            .WithSize(520, 340)
            .WithTitle("styled audience lift")
            .WithSubtitle("Color, cursive, italic, and underline controls")
            .WithXAxis("Quarter")
            .WithYAxis("Audience")
            .WithXLabels("first quarter", "second quarter", "third quarter", "fourth quarter")
            .WithDataLabels()
            .WithLegendPosition(ChartLegendPosition.Right)
            .WithTitleStyle(style => style.WithColor("#be123c").WithFontFamily("Comic Sans MS, cursive").WithWeight("900").WithItalic().WithUnderline(TextDecorationStyle.Wavy).WithStrikethrough(TextDecorationStyle.Wavy).WithSuperscript().WithTextCase(TextCaseTransform.Uppercase).WithFontSize(24))
            .WithSubtitleStyle(style => style.WithColor("#0e7490").WithItalic())
            .WithAxisTitleStyle(style => style.WithColor("#7c3aed").WithUnderline(TextDecorationStyle.Double).WithTextCase(TextCaseTransform.Lowercase))
            .WithTickLabelStyle(style => style.WithColor("#2563eb").WithWeight("650").WithItalic().WithTextCase(TextCaseTransform.Uppercase))
            .WithLegendStyle(style => style.WithColor("#15803d").WithUnderline())
            .WithDataLabelStyle(style => style.WithColor("#b45309").WithWeight("800").WithUnderline(TextDecorationStyle.Dotted))
            .AddBar("North America adoption is intentionally long", Points(28, 41, 64, 83))
            .AddLine("Europe expansion is also intentionally long", Points(18, 35, 52, 74));
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"chart-title\"", StringComparison.Ordinal) && svg.Contains("fill=\"#BE123C\"", StringComparison.Ordinal), "SVG titles should honor text style colors.");
        Assert(svg.Contains("font-family=\"Comic Sans MS, cursive\"", StringComparison.Ordinal), "SVG text styles should support role-specific font families.");
        Assert(svg.Contains("font-style=\"italic\"", StringComparison.Ordinal), "SVG text styles should support italic text.");
        Assert(svg.Contains("text-decoration=\"underline\"", StringComparison.Ordinal), "SVG text styles should support underlined text.");
        Assert(svg.Contains("text-decoration=\"underline line-through\"", StringComparison.Ordinal) && svg.Contains("text-decoration-style=\"wavy\"", StringComparison.Ordinal), "SVG text styles should preserve combined underline and strikethrough decoration semantics.");
        Assert(svg.Contains("text-decoration-style=\"double\"", StringComparison.Ordinal) && svg.Contains("text-decoration-style=\"dotted\"", StringComparison.Ordinal), "SVG text roles should preserve double and dotted decoration patterns.");
        Assert(svg.Contains("baseline-shift=\"super\"", StringComparison.Ordinal) && svg.Contains(">STYLED AUDIENCE LIFT</text>", StringComparison.Ordinal), "SVG text styles should preserve script placement and transformed casing.");
        Assert(svg.Contains(">quarter</text>", StringComparison.Ordinal) && svg.Contains(">FIRST QUARTER</text>", StringComparison.Ordinal) && svg.Contains("fill=\"#2563EB\"", StringComparison.Ordinal), "SVG axis titles and tick labels should apply role-specific casing and colors before fitting.");
        Assert(svg.Contains("font-weight=\"650\"", StringComparison.Ordinal), "SVG axis tick and category labels should honor numeric text weights.");
        Assert(svg.Contains("data-cfx-role=\"legend-label\"", StringComparison.Ordinal) && svg.Contains("fill=\"#15803D\"", StringComparison.Ordinal), "SVG legends should honor role-specific text colors.");
        Assert(svg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal) && svg.Contains("fill=\"#B45309\"", StringComparison.Ordinal), "SVG data labels should honor role-specific text colors.");
        Assert(!svg.Contains("> font-style=", StringComparison.Ordinal) && !svg.Contains("> text-decoration=", StringComparison.Ordinal) && !svg.Contains("> baseline-shift=", StringComparison.Ordinal), "Streamed SVG typography must serialize as attributes rather than visible text.");
        Assert(chart.ToPng().Length > 64, "Styled text should render PNG output.");
        var regularTitle = Chart.Create().WithSize(360, 220).WithTitle("Raster Italic Title").AddLine("Values", Points(1, 3, 2)).ToPng();
        var italicTitle = Chart.Create().WithSize(360, 220).WithTitle("Raster Italic Title").WithTitleStyle(style => style.WithItalic()).AddLine("Values", Points(1, 3, 2)).ToPng();
        Assert(!regularTitle.SequenceEqual(italicTitle), "PNG chart titles should render italic pixels instead of silently using regular text.");
        var normalWeightTitle = Chart.Create().WithSize(360, 220).WithTitle("Raster Weight Title").WithTitleStyle(style => style.WithWeight("normal")).AddLine("Values", Points(1, 3, 2)).ToPng();
        var boldWeightTitle = Chart.Create().WithSize(360, 220).WithTitle("Raster Weight Title").WithTitleStyle(style => style.WithWeight("bold")).AddLine("Values", Points(1, 3, 2)).ToPng();
        Assert(!normalWeightTitle.SequenceEqual(boldWeightTitle), "PNG text styles should honor explicit normal and bold font weights.");
        var serifTitle = Chart.Create().WithSize(360, 220).WithTitle("MMMM Raster Family iii").WithTitleStyle(style => style.WithFontFamily("serif")).AddLine("Values", Points(1, 3, 2)).ToPng();
        var monospaceTitle = Chart.Create().WithSize(360, 220).WithTitle("MMMM Raster Family iii").WithTitleStyle(style => style.WithFontFamily("monospace")).AddLine("Values", Points(1, 3, 2)).ToPng();
        var serifFont = ChartForgeX.Raster.TrueTypeFont.TryLoadForFamily("serif", out _);
        var monospaceFont = ChartForgeX.Raster.TrueTypeFont.TryLoadForFamily("monospace", out _);
        if (serifFont != null && monospaceFont != null && !string.Equals(serifFont.DisplayName, monospaceFont.DisplayName, StringComparison.OrdinalIgnoreCase)) {
            Assert(!serifTitle.SequenceEqual(monospaceTitle), "PNG text styles should honor role-specific font families when distinct platform fonts are available.");
        }
        var regularVerticalTitle = Chart.Create().WithSize(360, 240).WithYAxis("Engagement").AddLine("Values", Points(1, 3, 2)).ToPng();
        var decoratedVerticalTitle = Chart.Create().WithSize(360, 240).WithYAxis("Engagement").WithAxisTitleStyle(style => style.WithUnderline(TextDecorationStyle.Wavy).WithStrikethrough(TextDecorationStyle.Double).WithSuperscript().WithTextCase(TextCaseTransform.Uppercase)).AddLine("Values", Points(1, 3, 2)).ToPng();
        Assert(!regularVerticalTitle.SequenceEqual(decoratedVerticalTitle), "PNG rotated axis titles should preserve casing, baseline shifts, underline variants, and strikethrough during rotation.");
        var bulletSvg = Chart.Create().WithSize(560, 260).WithDataLabels().WithDataLabelStyle(style => style.WithFontSize(15).WithTextCase(TextCaseTransform.Uppercase).WithUnderline(TextDecorationStyle.Dashed).WithStrikethrough(TextDecorationStyle.Dashed).WithSubscript()).AddBullet("control posture", 82, 90).ToSvg();
        Assert(bulletSvg.Contains("CONTROL POSTURE", StringComparison.Ordinal), "Specialized SVG chart paths should apply casing before fitting.");
        Assert(bulletSvg.Contains("baseline-shift=\"sub\"", StringComparison.Ordinal), "Specialized SVG chart paths should preserve script placement.");
        Assert(bulletSvg.Contains("font-size=\"9.75\"", StringComparison.Ordinal), "Specialized SVG chart paths should apply script scaling exactly once.");
        Assert(bulletSvg.Contains("text-decoration-style=\"dashed\"", StringComparison.Ordinal), "Specialized SVG chart paths should preserve decoration variants.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().WithTitleStyle(null!), "Text style callbacks should reject null callbacks.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTextStyle((ChartTextRole)999, _ => { }), "Text styles should reject unknown roles.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTitleStyle(style => style.WithFontSize(0)), "Text styles should reject non-positive font sizes.");
    }

    private static void DonutAndRadialCenterLabelsAreOptional() {
        var donut = Chart.Create()
            .WithSize(420, 280)
            .WithDonutCenterLabel(false)
            .WithXLabels("Male", "Female")
            .AddDonut("Audience", Points(60, 40));
        var donutSvg = donut.ToSvg();
        Assert(donutSvg.Contains("data-cfx-role=\"donut-slice\"", StringComparison.Ordinal), "Donut center labels should be optional without hiding slices.");
        Assert(!donutSvg.Contains("data-cfx-role=\"donut-total-label\"", StringComparison.Ordinal), "Donut center totals should be optional.");
        Assert(!donutSvg.Contains("data-cfx-role=\"donut-title\"", StringComparison.Ordinal), "Donut center titles should be optional.");
        Assert(donut.ToPng().Length > 64, "Donut center label options should render PNG output.");

        var customDonut = Chart.Create()
            .WithSize(420, 280)
            .WithDonutCenterText("60.5%", "Male")
            .WithDonutInnerRadiusRatio(0.68)
            .WithXLabels("Male", "Female")
            .AddDonut("Audience", Points(60.5, 39.5));
        var customDonutSvg = customDonut.ToSvg();
        Assert(customDonutSvg.Contains("data-cfx-inner-radius-ratio=\"0.68\"", StringComparison.Ordinal), "Donut slices should expose custom inner-radius metadata.");
        Assert(customDonutSvg.Contains(">60.5%</text>", StringComparison.Ordinal), "Donut charts should support custom primary center text.");
        Assert(customDonutSvg.Contains(">Male</text>", StringComparison.Ordinal), "Donut charts should support custom secondary center text.");
        Assert(customDonut.ToPng().Length > 64, "Custom donut center text should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithDonutInnerRadiusRatio(0.2), "Donut inner radius ratio should reject tiny holes.");

        var regularCenter = Chart.Create()
            .WithSize(420, 280)
            .WithLegend(false)
            .WithDonutCenterText("60", "A")
            .WithDataLabelStyle(style => style.WithColor("#ff00ff").WithFontSize(32))
            .WithXLabels("Male", "Female")
            .AddDonut("Audience", Points(60.5, 39.5));
        var scriptedCenter = Chart.Create()
            .WithSize(420, 280)
            .WithLegend(false)
            .WithDonutCenterText("60", "A")
            .WithDataLabelStyle(style => style.WithColor("#ff00ff").WithFontSize(32).WithSuperscript())
            .WithXLabels("Male", "Female")
            .AddDonut("Audience", Points(60.5, 39.5));
        var regularCenterPixels = ReadPngRgba(regularCenter.ToPng(), out var centerWidth, out _);
        var scriptedCenterPixels = ReadPngRgba(scriptedCenter.ToPng(), out _, out _);
        var regularCenterBounds = FindNearColorBounds(regularCenterPixels, centerWidth, 255, 0, 255, 10);
        var scriptedCenterBounds = FindNearColorBounds(scriptedCenterPixels, centerWidth, 255, 0, 255, 10);
        Assert(!regularCenterBounds.IsEmpty && !scriptedCenterBounds.IsEmpty, "PNG center-label script proof should find both configured center labels.");
        Assert(scriptedCenterBounds.Height > regularCenterBounds.Height * 0.50 && scriptedCenterBounds.Height < regularCenterBounds.Height * 0.82, "PNG center labels should apply script scaling exactly once instead of shrinking to roughly forty-two percent. Regular height: " + regularCenterBounds.Height + "; scripted height: " + scriptedCenterBounds.Height + ".");

        var calloutDonut = Chart.Create()
            .WithSize(520, 320)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Outside)
            .WithDataLabelConnectorColor("#DB2777")
            .WithDataLabelConnectorOpacity(0.72)
            .WithDataLabelConnectorStrokeWidth(2.4)
            .WithDataLabelConnectorStyle(ChartDataLabelConnectorStyle.Curve)
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)
            .WithPieOutsideLabelDistance(1.26)
            .WithXLabels("Passed", "Warnings", "Failed")
            .AddDonut("Checks", Points(75, 20, 5));
        var calloutDonutSvg = calloutDonut.ToSvg();
        Assert(calloutDonut.Options.PieSliceLabelContent == ChartPieSliceLabelContent.LabelAndPercent, "Pie slice label content should be configurable.");
        Assert(calloutDonut.Options.PieOutsideLabelDistanceRatio == 1.26, "Outside pie and donut label distance should be configurable.");
        Assert(calloutDonutSvg.Contains("stroke=\"#DB2777\"", StringComparison.Ordinal), "Data-label connectors should support custom colors.");
        Assert(calloutDonutSvg.Contains("stroke-opacity=\"0.72\"", StringComparison.Ordinal), "Data-label connectors should support custom opacity.");
        Assert(calloutDonutSvg.Contains("stroke-width=\"2.4\"", StringComparison.Ordinal), "Data-label connectors should support custom stroke width.");
        Assert(calloutDonutSvg.Contains("data-cfx-connector-style=\"Curve\"", StringComparison.Ordinal) && calloutDonutSvg.Contains(" C ", StringComparison.Ordinal), "Data-label connectors should support curved leaders.");
        Assert(calloutDonutSvg.Contains(">Passed 75%</text>", StringComparison.Ordinal), "Pie and donut labels should support category plus percent callouts.");
        Assert(calloutDonut.ToPng().Length > 64, "Pie slice label content should render PNG output.");
        var autoConnectorDonut = Chart.Create()
            .WithSize(520, 320)
            .WithPalette("#E11D48", "#14B8A6")
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Outside)
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)
            .WithXLabels("Primary", "Secondary")
            .AddDonut("Audience", Points(60, 40));
        autoConnectorDonut.Series[0].WithPointColor(1, "#8B5CF6");
        var autoConnectorSvg = autoConnectorDonut.ToSvg();
        Assert(autoConnectorSvg.Contains("stroke=\"#E11D48\"", StringComparison.Ordinal), "Pie and donut callout connectors should use slice colors by default.");
        Assert(autoConnectorSvg.Contains("fill=\"#8B5CF6\"", StringComparison.Ordinal) && autoConnectorSvg.Contains("stroke=\"#8B5CF6\"", StringComparison.Ordinal), "Pie and donut slices and callout connectors should honor point-level colors.");
        Assert(autoConnectorSvg.Contains("<rect", StringComparison.Ordinal) && autoConnectorSvg.Contains("fill=\"#8B5CF6\"", StringComparison.Ordinal), "Pie and donut legends should use point-level slice colors.");
        Assert(autoConnectorDonut.ToPng().Length > 64, "Slice-colored pie and donut callout connectors should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPieSliceLabelContent((ChartPieSliceLabelContent)999), "Pie slice label content should reject unknown values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithDataLabelConnectorStyle((ChartDataLabelConnectorStyle)999), "Data-label connector style should reject unknown values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithDataLabelConnectorOpacity(1.5), "Data-label connector opacity should reject values above one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithDataLabelConnectorStrokeWidth(0), "Data-label connector stroke width should reject non-positive values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPieOutsideLabelDistance(0.5), "Outside pie and donut label distance should reject tiny ratios.");

        calloutDonut.WithPieSliceLabelFormatter(slice => slice.Label + ": " + slice.FormattedPercent);
        Assert(calloutDonut.ToSvg().Contains(">Passed: 75%</text>", StringComparison.Ordinal), "Pie and donut labels should support custom slice label formatters.");
        Assert(calloutDonut.ToPng().Length > 64, "Custom pie slice label formatters should render PNG output.");
        calloutDonut.WithPieSliceLabelFormatter(null);
        Assert(calloutDonut.ToSvg().Contains(">Passed 75%</text>", StringComparison.Ordinal), "Clearing a custom slice label formatter should restore the configured content mode.");
        calloutDonut.Series[0].WithPointSliceOffset(1, 0.12);
        Assert(calloutDonut.ToSvg().Contains("data-cfx-slice-offset=\"0.12\"", StringComparison.Ordinal), "Pie and donut slices should support point-level slice offsets.");
        Assert(calloutDonut.ToPng().Length > 64, "Point-level pie slice offsets should render PNG output.");
        calloutDonut.Series[0].UseDefaultSliceOffset(1);
        Assert(!calloutDonut.ToSvg().Contains("data-cfx-slice-offset=\"0.12\"", StringComparison.Ordinal), "Pie and donut slice offsets should be clearable.");
        AssertThrows<ArgumentOutOfRangeException>(() => calloutDonut.Series[0].WithPointSliceOffset(-1, 0.1), "Slice offsets should reject negative point indexes.");
        AssertThrows<ArgumentOutOfRangeException>(() => calloutDonut.Series[0].WithPointSliceOffset(99, 0.1), "Slice offsets should reject missing point indexes.");
        AssertThrows<ArgumentOutOfRangeException>(() => calloutDonut.Series[0].WithPointSliceOffset(1, 0.5), "Slice offsets should reject large ratios.");

        var longCalloutDonut = Chart.Create()
            .WithSize(380, 260)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Outside)
            .WithDataLabelConnectorColor((ChartForgeX.Primitives.ChartColor?)null)
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)
            .WithXLabels("Extremely long returning audience segment with several words", "Short segment")
            .AddDonut("Audience", Points(64, 36));
        var longCalloutSvg = longCalloutDonut.ToSvg();
        Assert(longCalloutSvg.Contains("...", StringComparison.Ordinal), "Outside pie and donut labels should trim long callouts to their side lanes.");
        Assert(!longCalloutSvg.Contains(">Extremely long returning audience segment with several words 64%</text>", StringComparison.Ordinal), "Outside pie and donut labels should not render untrimmed long callouts.");
        Assert(longCalloutDonut.ToPng().Length > 64, "Trimmed outside pie and donut labels should render PNG output.");

        var radial = Chart.Create()
            .WithSize(420, 280)
            .WithRadialBarCenterLabel(false)
            .WithRadialBarRadiusScale(1.12)
            .WithRadialBarStrokeScale(1.25)
            .AddRadialBar("Scores", Points(75, 60, 39));
        var radialSvg = radial.ToSvg();
        Assert(radialSvg.Contains("data-cfx-role=\"radial-bar-ring\"", StringComparison.Ordinal), "Radial-bar center labels should be optional without hiding rings.");
        Assert(radialSvg.Contains("data-cfx-radius-scale=\"1.12\"", StringComparison.Ordinal), "Radial-bar charts should expose radius scale metadata.");
        Assert(radialSvg.Contains("data-cfx-stroke-scale=\"1.25\"", StringComparison.Ordinal), "Radial-bar charts should expose stroke scale metadata.");
        Assert(!radialSvg.Contains("data-cfx-role=\"radial-bar-total\"", StringComparison.Ordinal), "Radial-bar center totals should be optional.");
        Assert(radial.ToPng().Length > 64, "Radial-bar center label options should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithRadialBarRadiusScale(0.5), "Radial-bar radius scale should reject tiny values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithRadialBarStrokeScale(2.0), "Radial-bar stroke scale should reject huge values.");

        var circle = Chart.Create()
            .WithSize(420, 280)
            .WithCircleStatusLabel(false)
            .WithCircleRadiusScale(1.18)
            .WithCircleStrokeScale(1.32)
            .AddCircle("Awareness", 75);
        var circleSvg = circle.ToSvg();
        Assert(circleSvg.Contains("data-cfx-role=\"circle-value\"", StringComparison.Ordinal), "Circle status labels should be optional without hiding the value ring.");
        Assert(circleSvg.Contains("data-cfx-radius-scale=\"1.18\"", StringComparison.Ordinal), "Circle charts should expose radius scale metadata.");
        Assert(circleSvg.Contains("data-cfx-stroke-scale=\"1.32\"", StringComparison.Ordinal), "Circle charts should expose stroke scale metadata.");
        Assert(!circleSvg.Contains("data-cfx-role=\"circle-status-label\"", StringComparison.Ordinal), "Circle status text should be optional.");
        Assert(!circleSvg.Contains("data-cfx-role=\"circle-status-marker\"", StringComparison.Ordinal), "Circle status markers should be optional.");
        Assert(circle.ToPng().Length > 64, "Circle status label options should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithCircleRadiusScale(0.5), "Circle radius scale should reject tiny values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithCircleStrokeScale(2.0), "Circle stroke scale should reject huge values.");
    }
}
