using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Paints one canonical weighted DAG as native ribbons, bars and measured labels.</summary>
internal static partial class VisualSankeyCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) => chart.Series[0].ShowInLegend
        ? new[] { new VisualLegendEntry(chart.Series[0].Name, ChartSeriesColours.Resolve(chart.Series[0], 0, colors), "series-0", ChartSeriesKind.Sankey,
            chart.Series[0].FillPattern, chart.Series[0].StateRole, chart.Series[0].InteractionIdentityKey) } : Array.Empty<VisualLegendEntry>();

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        if (chart.Series.Count != 1 || chart.Series[0].Kind != ChartSeriesKind.Sankey) throw new InvalidOperationException("Prepared Sankey charts require one weighted-flow series.");
        var series = chart.Series[0]; var model = ChartSankeyLayout.Build(chart); var colors = context.Theme.Resolve(context.ThemeMode);
        if (model.Nodes.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("sankey.no-data", "The Sankey chart has no flows.")); return; }
        if (chart.Options.SankeyNodeStates.Any(pair => !series.Relationships!.ContainsNode(pair.Key) || !Enum.IsDefined(typeof(ChartSeriesState), pair.Value)))
            throw new InvalidOperationException("Sankey node states must reference an existing node and a defined semantic state.");
        bool showLabels = series.ShowDataLabels != false;
        var styles = model.Nodes.Select(node => Style(chart, context, node.Index, colors.Foreground)).ToArray();
        var formatted = model.Nodes.Select(node => ChartNumericFormatter.FormatValue(chart.Options, node.Value)).ToArray();
        var labels = model.Nodes.Select(node => node.Label + " " + formatted[node.Index]).ToArray();
        double reserve = showLabels ? Math.Min(plot.Width * .22, model.Nodes.Max(node => builder.MeasureText(labels[node.Index], styles[node.Index]).Width) + 16) : 2;
        var nodePlot = new ChartRect(plot.X + reserve, plot.Y + 1, plot.Width - reserve * 2, Math.Max(1, plot.Height - 2));
        if (nodePlot.Width <= 1) throw new NotSupportedException("Sankey labels require a wider common viewport.");
        ChartSankeyLayout.Layout(model, nodePlot, 10, gap: Math.Max(12, context.Theme.Spacing * 1.5));
        using (builder.PushGroup("series-0", "sankey-series", new Dictionary<string, string> {
            ["data-cfx-series"] = "0", ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-series-name"] = series.Name,
            ["data-cfx-state"] = series.StateRole.ToString(), ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
            ["data-cfx-weight-reference"] = N(model.WeightReference), ["data-cfx-normalized-weight-scale"] = N(model.NormalizedWeightScale)
        })) {
            foreach (var link in model.Links) {
                var source = model.Nodes[link.Source]; var target = model.Nodes[link.Target]; var color = Color(chart, colors, source.Index);
                string full = source.Label + " to " + target.Label + ": " + ChartNumericFormatter.FormatValue(chart.Options, link.Value);
                var path = ChartSankeyLayout.Ribbon(model, link);
                double y = Math.Min(link.SourceY, link.TargetY) - link.Width / 2;
                var bounds = new ChartRect(source.X + model.NodeWidth, y, target.X - source.X - model.NodeWidth, Math.Abs(link.TargetY - link.SourceY) + link.Width);
                var metadata = ChartRelationshipMetadata.Link(series, link.Id, source.Id, target.Id, source.Label, target.Label, link.Index, link.Value);
                metadata["data-cfx-full-label"] = full; metadata["data-cfx-width"] = N(link.Width);
                using (builder.PushGroup(ChartRelationshipMetadata.SourceId("link", link.Id), "sankey-link", metadata)) {
                    builder.Path(path, ChartColorMath.WithOpacity(color, .35), role: "sankey-ribbon", close: true,
                        paint: VisualChartPaint.Fill(Paint(chart, color, source.Index).WithOpacity(ChartColorMath.WithOpacity(color, .35), .35)));
                    var pattern = Pattern(series, source.Index);
                    if (pattern != ChartFillPattern.None) builder.Pattern(path, pattern, ChartColorMath.WithOpacity(color, .6), role: "sankey-ribbon-pattern", paint: Paint(chart, color, source.Index).WithOpacity(ChartColorMath.WithOpacity(color, .6), .6));
                }
                builder.AddRegion(new VisualSemanticRegion(ChartRelationshipMetadata.SourceId("link", link.Id), "sankey-link", bounds, full));
            }
            foreach (var node in model.Nodes) {
                var bounds = new ChartRect(node.X, node.Y, model.NodeWidth, node.Height); var color = Color(chart, colors, node.Index, neutralDefault: true);
                var metadata = ChartRelationshipMetadata.Node(series, node.Id, node.Label, node.Index);
                metadata["data-cfx-layer"] = N(node.Layer); metadata["data-cfx-value"] = N(node.Value);
                metadata["data-cfx-incoming"] = N(node.Incoming); metadata["data-cfx-outgoing"] = N(node.Outgoing);
                metadata["data-cfx-full-label"] = labels[node.Index];
                metadata["data-cfx-state"] = chart.Options.SankeyNodeStates.TryGetValue(node.Id, out var state) ? state.ToString() : series.StateRole.ToString();
                using (builder.PushGroup(ChartRelationshipMetadata.SourceId("node", node.Id), "sankey-node", metadata)) {
                    builder.Rect(bounds, color, radius: Math.Min(context.Theme.BarRadius, node.Height / 2), role: "sankey-node-mark", paint: VisualChartPaint.Fill(Paint(chart, color, node.Index, neutralDefault: true)));
                    var pattern = Pattern(series, node.Index);
                    if (pattern != ChartFillPattern.None) builder.Pattern(Rectangle(bounds), pattern, ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: "sankey-node-pattern");
                }
                builder.AddRegion(new VisualSemanticRegion(ChartRelationshipMetadata.SourceId("node", node.Id), "sankey-node", bounds, labels[node.Index]));
            }
            if (showLabels) Labels(builder, plot, model, styles, labels, colors, context.Theme.Spacing);
        }
    }

    private static ChartColor Color(Chart chart, VisualThemeColors colors, int node, bool neutralDefault = false) {
        var series = chart.Series[0];
        if (node < series.PointColors.Count && series.PointColors[node].HasValue) return series.PointColors[node]!.Value;
        if (series.Color.HasValue) return series.Color.Value;
        var role = chart.Options.SankeyNodeStates.TryGetValue(series.Nodes[node].Id, out var state) ? state : series.StateRole;
        return ChartSeriesColours.State(role, colors, neutralDefault ? colors.Status.Neutral.Fill : colors.Palette[node % colors.Palette.Count]);
    }
    private static SvgPaint Paint(Chart chart, ChartColor color, int node, bool neutralDefault = false) {
        var series = chart.Series[0];
        var explicitColor = series.Color.HasValue || node < series.PointColors.Count && series.PointColors[node].HasValue;
        var state = chart.Options.SankeyNodeStates.TryGetValue(series.Nodes[node].Id, out var configured) ? configured : series.StateRole;
        return SvgPaint.Of(color, explicitColor || state == ChartSeriesState.None && !neutralDefault ? SvgColorRole.Series : SvgColorRole.Status);
    }
    private static TextStyle Style(Chart chart, VisualRenderContext context, int node, ChartColor color) {
        var series = chart.Series[0];
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = color }));
        return node < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[node] != null ? series.PointDataLabelStyles[node]!.Resolve(style) : style;
    }
    private static ChartFillPattern Pattern(ChartSeries series, int point) => point < series.PointFillPatterns.Count && series.PointFillPatterns[point].HasValue ? series.PointFillPatterns[point]!.Value : series.FillPattern;
    private static ChartPath Rectangle(ChartRect b) => new(new[] { ChartPathCommand.MoveTo(b.X, b.Y), ChartPathCommand.LineTo(b.Right, b.Y), ChartPathCommand.LineTo(b.Right, b.Bottom), ChartPathCommand.LineTo(b.X, b.Bottom) });
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
