using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal sealed partial class TopologyPreparedNodeBasis {
    /// <summary>Shares the exact raw-node transform between paint, capture and logical geometry projection.</summary>
    internal ChartPoint OriginalOffset(TopologyNode node, double outerScale, ChartPoint outerOffset) {
        if (Projection is { } captured) {
            var origin = captured.OffsetFor(node);
            if (outerScale == 1 && outerOffset.X == 0 && outerOffset.Y == 0) return origin;
            return new ChartPoint(outerOffset.X + origin.X * outerScale, outerOffset.Y + origin.Y * outerScale);
        }
        // Preserve the precise incumbent arithmetic for old/absent projection facts.
        var scale = outerScale * Factor;
        var anchor = new ChartPoint(outerOffset.X + node.X * outerScale, outerOffset.Y + node.Y * outerScale);
        return new ChartPoint(anchor.X - OriginalBody.X * scale, anchor.Y - OriginalBody.Y * scale);
    }
}

/// <summary>Immutable observations inside the existing prepared-node basis, not an authored transform policy.</summary>
internal sealed class TopologyPreparedNodeProjection {
    internal TopologyPreparedNodeProjection(ChartPoint offset, ChartPoint bodyOrigin) {
        Bounded(offset.X); Bounded(offset.Y); Bounded(bodyOrigin.X); Bounded(bodyOrigin.Y);
        Offset = offset; BodyOrigin = bodyOrigin;
    }
    internal ChartPoint Offset { get; }
    internal ChartPoint BodyOrigin { get; }

    internal ChartPoint OffsetFor(TopologyNode node) => node.X == BodyOrigin.X && node.Y == BodyOrigin.Y ? Offset
        : new ChartPoint(Offset.X + (node.X - BodyOrigin.X), Offset.Y + (node.Y - BodyOrigin.Y));

    internal void Validate(ChartRect original, double factor, double width, double height) {
        var x = Offset.X + original.X * factor; var y = Offset.Y + original.Y * factor;
        Bounded(x); Bounded(y); Bounded(Offset.X + original.Right * factor); Bounded(Offset.Y + original.Bottom * factor);
        Bounded(BodyOrigin.X + width); Bounded(BodyOrigin.Y + height);
        SameDerivedOrigin(x, BodyOrigin.X, Math.Max(Math.Abs(Offset.X), Math.Abs(original.X * factor)));
        SameDerivedOrigin(y, BodyOrigin.Y, Math.Max(Math.Abs(Offset.Y), Math.Abs(original.Y * factor)));
    }
    internal void ValidateAt(double x, double y, ChartRect original, double factor) {
        var offsetX = Offset.X + (x - BodyOrigin.X); var offsetY = Offset.Y + (y - BodyOrigin.Y);
        Bounded(offsetX); Bounded(offsetY);
        Bounded(offsetX + original.X * factor); Bounded(offsetY + original.Y * factor);
        Bounded(offsetX + original.Right * factor); Bounded(offsetY + original.Bottom * factor);
    }
    private static void SameDerivedOrigin(double actual, double stored, double termMagnitude) {
        // A repeated uniform fit can reassociate one bounded multiply. The exact retained offset remains authoritative.
        const double machineEpsilon = 2.2204460492503131e-16;
        if (Math.Abs(actual - stored) > 16 * machineEpsilon * Math.Max(1, Math.Max(termMagnitude, Math.Abs(stored))))
            throw new ArgumentException("Captured node projection must agree with its original body and captured output origin.");
    }
    private static void Bounded(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > TopologyPathGeometry.MaximumCoordinate)
            throw new ArgumentOutOfRangeException(nameof(value), "Captured node projection coordinates must be finite within one billion units.");
    }
}
