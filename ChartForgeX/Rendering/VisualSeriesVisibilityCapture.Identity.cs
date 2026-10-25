using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSeriesVisibilityCapture {
    private SeriesRow[] CaptureSeries() {
        var nodes = _basis.Scene.Nodes; var rows = new List<SeriesRow>(); var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < nodes.Count; index++) {
            if (nodes[index] is not VisualSceneGroup group || group.Role != "series") continue;
            if (!group.Metadata.TryGetValue("data-cfx-series-key", out var key) || string.IsNullOrWhiteSpace(key)
                || !group.Metadata.TryGetValue("data-cfx-series-scope", out var scope) || scope != "shared"
                || !group.Metadata.TryGetValue("data-cfx-kind", out var kind) || (kind != "Line" && kind != "Bar" && !(_groupingOnly && kind == "BoxPlot"))
                || !group.Metadata.TryGetValue("data-cfx-series", out var sourceIndex)
                || !int.TryParse(sourceIndex, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || !keys.Add(key))
                throw new NotSupportedException("Native visibility requires unique authored whole-series Line/Bar identity facts.");
            var plotScope = index; var parent = _tree.Parent[index];
            // Preserve the real per-series plot clip inside the composite; labels retain their separately captured clip context.
            if (parent >= 0 && _tree.End[parent] == _tree.End[index] + 1 && parent + 1 == index
                && nodes[parent] is VisualSceneGroup shell && shell.Role == null && shell.Id == null && shell.Metadata.Count == 0
                && shell.Opacity == 1 && shell.Href == null && shell.Tooltip == null && shell.ImageResource == null && shell.ColorScope == null)
                plotScope = parent;
            var row = new SeriesRow(key, number, index, plotScope); rows.Add(row); _plots.Add(plotScope, row);
            for (var nested = index; nested <= _tree.End[index]; nested++) if (nodes[nested] is VisualSceneGroup member && member.Id != null) {
                if (_regionOwners.TryGetValue(member.Id, out var prior) && !ReferenceEquals(prior, row))
                    throw new NotSupportedException("A native semantic region cannot belong to two series.");
                _regionOwners[member.Id] = row;
            }
        }
        if (rows.Count == 0 || rows.Select(row => row.Index).Distinct().Count() != rows.Count)
            throw new NotSupportedException("Native visibility requires a usable captured Cartesian series catalog.");
        foreach (var hidden in View.HiddenSeries) if (!keys.Contains(hidden.Value))
            throw new NotSupportedException("The visibility view references an unknown captured series.");
        var markOwners = new Dictionary<string, SeriesRow>(StringComparer.Ordinal);
        foreach (var pair in _regionOwners) markOwners.Add(pair.Key, pair.Value);
        for (var index = 0; index < nodes.Count; index++) {
            if (nodes[index] is not VisualSceneGroup group || group.Role != "data-label-source") continue;
            if (!group.Metadata.TryGetValue("data-cfx-label-for", out var markId) || !markOwners.TryGetValue(markId, out var row))
                throw new NotSupportedException("A native series label requires its exact captured AssociatedMarkId owner.");
            if (_tree.IsAncestor(row.Start, index)) throw new NotSupportedException("Native deferred labels must retain their separately captured placement owner.");
            row.Labels.Add(index); _labels.Add(index, row);
        }
        // Legend rows remain present, including omitted catalog rows. A point/group target is never silently reinterpreted.
        foreach (var group in nodes.OfType<VisualSceneGroup>().Where(group => group.Role is "legend-entry" or "legend-entry-omitted")) {
            if (!_groupingOnly && group.Metadata.TryGetValue("data-cfx-legend-target-kind", out var target) && target != "series")
                throw new NotSupportedException("Native visibility does not target point, group or derived legend policies.");
            if (group.Metadata.TryGetValue("data-cfx-series-key", out var key) && key.Length != 0 && !keys.Contains(key) && !_groupingOnly)
                throw new NotSupportedException("A native legend row references an unknown series.");
        }
        return rows.OrderBy(row => row.Index).ToArray();
    }

    /// <summary>Validates balanced owner spans once; indices are never inferred from source IDs or SVG strings.</summary>
    internal sealed class SpanTree {
        internal SpanTree(IReadOnlyList<VisualSceneNode> nodes) {
            Parent = new int[nodes.Count]; End = new int[nodes.Count]; var stack = new Stack<int>();
            for (var index = 0; index < nodes.Count; index++) {
                Parent[index] = stack.Count == 0 ? -1 : stack.Peek(); End[index] = index;
                if (nodes[index] is VisualSceneGroup) stack.Push(index);
                else if (nodes[index] is VisualSceneEndGroup) {
                    if (stack.Count == 0) throw new InvalidOperationException("The captured native display list is unbalanced.");
                    End[stack.Pop()] = index;
                }
            }
            if (stack.Count != 0) throw new InvalidOperationException("The captured native display list is unbalanced.");
        }
        internal int[] Parent { get; }
        internal int[] End { get; }
        internal bool IsAncestor(int ancestor, int child) { for (var index = Parent[child]; index >= 0; index = Parent[index]) if (index == ancestor) return true; return false; }
        internal List<int> Ancestors(int child) { var rows = new List<int>(); for (var index = Parent[child]; index >= 0; index = Parent[index]) rows.Add(index); rows.Reverse(); return rows; }
    }
}
