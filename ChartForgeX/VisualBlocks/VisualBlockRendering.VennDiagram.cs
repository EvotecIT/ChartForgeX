using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public const int MaximumVennSets = 3;
    public const int MaximumVennIntersections = 32;
    public const int MaximumVennTextNodes = 64;

    public static void ValidateVennDiagram(VennDiagramBlock block) {
        if (block.Sets.Count == 0) throw new InvalidOperationException("Venn diagrams must contain at least one set.");
        if (block.Sets.Count > MaximumVennSets) throw new InvalidOperationException("Venn diagrams currently render no more than three sets.");
        if (block.Intersections.Count > MaximumVennIntersections) throw new InvalidOperationException("Venn diagrams must contain no more than " + MaximumVennIntersections.ToString(CultureInfo.InvariantCulture) + " intersections.");
        if (block.TextNodes.Count > MaximumVennTextNodes) throw new InvalidOperationException("Venn diagrams must contain no more than " + MaximumVennTextNodes.ToString(CultureInfo.InvariantCulture) + " text nodes.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in block.Sets) {
            if (set.Id.Length == 0) throw new InvalidOperationException("Venn sets must define ids.");
            if (set.Label.Length == 0) throw new InvalidOperationException("Venn sets must define labels.");
            if (!IsFinite(set.Size) || set.Size < 0) throw new InvalidOperationException("Venn set sizes must be finite and zero or greater.");
            if (!ids.Add(set.Id)) throw new InvalidOperationException("Venn set ids must be unique: " + set.Id + ".");
        }

        foreach (var intersection in block.Intersections) {
            ValidateVennRegionIds(ids, intersection.SetIds, "Venn intersection");
            if (!IsFinite(intersection.Size) || intersection.Size < 0) throw new InvalidOperationException("Venn intersection sizes must be finite and zero or greater.");
        }

        foreach (var textNode in block.TextNodes) {
            if (textNode.Id.Length == 0) throw new InvalidOperationException("Venn text nodes must define ids.");
            if (textNode.Label.Length == 0) throw new InvalidOperationException("Venn text nodes must define labels.");
            ValidateVennRegionIds(ids, textNode.SetIds, "Venn text node");
        }
    }

    public static IReadOnlyList<VennSetPlacement> VennSetPlacements(VennDiagramBlock block, ChartRect rect) {
        var count = block.Sets.Count;
        var placements = new List<VennSetPlacement>(count);
        var width = Math.Max(1, rect.Width);
        var height = Math.Max(1, rect.Height);
        var radius = count == 1
            ? Math.Min(width, height) * 0.30
            : Math.Min(width / (count == 2 ? 2.45 : 2.55), height / 2.15);
        radius = Math.Max(24, radius);

        if (count == 1) {
            placements.Add(CreateVennPlacement(block, 0, rect.X + width / 2, rect.Y + height * 0.52, radius));
        } else if (count == 2) {
            placements.Add(CreateVennPlacement(block, 0, rect.X + width * 0.42, rect.Y + height * 0.52, radius));
            placements.Add(CreateVennPlacement(block, 1, rect.X + width * 0.58, rect.Y + height * 0.52, radius));
        } else {
            placements.Add(CreateVennPlacement(block, 0, rect.X + width * 0.42, rect.Y + height * 0.46, radius));
            placements.Add(CreateVennPlacement(block, 1, rect.X + width * 0.58, rect.Y + height * 0.46, radius));
            placements.Add(CreateVennPlacement(block, 2, rect.X + width * 0.50, rect.Y + height * 0.62, radius));
        }

        return placements;
    }

    public static ChartPoint VennRegionCenter(IReadOnlyList<VennSetPlacement> placements, IReadOnlyList<string> setIds) {
        var x = 0.0;
        var y = 0.0;
        var count = 0;
        for (var idIndex = 0; idIndex < setIds.Count; idIndex++) {
            for (var placementIndex = 0; placementIndex < placements.Count; placementIndex++) {
                if (!string.Equals(placements[placementIndex].Set.Id, setIds[idIndex], StringComparison.Ordinal)) continue;
                x += placements[placementIndex].X;
                y += placements[placementIndex].Y;
                count++;
                break;
            }
        }

        if (count == 0) return new ChartPoint(0, 0);
        return new ChartPoint(x / count, y / count);
    }

    private static VennSetPlacement CreateVennPlacement(VennDiagramBlock block, int index, double x, double y, double radius) =>
        new(block.Sets[index], index, x, y, radius);

    private static void ValidateVennRegionIds(HashSet<string> ids, IReadOnlyList<string> setIds, string label) {
        if (setIds.Count == 0) throw new InvalidOperationException(label + " must reference at least one set.");
        if (setIds.Count > MaximumVennSets) throw new InvalidOperationException(label + " must reference no more than three sets.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in setIds) {
            if (id.Length == 0) throw new InvalidOperationException(label + " set ids must not be empty.");
            if (!ids.Contains(id)) throw new InvalidOperationException(label + " references unknown set id: " + id + ".");
            if (!seen.Add(id)) throw new InvalidOperationException(label + " set ids must be unique: " + id + ".");
        }
    }

    public readonly struct VennSetPlacement {
        public VennSetPlacement(VennSet set, int index, double x, double y, double radius) {
            Set = set;
            Index = index;
            X = x;
            Y = y;
            Radius = radius;
        }

        public VennSet Set { get; }
        public int Index { get; }
        public double X { get; }
        public double Y { get; }
        public double Radius { get; }
    }
}
