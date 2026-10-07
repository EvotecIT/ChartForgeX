using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    private static void Pictorial(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var options = chart.Options; var series = chart.Series[0]; var colors = context.Theme.Resolve(context.ThemeMode);
        var maximum = options.PictorialMaximum ?? series.Points.Max(point => point.Y);
        var columns = options.PictorialColumns; var unit = options.PictorialValuePerSymbol;
        var showValues = options.ShowPictorialValues;
        var rows = new int[series.Points.Count]; var requestedRows = new double[rows.Length];
        // A viewport cannot display arbitrarily many unit symbols. Keep full values and report omitted rows.
        var rowBudget = Math.Max(1, (int)Math.Min(256, Math.Floor(plot.Height / rows.Length / 4)));
        for (var index = 0; index < rows.Length; index++) {
            requestedRows[index] = unit.HasValue ? Math.Max(1, Math.Ceiling(series.Points[index].Y / unit.Value / columns)) : 1;
            rows[index] = (int)Math.Min(rowBudget, requestedRows[index]);
        }
        if (rows.Where((row, index) => row < requestedRows[index]).Any()) builder.AddDiagnostic(new VisualDiagnostic(
            "pictorial.overflow", "Some unit-symbol rows do not fit the fixed viewport; complete values remain in descriptive regions."));
        var totalRows = rows.Sum();
        var itemGap = Math.Min(context.Theme.Spacing, plot.Height / rows.Length * .08);
        var rowHeight = Math.Max(0, (plot.Height - itemGap * (rows.Length - 1)) / totalRows);
        var labelWidth = plot.Width * .25; var valueWidth = showValues ? plot.Width * .14 : 0;
        var margin = Math.Min(context.Theme.Spacing, plot.Width * .025);
        var symbolWidth = Math.Max(0, plot.Width - labelWidth - valueWidth - margin * (showValues ? 2 : 1));
        var gap = Math.Min(context.Theme.Spacing / 2, symbolWidth / columns * .16);
        var slot = Math.Max(0, (symbolWidth - gap * (columns - 1)) / columns);
        var size = Math.Min(slot, Math.Min(rowHeight, slot) * .82 * options.PictorialSymbolScale);
        var startX = plot.Left + labelWidth + margin;
        var custom = CustomPictorialPath(options, size);
        using var chartGroup = builder.PushGroup("pictorial-chart", "pictorial-chart", new Dictionary<string, string> {
            ["data-cfx-shape"] = options.PictorialShape.ToString(), ["data-cfx-custom-symbol"] = custom != null ? "true" : "false",
            ["data-cfx-columns"] = N(columns), ["data-cfx-maximum"] = N(maximum),
            ["data-cfx-value-per-symbol"] = unit.HasValue ? N(unit.Value) : "proportional",
            ["data-cfx-show-values"] = showValues ? "true" : "false", ["data-cfx-symbol-scale"] = N(options.PictorialSymbolScale),
            ["data-cfx-empty-opacity"] = N(options.PictorialEmptyOpacity)
        });
        var rowOffset = 0;
        for (var index = 0; index < rows.Length; index++) {
            var top = plot.Top + rowOffset * rowHeight + index * itemGap;
            var itemHeight = rows[index] * rowHeight;
            var bounds = new ChartRect(plot.Left, top, plot.Width, itemHeight);
            var raw = series.Points[index].Y;
            var filled = unit.HasValue ? raw / unit.Value : maximum <= 0 ? 0 : raw / maximum * columns;
            var color = Color(series, index, colors);
            using (Point(chart, builder, index, "pictorial-item", bounds, extra: new Dictionary<string, string> {
                ["data-cfx-required-symbol-rows"] = double.IsInfinity(requestedRows[index]) ? "overflow" : N(requestedRows[index]),
                ["data-cfx-rendered-symbol-rows"] = N(rows[index]), ["data-cfx-truncated"] = rows[index] < requestedRows[index] ? "true" : "false"
            })) {
                VisualStateSceneTools.Text(builder, Category(chart, index), new ChartRect(plot.Left, top, labelWidth, itemHeight),
                    VisualStateSceneTools.TickStyle(chart, context), "pictorial-label", Id(index) + "-label", TextAlignment.Right, shrink: true);
                for (var row = 0; row < rows[index]; row++) for (var column = 0; column < columns; column++) {
                    var amount = Math.Max(0, Math.Min(1, filled - row * columns - column));
                    var x = startX + column * (slot + gap) + (slot - size) / 2;
                    var y = top + row * rowHeight + (rowHeight - size) / 2;
                    var symbolBounds = new ChartRect(x, y, size, size);
                    var path = PictorialPath(options.PictorialShape, symbolBounds, custom);
                    using (builder.PushGroup(Id(index) + "-symbol-" + row + "-" + column, "pictorial-symbol", new Dictionary<string, string> {
                        ["data-cfx-point"] = N(index), ["data-cfx-row"] = N(row), ["data-cfx-column"] = N(column),
                        ["data-cfx-fill"] = N(amount), ["data-cfx-partial-fill"] = amount > 0 && amount < 1 ? "clip" : "none"
                    })) {
                        if (amount < 1) builder.Path(path, colors.Border.WithOpacity(options.PictorialEmptyOpacity), role: "pictorial-empty", close: true);
                        if (amount > 0) {
                            using (builder.PushClip(new ChartRect(x, y, size * amount, size))) {
                                builder.Path(path, color, role: "pictorial-fill", close: true);
                                Pattern(chart, builder, index, path, color);
                            }
                        }
                    }
                }
                if (showValues) VisualStateSceneTools.Text(builder, VisualStateSceneTools.Value(chart, series, index, raw),
                    new ChartRect(plot.Right - valueWidth, top, valueWidth, itemHeight), Style(chart, context, index, colors.Foreground),
                    "pictorial-value", Id(index) + "-value", shrink: true);
            }
            rowOffset += rows[index];
        }
    }

    private static IReadOnlyList<IReadOnlyList<ChartPoint>>? CustomPictorialPath(ChartOptions options, double size) {
        if (options.PictorialSvgPathData == null) return null;
        var source = options.PictorialSvgPathViewBox;
        if (double.IsNaN(source.Left) || double.IsInfinity(source.Left) || double.IsNaN(source.Top) || double.IsInfinity(source.Top)
            || double.IsNaN(source.Width) || double.IsInfinity(source.Width) || double.IsNaN(source.Height) || double.IsInfinity(source.Height))
            throw new InvalidOperationException("Pictorial custom-path viewBox must be finite.");
        // Parse only the user-supplied input, once. Both painters receive this same detached numeric shape.
        var paths = ChartMapPathParser.ParseSubpaths(options.PictorialSvgPathData, Math.Max(size / source.Width, size / source.Height) * 4);
        if (paths.Count == 0 || paths.All(path => path.Points.Count < 3)) throw new InvalidOperationException("Pictorial custom path must contain a fillable contour.");
        return paths.Select(path => (IReadOnlyList<ChartPoint>)path.Points.Select(point =>
            new ChartPoint((point.X - source.Left) / source.Width, (point.Y - source.Top) / source.Height)).ToArray()).ToArray();
    }
}
