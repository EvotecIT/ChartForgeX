using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>Shares parent-tree integrity and recursion limits across native layout and portable semantics.</summary>
internal static class TopologyGroupHierarchy {
    internal const int MaximumDepth = 128;

    internal static TopologyValidationResult Validate(IEnumerable<(string Id, string? ParentId)> groups) {
        var result = new TopologyValidationResult();
        var parents = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var group in groups) {
            if (!parents.ContainsKey(group.Id)) parents.Add(group.Id, group.ParentId);
        }
        foreach (var group in parents) {
            var visited = new HashSet<string>(StringComparer.Ordinal) { group.Key };
            var parentId = group.Value;
            while (parentId != null) {
                if (!parents.TryGetValue(parentId, out var next)) {
                    Add("missing-parent-group", "Group '" + group.Key + "' references missing parent '" + parentId + "'.");
                    break;
                }
                if (!visited.Add(parentId)) {
                    Add("group-parent-cycle", "Group '" + group.Key + "' has a cyclic parent relationship.");
                    break;
                }
                if (visited.Count > MaximumDepth) {
                    Add("group-parent-depth", "Topology group nesting must not exceed " + MaximumDepth + " regions.");
                    break;
                }
                parentId = next;
            }

            void Add(string code, string message) => result.Errors.Add(new TopologyValidationIssue { Code = code, Message = message, ItemId = group.Key });
        }
        return result;
    }
}
