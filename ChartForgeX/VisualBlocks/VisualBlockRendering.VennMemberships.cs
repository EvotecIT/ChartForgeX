using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    private static void ValidateVennMemberships(VennDiagramBlock block) {
        var regions = new Dictionary<string, double>(StringComparer.Ordinal);
        var sizes = block.Sets.ToDictionary(set => set.Id, set => set.Size, StringComparer.Ordinal);
        var maximum = block.Sets.Max(set => set.Size); var divisor = maximum > 0 ? maximum : 1;
        foreach (var region in block.Intersections) {
            var key = VennRegionKey(region.SetIds);
            if (regions.ContainsKey(key)) throw new VennDiagramValidationException("Venn intersections must identify distinct set memberships.", region.SetIds);
            if (region.Size / divisor > region.SetIds.Min(id => sizes[id] / divisor) + 1e-12)
                throw new VennDiagramValidationException("A Venn intersection cannot exceed any of its participating sets.", region.SetIds);
            regions.Add(key, region.Size / divisor);
        }
        if (block.Sets.Count < 3) return;
        var ids = block.Sets.Select(set => set.Id).ToArray(); var all = VennRegionKey(ids);
        var tripleKnown = regions.TryGetValue(all, out var triple);
        var pairs = new double?[3]; var pairIds = new[] { new[] { ids[0], ids[1] }, new[] { ids[0], ids[2] }, new[] { ids[1], ids[2] } };
        for (var i = 0; i < pairs.Length; i++) if (regions.TryGetValue(VennRegionKey(pairIds[i]), out var pair)) pairs[i] = pair;
        var lower = 0.0; var upper = ids.Min(id => sizes[id] / divisor);
        foreach (var pair in pairs) if (pair.HasValue) upper = Math.Min(upper, pair.Value);
        for (var set = 0; set < 3; set++) {
            var pairIndices = set == 0 ? new[] { 0, 1 } : set == 1 ? new[] { 0, 2 } : new[] { 1, 2 };
            if (pairs[pairIndices[0]].HasValue && pairs[pairIndices[1]].HasValue)
                lower = Math.Max(lower, pairs[pairIndices[0]]!.Value + pairs[pairIndices[1]]!.Value - sizes[ids[set]] / divisor);
        }
        if (tripleKnown && (triple > upper + 1e-12 || triple + 1e-12 < lower) || lower > upper + 1e-12)
            throw new VennDiagramValidationException("Venn memberships would require a negative exclusive region or a triple intersection larger than a pair intersection.", ids);
    }
    private static string VennRegionKey(IEnumerable<string> ids) => string.Join("|", ids.OrderBy(id => id, StringComparer.Ordinal).Select(id => id.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + id));
}
