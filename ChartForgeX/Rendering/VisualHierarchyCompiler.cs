using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Native hierarchy marks use the canonical numeric layouts and retained, measured labels.</summary>
internal static partial class VisualHierarchyCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (!series.ShowInLegend) return Array.Empty<VisualLegendEntry>();
        if (series.Kind == ChartSeriesKind.Treemap && chart.Options.ShowPointLegend)
            return series.Points.Select((point, index) => new VisualLegendEntry(Category(chart, point.X), Color(series, index, colors), Id("point", index), series.Kind,
                Pattern(series, index), series.StateRole, series.InteractionIdentityKey)).ToArray();
        return new[] { new VisualLegendEntry(series.Name, ChartSeriesColours.Resolve(series, 0, colors), "series-0", series.Kind,
            series.FillPattern, series.StateRole, series.InteractionIdentityKey) };
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        if (chart.Series.Count != 1) throw new InvalidOperationException("Prepared hierarchy charts require one series.");
        var series = chart.Series[0];
        if (series.Points.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("hierarchy.no-data", "The hierarchy has no observations.")); return; }
        if (plot.Width <= 2 || plot.Height <= 2) { builder.AddDiagnostic(new VisualDiagnostic("hierarchy.insufficient-space", "No plotting area remains.")); return; }
        var colors = context.Theme.Resolve(context.ThemeMode);
        // Keep stroke coverage inside the common content rectangle.
        plot = new ChartRect(plot.X + 1, plot.Y + 1, plot.Width - 2, plot.Height - 2);
        using (builder.PushGroup("series-0", "hierarchy-series", new Dictionary<string, string> {
            ["data-cfx-series"] = "0", ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-series-name"] = series.Name,
            ["data-cfx-kind"] = series.Kind.ToString(), ["data-cfx-state"] = series.StateRole.ToString(), ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty
        })) {
            if (series.Kind == ChartSeriesKind.Treemap) Treemap(chart, context, builder, plot, colors);
            else {
                ValidateTree(series, chart.Options.TreeNodeLabels.Count);
                if (series.Kind == ChartSeriesKind.Tree) Tree(chart, context, builder, plot, colors);
                else if (series.Kind == ChartSeriesKind.Sunburst) Sunburst(chart, context, builder, plot, colors);
                else throw new NotSupportedException("Unknown hierarchy family.");
            }
        }
    }

    private static void Tree(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var model = ChartTreeLayout.Build(chart, plot, fitViewport: true); var series = chart.Series[0];
        foreach (var link in model.Links) {
            var source = model.Nodes[link.Parent]; var target = model.Nodes[link.Child];
            double x1 = source.X + model.NodeWidth, y1 = source.Y + model.NodeHeight / 2, x2 = target.X, y2 = target.Y + model.NodeHeight / 2;
            double mid = (x1 + x2) / 2;
            var color = Color(series, source.Index, colors, source.Depth);
            double width = Math.Max(1, context.Theme.SeriesStrokeWidth * (1 + link.Value / model.MaxLinkValue));
            using (builder.PushGroup(Id("link", link.Child), "tree-link", new Dictionary<string, string> {
                ["data-cfx-parent"] = N(link.Parent), ["data-cfx-child"] = N(link.Child), ["data-cfx-value"] = N(link.Value),
                ["data-cfx-source-label"] = source.Label, ["data-cfx-target-label"] = target.Label
            })) builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(x1, y1), ChartPathCommand.CubicTo(mid, y1, mid, y2, x2, y2) }),
                stroke: ChartColorMath.WithOpacity(color, .65), strokeWidth: width, role: "tree-link-path",
                paint: VisualChartPaint.Stroke(VisualChartPaint.Series(series, color, source.Index).WithOpacity(ChartColorMath.WithOpacity(color, .65), .65)));
        }
        foreach (var node in model.Nodes) {
            var b = new ChartRect(node.X, node.Y, model.NodeWidth, model.NodeHeight); var color = Color(series, node.Index, colors, node.Depth);
            var metadata = Metadata(node.Label, node.Index, node.Depth, 0); metadata["data-cfx-label"] = node.Label;
            metadata.Remove("data-cfx-value");
            if (node.Depth > 0) metadata["data-cfx-value"] = N(model.Links.First(link => link.Child == node.Index).Value);
            using (builder.PushGroup(Id("node", node.Index), "tree-node", metadata)) {
                builder.Rect(b, color, colors.Surface, context.Theme.AxisStrokeWidth, Math.Min(context.Theme.BarRadius, model.NodeHeight / 2), "tree-node-mark",
                    paint: new VisualScenePaintBinding(VisualChartPaint.Series(series, color, node.Index), SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                Pattern(builder, Rectangle(b), series, node.Index, color, "tree-node-pattern");
                if (series.ShowDataLabels != false) Label(chart, context, builder, node.Label, b, color, node.Index, "tree-node-label", center: true);
            }
            builder.AddRegion(new VisualSemanticRegion(Id("node", node.Index), "tree-node", b, node.Label + ": level " + N(node.Depth)));
        }
    }

    private static void Treemap(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.Points.Any(point => !Finite(point.X) || !Finite(point.Y) || point.Y < 0)) throw new InvalidOperationException("Treemap values must be finite and non-negative.");
        if (!Finite(series.Points.Sum(point => point.Y))) throw new InvalidOperationException("Treemap aggregate weights exceed the finite range.");
        var tiles = ChartTreemapLayout.Compute(series, plot);
        if (tiles.Count == 0) builder.AddDiagnostic(new VisualDiagnostic("hierarchy.no-data", "The treemap has no positive values."));
        foreach (var tile in tiles) {
            var color = Color(series, tile.PointIndex, colors); string label = Category(chart, tile.Point.X), value = ChartNumericFormatter.FormatValue(chart.Options, tile.Point.Y);
            string full = tile.PointIndex < series.PointLabels.Count && series.PointLabels[tile.PointIndex] != null ? series.PointLabels[tile.PointIndex]! : label;
            var metadata = Metadata(label, tile.PointIndex, 0, tile.Point.Y); metadata["data-cfx-full-label"] = full; metadata["data-cfx-formatted-value"] = value;
            using (builder.PushGroup(Id("point", tile.PointIndex), "treemap-tile", metadata)) {
                builder.Rect(tile.Rect, color, radius: Math.Min(context.Theme.BarRadius, Math.Min(tile.Rect.Width, tile.Rect.Height) * .1), role: "treemap-tile-mark", paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color, tile.PointIndex)));
                Pattern(builder, Rectangle(tile.Rect), series, tile.PointIndex, color, "treemap-pattern");
                if (series.ShowDataLabels != false) Label(chart, context, builder, full + "\n" + value, tile.Rect, color, tile.PointIndex, "treemap-label");
            }
            builder.AddRegion(new VisualSemanticRegion(Id("point", tile.PointIndex), "treemap-tile", tile.Rect, full + ": " + value));
        }
        for (int index = 0; index < series.Points.Count; index++) if (series.Points[index].Y == 0) {
            string label = Category(chart, series.Points[index].X), value = ChartNumericFormatter.FormatValue(chart.Options, 0);
            using (builder.PushGroup(Id("point", index), "treemap-zero-value", Metadata(label, index, 0, 0))) { }
            builder.AddRegion(new VisualSemanticRegion(Id("point", index), "treemap-zero-value", new ChartRect(plot.X, plot.Y, 0, 0), label + ": " + value));
        }
    }

    private static void Sunburst(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var model = ChartSunburstLayout.Compute(chart, plot); var series = chart.Series[0];
        double total = model.Nodes[model.Root].Value;
        if (!Finite(total)) throw new InvalidOperationException("Sunburst aggregate weights exceed the supported finite range.");
        foreach (var node in model.Nodes.OrderByDescending(node => node.Depth)) {
            double sweep = node.EndAngle - node.StartAngle, mid = node.StartAngle + sweep / 2;
            var color = Color(series, node.Index, colors, node.Depth == 0 ? 0 : node.Index + node.Depth - 1);
            var paint = VisualChartPaint.Series(series, color, node.Index);
            if (node.Depth == 0 && !series.Color.HasValue && series.StateRole == ChartSeriesState.None) {
                var source = color; color = ChartColorMath.Blend(colors.Surface, source, .28);
                paint = SvgPaint.Mix(color, colors.Surface, SvgColorRole.Surface, source, VisualChartPaint.SeriesRole(series, node.Index), .28);
            }
            string formatted = ChartNumericFormatter.FormatValue(chart.Options, node.Value);
            var metadata = Metadata(node.Label, node.Index, node.Depth, node.Value); metadata["data-cfx-parent"] = N(node.Parent);
            metadata["data-cfx-authored-weight"] = N(node.IncomingValue);
            metadata["data-cfx-percent"] = N(node.Value / total); metadata["data-cfx-start-angle"] = N(node.StartAngle); metadata["data-cfx-sweep"] = N(sweep);
            metadata["data-cfx-inner-radius"] = N(node.InnerRadius); metadata["data-cfx-outer-radius"] = N(node.OuterRadius);
            using (builder.PushGroup(Id("node", node.Index), "sunburst-segment", metadata)) {
                builder.Slice(model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, sweep, color, colors.Surface, 1, "sunburst-segment-mark",
                    paint: new VisualScenePaintBinding(paint, SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                var pattern = Pattern(series, node.Index);
                if (pattern != ChartFillPattern.None) builder.PatternSlice(model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, sweep, pattern,
                    ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: "sunburst-pattern");
                if (series.ShowDataLabels != false) {
                    double radius = node.Depth == 0 ? 0 : (node.InnerRadius + node.OuterRadius) / 2;
                    double thickness = node.OuterRadius - node.InnerRadius;
                    double width = node.Depth == 0 ? node.OuterRadius * 1.5 : Math.Min(thickness * .65, 2 * radius * Math.Sin(Math.Min(Math.PI / 2, sweep / 2)) * .45);
                    double height = Math.Min(thickness * .5, width);
                    double x = model.CenterX + Math.Cos(mid) * radius, y = model.CenterY + Math.Sin(mid) * radius;
                    Label(chart, context, builder, node.Label, new ChartRect(x - width / 2, y - height / 2, width, height), color, node.Index, "sunburst-label", center: true);
                }
            }
            var bounds = new ChartRect(model.CenterX - node.OuterRadius, model.CenterY - node.OuterRadius, node.OuterRadius * 2, node.OuterRadius * 2);
            builder.AddRegion(new VisualSemanticRegion(Id("node", node.Index), "sunburst-segment", bounds, node.Label + ": " + formatted));
        }
    }

    private static Dictionary<string, string> Metadata(string label, int index, int depth, double value) => new() {
        ["data-cfx-label"] = label, ["data-cfx-point"] = N(index), ["data-cfx-depth"] = N(depth), ["data-cfx-value"] = N(value)
    };
    private static string Category(Chart chart, double value) => ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxisLabels, value) ?? ChartAxisValueFormatter.Format(chart.Options.XAxis, value);
    private static string Id(string role, int index) => "series-0-" + role + "-" + N(index);
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static ChartColor Color(ChartSeries series, int index, VisualThemeColors colors, int? paletteIndex = null) => index < series.PointColors.Count && series.PointColors[index].HasValue
        ? series.PointColors[index]!.Value : series.Color ?? ChartSeriesColours.State(series.StateRole, colors, colors.Palette[(paletteIndex ?? index) % colors.Palette.Count]);
    private static ChartFillPattern Pattern(ChartSeries series, int index) => index < series.PointFillPatterns.Count && series.PointFillPatterns[index].HasValue ? series.PointFillPatterns[index]!.Value : series.FillPattern;
    private static ChartPath Rectangle(ChartRect b) => new(new[] { ChartPathCommand.MoveTo(b.X, b.Y), ChartPathCommand.LineTo(b.Right, b.Y), ChartPathCommand.LineTo(b.Right, b.Bottom), ChartPathCommand.LineTo(b.X, b.Bottom) });
    private static void Pattern(VisualSceneBuilder builder, ChartPath path, ChartSeries series, int index, ChartColor color, string role) {
        var pattern = Pattern(series, index); if (pattern != ChartFillPattern.None) builder.Pattern(path, pattern, ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: role);
    }
}
