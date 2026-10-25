using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Rendering;

internal sealed partial class NativeTablePaintPlan {
    private void MicroVisual(NativeTableCell cell, double x, double y, double width, double height, int column) {
        var color = cell.MicroColor ?? VisualBlockRendering.PaletteAt(_data.Theme, column); var s = _data.Scale;
        var count = cell.Sparkline?.Values.Count ?? cell.Values.Count;
        _svg.Add(new SvgElement("g", new[] { A("data-cfx-role", "table-cell-microvisual"), A("data-cfx-kind", cell.MicroVisual.ToString()), A("data-cfx-values", count) }, group: true));
        using (_builder.PushGroup(null, "table-cell-microvisual", new Dictionary<string, string> { ["data-cfx-kind"] = cell.MicroVisual.ToString(), ["data-cfx-values"] = count.ToString(System.Globalization.CultureInfo.InvariantCulture) })) {
            if (cell.MicroVisual == NativeTableMicroVisual.MiniBars) {
                var metrics = VisualBlockRendering.FitRepeatedItems(cell.Values.Count, width, (cell.Values.Count > 8 ? 2d : 3d) * s, 2 * s);
                var bounds = MicroBounds(cell.Values, cell.Minimum, cell.Maximum);
                var minimum = bounds.Minimum; var maximum = bounds.Maximum;
                for (var i = 0; i < cell.Values.Count; i++) {
                    var ratio = Math.Max(0, Math.Min(1, (cell.Values[i] - minimum) / (maximum - minimum)));
                    var barHeight = Math.Max(2 * s, height * ratio);
                    Rect("table-cell-mini-bar", new ChartRect(x + i * (metrics.ItemWidth + metrics.Gap), y + height - barHeight, metrics.ItemWidth, barHeight),
                        Math.Min(3 * s, metrics.ItemWidth * .45), color.WithAlpha(i == cell.Values.Count - 1 ? (byte)230 : (byte)135));
                }
            } else {
                var data = cell.Sparkline ?? SparklineData.FromValues(cell.Values, cell.Minimum, cell.Maximum);
                foreach (var segment in ChartPointSegments.Split(SparklineLayout.Project(data, new ChartRect(x, y, width, height)))) {
                    var points = new ChartPoint[segment.Count == 1 ? 2 : segment.Count];
                    for (var i = 0; i < points.Length; i++) points[i] = segment[Math.Min(i, segment.Count - 1)];
                    Polyline(points, color, 2 * s, "table-cell-sparkline");
                }
            }
        }
        _svg.Add(new SvgElement(null, Array.Empty<KeyValuePair<string, string?>>()));
    }
    private void Badge(NativeTableCell cell, double x, double y, double width, double height) {
        var theme = _data.Theme; var s = _data.Scale; var scale = theme.DataLabelFontSize / 12;
        var color = cell.BadgeColor ?? VisualBlockRendering.StatusColor(theme, cell.BadgeStatus);
        var size = Math.Max(_data.Dense ? 10 : 9, (_data.Dense ? 10.5 : 11.5) * scale) * s;
        var caption = VisualBlockRendering.SingleLineText(cell.BadgeText);
        var natural = VisualBlockRendering.MeasureTextWidth(caption, size, _data.Font.Family, 700);
        var badgeWidth = Math.Min(width, Math.Max(28 * scale * s, natural + 18 * scale * s));
        var badgeHeight = Math.Min(22 * scale * s, Math.Max(18 * scale * s, height));
        var alignment = cell.Alignment ?? TextAlignment.Left;
        var plan = NativeBadgePaintPlan.Table(x, y, width, height, badgeWidth, badgeHeight, scale, s, alignment);
        var left = plan.Surface.X; var top = plan.Surface.Y;
        var fill = cell.BadgeTreatment == NativeTableBadgeTreatment.Solid ? color : cell.BadgeTreatment == NativeTableBadgeTreatment.Outline ? ChartColor.Transparent : color.WithAlpha(42);
        var stroke = cell.BadgeTreatment == NativeTableBadgeTreatment.Solid ? color : color.WithAlpha(cell.BadgeTreatment == NativeTableBadgeTreatment.Outline ? (byte)190 : (byte)84);
        var ink = cell.BadgeTreatment == NativeTableBadgeTreatment.Solid ? ChartColor.White : color;
        fill = Paint(fill); stroke = Paint(stroke);
        _svg.Add(new SvgElement("rect", new[] { A("data-cfx-role", "visual-badge"), A("data-cfx-style", cell.BadgeTreatment.ToString()),
            A("x", left), A("y", top), A("width", badgeWidth), A("height", badgeHeight), A("rx", plan.Radius), A("fill", fill.ToCss()), A("stroke", stroke.ToCss()) }));
        plan.AppendSurfaceTo(_builder, fill, stroke, s, "visual-badge");
        Text(caption, plan.Text.X, plan.Baseline, plan.Text.Width, TextAlignment.Center, ink, size, 700);
    }
    private static double Minimum(IReadOnlyList<double> values) { var value = double.PositiveInfinity; foreach (var item in values) value = Math.Min(value, item); return value; }
    private static double Maximum(IReadOnlyList<double> values) { var value = double.NegativeInfinity; foreach (var item in values) value = Math.Max(value, item); return value; }
}
