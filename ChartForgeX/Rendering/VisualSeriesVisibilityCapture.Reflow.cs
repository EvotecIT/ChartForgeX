using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSeriesVisibilityCapture {
    private readonly string? _reflowViewSvg;
    internal PreparedVisual CompleteValueBasis => ApplyAppearance(_basis);
    internal ChartVisibilitySource? ReflowSource { get; }
    internal string? SourceKey { get; }
    internal IReadOnlyList<ChartVisibilityTarget> TargetCatalog => ReflowSource?.TargetCatalog() ?? Array.Empty<ChartVisibilityTarget>();
    internal IReadOnlyList<int>? RenderedSourceIndices(int series) => ReflowSource?.RenderedSourceIndices(series, View);

    private VisualSeriesVisibilityCapture(PreparedVisual complete, PreparedVisual visible, ChartVisibilityView view,
        ChartVisibilitySource source, VisualRenderContext context, bool labelsEnabled) {
        _basis = complete; View = view; _labelsEnabled = labelsEnabled; _tree = new SpanTree(complete.Scene.Nodes);
        _series = source.SeriesRows.Select((row, index) => new SeriesRow(row.Id.Value, index, -1, -1)).ToArray();
        _seriesKeys = Array.AsReadOnly(_series.Select(row => row.Key).ToArray());
        ReflowSource = source; SourceKey = source.SourceKey(context, complete);
        _sourceSvg = complete.ToSvg("cfx-native-visibility-source"); _reflowViewSvg = visible.ToSvg("cfx-native-reflow-view");
    }
    internal static PreparedVisual PrepareReflow(PreparedVisual complete, PreparedVisual visible, ChartVisibilityView view,
        ChartVisibilitySource source, VisualRenderContext context) {
        source.ValidateView(view);
        var presentation = source.CopyPresentation();
        var labels = source.SeriesRows.Any(row => row.Labels ?? presentation.ShowDataLabels ?? false);
        if (view.LabelLayering == ChartVisibilityLabelLayering.GroupWithSeries && visible.Scene.Nodes.OfType<VisualSceneGroup>().Any(group => group.Role == "series")) {
            var groupedView = new ChartVisibilityView(labelLayering: ChartVisibilityLabelLayering.GroupWithSeries);
            var grouped = new VisualSeriesVisibilityCapture(visible, groupedView, labels, groupingOnly: true);
            visible = grouped.Paint(visible, grouped._series.Select(_ => 1d).ToArray(), groupedView);
        }
        var scene = visible.Scene;
        var fullLegend = complete.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role is "legend-entry" or "legend-entry-omitted").ToDictionary(group => group.Id!, StringComparer.Ordinal);
        var fullPoints = ReflowPointFacts(complete);
        var nodes = scene.Nodes.Select(node => {
            if (node is not VisualSceneGroup group || group.Role is not ("legend-entry" or "legend-entry-omitted")
                || !group.Metadata.TryGetValue("data-cfx-series-key", out var key)) return node;
            var facts = ReflowLegendFacts(group, fullLegend, fullPoints);
            var kind = facts.TryGetValue("data-cfx-legend-target-kind", out var nativeKind) && nativeKind == "node" ? ChartVisibilityTargetKind.HierarchyItem
                : facts.TryGetValue("data-cfx-derived", out var derived) && derived == "radial-slice"
                && facts.TryGetValue("data-cfx-point", out var ordinal) && ordinal == "-1" ? ChartVisibilityTargetKind.RadialOther
                : facts.TryGetValue("data-cfx-group-id", out _) ? ChartVisibilityTargetKind.BoxPlotGroup
                : facts.TryGetValue("data-cfx-observation-id", out _) ? ChartVisibilityTargetKind.Observation : ChartVisibilityTargetKind.Series;
            var id = kind == ChartVisibilityTargetKind.HierarchyItem ? facts["data-cfx-legend-target-id"] : kind == ChartVisibilityTargetKind.BoxPlotGroup ? facts["data-cfx-group-id"] : kind == ChartVisibilityTargetKind.Observation ? facts["data-cfx-observation-id"] : null;
            source.TryTarget(kind, key, id, out var target);
            var metadata = new Dictionary<string, string>(); foreach (var pair in facts) metadata.Add(pair.Key, pair.Value);
            if (target != null && (view.IsHidden(key) || view.IsHidden(target))) metadata["data-cfx-target-hidden"] = "true";
            if (target?.Kind == ChartVisibilityTargetKind.HierarchyItem) {
                var item = source.SeriesRows[0].HierarchyItems.Select((row, index) => (row, index)).Single(row => row.row.Id == target.NodeId).index;
                var membership = visible.HierarchyPreparation!;
                if (!membership.Contains(item)) metadata["data-cfx-target-hidden"] = "true";
                if (membership.VisibilityBlocked(item)) metadata["data-cfx-target-blocked"] = "true";
            }
            return (VisualSceneNode)Copy(group, metadata, group.Opacity);
        }).ToArray();
        visible = visible.WithNativeFrame(new VisualScene(scene.Size, nodes, scene.Diagnostics, scene.Regions, scene.DataProjection));
        var capture = new VisualSeriesVisibilityCapture(complete, visible, view, source, context, labels);
        return visible.WithNativeFrame(new VisualScene(scene.Size, nodes, scene.Diagnostics, scene.Regions, scene.DataProjection, valueCapture: null, visibilityCapture: capture));
    }
    internal static IReadOnlyList<(PreparedVisual Frame, bool Source, double Opacity)> ReflowFrames(PreparedVisual from, PreparedVisual to,
        int frameCount, CancellationToken cancellationToken = default, bool sourceReflow = false) {
        if (from == null) throw new ArgumentNullException(nameof(from)); if (to == null) throw new ArgumentNullException(nameof(to));
        cancellationToken.ThrowIfCancellationRequested(); var first = from.Scene.VisibilityCapture; var last = to.Scene.VisibilityCapture;
        if (first?.ReflowSource == null || last?.ReflowSource == null || first.View.Layout != ChartVisibilityLayout.Reflow || last.View.Layout != ChartVisibilityLayout.Reflow
            || first._appearanceKey != last._appearanceKey || (!sourceReflow && first.SourceKey != last.SourceKey) || first.View.LabelLayering != last.View.LabelLayering || first.TargetCatalog.Count != last.TargetCatalog.Count
            || first.TargetCatalog.Where((row, index) => !ChartVisibilitySource.SameTarget(row, last.TargetCatalog[index])).Any())
            throw new NotSupportedException("Native Reflow frames require the same detached full authored source, native paint, catalog and explicit layout/label policy.");
        if (sourceReflow) {
            if (!VisualValueCapture.TryRawSource(from, out var a, out var fullA) || !VisualValueCapture.TryRawSource(to, out var b, out var fullB)
                || !a!.SamePolicy(b!) || a.StructureKey != b!.StructureKey || a.SourceKey(fullA) == b.SourceKey(fullB)
                || !a.Readings.Where((row, index) => row.Value != b.Readings[index].Value).Any())
                throw new NotSupportedException("SourceReflow requires the same detached raw authored structure/presentation and at least one changed full source reading.");
        }
        if (SameHidden(first, last)) throw new NotSupportedException("Native Reflow motion changes at least one absolute authored visibility target.");
        if (from.ToSvg("cfx-native-reflow-view") != first._reflowViewSvg || to.ToSvg("cfx-native-reflow-view") != last._reflowViewSvg)
            throw new NotSupportedException("Native Reflow endpoints must equal their exact captured completed native view.");
        return VisualValueCapture.ReflowOpacityFrames(from, to, frameCount, cancellationToken);
    }
    private static bool SameHidden(VisualSeriesVisibilityCapture first, VisualSeriesVisibilityCapture last) =>
        first.View.HiddenTargets.Count == last.View.HiddenTargets.Count && first.View.HiddenTargets.All(last.View.IsHidden)
        && first.View.HierarchyView?.RootId == last.View.HierarchyView?.RootId;
}
