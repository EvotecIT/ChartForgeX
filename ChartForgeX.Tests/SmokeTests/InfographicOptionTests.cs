using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TextStyleOverridesRenderAcrossRoles() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
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
        var prepared = PrepareForTypography(chart);
        AssertNativeStyledText(chart, "frame-heading", "STYLED AUDIENCE LIFT", 24 * .65, "#BE123C", "Comic Sans MS, cursive", true);
        Assert(svg.Contains("font-family=\"Comic Sans MS, cursive\"", StringComparison.Ordinal), "SVG text styles should support role-specific font families.");
        Assert(svg.Contains("font-style=\"italic\"", StringComparison.Ordinal), "SVG text styles should support italic text.");
        Assert(prepared.Scene.Nodes.OfType<VisualScenePath>().Any(node => node.Role == "text-decoration"),
            "Wavy underline and strike decorations should be materialized as shared path geometry.");
        Assert(prepared.Scene.Nodes.OfType<VisualSceneLine>().Any(node => node.Role == "text-decoration" && node.Dash != null),
            "Dotted decorations should be materialized as shared dashed line geometry.");
        Assert(prepared.Scene.Nodes.OfType<VisualSceneText>().Any(node => node.Text.Style.UnderlineStyle == TextDecorationStyle.Double)
            && prepared.Scene.Nodes.OfType<VisualSceneText>().Any(node => node.Text.Style.UnderlineStyle == TextDecorationStyle.Dotted),
            "Resolved text should retain distinct decoration styles for the geometry producer.");
        Assert(svg.Contains(">quarter</text>", StringComparison.Ordinal) && svg.Contains(">FIRST QUARTER</text>", StringComparison.Ordinal) && svg.Contains("fill=\"#2563EB\"", StringComparison.Ordinal), "SVG axis titles and tick labels should apply role-specific casing and colors before fitting.");
        Assert(svg.Contains("font-weight=\"650\"", StringComparison.Ordinal), "SVG axis tick and category labels should honor numeric text weights.");
        Assert(svg.Contains("data-cfx-role=\"legend-label\"", StringComparison.Ordinal) && svg.Contains("fill=\"#15803D\"", StringComparison.Ordinal), "SVG legends should honor role-specific text colors.");
        Assert(svg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal) && svg.Contains("fill=\"#B45309\"", StringComparison.Ordinal), "SVG data labels should honor role-specific text colors.");
        Assert(!svg.Contains("> font-style=", StringComparison.Ordinal) && !svg.Contains("> text-decoration=", StringComparison.Ordinal) && !svg.Contains("> baseline-shift=", StringComparison.Ordinal), "Streamed SVG typography must serialize as attributes rather than visible text.");
        Assert(chart.ToPng().Length > 64, "Styled text should render PNG output.");
        var regularTitle = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(360, 220).WithTitle("Raster Italic Title").AddLine("Values", Points(1, 3, 2)).ToPng();
        var italicTitle = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(360, 220).WithTitle("Raster Italic Title").WithTitleStyle(style => style.WithItalic()).AddLine("Values", Points(1, 3, 2)).ToPng();
        Assert(!regularTitle.SequenceEqual(italicTitle), "PNG chart titles should render italic pixels instead of silently using regular text.");
        var normalWeightTitle = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(360, 220).WithTitle("Raster Weight Title").WithTitleStyle(style => style.WithWeight("normal")).AddLine("Values", Points(1, 3, 2)).ToPng();
        var boldWeightTitle = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(360, 220).WithTitle("Raster Weight Title").WithTitleStyle(style => style.WithWeight("bold")).AddLine("Values", Points(1, 3, 2)).ToPng();
        Assert(!normalWeightTitle.SequenceEqual(boldWeightTitle), "PNG text styles should honor explicit normal and bold font weights.");
        var serifTitle = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(360, 220).WithTitle("MMMM Raster Family iii").WithTitleStyle(style => style.WithFontFamily("serif")).AddLine("Values", Points(1, 3, 2)).ToPng();
        var monospaceTitle = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(360, 220).WithTitle("MMMM Raster Family iii").WithTitleStyle(style => style.WithFontFamily("monospace")).AddLine("Values", Points(1, 3, 2)).ToPng();
        var serifFont = ChartForgeX.Raster.TrueTypeFont.TryLoadForFamily("serif", out _);
        var monospaceFont = ChartForgeX.Raster.TrueTypeFont.TryLoadForFamily("monospace", out _);
        if (serifFont != null && monospaceFont != null && !string.Equals(serifFont.DisplayName, monospaceFont.DisplayName, StringComparison.OrdinalIgnoreCase)) {
            Assert(!serifTitle.SequenceEqual(monospaceTitle), "PNG text styles should honor role-specific font families when distinct platform fonts are available.");
        }
        var regularVerticalTitle = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(360, 240).WithYAxis("Engagement").AddLine("Values", Points(1, 3, 2)).ToPng();
        var decoratedVerticalTitle = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(360, 240).WithYAxis("Engagement").WithAxisTitleStyle(style => style.WithUnderline(TextDecorationStyle.Wavy).WithStrikethrough(TextDecorationStyle.Double).WithSuperscript().WithTextCase(TextCaseTransform.Uppercase)).AddLine("Values", Points(1, 3, 2)).ToPng();
        Assert(!regularVerticalTitle.SequenceEqual(decoratedVerticalTitle), "PNG rotated axis titles should preserve casing, baseline shifts, underline variants, and strikethrough during rotation.");
        var bulletSvg = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(560, 260).WithDataLabels().WithDataLabelStyle(style => style.WithFontSize(15).WithTextCase(TextCaseTransform.Uppercase).WithUnderline(TextDecorationStyle.Dashed).WithStrikethrough(TextDecorationStyle.Dashed).WithSubscript()).AddBullet("control posture", 82, 90).ToSvg();
        Assert(bulletSvg.Contains("CONTROL POSTURE", StringComparison.Ordinal), "Specialized SVG chart paths should apply casing before fitting.");
        Assert(bulletSvg.Contains("font-size=\"9.75\"", StringComparison.Ordinal), "Specialized SVG chart paths should apply script scaling exactly once.");
        Assert(System.Xml.Linq.XDocument.Parse(bulletSvg).Descendants().Any(element => element.Attribute("stroke-dasharray") != null
            && (string?)element.Attribute("data-cfx-role") == "text-decoration"), "Specialized SVG chart paths should preserve native dashed decoration geometry.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithTitleStyle(null!), "Text style callbacks should reject null callbacks.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithTextStyle((ChartTextRole)999, _ => { }), "Text styles should reject unknown roles.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithTitleStyle(style => style.WithFontSize(0)), "Text styles should reject non-positive font sizes.");
    }

    private static void DonutAndRadialCenterLabelsAreOptional() {
        var donut = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithDonutCenterLabel(false)
            .WithXLabels("Male", "Female")
            .AddDonut("Audience", Points(60, 40));
        var donutSvg = donut.ToSvg();
        Assert(donutSvg.Contains("data-cfx-role=\"donut-slice\"", StringComparison.Ordinal), "Donut center labels should be optional without hiding slices.");
        Assert(!donutSvg.Contains("data-cfx-role=\"donut-total-label\"", StringComparison.Ordinal), "Donut center totals should be optional.");
        Assert(!donutSvg.Contains("data-cfx-role=\"donut-title\"", StringComparison.Ordinal), "Donut center titles should be optional.");
        Assert(donut.ToPng().Length > 64, "Donut center label options should render PNG output.");

        var customDonut = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithDonutCenterText("60.5%", "Male")
            .WithDonutInnerRadiusRatio(0.68)
            .WithXLabels("Male", "Female")
            .AddDonut("Audience", Points(60.5, 39.5));
        var customDonutSvg = customDonut.ToSvg();
        Assert(PrepareForTypography(customDonut).Scene.Nodes.OfType<VisualSceneSlice>().All(slice => Math.Abs(slice.Inner / slice.Outer - .68) < .000001),
            "Donut geometry should apply the custom inner-radius ratio in both export backends.");
        Assert(customDonutSvg.Contains(">60.5%</text>", StringComparison.Ordinal), "Donut charts should support custom primary center text.");
        Assert(customDonutSvg.Contains(">Male</text>", StringComparison.Ordinal), "Donut charts should support custom secondary center text.");
        Assert(customDonut.ToPng().Length > 64, "Custom donut center text should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithDonutInnerRadiusRatio(0.2), "Donut inner radius ratio should reject tiny holes.");

        var regularCenter = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithPngOutputScale(2)
            .WithLegend(false)
            .WithDonutCenterText("60", "A")
            .WithDataLabelStyle(style => style.WithColor("#ff00ff").WithFontSize(32))
            .WithXLabels("Male", "Female")
            .AddDonut("Audience", Points(60.5, 39.5));
        var scriptedCenter = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithPngOutputScale(2)
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

        var calloutDonut = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
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
        var connectors = PrepareForTypography(calloutDonut).Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "data-label-connector").ToArray();
        Assert(connectors.Length > 0 && connectors.All(node => node.Stroke!.Value.R == 219 && node.Stroke.Value.G == 39 && node.Stroke.Value.B == 119
            && node.Stroke.Value.A == (byte)Math.Round(255 * .72) && Math.Abs(node.StrokeWidth - 2.4) < .000001),
            "Data-label connectors should retain authored color, opacity and width in the shared scene.");
        Assert(connectors.All(node => node.Commands.Any(command => command.Kind == ChartPathCommandKind.CubicTo)), "Data-label connectors should support curved leaders.");
        Assert(calloutDonutSvg.Contains(">Passed 75%</text>", StringComparison.Ordinal), "Pie and donut labels should support category plus percent callouts.");
        Assert(calloutDonut.ToPng().Length > 64, "Pie slice label content should render PNG output.");
        var autoConnectorDonut = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(520, 320)
            .WithPalette("#E11D48", "#14B8A6")
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Outside)
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)
            .WithXLabels("Primary", "Secondary")
            .AddDonut("Audience", Points(60, 40));
        autoConnectorDonut.Series[0].WithPointColor(1, "#8B5CF6");
        var automatic = PrepareForTypography(autoConnectorDonut);
        var automaticConnectors = automatic.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "data-label-connector").ToArray();
        Assert(automaticConnectors.Any(node => node.Stroke!.Value.R == 225 && node.Stroke.Value.G == 29 && node.Stroke.Value.B == 72), "Pie and donut callout connectors should use slice colors by default.");
        Assert(automatic.Scene.Nodes.OfType<VisualSceneSlice>().Any(node => node.Fill!.Value.ToHex() == "#8B5CF6")
            && automaticConnectors.Any(node => node.Stroke!.Value.R == 139 && node.Stroke.Value.G == 92 && node.Stroke.Value.B == 246), "Pie and donut slices and callout connectors should honor point-level colors.");
        Assert(automatic.Scene.Nodes.OfType<VisualSceneRectangle>().Any(node => node.Role == "legend-swatch" && node.Fill!.Value.ToHex() == "#8B5CF6"), "Pie and donut legends should use point-level slice colors.");
        Assert(autoConnectorDonut.ToPng().Length > 64, "Slice-colored pie and donut callout connectors should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithPieSliceLabelContent((ChartPieSliceLabelContent)999), "Pie slice label content should reject unknown values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithDataLabelConnectorStyle((ChartDataLabelConnectorStyle)999), "Data-label connector style should reject unknown values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithDataLabelConnectorOpacity(1.5), "Data-label connector opacity should reject values above one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithDataLabelConnectorStrokeWidth(0), "Data-label connector stroke width should reject non-positive values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithPieOutsideLabelDistance(0.5), "Outside pie and donut label distance should reject tiny ratios.");

        calloutDonut.WithPieSliceLabelFormatter(slice => slice.Label + ": " + slice.FormattedPercent);
        Assert(calloutDonut.ToSvg().Contains(">Passed: 75%</text>", StringComparison.Ordinal), "Pie and donut labels should support custom slice label formatters.");
        Assert(calloutDonut.ToPng().Length > 64, "Custom pie slice label formatters should render PNG output.");
        calloutDonut.WithPieSliceLabelFormatter(null);
        Assert(calloutDonut.ToSvg().Contains(">Passed 75%</text>", StringComparison.Ordinal), "Clearing a custom slice label formatter should restore the configured content mode.");
        calloutDonut.Series[0].WithPointSliceOffset(1, 0.12);
        var displaced = PrepareForTypography(calloutDonut).Scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        var distance = Math.Sqrt(Math.Pow(displaced[1].Cx - displaced[0].Cx, 2) + Math.Pow(displaced[1].Cy - displaced[0].Cy, 2));
        Assert(Math.Abs(distance - displaced[1].Outer * .12) < .000001, "Pie and donut slices should apply point-level slice offsets to native geometry.");
        Assert(calloutDonut.ToPng().Length > 64, "Point-level pie slice offsets should render PNG output.");
        calloutDonut.Series[0].UseDefaultSliceOffset(1);
        var reset = PrepareForTypography(calloutDonut).Scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        Assert(reset.All(slice => slice.Cx == reset[0].Cx && slice.Cy == reset[0].Cy), "Pie and donut slice offsets should be clearable.");
        AssertThrows<ArgumentOutOfRangeException>(() => calloutDonut.Series[0].WithPointSliceOffset(-1, 0.1), "Slice offsets should reject negative point indexes.");
        AssertThrows<ArgumentOutOfRangeException>(() => calloutDonut.Series[0].WithPointSliceOffset(99, 0.1), "Slice offsets should reject missing point indexes.");
        AssertThrows<ArgumentOutOfRangeException>(() => calloutDonut.Series[0].WithPointSliceOffset(1, 0.5), "Slice offsets should reject large ratios.");

        var longCalloutDonut = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
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

        var radial = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithProgressRingCenterLabel(false)
            .WithRadialProgressRadiusScale(1.12)
            .WithRadialProgressStrokeScale(1.25)
            .AddProgressRing("Scores", Points(75, 60, 39));
        var radialSvg = radial.ToSvg();
        Assert(radialSvg.Contains("data-cfx-role=\"progress-ring-ring\"", StringComparison.Ordinal), "Radial-bar center labels should be optional without hiding rings.");
        var radialDefault = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(420, 280).WithProgressRingCenterLabel(false).AddProgressRing("Scores", Points(75, 60, 39));
        var radialPrepared = PrepareForTypography(radial); var radialDefaultPrepared = PrepareForTypography(radialDefault);
        Assert(radialPrepared.Regions.First(region => region.Role == "progress-ring-ring").Bounds.Width > radialDefaultPrepared.Regions.First(region => region.Role == "progress-ring-ring").Bounds.Width,
            "Radial-bar radius scale should change the ring extent.");
        var scaledRing = radialPrepared.Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role == "progress-ring-ring");
        var defaultRing = radialDefaultPrepared.Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role == "progress-ring-ring");
        Assert(scaledRing.Outer - scaledRing.Inner > defaultRing.Outer - defaultRing.Inner,
            "Radial-bar stroke scale should change native stroke width.");
        Assert(!radialPrepared.Scene.Nodes.OfType<VisualSceneText>().Any(node => node.Role == "progress-ring-value" || node.Role == "progress-ring-title"), "Radial-bar center labels should be optional.");
        Assert(radial.ToPng().Length > 64, "Radial-bar center label options should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithRadialProgressRadiusScale(0.5), "Radial-bar radius scale should reject tiny values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithRadialProgressStrokeScale(2.0), "Radial-bar stroke scale should reject huge values.");

        var circle = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
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
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithCircleRadiusScale(0.5), "Circle radius scale should reject tiny values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithCircleStrokeScale(2.0), "Circle stroke scale should reject huge values.");
    }
}
