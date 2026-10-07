using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles geographic points, reusable region geometry and categorical hex tiles into one numeric scene.</summary>
internal static partial class VisualMapCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) =>
        chart.Series.Where(series => series.ShowInLegend).Select(series => new VisualLegendEntry(series.Name,
            ChartSeriesColours.Resolve(series, 0, colors), "series-0", series.Kind, series.FillPattern, series.StateRole, series.InteractionIdentityKey)).ToArray();

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        if (chart.Series.Count != 1) throw new InvalidOperationException("Prepared map charts require one map series.");
        var series = chart.Series[0];
        if (!Finite(chart.Options.MapRegionStrokeWidth) || chart.Options.MapRegionStrokeWidth < 0)
            throw new InvalidOperationException("Map region stroke width must be non-negative and finite.");
        var colors = context.Theme.Resolve(context.ThemeMode);
        if (series.Kind == ChartSeriesKind.DottedMap) { Dotted(chart, context, builder, plot, colors); return; }
        if (series.Points.Any(point => !Finite(point.X) || !Finite(point.Y) || point.Y < 0))
            throw new InvalidOperationException("Map values must be finite and non-negative.");
        var minimum = series.Points.Count == 0 ? 0 : series.Points.Min(point => point.Y);
        var maximum = series.Points.Count == 0 ? 0 : series.Points.Max(point => point.Y);
        var layout = ScaleLayout(chart, context, builder, plot, minimum, maximum);
        using (builder.PushGroup("series-0", "map-series", new Dictionary<string, string> {
            ["data-cfx-series"] = "0", ["data-cfx-series-key"] = series.InteractionIdentityKey,
            ["data-cfx-label"] = series.Name, ["data-cfx-min-value"] = N(minimum), ["data-cfx-max-value"] = N(maximum)
        })) {
            if (series.Kind == ChartSeriesKind.RegionMap) Regions(chart, context, builder, layout.Map, colors, minimum, maximum);
            else if (series.Kind == ChartSeriesKind.TileMap) Tiles(chart, context, builder, layout.Map, colors, minimum, maximum);
            else throw new InvalidOperationException("The series is not a map family.");
            DrawScale(chart, context, builder, layout, colors, minimum, maximum);
        }
    }

    private static Dictionary<string, int> Values(Chart chart, Func<string, string?> resolve) {
        var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < chart.Series[0].Points.Count; index++) {
            var key = ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxisLabels, chart.Series[0].Points[index].X);
            var code = key == null ? null : resolve(key);
            if (code == null) throw new InvalidOperationException("A map observation must identify a known, unambiguous region.");
            if (values.ContainsKey(code)) throw new InvalidOperationException("Map observations must contain one aggregated value per region.");
            values.Add(code, index);
        }
        return values;
    }

    private static ChartColor? High(ChartSeries series, VisualThemeColors colors) => series.Color ??
        (series.StateRole == ChartSeriesState.None ? (ChartColor?)null : ChartSeriesColours.State(series.StateRole, colors, colors.Palette[0]));
    private static ChartColor Fill(Chart chart, VisualThemeColors colors, int? index, double min, double max) {
        if (!index.HasValue) return ChartHeatmapSurface.MapNoDataColor(chart, colors);
        var series = chart.Series[0]; var point = index.Value;
        return ChartHeatmapSurface.MapColor(chart, colors,
            point < series.PointColors.Count ? series.PointColors[point] : null, High(series, colors), series.Points[point].Y, min, max);
    }
    private static ChartFillPattern Pattern(ChartSeries series, int? index) => index.HasValue && index.Value < series.PointFillPatterns.Count && series.PointFillPatterns[index.Value].HasValue
        ? series.PointFillPatterns[index.Value]!.Value : series.FillPattern;
    private static TextStyle TickStyle(Chart chart, VisualRenderContext context, ChartColor? color = null) => chart.Options.TickLabelStyle.Resolve(new TextStyle {
        Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = color ?? context.Theme.Resolve(context.ThemeMode).MutedForeground
    });
    private static string Formatted(Chart chart, int? source) => source.HasValue ? ChartNumericFormatter.FormatValue(chart.Options, chart.Series[0].Points[source.Value].Y) : chart.Options.Labels.NoData;
    private static void Surface(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect bounds, VisualThemeColors colors, string role) {
        if (!chart.Options.ShowMapSurface) return;
        builder.Rect(bounds, ChartColorMath.WithOpacity(ChartColorMath.Blend(colors.Surface, colors.Border, .12), .16),
            ChartColorMath.WithOpacity(colors.Border, .16), context.Theme.GridStrokeWidth, Math.Min(context.Theme.Spacing, Math.Min(bounds.Width, bounds.Height) / 8), role);
    }
    private static Dictionary<string, string> Source(Chart chart, string code, string name, int? source, double? value, string full, double min, double max) {
        var metadata = new Dictionary<string, string> {
            ["data-cfx-region"] = code, ["data-cfx-region-name"] = name, ["data-cfx-label"] = name, ["data-cfx-point"] = source.HasValue ? N(source.Value) : "-1",
            ["data-cfx-empty"] = source.HasValue ? "false" : "true", ["data-cfx-value"] = value.HasValue ? N(value.Value) : "",
            ["data-cfx-formatted-value"] = full, ["aria-label"] = name + " (" + code + "): " + full
        };
        if (!value.HasValue) metadata["data-cfx-status"] = "empty";
        else {
            var ratio = ChartHeatmapSurface.MapRatio(chart, value.Value, min, max);
            metadata["data-cfx-level"] = N(ChartHeatmapSurface.Level(ratio));
            var status = chart.Options.MapColorScale == null ? ChartHeatmapSurface.CellStatus(chart, ratio) : null;
            if (status != null) metadata["data-cfx-status"] = status;
        }
        return metadata;
    }
    private static ChartRect Fit(ChartRect plot, double width, double height, double inset = 0) {
        var availableWidth = Math.Max(0, plot.Width - inset * 2); var availableHeight = Math.Max(0, plot.Height - inset * 2);
        var scale = Math.Min(availableWidth / width, availableHeight / height);
        return new ChartRect(plot.Left + (plot.Width - width * scale) / 2, plot.Top + (plot.Height - height * scale) / 2, width * scale, height * scale);
    }
    private static ChartPoint Project(ChartPoint point, ChartRect source, ChartRect target) => new(
        target.Left + (point.X - source.Left) / source.Width * target.Width, target.Top + (point.Y - source.Top) / source.Height * target.Height);
    private static ChartRect Bounds(IEnumerable<ChartPoint> points) {
        var array = points.ToArray();
        if (array.Length == 0) return new ChartRect(0, 0, 0, 0);
        var x = array.Min(point => point.X); var y = array.Min(point => point.Y);
        return new ChartRect(x, y, array.Max(point => point.X) - x, array.Max(point => point.Y) - y);
    }
    private static ChartRect Intersect(ChartRect a, ChartRect b) => new(Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top),
        Math.Max(0, Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left)), Math.Max(0, Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top)));
    private static ChartPath Path(IEnumerable<IReadOnlyList<ChartPoint>> rings) {
        var commands = new List<ChartPathCommand>();
        foreach (var ring in rings) for (var index = 0; index < ring.Count; index++)
            commands.Add(index == 0 ? ChartPathCommand.MoveTo(ring[index].X, ring[index].Y) : ChartPathCommand.LineTo(ring[index].X, ring[index].Y));
        return new ChartPath(commands);
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
