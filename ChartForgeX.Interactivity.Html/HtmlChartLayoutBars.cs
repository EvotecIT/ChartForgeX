using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

// Serialize canonical slot ownership and mapped source anchors, including absent observations and explicit stack groups.
internal static class HtmlChartLayoutBars {
    internal static void Append(StringBuilder json, Chart chart, PreparedVisual prepared) {
        json.Append(",\"bars\":[");
        var projection = prepared.Scene.DataProjection;
        if (projection == null || projection.Horizontal) { json.Append(']'); return; }
        var participants = Enumerable.Range(0, chart.Series.Count).Where(i => chart.Series[i].Kind == ChartSeriesKind.Bar
            && (chart.Series[i].HistogramBinLayout == null || chart.Series[i].HistogramBinLayout!.Minimum == chart.Series[i].HistogramBinLayout!.Maximum)).ToArray();
        if (participants.Length == 0) { json.Append(']'); return; }
        var coordinates = ChartBarCoordinateMap.Create(chart); var stacks = ChartStackLayout.Create(chart, coordinates);
        var axis = projection.X;
        // Clone the complete authored policy and attach the completed native calendar domain.
        // This also covers compact scenes that deliberately have no public viewport projection.
        var policy = chart.Options.XAxis.Clone(); policy.PreparedTimeDomain = axis.TimeDomain;
        var map = new ChartViewportAxisProjection(axis.Minimum, axis.Maximum, axis.Minimum, axis.Maximum,
            projection.Plot.Left, projection.Plot.Right, policy, vertical: false);
        var categoryAnchors = new List<double>();
        var first = true;
        foreach (var index in participants) {
            var slot = stacks.Slot(participants, index);
            for (var point = 0; point < chart.Series[index].RenderPoints.Count; point++) {
                if (!first) json.Append(','); first = false;
                var anchor = map.Project(chart.Series[index].RenderPoints[point].X);
                var categoryAnchor = map.Project(coordinates.Resolve(index, point).Value);
                categoryAnchors.Add(categoryAnchor);
                json.Append("{\"series\":").Append(index).Append(",\"point\":").Append(point)
                    .Append(",\"count\":").Append(slot.Count).Append(",\"position\":").Append(slot.Position)
                    .Append(",\"anchor\":").Append(anchor.ToString("R", CultureInfo.InvariantCulture))
                    .Append(",\"categoryAnchor\":").Append(categoryAnchor.ToString("R", CultureInfo.InvariantCulture)).Append('}');
            }
        }
        var maximumPitch = axis.TimeDomain?.Unit == true
            ? projection.Plot.Width / (axis.TransformedMaximum - axis.TransformedMinimum) : double.PositiveInfinity;
        var pitch = ChartCategorySlotLayout.Create(categoryAnchors, projection.Plot.Width, chart.Options.Theme.Spacing,
            1, 0, requireMinimumFootprint: false, sampleSlots: chart.Options.IsSparkline ? chart.Options.SparklineSampleCount : 0,
            maximumPitch: maximumPitch).Pitch;
        // The native slot contract: the design band fill and, for stacks, the stacked thickness cap.
        var stacked = participants.Any(stacks.IsStacked);
        var maximum = chart.Options.MaximumBarWidth ?? ChartMarkSurface.MaximumThickness(chart.Options.Theme.MaximumBarWidth, stacked);
        json.Append(']').Append(",\"barPitch\":").Append(pitch.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"barFill\":").Append(ChartCategorySlotLayout.BandShare(chart.Options.IsSparkline ? chart.Options.SparklineSampleCount : 0).ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"maximumBarWidth\":").Append(maximum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"barWidthUnbounded\":").Append(chart.Options.IsSparkline ? "true" : "false");
    }
}
