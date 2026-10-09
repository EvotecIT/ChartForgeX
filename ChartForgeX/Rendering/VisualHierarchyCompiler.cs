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
        if (series.Kind == ChartSeriesKind.Treemap) {
            var surface = new ChartTreemapSurface(chart, colors);
            if (surface.Scale != null && chart.Options.Treemap.ShowColorScaleLegend) return Array.Empty<VisualLegendEntry>();
            if (chart.Options.ShowPointLegend) return series.TreemapItems.Select((item, index) => (item, index))
                .Where(entry => series.Relationships!.Children(entry.index).Count == 0)
                .Select(entry => {
                    var blend = surface.Blend(entry.index);
                    return new VisualLegendEntry(entry.item.Label, blend.Color,
                        ChartRelationshipMetadata.SourceId("node", entry.item.Id), series.Kind, Pattern(series, entry.index),
                        ChartRelationshipPaint.State(series, entry.index), series.InteractionIdentityKey,
                        paint: blend.Paint, targetKind: "node", targetId: entry.item.Id);
                }).ToArray();
        }
        var color = ChartSeriesColours.Resolve(series, 0, colors);
        return new[] { new VisualLegendEntry(series.Name, color, "series-0", series.Kind,
            series.FillPattern, series.StateRole, series.InteractionIdentityKey, paint: VisualChartPaint.Series(series, color)) };
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        if (chart.Series.Count != 1) throw new InvalidOperationException("Prepared hierarchy charts require one series.");
        var series = chart.Series[0];
        if (!series.HasSourceData) { builder.AddDiagnostic(new VisualDiagnostic("hierarchy.no-data", "The hierarchy has no observations.")); return; }
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
            var metadata = ChartRelationshipMetadata.Link(series, target.Id, source.Id, target.Id, source.Label, target.Label, link.Index, link.Value);
            metadata["data-cfx-parent"] = source.Id; metadata["data-cfx-child"] = target.Id;
            using (builder.PushGroup(ChartRelationshipMetadata.SourceId("link", target.Id), "tree-link", metadata))
                builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(x1, y1), ChartPathCommand.CubicTo(mid, y1, mid, y2, x2, y2) }),
                    stroke: ChartColorMath.WithOpacity(color, .65), strokeWidth: width, role: "tree-link-path",
                    paint: VisualChartPaint.Stroke(ChartRelationshipPaint.Paint(series, color, source.Index).WithOpacity(ChartColorMath.WithOpacity(color, .65), .65)));
            var bounds = new ChartRect(x1, Math.Min(y1, y2) - width / 2, x2 - x1, Math.Abs(y2 - y1) + width);
            builder.AddRegion(new VisualSemanticRegion(ChartRelationshipMetadata.SourceId("link", target.Id), "tree-link", bounds, metadata["data-cfx-full-label"]));
        }
        foreach (var node in model.Nodes) {
            var b = new ChartRect(node.X, node.Y, model.NodeWidth, model.NodeHeight); var color = Color(series, node.Index, colors, node.Depth);
            var metadata = ChartRelationshipMetadata.Node(series, node.Id, node.Label, node.Index);
            metadata["data-cfx-state"] = ChartRelationshipPaint.State(series, node.Index).ToString();
            metadata["data-cfx-depth"] = N(node.Depth);
            if (node.Depth > 0) {
                var incoming = model.Links[series.Relationships!.IncomingLink(node.Index)];
                metadata["data-cfx-parent"] = model.Nodes[incoming.Parent].Id;
                metadata["data-cfx-value"] = N(incoming.Value);
                metadata["data-cfx-authored-weight"] = N(incoming.Value);
                metadata["data-cfx-source-link-index"] = N(incoming.Index);
            }
            using (builder.PushGroup(ChartRelationshipMetadata.SourceId("node", node.Id), "tree-node", metadata)) {
                builder.Rect(b, color, colors.Surface, context.Theme.AxisStrokeWidth, Math.Min(context.Theme.BarRadius, model.NodeHeight / 2), "tree-node-mark",
                    paint: new VisualScenePaintBinding(ChartRelationshipPaint.Paint(series, color, node.Index), SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                Pattern(builder, Rectangle(b), series, node.Index, color, "tree-node-pattern");
                if (series.ShowDataLabels ?? chart.Options.ShowDataLabels) Label(chart, context, builder, node.Label, b, color, node.Index, "tree-node-label", center: true);
            }
            builder.AddRegion(new VisualSemanticRegion(ChartRelationshipMetadata.SourceId("node", node.Id), "tree-node", b, node.Label + ": level " + N(node.Depth)));
        }
    }

    private static void Sunburst(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var model = ChartSunburstLayout.Compute(chart, plot); var series = chart.Series[0];
        double total = model.Nodes[model.Root].Value;
        if (!Finite(total)) throw new InvalidOperationException("Sunburst aggregate weights exceed the supported finite range.");
        foreach (var node in model.Nodes.OrderByDescending(node => node.Depth)) {
            double sweep = node.EndAngle - node.StartAngle;
            var color = Color(series, node.Index, colors, node.Depth == 0 ? 0 : node.Index + node.Depth - 1);
            var paint = ChartRelationshipPaint.Paint(series, color, node.Index);
            if (node.Depth == 0 && !ChartRelationshipPaint.HasExplicitColor(series, node.Index) && ChartRelationshipPaint.State(series, node.Index) == ChartSeriesState.None) {
                var source = color; color = ChartColorMath.Blend(colors.Surface, source, .28);
                paint = SvgPaint.Mix(color, colors.Surface, SvgColorRole.Surface, source, ChartRelationshipPaint.Role(series, node.Index), .28);
            }
            string formatted = ChartNumericFormatter.FormatValue(chart.Options, node.Value);
            var metadata = ChartRelationshipMetadata.Node(series, node.Id, node.Label, node.Index);
            metadata["data-cfx-state"] = ChartRelationshipPaint.State(series, node.Index).ToString();
            metadata["data-cfx-depth"] = N(node.Depth); metadata["data-cfx-value"] = N(node.Value);
            if (node.Parent >= 0) {
                metadata["data-cfx-parent"] = model.Nodes[node.Parent].Id;
                metadata["data-cfx-authored-weight"] = N(node.IncomingValue);
                metadata["data-cfx-source-link-index"] = N(series.Relationships!.IncomingLink(node.Index));
            }
            metadata["data-cfx-geometry-status"] = sweep > 0 ? "visible" : "precision-collapse";
            metadata["data-cfx-percent"] = N(node.Value / total); metadata["data-cfx-start-angle"] = N(node.StartAngle); metadata["data-cfx-sweep"] = N(sweep);
            metadata["data-cfx-inner-radius"] = N(node.InnerRadius); metadata["data-cfx-outer-radius"] = N(node.OuterRadius);
            using (builder.PushGroup(ChartRelationshipMetadata.SourceId("node", node.Id), "sunburst-segment", metadata)) {
                builder.Slice(model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, sweep, color, colors.Surface, 1, "sunburst-segment-mark",
                    paint: new VisualScenePaintBinding(paint, SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                var pattern = Pattern(series, node.Index);
                if (pattern != ChartFillPattern.None) builder.PatternSlice(model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, sweep, pattern,
                    ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: "sunburst-pattern");
                if (series.ShowDataLabels ?? chart.Options.ShowDataLabels) SunburstLabel(chart, context, builder, model, node, color);
            }
            var bounds = new ChartRect(model.CenterX - node.OuterRadius, model.CenterY - node.OuterRadius, node.OuterRadius * 2, node.OuterRadius * 2);
            builder.AddRegion(new VisualSemanticRegion(ChartRelationshipMetadata.SourceId("node", node.Id), "sunburst-segment", bounds, node.Label + ": " + formatted));
        }
    }

    private static string Id(string role, int index) => "series-0-" + role + "-" + N(index);
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static ChartColor Color(ChartSeries series, int index, VisualThemeColors colors, int? paletteIndex = null) => ChartRelationshipPaint.Color(series, index, colors, paletteIndex);
    private static ChartFillPattern Pattern(ChartSeries series, int index) => ChartRelationshipPaint.Pattern(series, index);
    private static ChartPath Rectangle(ChartRect b) => new(new[] { ChartPathCommand.MoveTo(b.X, b.Y), ChartPathCommand.LineTo(b.Right, b.Y), ChartPathCommand.LineTo(b.Right, b.Bottom), ChartPathCommand.LineTo(b.X, b.Bottom) });
    private static void Pattern(VisualSceneBuilder builder, ChartPath path, ChartSeries series, int index, ChartColor color, string role) {
        var pattern = Pattern(series, index); if (pattern != ChartFillPattern.None) builder.Pattern(path, pattern, ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: role);
    }
}
