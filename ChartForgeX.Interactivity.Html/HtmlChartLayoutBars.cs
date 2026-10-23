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
        var coordinates = ChartBarCoordinateMap.Create(chart); var stacks = ChartStackLayout.Create(chart, coordinates);
        var axis = projection.X;
        var map = new ChartViewportAxisProjection(axis.Minimum, axis.Maximum, axis.Minimum, axis.Maximum,
            projection.Plot.Left, projection.Plot.Right, new ChartAxis { Scale = axis.Scale, Reversed = axis.Reversed,
                SymmetricLogarithmThreshold = axis.Threshold, TimeSessions = axis.TimeSessions }, vertical: false);
        var first = true;
        foreach (var index in participants) {
            var slot = stacks.Slot(participants, index);
            for (var point = 0; point < chart.Series[index].RenderPoints.Count; point++) {
                if (!first) json.Append(','); first = false;
                json.Append("{\"series\":").Append(index).Append(",\"point\":").Append(point)
                    .Append(",\"count\":").Append(slot.Count).Append(",\"position\":").Append(slot.Position)
                    .Append(",\"anchor\":").Append(map.Project(chart.Series[index].RenderPoints[point].X).ToString("R", CultureInfo.InvariantCulture))
                    .Append(",\"categoryAnchor\":").Append(map.Project(coordinates.Resolve(index, point).Value).ToString("R", CultureInfo.InvariantCulture)).Append('}');
            }
        }
        json.Append(']');
    }
}
