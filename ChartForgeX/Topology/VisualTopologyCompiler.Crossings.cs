using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private readonly Dictionary<(TopologyEdge Edge, bool Tail), ChartPath> _crossingPaintPaths = new();
    private TopologyCrossingPaintPlan? _crossingPaintPlan;

    /// <summary>Captures only final logical paint, after endpoint/trunk resolution and before fitting.</summary>
    private void PrepareCrossingPaintPaths() {
        var policy = _chart.CrossingPaint; if (policy == null) return;
        if (_options.EnableHtmlInteractions && _options.EnableHtmlForceGraphControls)
            throw new NotSupportedException("Native crossing paint cannot be rewritten by live force graph controls.");
        var strokes = new List<TopologyCrossingPaintPlan.Stroke>(); var owners = new List<(TopologyEdge Edge, bool Tail)>();
        var retainedPaths = _chart.PathDecorations.Count; var retainedCommands = _chart.PathDecorations.Sum(path => path.Commands.Count);
        foreach (var item in OrderedEdgesForRendering(_chart, _options)) {
            var edge = item.Edge; var trunk = _trunks.TryGetValue(edge, out var resolvedTrunk);
            if (Admit(edge, false)) Add(edge, false, _paintRoutes[edge],
                Clearance(edge, RenderedSourceMarker(edge, _options.IncludeDirectionMarkers), edge.SourceMarkerStyle),
                trunk ? 0 : Clearance(edge, RenderedTargetMarker(edge, _options.IncludeDirectionMarkers), edge.TargetMarkerStyle));
            if (trunk && ReferenceEquals(resolvedTrunk.Owner, edge) && Admit(edge, true))
                Add(edge, true, resolvedTrunk.Tail, 0,
                    Clearance(edge, RenderedTargetMarker(edge, _options.IncludeDirectionMarkers), edge.TargetMarkerStyle));
        }
        var plan = _crossingPaintPlan = TopologyCrossingPaintPlan.Capture(strokes, policy, retainedPaths, retainedCommands);
        foreach (var pair in plan.Paths) _crossingPaintPaths.Add(owners[pair.Key], pair.Value);

        bool Admit(TopologyEdge edge, bool tail) {
            if (CrossingPaintEligible(edge, tail)) return true;
            if (policy.CurveHandling == TopologyCrossingCurveHandling.Reject)
                throw new NotSupportedException("Orthogonal crossing paint cannot rewrite unsupported final native curves. Select SkipUnsupported to retain and exclude those paths.");
            var commands = CrossingPaintCommandCount(edge, tail);
            if (commands < 2 || commands > TopologyPathDecoration.MaximumCommands)
                throw new NotSupportedException("A retained crossing-policy path requires 2 through 4096 native commands.");
            retainedPaths = checked(retainedPaths + 1); retainedCommands = checked(retainedCommands + commands);
            return false;
        }
        double Clearance(TopologyEdge edge, TopologyMarkerKind marker, TopologyMarkerStyle? style) {
            var units = edge.PreparedEdgePlan?.DefaultUnits ?? policy.PreparedUnits;
            return marker == TopologyMarkerKind.None ? EdgeEndpointGap * units : Math.Max(EdgeEndpointGap * units,
                style == null ? MarkerExtent(null, _context.Theme.AxisStrokeWidth) * units : MarkerExtent(style, _context.Theme.AxisStrokeWidth * units));
        }
        void Add(TopologyEdge edge, bool tail, IReadOnlyList<ChartPoint> points, double start, double end) {
            if (points.Count < 2) return;
            var key = owners.Count; owners.Add((edge, tail)); strokes.Add(new TopologyCrossingPaintPlan.Stroke(key, edge, points, start, end));
        }
    }

    // The same final ordinary numeric path owns admission and command accounting. Diagonal M/L
    // segments remain eligible here; the existing paint planner excludes them individually.
    private bool CrossingPaintEligible(TopologyEdge edge, bool trunkTail) {
        if (edge.Interpolation.HasValue) {
            var path = OrdinaryLogicalPath(edge, trunkTail);
            return path != null && path.Commands.Count >= 2 && path.Commands[0].Kind == ChartPathCommandKind.MoveTo
                && path.Commands.Skip(1).All(command => command.Kind == ChartPathCommandKind.LineTo);
        }
        return (edge.PathGeometry == null || !edge.PathGeometry.Commands.Any(command => command.Command == 'C'))
            && (edge.PathGeometry != null || edge.Routing != TopologyEdgeRouting.Curved || edge.Waypoints.Count != 0)
            && !IsGeographicCurve(_chart, edge, _nodesById)
            && !ShouldRoundEdgeCorners(edge, trunkTail ? _trunks[edge].Tail : _paintRoutes[edge], _options);
    }
    private int CrossingPaintCommandCount(TopologyEdge edge, bool trunkTail) =>
        OrdinaryLogicalPath(edge, trunkTail)?.Commands.Count ?? (trunkTail ? _trunks[edge].Tail.Count : _paintRoutes[edge].Count);

    // A natural frame can replace the provisional builder; publish facts only after final fitting.
    private void EmitCrossingPaintDiagnostics() {
        var plan = _crossingPaintPlan; if (plan == null) return;
        if (plan.NonOrthogonalSegmentCount != 0)
            _builder.AddDiagnostic(new VisualDiagnostic("topology.crossings.non-orthogonal", "Crossing paint leaves non-orthogonal straight segments unchanged."));
        if (plan.NearEndpointOrBendCount != 0 || plan.CrowdedCrossingCount != 0)
            _builder.AddDiagnostic(new VisualDiagnostic("topology.crossings.clearance", "Unrepresentably small crossing spans are omitted to preserve endpoint, bend and neighboring-hop clearance."));

    }

    /// <summary>One additive hook shared with interpolation; ordinary input is already in output coordinates.</summary>
    private ChartPath CrossingPaintPath(TopologyEdge edge, bool trunkTail, ChartPath ordinaryScaledPath) =>
        _crossingPaintPaths.TryGetValue((edge, trunkTail), out var path) ? Transform(path) : ordinaryScaledPath;

    private bool HasCrossingPaintPath(TopologyEdge edge, bool trunkTail) => _crossingPaintPaths.ContainsKey((edge, trunkTail));

    private ChartRect IncludeCrossingPaintBounds(ChartRect content) {
        foreach (var pair in _crossingPaintPaths) {
            var bounds = pair.Value.Bounds(); var margin = Math.Max(8 * (pair.Key.Edge.PreparedEdgePlan?.DefaultUnits ?? _chart.CrossingPaint!.PreparedUnits), FittedRouteStrokeWidth(pair.Key.Edge) / 2);
            var left = Math.Min(content.Left, bounds.Left - margin); var top = Math.Min(content.Top, bounds.Top - margin);
            var right = Math.Max(content.Right, bounds.Right + margin); var bottom = Math.Max(content.Bottom, bounds.Bottom + margin);
            content = new ChartRect(left, top, right - left, bottom - top);
        }
        return content;
    }
}
