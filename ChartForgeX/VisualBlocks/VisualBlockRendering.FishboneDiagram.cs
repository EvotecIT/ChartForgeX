using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public const int MaximumFishboneNodes = 512;
    public const int MaximumFishboneDepth = 12;

    public static void ValidateFishboneDiagram(FishboneDiagramBlock block) {
        if (block.Effect.Trim().Length == 0) throw new InvalidOperationException("Fishbone diagrams must define an effect label.");
        if (block.Causes.Count == 0) throw new InvalidOperationException("Fishbone diagrams must contain at least one cause.");
        var count = 1;
        foreach (var cause in block.Causes) ValidateFishboneCause(cause, 1, ref count);
        if (count > MaximumFishboneNodes) throw new InvalidOperationException("Fishbone diagrams must contain no more than " + MaximumFishboneNodes.ToString(CultureInfo.InvariantCulture) + " nodes.");
    }

    public static FishboneLayout BuildFishboneLayout(FishboneDiagramBlock block, ChartRect content, double y, double bottomPadding, double totalHeight) {
        var body = new ChartRect(content.X, y, content.Width, Math.Max(1, totalHeight - bottomPadding - y));
        var headWidth = Math.Min(180, Math.Max(118, body.Width * 0.22));
        var headHeight = Math.Min(86, Math.Max(54, body.Height * 0.20));
        var spineY = body.Y + body.Height * 0.52;
        var head = new ChartRect(body.Right - headWidth, spineY - headHeight / 2, headWidth, headHeight);
        var spineStart = body.X + 24;
        var spineEnd = head.Left - 10;
        var pairCount = Math.Max(1, (int)Math.Ceiling(block.Causes.Count / 2.0));
        var spacing = Math.Max(1, (spineEnd - spineStart) / pairCount);
        var branchLength = Math.Min(Math.Max(76, body.Height * 0.30), Math.Max(76, body.Height * 0.38));
        var branches = new List<FishboneBranchPlacement>(block.Causes.Count);
        for (var index = 0; index < block.Causes.Count; index++) {
            var pair = index / 2;
            var side = index % 2 == 0 ? -1 : 1;
            var baseX = spineEnd - spacing * (pair + 0.5);
            var endX = baseX - branchLength * 0.58;
            var endY = spineY + side * branchLength;
            var labelY = endY + side * 16;
            var children = BuildFishboneChildPlacements(block.Causes[index], side, baseX, spineY, endX, endY, branchLength);
            branches.Add(new FishboneBranchPlacement(block.Causes[index], index, side, baseX, spineY, endX, endY, endX - 54, labelY, 108, children));
        }

        return new FishboneLayout(body, spineStart, spineEnd, spineY, head, branches);
    }

    private static void ValidateFishboneCause(FishboneCause cause, int depth, ref int count) {
        if (cause.Label.Trim().Length == 0) throw new InvalidOperationException("Fishbone causes must define labels.");
        if (depth > MaximumFishboneDepth) throw new InvalidOperationException("Fishbone cause depth must not exceed " + MaximumFishboneDepth.ToString(CultureInfo.InvariantCulture) + ".");
        count++;
        foreach (var child in cause.Children) ValidateFishboneCause(child, depth + 1, ref count);
    }

    private static IReadOnlyList<FishboneChildPlacement> BuildFishboneChildPlacements(FishboneCause cause, int side, double baseX, double baseY, double endX, double endY, double branchLength) {
        var flattened = new List<FishboneFlattenedCause>();
        foreach (var child in cause.Children) FlattenFishboneCause(child, 1, flattened);
        var placements = new List<FishboneChildPlacement>(flattened.Count);
        for (var index = 0; index < flattened.Count; index++) {
            var ratio = flattened.Count == 1 ? 0.55 : 0.25 + 0.60 * index / Math.Max(1, flattened.Count - 1);
            var anchorX = baseX + (endX - baseX) * ratio;
            var anchorY = baseY + (endY - baseY) * ratio;
            var stub = Math.Max(26, branchLength * 0.16) + flattened[index].Depth * 8;
            var labelX = anchorX - stub - 72;
            var labelY = anchorY + side * (8 + flattened[index].Depth * 4);
            placements.Add(new FishboneChildPlacement(flattened[index].Cause, flattened[index].Depth, anchorX, anchorY, anchorX - stub, anchorY + side * 18, labelX, labelY, 136));
        }

        return placements;
    }

    private static void FlattenFishboneCause(FishboneCause cause, int depth, List<FishboneFlattenedCause> results) {
        results.Add(new FishboneFlattenedCause(cause, depth));
        foreach (var child in cause.Children) FlattenFishboneCause(child, depth + 1, results);
    }

    public readonly struct FishboneLayout {
        public FishboneLayout(ChartRect body, double spineStartX, double spineEndX, double spineY, ChartRect head, IReadOnlyList<FishboneBranchPlacement> branches) {
            Body = body;
            SpineStartX = spineStartX;
            SpineEndX = spineEndX;
            SpineY = spineY;
            Head = head;
            Branches = branches;
        }

        public ChartRect Body { get; }
        public double SpineStartX { get; }
        public double SpineEndX { get; }
        public double SpineY { get; }
        public ChartRect Head { get; }
        public IReadOnlyList<FishboneBranchPlacement> Branches { get; }
    }

    public readonly struct FishboneBranchPlacement {
        public FishboneBranchPlacement(FishboneCause cause, int index, int side, double baseX, double baseY, double endX, double endY, double labelX, double labelY, double labelWidth, IReadOnlyList<FishboneChildPlacement> children) {
            Cause = cause;
            Index = index;
            Side = side;
            BaseX = baseX;
            BaseY = baseY;
            EndX = endX;
            EndY = endY;
            LabelX = labelX;
            LabelY = labelY;
            LabelWidth = labelWidth;
            Children = children;
        }

        public FishboneCause Cause { get; }
        public int Index { get; }
        public int Side { get; }
        public double BaseX { get; }
        public double BaseY { get; }
        public double EndX { get; }
        public double EndY { get; }
        public double LabelX { get; }
        public double LabelY { get; }
        public double LabelWidth { get; }
        public IReadOnlyList<FishboneChildPlacement> Children { get; }
    }

    public readonly struct FishboneChildPlacement {
        public FishboneChildPlacement(FishboneCause cause, int depth, double anchorX, double anchorY, double endX, double endY, double labelX, double labelY, double labelWidth) {
            Cause = cause;
            Depth = depth;
            AnchorX = anchorX;
            AnchorY = anchorY;
            EndX = endX;
            EndY = endY;
            LabelX = labelX;
            LabelY = labelY;
            LabelWidth = labelWidth;
        }

        public FishboneCause Cause { get; }
        public int Depth { get; }
        public double AnchorX { get; }
        public double AnchorY { get; }
        public double EndX { get; }
        public double EndY { get; }
        public double LabelX { get; }
        public double LabelY { get; }
        public double LabelWidth { get; }
    }

    private readonly struct FishboneFlattenedCause {
        public FishboneFlattenedCause(FishboneCause cause, int depth) {
            Cause = cause;
            Depth = depth;
        }

        public FishboneCause Cause { get; }
        public int Depth { get; }
    }
}
