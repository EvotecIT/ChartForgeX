using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal sealed partial class TopologyAlignmentLayout {
    internal static void ValidateState(TopologyChart chart) {
        if (chart.LayoutMode == TopologyLayoutMode.Manual) return;
        var unsupported = chart.LayoutMode != TopologyLayoutMode.Layered
            || chart.Nodes.Any(node => node.HasPositionOverride || Math.Abs(node.X) >= 0.0001 || Math.Abs(node.Y) >= 0.0001 || node.Metadata.ContainsKey("layer"))
            || chart.Groups.Any(group => group.HasPositionOverride || group.X != 0 || group.Y != 0 || group.Width > 0 || group.Height > 0)
            || chart.Edges.Any(edge => edge.MinimumRankSpan > 1);
        if (!unsupported) return;
        var result = new TopologyValidationResult();
        result.Errors.Add(new TopologyValidationIssue { Code = "alignment-unsupported-state", ItemId = chart.Alignments[0].Id,
            Message = "Automatic alignments require fully automatic Layered geometry without authored ranks or minimum-rank spans above one. Use Manual to validate supplied geometry." });
        throw new TopologyValidationException(result);
    }

    internal static void ValidateManual(TopologyChart chart, TopologyRenderOptions? options = null) {
        var result = new TopologyValidationResult();
        ValidateManual(chart, result, options);
        if (!result.IsValid) throw new TopologyValidationException(result);
    }

    /// <summary>Portable Manual geometry has body bounds, but no recorded native typography or spacing gap.</summary>
    internal static void ValidateManual(TopologyChart chart, TopologyValidationResult result, TopologyRenderOptions? options = null) {
        if (chart.Alignments.Count == 0) return;
        const double tolerance = 0.000001;
        options ??= chart.RenderOptions ?? chart.DefaultRenderOptions ?? new TopologyRenderOptions();
        var budget = new TopologyAlignmentBudget(chart.Alignments[0].Id);
        budget.Work((long)chart.Nodes.Count * 2 + (long)chart.Groups.Count * 2);
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var groups = chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        foreach (var declaration in chart.Alignments) {
            if (declaration == null || declaration.NodeIds.Count < 2 || declaration.NodeIds.Any(id => !nodes.ContainsKey(id))) continue;
            budget.Work(declaration.NodeIds.Count);
            var first = nodes[declaration.NodeIds[0]];
            var previous = first;
            foreach (var id in declaration.NodeIds) {
                var node = nodes[id];
                var row = declaration.Kind == TopologyAlignmentKind.Row;
                var equal = row ? Math.Abs(first.Y + first.Height / 2 - node.Y - node.Height / 2) <= tolerance
                    : Math.Abs(first.X + first.Width / 2 - node.X - node.Width / 2) <= tolerance;
                var ordered = ReferenceEquals(first, node) || (row ? previous.X + previous.Width <= node.X + tolerance : previous.Y + previous.Height <= node.Y + tolerance);
                if (!equal || !ordered) { Error("alignment-geometry", "Manual alignment requires equal body centers and non-overlapping members in authored order.", declaration.Id); return; }
                previous = node;
            }
        }
        foreach (var node in chart.Nodes) {
            if (node.GroupId != null && groups.TryGetValue(node.GroupId, out var parent)
                && !Contains(parent, node.X, node.Y, node.Width, node.Height))
                { Error("alignment-containment", "Manual node bounds must lie inside their actual parent group.", node.Id); return; }
        }
        foreach (var group in chart.Groups) {
            if (group.ParentGroupId != null && groups.TryGetValue(group.ParentGroupId, out var parent)
                && !Contains(parent, group.X, group.Y, group.Width, group.Height))
                { Error("alignment-containment", "Manual child group bounds must lie inside their actual parent.", group.Id); return; }
        }
        // Check real siblings; ancestor shells intentionally contain their descendants.
        var siblings = new Dictionary<string, List<(double X, double Y, double Right, double Bottom, string Id)>>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (ChartForgeX.Topology.TopologyRenderPrimitives.EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Hidden) continue;
            Add(node.GroupId, node.X, node.Y, node.Width, node.Height, node.Id);
        }
        foreach (var group in chart.Groups) Add(group.ParentGroupId, group.X, group.Y, group.Width, group.Height, group.Id);
        foreach (var set in siblings.Values) {
            // Charge the stable framework sort before its allocations. Comparers do
            // not throw or hide a resource exception inside Array.Sort wrappers.
            var levels = 1; for (var size = set.Count; size > 1; size /= 2) levels++;
            budget.Work((long)set.Count * (levels + 2));
            var ordered = set.OrderBy(item => item.X).ToArray();
            for (var i = 0; i < ordered.Length; i++) {
                var a = ordered[i];
                for (var j = i + 1; j < ordered.Length; j++) {
                    budget.Work(); var b = ordered[j]; if (b.X >= a.Right - tolerance) break;
                    if (a.Y < b.Bottom - tolerance && b.Y < a.Bottom - tolerance)
                        { Error("alignment-overlap", "Manual sibling bodies and group shells must not overlap.", a.Id); return; }
                }
            }
        }
        bool Contains(TopologyGroup parent, double x, double y, double width, double height) =>
            x >= parent.X - tolerance && y >= parent.Y - tolerance && x + width <= parent.X + parent.Width + tolerance && y + height <= parent.Y + parent.Height + tolerance;
        void Error(string code, string message, string id) => result.Errors.Add(new TopologyValidationIssue { Code = code, Message = message, ItemId = id });
        void Add(string? parent, double x, double y, double width, double height, string id) {
            var key = parent ?? string.Empty;
            if (!siblings.TryGetValue(key, out var set)) siblings.Add(key, set = new List<(double, double, double, double, string)>());
            set.Add((x, y, x + width, y + height, id));
        }
    }
}
