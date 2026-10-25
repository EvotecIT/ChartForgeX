using System;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Rendering;

internal sealed partial class NativeTablePaintPlan {
    /// <summary>Measures unconstrained native content through the same family font and role policies as paint.</summary>
    internal static double PreferredWidth(NativeTableData data) {
        var sum = 0d; var flexible = 0; var flexibleMinimum = 0d; var scale = data.Theme.DataLabelFontSize / 12;
        var body = data.AbsoluteDataLabelSize is double absolute
            ? (data.Dense ? 10.5 : 11.5) * (absolute / 12)
            : (data.Dense ? 10.5 : 11.5) * scale * data.Scale;
        for (var column = 0; column < data.Columns.Count; column++) {
            var source = data.Columns[column];
            if (source.Width.HasValue) { sum += source.Width.Value * data.Scale; continue; }
            var width = data.ShowHeader ? VisualBlockRendering.MeasureTextWidth(source.Label, OutputFontSize(data, data.Theme.SubtitleFontSize), data.Font.Family, data.WeightOverride ?? 700) + 18 * data.Scale : 0;
            foreach (var row in data.Rows) {
                var cell = row.Cells[column]; var reserve = data.StatusColumn == column ? 28 : 16;
                width = Math.Max(width, VisualBlockRendering.MeasureTextWidth(cell.Text, body, data.Font.Family, data.WeightOverride ?? 400) + reserve * data.Scale);
            }
            flexible++; flexibleMinimum = Math.Max(flexibleMinimum, Math.Max(24 * data.Scale, width));
        }
        // Invert the existing fixed/equal-flex ColumnWidths allocation: every flexible share must fit its widest minimum.
        sum += flexible * flexibleMinimum;
        if (data.Title.Length > 0) sum = Math.Max(sum, VisualBlockRendering.MeasureTextWidth(data.Title, OutputFontSize(data, data.Theme.TitleFontSize), data.Font.Family, data.WeightOverride ?? 700));
        if (data.Subtitle.Length > 0) sum = Math.Max(sum, VisualBlockRendering.MeasureTextWidth(data.Subtitle, OutputFontSize(data, data.Theme.SubtitleFontSize), data.Font.Family, data.WeightOverride ?? 400));
        return sum;
    }
    internal static (double Minimum, double Maximum) MicroBounds(System.Collections.Generic.IReadOnlyList<double> values, double? requestedMinimum, double? requestedMaximum) {
        var minimum = requestedMinimum ?? Minimum(values); var maximum = requestedMaximum ?? Maximum(values);
        if (Math.Abs(maximum - minimum) < double.Epsilon) maximum = minimum + 1;
        return (minimum, maximum);
    }
}
