using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            ValidateVennPaint(set.StrokeWidth, set.FillOpacity);
            if (!ids.Add(set.Id)) throw new InvalidOperationException("Venn set ids must be unique: " + set.Id + ".");
        }

        foreach (var intersection in block.Intersections) {
            if (intersection.SetIds.Count < 2) throw new VennDiagramValidationException("Venn intersections require at least two distinct sets.", intersection.SetIds);
            ValidateVennRegionIds(ids, intersection.SetIds, "Venn intersection");
            ValidateVennPaint(intersection.StrokeWidth, intersection.FillOpacity);
            if (!IsFinite(intersection.Size) || intersection.Size < 0) throw new InvalidOperationException("Venn intersection sizes must be finite and zero or greater.");
        }

        ValidateVennMemberships(block);

        foreach (var textNode in block.TextNodes) {
            if (textNode.Id.Length == 0) throw new InvalidOperationException("Venn text nodes must define ids.");
            if (textNode.Label.Length == 0) throw new InvalidOperationException("Venn text nodes must define labels.");
            ValidateVennRegionIds(ids, textNode.SetIds, "Venn text node");
        }
    }

    public static IReadOnlyList<VennSetPlacement> VennSetPlacements(VennDiagramBlock block, ChartRect rect) {
        var layout = VennLayout(block, rect);
        return layout.Circles.Select((circle, index) => new VennSetPlacement(block.Sets[index], index, circle.Center.X, circle.Center.Y, circle.Radius)).ToArray();
    }

    public static ChartPoint VennRegionCenter(IReadOnlyList<VennSetPlacement> placements, IReadOnlyList<string> setIds) {
        var circles = placements.Select(item => new VennCircle(item.X, item.Y, item.Radius)).ToArray();
        var indices = setIds.Select(id => { for (var i = 0; i < placements.Count; i++) if (placements[i].Set.Id == id) return i; throw new InvalidOperationException("Unknown Venn set."); }).ToArray();
        return VennIntersection(circles, indices).Anchor;
    }

    private static void ValidateVennPaint(double strokeWidth, double? opacity) {
        if (!IsFinite(strokeWidth) || strokeWidth < 0) throw new InvalidOperationException("Venn stroke widths must be finite and non-negative.");
        if (opacity.HasValue && (!IsFinite(opacity.Value) || opacity < 0 || opacity > 1)) throw new InvalidOperationException("Venn fill opacity must be finite and between zero and one.");
    }

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
