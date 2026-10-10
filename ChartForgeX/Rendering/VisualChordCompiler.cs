using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Builds one native weighted ribbon scene; SVG and PNG consume the same prepared paths and labels.</summary>
internal static partial class VisualChordCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) => chart.Series[0].ShowInLegend
        ? new[] { new VisualLegendEntry(chart.Series[0].Name, ChartSeriesColours.Resolve(chart.Series[0], 0, colors), "series-0", ChartSeriesKind.Chord,
            chart.Series[0].FillPattern, chart.Series[0].StateRole, chart.Series[0].InteractionIdentityKey) } : Array.Empty<VisualLegendEntry>();

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        if (chart.Series.Count != 1 || chart.Series[0].Kind != ChartSeriesKind.Chord) throw new InvalidOperationException("Prepared chord charts require one weighted-flow series.");
        var series = chart.Series[0]; var options = chart.Options.Chord; var colors = context.Theme.Resolve(context.ThemeMode);
        var styles = series.Nodes.Select((_, index) => ChartRelationshipPaint.LabelStyle(chart, context, index, colors.Foreground)).ToArray();
        var labels = series.Nodes.Select((node, index) => Label(chart, index)).ToArray();
        var showLabels = series.ShowDataLabels != false && options.LabelContent != ChartChordLabelContent.None;
        var model = ChartChordLayout.Compute(series, options, plot);
        // Semantic-only nodes have no rendered caption, including positive totals whose native arcs collapse.
        var captionNodes = showLabels ? model.Nodes.Where(node => node.IsVisible).ToArray() : Array.Empty<ChartChordNode>();
        var reserveX = captionNodes.Length > 0 ? Math.Min(plot.Width * .22, captionNodes.Select(node => builder.MeasureText(labels[node.Index], styles[node.Index]).Width).Max() + context.Theme.Spacing) : 0;
        var reserveY = captionNodes.Length > 0 ? Math.Min(plot.Height * .2, captionNodes.Select(node => builder.MeasureText(labels[node.Index], styles[node.Index]).Height).Max() + context.Theme.Spacing) : 0;
        var circle = new ChartRect(plot.Left + reserveX, plot.Top + reserveY, Math.Max(0, plot.Width - reserveX * 2), Math.Max(0, plot.Height - reserveY * 2));
        if (captionNodes.Length > 0) model = ChartChordLayout.Compute(series, options, circle);
        if (model.WeightReference == 0) builder.AddDiagnostic(new VisualDiagnostic("chord.no-positive-flow", "The chord chart has no positive flows; zero-valued source facts retain their identities without filled arcs or ribbons."));
        if (model.OuterRadius <= 0) builder.AddDiagnostic(new VisualDiagnostic("chord.insufficient-space", "No positive chord radius remains inside the common viewport."));
        using (builder.PushGroup("series-0", "chord-series", new Dictionary<string, string> {
            ["data-cfx-series"] = "0", ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-series-name"] = series.Name,
            ["data-cfx-kind"] = series.Kind.ToString(), ["data-cfx-coordinate-system"] = "polar",
            ["data-cfx-state"] = series.StateRole.ToString(), ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
            ["data-cfx-weight-reference"] = N(model.WeightReference), ["data-cfx-normalized-angular-scale"] = N(model.NormalizedAngularScale),
            ["data-cfx-node-order"] = "source", ["data-cfx-center-x"] = N(model.CenterX), ["data-cfx-center-y"] = N(model.CenterY)
        })) {
            foreach (var link in model.Links) Link(chart, builder, model, link, colors);
            foreach (var node in model.Nodes) Node(chart, builder, model, node, colors);
            if (showLabels && model.WeightReference > 0 && model.OuterRadius > 0) Labels(chart, context, builder, plot, model, styles, labels, colors);
        }
    }

    private static void Link(Chart chart, VisualSceneBuilder builder, ChartChordModel model, ChartChordLink link, VisualThemeColors colors) {
        var series = chart.Series[0]; var source = model.Nodes[link.Source]; var target = model.Nodes[link.Target];
        var metadata = ChartRelationshipMetadata.Link(series, link.Fact.Id, source.Fact.Id, target.Fact.Id, source.Fact.Label, target.Fact.Label, link.Index, link.Fact.Value);
        metadata["data-cfx-coordinate-system"] = "polar";
        metadata["data-cfx-source-start-angle"] = N(link.SourceStart); metadata["data-cfx-source-sweep"] = N(link.Sweep);
        metadata["data-cfx-target-start-angle"] = N(link.TargetStart); metadata["data-cfx-target-sweep"] = N(link.Sweep);
        metadata["data-cfx-direction"] = "source-to-target"; metadata["data-cfx-direction-cue"] = chart.Options.Chord.DirectionCue.ToString();
        metadata["data-cfx-state"] = ChartRelationshipPaint.State(series, source.Index).ToString();
        metadata["data-cfx-full-label"] = source.Fact.Label + " to " + target.Fact.Label + ": " + ChartNumericFormatter.FormatValue(chart.Options, link.Fact.Value);
        var visible = link.IsVisible && model.InnerRadius > 0;
        metadata["data-cfx-geometry-status"] = visible ? "visible" : link.Fact.Value == 0 ? "zero" : "precision-collapse";
        var id = ChartRelationshipMetadata.SourceId("link", link.Fact.Id);
        var anchor = model.On(link.SourceStart, model.InnerRadius);
        var bounds = new ChartRect(anchor.X, anchor.Y, 0, 0);
        using (builder.PushGroup(id, "chord-link", metadata)) {
            if (visible) {
                var path = ChartChordLayout.Ribbon(model, link);
                bounds = ChartChordLayout.Bounds(path.Flatten(1));
                var color = ChartRelationshipPaint.Color(series, source.Index, colors);
                var fill = ChartColorMath.WithOpacity(color, chart.Options.Chord.RibbonOpacity);
                var paint = ChartRelationshipPaint.Paint(series, color, source.Index).WithOpacity(fill, chart.Options.Chord.RibbonOpacity);
                builder.Path(path, fill, role: "chord-ribbon", close: true, paint: VisualChartPaint.Fill(paint));
                var pattern = ChartRelationshipPaint.Pattern(series, source.Index);
                if (pattern != ChartFillPattern.None) builder.Pattern(path, pattern, fill, role: "chord-ribbon-pattern", paint: paint);
                if (chart.Options.Chord.DirectionCue == ChartChordDirectionCue.TargetChevron)
                    builder.Path(ChartChordLayout.DirectionCue(model, link), color, role: "chord-target-cue", close: true,
                        paint: VisualChartPaint.Fill(ChartRelationshipPaint.Paint(series, color, source.Index)));
            }
        }
        if (link.Fact.Value > 0 && !visible) builder.AddDiagnostic(new VisualDiagnostic("chord.precision-collapse", "A positive chord flow cannot form distinct endpoint angles at this scale; its raw value and source/target identity remain in semantics."));
        builder.AddRegion(new VisualSemanticRegion(id, "chord-link", bounds, metadata["data-cfx-full-label"]));
    }

    private static void Node(Chart chart, VisualSceneBuilder builder, ChartChordModel model, ChartChordNode node, VisualThemeColors colors) {
        var series = chart.Series[0]; var metadata = ChartRelationshipMetadata.Node(series, node.Fact.Id, node.Fact.Label, node.Index);
        metadata["data-cfx-coordinate-system"] = "polar";
        metadata["data-cfx-value"] = N(node.Value); metadata["data-cfx-incoming"] = N(node.Incoming); metadata["data-cfx-outgoing"] = N(node.Outgoing);
        metadata["data-cfx-start-angle"] = N(node.Start); metadata["data-cfx-sweep"] = N(node.Sweep);
        metadata["data-cfx-center-x"] = N(model.CenterX); metadata["data-cfx-center-y"] = N(model.CenterY);
        metadata["data-cfx-inner-radius"] = N(model.InnerRadius); metadata["data-cfx-outer-radius"] = N(model.OuterRadius);
        metadata["data-cfx-state"] = ChartRelationshipPaint.State(series, node.Index).ToString();
        metadata["data-cfx-full-label"] = node.Fact.Label + ": " + ChartNumericFormatter.FormatValue(chart.Options, node.Value)
            + " (incoming " + ChartNumericFormatter.FormatValue(chart.Options, node.Incoming) + ", outgoing " + ChartNumericFormatter.FormatValue(chart.Options, node.Outgoing) + ")";
        var visible = node.IsVisible && model.OuterRadius > 0;
        metadata["data-cfx-geometry-status"] = visible ? "visible" : node.Value == 0 ? "zero" : "precision-collapse";
        var id = ChartRelationshipMetadata.SourceId("node", node.Fact.Id);
        using (builder.PushGroup(id, "chord-node", metadata)) {
            if (visible) {
                var color = ChartRelationshipPaint.Color(series, node.Index, colors);
                builder.Slice(model.CenterX, model.CenterY, model.OuterRadius, model.InnerRadius, node.Start, node.Sweep,
                    color, role: "chord-node-mark", paint: VisualChartPaint.Fill(ChartRelationshipPaint.Paint(series, color, node.Index)));
                var pattern = ChartRelationshipPaint.Pattern(series, node.Index);
                if (pattern != ChartFillPattern.None) builder.PatternSlice(model.CenterX, model.CenterY, model.OuterRadius, model.InnerRadius, node.Start, node.Sweep,
                    pattern, ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: "chord-node-pattern");
            }
        }
        if (node.Value == 0) builder.AddDiagnostic(new VisualDiagnostic("chord.zero-node", "A node with no positive endpoints retains its identity without an allocated arc."));
        else if (!visible) builder.AddDiagnostic(new VisualDiagnostic("chord.precision-collapse", "A positive node endpoint total cannot form a distinct arc at this scale; the raw totals remain in semantics."));
        builder.AddRegion(new VisualSemanticRegion(id, "chord-node", ChartChordLayout.NodeBounds(model, node), metadata["data-cfx-full-label"]));
    }

    private static string Label(Chart chart, int index) {
        var series = chart.Series[0]; var facts = series.Relationships!; var node = facts.Nodes[index];
        var label = index < series.PointLabels.Count && series.PointLabels[index] != null ? series.PointLabels[index]! : node.Label;
        string Value(double value) => ChartNumericFormatter.FormatValue(chart.Options, value);
        return chart.Options.Chord.LabelContent switch {
            ChartChordLabelContent.LabelAndValue => label + " " + Value(facts.FlowEndpointValues[index]),
            ChartChordLabelContent.LabelAndTotals => label + "\n" + Value(facts.FlowIncomingValues[index]) + " in / " + Value(facts.FlowOutgoingValues[index]) + " out",
            _ => label
        };
    }

    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
