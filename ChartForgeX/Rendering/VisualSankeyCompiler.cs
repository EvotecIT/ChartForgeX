using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Paints one canonical weighted DAG as native ribbons, bars and measured labels.</summary>
internal static partial class VisualSankeyCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) => chart.Series[0].ShowInLegend
        ? new[] { new VisualLegendEntry(chart.Series[0].Name, ChartSeriesColours.Resolve(chart.Series[0], 0, colors), "series-0", ChartSeriesKind.Sankey,
            chart.Series[0].FillPattern, chart.Series[0].StateRole, chart.Series[0].InteractionIdentityKey) } : Array.Empty<VisualLegendEntry>();

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        if (chart.Series.Count != 1 || chart.Series[0].Kind != ChartSeriesKind.Sankey) throw new InvalidOperationException("Prepared Sankey charts require one weighted-flow series.");
        var series = chart.Series[0]; var options = chart.Options.Sankey; var model = ChartSankeyLayout.Build(chart); var colors = context.Theme.Resolve(context.ThemeMode);
        if (model.Nodes.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("sankey.no-data", "The Sankey chart has no flows.")); return; }
        bool showLabels = series.ShowDataLabels != false;
        var styles = model.Nodes.Select(node => ChartRelationshipPaint.LabelStyle(chart, context, node.Index, colors.Foreground)).ToArray();
        var formatted = model.Nodes.Select(node => ChartNumericFormatter.FormatValue(chart.Options, node.Value)).ToArray();
        var labels = model.Nodes.Select(node => node.Label + " " + formatted[node.Index]).ToArray();
        double reserve = showLabels ? Math.Min(plot.Width * .22, model.Nodes.Max(node => builder.MeasureText(labels[node.Index], styles[node.Index]).Width) + 16) : 2;
        var nodePlot = new ChartRect(plot.X + reserve, plot.Y + 1, plot.Width - reserve * 2, Math.Max(1, plot.Height - 2));
        if (nodePlot.Width <= 1) throw new NotSupportedException("Sankey labels require a wider common viewport.");
        double nodeGap = options.NodeGap ?? Math.Max(12, context.Theme.Spacing * 1.5);
        ChartSankeyLayout.Layout(model, nodePlot, options, nodeGap);
        using (builder.PushGroup("series-0", "sankey-series", new Dictionary<string, string> {
            ["data-cfx-series"] = "0", ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-series-name"] = series.Name,
            ["data-cfx-state"] = series.StateRole.ToString(), ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
            ["data-cfx-weight-reference"] = N(model.WeightReference), ["data-cfx-normalized-weight-scale"] = N(model.NormalizedWeightScale),
            ["data-cfx-alignment"] = options.Alignment.ToString(), ["data-cfx-vertical-alignment"] = options.VerticalAlignment.ToString(),
            ["data-cfx-node-order"] = options.NodeOrder.ToString(), ["data-cfx-node-width"] = N(model.NodeWidth), ["data-cfx-node-gap"] = N(nodeGap)
        })) {
            foreach (var link in model.Links) {
                var source = model.Nodes[link.Source]; var target = model.Nodes[link.Target]; var color = ChartRelationshipPaint.Color(series, source.Index, colors, fillOverride: options.RibbonFill);
                string full = source.Label + " to " + target.Label + ": " + ChartNumericFormatter.FormatValue(chart.Options, link.Value);
                var path = ChartSankeyLayout.Ribbon(model, link);
                double y = Math.Min(link.SourceY, link.TargetY) - link.Width / 2;
                var bounds = new ChartRect(source.X + model.NodeWidth, y, target.X - source.X - model.NodeWidth, Math.Abs(link.TargetY - link.SourceY) + link.Width);
                var metadata = ChartRelationshipMetadata.Link(series, link.Id, source.Id, target.Id, source.Label, target.Label, link.Index, link.Value);
                metadata["data-cfx-full-label"] = full; metadata["data-cfx-width"] = N(link.Width);
                using (builder.PushGroup(ChartRelationshipMetadata.SourceId("link", link.Id), "sankey-link", metadata)) {
                    builder.Path(path, ChartColorMath.WithOpacity(color, options.RibbonOpacity), role: "sankey-ribbon", close: true,
                        paint: VisualChartPaint.Fill(ChartRelationshipPaint.Paint(series, color, source.Index, fillOverride: options.RibbonFill).WithOpacity(ChartColorMath.WithOpacity(color, options.RibbonOpacity), options.RibbonOpacity)));
                    var pattern = ChartRelationshipPaint.Pattern(series, source.Index);
                    if (pattern != ChartFillPattern.None) builder.Pattern(path, pattern, ChartColorMath.WithOpacity(color, .6), role: "sankey-ribbon-pattern", paint: ChartRelationshipPaint.Paint(series, color, source.Index, fillOverride: options.RibbonFill).WithOpacity(ChartColorMath.WithOpacity(color, .6), .6));
                }
                builder.AddRegion(new VisualSemanticRegion(ChartRelationshipMetadata.SourceId("link", link.Id), "sankey-link", bounds, full));
            }
            foreach (var node in model.Nodes) {
                var bounds = new ChartRect(node.X, node.Y, model.NodeWidth, node.Height); var color = ChartRelationshipPaint.Color(series, node.Index, colors, neutralDefault: true, fillOverride: options.NodeFill);
                var metadata = ChartRelationshipMetadata.Node(series, node.Id, node.Label, node.Index);
                metadata["data-cfx-layer"] = N(node.Layer); metadata["data-cfx-value"] = N(node.Value);
                metadata["data-cfx-order"] = N(node.Order);
                metadata["data-cfx-incoming"] = N(node.Incoming); metadata["data-cfx-outgoing"] = N(node.Outgoing);
                metadata["data-cfx-full-label"] = labels[node.Index];
                metadata["data-cfx-state"] = ChartRelationshipPaint.State(series, node.Index).ToString();
                using (builder.PushGroup(ChartRelationshipMetadata.SourceId("node", node.Id), "sankey-node", metadata)) {
                    builder.Rect(bounds, color, radius: options.NodeCornerRadius ?? context.Theme.BarRadius, role: "sankey-node-mark", paint: VisualChartPaint.Fill(ChartRelationshipPaint.Paint(series, color, node.Index, neutralDefault: true, fillOverride: options.NodeFill)));
                    var pattern = ChartRelationshipPaint.Pattern(series, node.Index);
                    if (pattern != ChartFillPattern.None) builder.Pattern(Rectangle(bounds), pattern, ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: "sankey-node-pattern");
                }
                builder.AddRegion(new VisualSemanticRegion(ChartRelationshipMetadata.SourceId("node", node.Id), "sankey-node", bounds, labels[node.Index]));
            }
            if (showLabels) Labels(builder, plot, model, styles, labels, colors, context.Theme.Spacing, options);
        }
    }

    private static ChartPath Rectangle(ChartRect b) => new(new[] { ChartPathCommand.MoveTo(b.X, b.Y), ChartPathCommand.LineTo(b.Right, b.Y), ChartPathCommand.LineTo(b.Right, b.Bottom), ChartPathCommand.LineTo(b.X, b.Bottom) });
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
