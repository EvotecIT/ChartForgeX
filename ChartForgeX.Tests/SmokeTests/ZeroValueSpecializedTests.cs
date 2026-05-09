using System;
using System.Globalization;
using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ZeroValueSpecializedChartsPreservePointIndexes() {
        var pie = Chart.Create()
            .WithSize(540, 320)
            .WithXLabels("Zero", "Live", "Tail")
            .AddPie("Slices", Points(0, 60, 40));
        pie.Series[0].WithPointColor(1, "#E11D48").WithPointSliceOffset(1, 0.12);
        var pieSvg = pie.ToSvg();
        Assert(CountOccurrences(pieSvg, "data-cfx-role=\"pie-slice\"") == 2, "Zero pie slices should not draw arcs.");
        Assert(pieSvg.Contains("data-cfx-role=\"pie-slice\" data-cfx-point=\"1\" data-cfx-label=\"Live\" data-cfx-value=\"60\"", StringComparison.Ordinal), "Positive pie slices after zero values should keep their original point indexes.");
        Assert(pieSvg.Contains("data-cfx-role=\"slice-legend-label\" data-cfx-point=\"0\"", StringComparison.Ordinal) && pieSvg.Contains("data-cfx-role=\"slice-legend-percent\" data-cfx-point=\"0\"", StringComparison.Ordinal), "Zero pie slices should remain visible in legends as zero-percent categories.");
        Assert(pieSvg.Contains("data-cfx-role=\"slice-legend-swatch\" data-cfx-point=\"0\" data-cfx-zero=\"true\"", StringComparison.Ordinal), "Zero pie legend swatches should be marked as zero-value keys.");
        Assert(pieSvg.Contains("fill=\"#E11D48\"", StringComparison.Ordinal) && pieSvg.Contains("data-cfx-slice-offset=\"0.12\"", StringComparison.Ordinal), "Pie point colors and offsets should not shift when earlier slices are zero.");
        Assert(pie.ToPng().Length > 64, "Pie point colors after zero values should render PNG output.");

        var polarArea = Chart.Create()
            .WithSize(540, 320)
            .WithXLabels("Zero", "Live", "Tail")
            .AddPolarArea("Segments", Points(0, 60, 40));
        polarArea.Series[0].WithPointColor(1, "#0F766E");
        var polarAreaSvg = polarArea.ToSvg();
        Assert(CountOccurrences(polarAreaSvg, "data-cfx-role=\"polar-area-segment\"") == 2, "Zero polar-area segments should not draw radial segments.");
        Assert(polarAreaSvg.Contains("data-cfx-role=\"polar-area-zero-slot\" data-cfx-point=\"0\"", StringComparison.Ordinal), "Zero polar-area values should render subtle empty slots so missing categories remain visually anchored.");
        Assert(polarAreaSvg.Contains("data-cfx-inner-radius-factor=\"0.93\"", StringComparison.Ordinal), "Zero polar-area slots should render as thin rim markers instead of full-radius area wedges.");
        Assert(polarAreaSvg.Contains("data-cfx-role=\"polar-area-segment\" data-cfx-point=\"1\" data-cfx-label=\"Live\" data-cfx-value=\"60\"", StringComparison.Ordinal), "Positive polar-area segments after zero values should keep their original point indexes.");
        Assert(polarAreaSvg.Contains("data-cfx-role=\"slice-legend-label\" data-cfx-point=\"0\"", StringComparison.Ordinal) && polarAreaSvg.Contains("data-cfx-role=\"slice-legend-percent\" data-cfx-point=\"0\"", StringComparison.Ordinal), "Zero polar-area segments should remain visible in legends as zero-percent categories.");
        Assert(polarAreaSvg.Contains("fill=\"#0F766E\"", StringComparison.Ordinal), "Polar-area point colors should not shift when earlier segments are zero.");
        Assert(polarAreaSvg.Contains("data-cfx-point=\"2\"", StringComparison.Ordinal) && polarAreaSvg.Contains(" A ", StringComparison.Ordinal), "Polar-area positive segments should keep original angular slots when earlier segments are zero.");
        Assert(polarArea.ToPng().Length > 64, "Polar-area point colors after zero values should render PNG output.");

        var funnel = Chart.Create()
            .WithSize(920, 560)
            .WithXLabels("Opened", "Deferred", "Closed")
            .AddFunnel("Pipeline", Points(120, 0, 32));
        funnel.Series[0].WithPointColor(2, "#7C3AED");
        var funnelSvg = funnel.ToSvg();
        Assert(CountOccurrences(funnelSvg, "data-cfx-role=\"funnel-segment\"") == 3, "Zero funnel stages should render as explicit stages instead of disappearing.");
        Assert(funnelSvg.Contains("data-cfx-role=\"funnel-segment\" data-cfx-point=\"1\" data-cfx-label=\"Deferred\" data-cfx-value=\"0\"", StringComparison.Ordinal), "Zero funnel stages should keep their original point metadata.");
        Assert(FunnelSegmentTopWidth(funnelSvg, 1) <= 16, "Zero funnel stages should render as narrow explicit markers instead of implying nonzero volume.");
        Assert(funnelSvg.Contains("data-cfx-role=\"funnel-segment\" data-cfx-point=\"2\" data-cfx-label=\"Closed\" data-cfx-value=\"32\" data-cfx-retention=\"0.267\" data-cfx-dropoff=\"0\"", StringComparison.Ordinal), "Funnel stages after a zero previous stage should not report a fake drop-off from a zero baseline.");
        Assert(funnelSvg.Contains("funnelPointFill2", StringComparison.Ordinal) && funnelSvg.Contains("stop-color=\"#7C3AED\"", StringComparison.Ordinal), "Funnel point colors should not shift when earlier stages are zero.");
        Assert(funnelSvg.Contains(">prev stage was 0</text>", StringComparison.Ordinal), "Funnel drop-off labels from a zero previous stage should describe the zero baseline instead of implying no change.");
        Assert(double.Parse(GetStringAttribute(funnelSvg, "data-cfx-role=\"funnel-label\"", "font-size"), CultureInfo.InvariantCulture) >= 12, "Wide funnel segments next to zero stages should not shrink labels against the narrow endpoint.");
        Assert(funnelSvg.Contains("stroke=\"rgba(15,23,42,0.588)\"", StringComparison.Ordinal) && funnelSvg.Contains("stroke-width=\"2.2\"", StringComparison.Ordinal), "White funnel labels should use a dark contrast halo instead of a light blur.");
        Assert(funnel.ToPng().Length > 64, "Funnel zero stages should render PNG output.");
    }

    private static double FunnelSegmentTopWidth(string svg, int pointIndex) {
        var d = GetStringAttribute(svg, "data-cfx-role=\"funnel-segment\" data-cfx-point=\"" + pointIndex.ToString(CultureInfo.InvariantCulture) + "\"", "d");
        var parts = d.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return double.Parse(parts[4], CultureInfo.InvariantCulture) - double.Parse(parts[1], CultureInfo.InvariantCulture);
    }

    private static string GetStringAttribute(string text, string marker, string attribute) {
        var start = text.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Missing marker: " + marker);
        var attributeMarker = " " + attribute + "=\"";
        start = text.IndexOf(attributeMarker, start, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Missing attribute: " + attribute);
        start += attributeMarker.Length;
        var end = text.IndexOf("\"", start, StringComparison.Ordinal);
        return text.Substring(start, end - start);
    }
}
