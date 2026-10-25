using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

/// <summary>Immutable numeric observations of the ordinary route and its existing caption allocations.</summary>
/// <remarks>Projected native facts remain finite. Explicit portable validation applies the coordinate and aggregate
/// budgets; merely preparing a static scene does not serialize its metadata or impose a wire-only extent bound.</remarks>
internal sealed class TopologyPreparedEdgeGeometry {
    private readonly ChartPoint[] _input;
    internal TopologyPreparedEdgeGeometry(TopologyEdgeInterpolation? interpolation, double? cornerBlendLength,
        IEnumerable<ChartPoint> input, ChartPath fullPath, ChartPath? bodyPath, ChartPath? tailPath,
        int order, int? trunkOwnerOrder,
        TopologyPreparedMainLabel? mainLabel = null, TopologyPreparedEndpointLabel? sourceLabel = null, TopologyPreparedEndpointLabel? targetLabel = null) {
        if (interpolation.HasValue) TopologyModelGuards.EnumDefined(interpolation.Value, nameof(interpolation));
        if (!interpolation.HasValue && cornerBlendLength.HasValue) throw new ArgumentException("An ordinary captured route cannot introduce interpolation blending.", nameof(cornerBlendLength));
        if (cornerBlendLength.HasValue) TopologyModelGuards.NonNegativeFinite(cornerBlendLength.Value, nameof(cornerBlendLength));
        if (input == null) throw new ArgumentNullException(nameof(input));
        _input = input.Take(10001).ToArray();
        if (_input.Length < 2 || _input.Length > 10000) throw new ArgumentException("Captured interpolation requires 2 through 10000 input vertices.", nameof(input));
        if (order < 0 || trunkOwnerOrder < 0) throw new ArgumentOutOfRangeException(nameof(order));
        foreach (var point in _input) {
            TopologyModelGuards.Finite(point.X, nameof(input)); TopologyModelGuards.Finite(point.Y, nameof(input));
            if (point.BreakBefore) throw new ArgumentException("Captured interpolation requires continuous input vertices.", nameof(input));
        }
        Interpolation = interpolation; CornerBlendLength = cornerBlendLength;
        FullPath = Copy(fullPath); BodyPath = bodyPath == null ? null : Copy(bodyPath); TailPath = tailPath == null ? null : Copy(tailPath);
        Order = order; TrunkOwnerOrder = trunkOwnerOrder;
        MainLabel = mainLabel; SourceLabel = sourceLabel; TargetLabel = targetLabel;
    }
    internal TopologyEdgeInterpolation? Interpolation { get; }
    internal double? CornerBlendLength { get; }
    internal IReadOnlyList<ChartPoint> Input => Array.AsReadOnly(_input);
    internal ChartPath FullPath { get; }
    internal ChartPath? BodyPath { get; }
    internal ChartPath? TailPath { get; }
    internal int Order { get; }
    internal int? TrunkOwnerOrder { get; }
    internal TopologyPreparedMainLabel? MainLabel { get; }
    internal TopologyPreparedEndpointLabel? SourceLabel { get; }
    internal TopologyPreparedEndpointLabel? TargetLabel { get; }

    internal TopologyPreparedEdgeGeometry WithCaptions(TopologyPreparedMainLabel? main,
        TopologyPreparedEndpointLabel? source, TopologyPreparedEndpointLabel? target) => new(Interpolation, CornerBlendLength,
            _input, FullPath, BodyPath, TailPath, Order, TrunkOwnerOrder, main, source, target);

    internal TopologyPreparedEdgeGeometry Transform(Func<ChartPoint, ChartPoint> point, double scale) => new(Interpolation,
        CornerBlendLength * scale, _input.Select(point), FullPath.Transform(point), BodyPath?.Transform(point), TailPath?.Transform(point),
        Order, TrunkOwnerOrder, MainLabel?.Transform(point, scale), SourceLabel?.Transform(point, scale), TargetLabel?.Transform(point, scale));

    private static ChartPath Copy(ChartPath path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (path.Commands.Count < 2 || path.Commands.Count > TopologyPathDecoration.MaximumCommands)
            throw new ArgumentException("A captured ordinary path requires 2 through 4096 commands.", nameof(path));
        foreach (var command in path.Commands) {
            if (command.Kind is not (ChartPathCommandKind.MoveTo or ChartPathCommandKind.LineTo or ChartPathCommandKind.CubicTo))
                throw new ArgumentException("Captured ordinary routes support only move, line and cubic commands.", nameof(path));
            TopologyModelGuards.Finite(command.X, nameof(path)); TopologyModelGuards.Finite(command.Y, nameof(path));
            TopologyModelGuards.Finite(command.Control1X, nameof(path)); TopologyModelGuards.Finite(command.Control1Y, nameof(path));
            TopologyModelGuards.Finite(command.Control2X, nameof(path)); TopologyModelGuards.Finite(command.Control2Y, nameof(path));
        }
        return new ChartPath(Array.AsReadOnly(path.Commands.ToArray()));
    }
}

/// <summary>Captures the existing backplate allocation and leader anchor independently of visible text ink.</summary>
internal sealed class TopologyPreparedMainLabel {
    internal TopologyPreparedMainLabel(ChartRect bounds, ChartPoint anchor) { Bounds = bounds; Anchor = anchor; }
    internal ChartRect Bounds { get; }
    internal ChartPoint Anchor { get; }
    internal TopologyPreparedMainLabel Transform(Func<ChartPoint, ChartPoint> point, double scale) {
        var origin = point(new ChartPoint(Bounds.X, Bounds.Y));
        return new TopologyPreparedMainLabel(new ChartRect(origin.X, origin.Y, Bounds.Width * scale, Bounds.Height * scale), point(Anchor));
    }
}

/// <summary>Captures one actual native endpoint allocation and its fitted output font size.</summary>
internal sealed class TopologyPreparedEndpointLabel {
    internal TopologyPreparedEndpointLabel(ChartRect bounds, double fontSize) {
        TopologyModelGuards.PositiveFinite(fontSize, nameof(fontSize)); Bounds = bounds; FontSize = fontSize;
    }
    internal ChartRect Bounds { get; }
    internal double FontSize { get; }
    internal TopologyPreparedEndpointLabel Transform(Func<ChartPoint, ChartPoint> point, double scale) {
        var origin = point(new ChartPoint(Bounds.X, Bounds.Y));
        return new TopologyPreparedEndpointLabel(new ChartRect(origin.X, origin.Y, Bounds.Width * scale, Bounds.Height * scale), FontSize * scale);
    }
}
