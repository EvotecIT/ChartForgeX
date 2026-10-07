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
        if (chart.Options.SankeyNodeStates.Any(pair => pair.Key < 0 || pair.Key >= model.Nodes.Count || !Enum.IsDefined(typeof(ChartSeriesState), pair.Value)))
            throw new InvalidOperationException("Sankey node states must reference an existing node and a defined semantic state.");
        bool showLabels = series.ShowDataLabels != false;
        var styles = model.Nodes.Select(node => Style(chart, context, node.Index, colors.Foreground)).ToArray();
        var formatted = model.Nodes.Select(node => ChartNumericFormatter.FormatValue(chart.Options, node.Value)).ToArray();
        var labels = model.Nodes.Select(node => node.Label + " " + formatted[node.Index]).ToArray();
        double reserve = showLabels ? Math.Min(plot.Width * .22, model.Nodes.Max(node => builder.MeasureText(labels[node.Index], styles[node.Index]).Width) + 16) : 2;
        var nodePlot = new ChartRect(plot.X + reserve, plot.Y + 1, plot.Width - reserve * 2, Math.Max(1, plot.Height - 2));
        if (nodePlot.Width <= 1) throw new NotSupportedException("Sankey labels require a wider common viewport.");
        ChartSankeyLayout.Layout(model, nodePlot, Math.Max(10, Math.Min(24, context.Theme.BarRadius * 4)), gap: Math.Max(12, context.Theme.Spacing * 1.5));
        using (builder.PushGroup("series-0", "sankey-series", new Dictionary<string, string> {
            ["data-cfx-series"] = "0", ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-series-name"] = series.Name,
            ["data-cfx-state"] = series.StateRole.ToString(), ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty, ["data-cfx-weight-scale"] = N(model.Scale)
        })) {
            foreach (var link in model.Links) {
                var source = model.Nodes[link.Source]; var target = model.Nodes[link.Target]; var color = Color(chart, colors, source.Index);
                string full = source.Label + " to " + target.Label + ": " + ChartNumericFormatter.FormatValue(chart.Options, link.Value);
                var path = ChartSankeyLayout.Ribbon(model, link);
                double y = Math.Min(link.SourceY, link.TargetY) - link.Width / 2;
                var bounds = new ChartRect(source.X + model.NodeWidth, y, target.X - source.X - model.NodeWidth, Math.Abs(link.TargetY - link.SourceY) + link.Width);
                using (builder.PushGroup(Id("link", link.Index), "sankey-link", new Dictionary<string, string> {
                    ["data-cfx-source"] = N(source.Index), ["data-cfx-target"] = N(target.Index), ["data-cfx-value"] = N(link.Value), ["data-cfx-width"] = N(link.Width),
                    ["data-cfx-source-label"] = source.Label, ["data-cfx-target-label"] = target.Label, ["data-cfx-full-label"] = full, ["data-cfx-point"] = N(link.Index * 2)
                })) {
                    builder.Path(path, ChartColorMath.WithOpacity(color, .35), role: "sankey-ribbon", close: true);
                    var pattern = Pattern(series, link.Index * 2);
                    if (pattern != ChartFillPattern.None) builder.Pattern(path, pattern, ChartColorMath.WithOpacity(color, .6), role: "sankey-ribbon-pattern");
                }
                builder.AddRegion(new VisualSemanticRegion(Id("link", link.Index), "sankey-link", bounds, full));
            }
            foreach (var node in model.Nodes) {
                var bounds = new ChartRect(node.X, node.Y, model.NodeWidth, node.Height); var color = Color(chart, colors, node.Index);
                using (builder.PushGroup(Id("node", node.Index), "sankey-node", new Dictionary<string, string> {
                    ["data-cfx-node"] = N(node.Index), ["data-cfx-layer"] = N(node.Layer), ["data-cfx-label"] = node.Label,
                    ["data-cfx-value"] = N(node.Value), ["data-cfx-incoming"] = N(node.Incoming), ["data-cfx-outgoing"] = N(node.Outgoing),
                    ["data-cfx-full-label"] = labels[node.Index], ["data-cfx-state"] = chart.Options.SankeyNodeStates.TryGetValue(node.Index, out var state) ? state.ToString() : series.StateRole.ToString()
                })) {
                    builder.Rect(bounds, color, radius: Math.Min(context.Theme.BarRadius, node.Height / 2), role: "sankey-node-mark");
                    var pattern = Pattern(series, node.Index);
                    if (pattern != ChartFillPattern.None) builder.Pattern(Rectangle(bounds), pattern, ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: "sankey-node-pattern");
                }
                builder.AddRegion(new VisualSemanticRegion(Id("node", node.Index), "sankey-node", bounds, labels[node.Index]));
            }
            if (showLabels) Labels(builder, plot, model, styles, labels, colors, context.Theme.Spacing);
        }
    }

    private static ChartColor Color(Chart chart, VisualThemeColors colors, int node) {
        var series = chart.Series[0];
        if (node < series.PointColors.Count && series.PointColors[node].HasValue) return series.PointColors[node]!.Value;
        if (series.Color.HasValue) return series.Color.Value;
        var role = chart.Options.SankeyNodeStates.TryGetValue(node, out var state) ? state : series.StateRole;
        return ChartSeriesColours.State(role, colors, colors.Palette[node % colors.Palette.Count]);
    }
    private static TextStyle Style(Chart chart, VisualRenderContext context, int node, ChartColor color) {
        var series = chart.Series[0];
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = color }));
        return node < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[node] != null ? series.PointDataLabelStyles[node]!.Resolve(style) : style;
    }
    private static ChartFillPattern Pattern(ChartSeries series, int point) => point < series.PointFillPatterns.Count && series.PointFillPatterns[point].HasValue ? series.PointFillPatterns[point]!.Value : series.FillPattern;
    private static ChartPath Rectangle(ChartRect b) => new(new[] { ChartPathCommand.MoveTo(b.X, b.Y), ChartPathCommand.LineTo(b.Right, b.Y), ChartPathCommand.LineTo(b.Right, b.Bottom), ChartPathCommand.LineTo(b.X, b.Bottom) });
    private static string Id(string role, int index) => "series-0-" + role + "-" + index.ToString(CultureInfo.InvariantCulture);
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
