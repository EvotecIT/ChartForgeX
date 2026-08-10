using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>Provides deterministic, machine-readable layout diagnostics for topology hosts and tests.</summary>
public static class TopologyLayoutDiagnostics {
    /// <summary>Prepares a topology through the normal layout pipeline and returns its geometry diagnostics.</summary>
    public static TopologyLayoutDiagnosticReport Analyze(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var effective = (options ?? new TopologyRenderOptions()).CloneForRendering();
        var validator = new TopologyChartValidator();
        var sourceValidation = validator.ValidateScenarioReferences(chart);
        if (!sourceValidation.IsValid) throw new TopologyValidationException(sourceValidation);
        var prepared = TopologyLayoutEngine.Prepare(chart, effective.View, effective);
        var validation = validator.Validate(prepared, validateScenarioReferences: false, effective);
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        return AnalyzePrepared(prepared, effective);
    }

    internal static TopologyLayoutDiagnosticReport AnalyzePrepared(TopologyChart chart, TopologyRenderOptions options) {
        var report = new TopologyLayoutDiagnosticReport(chart.Viewport.Width, chart.Viewport.Height, chart.LayoutMode, chart.LayoutDirection);
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var group in chart.Groups) report.Groups.Add(new TopologyLayoutBoundsDiagnostic(group.Id, new ChartRect(group.X, group.Y, group.Width, group.Height)));
        foreach (var node in chart.Nodes) {
            var diagnostic = new TopologyLayoutNodeDiagnostic(node.Id, new ChartRect(node.X, node.Y, node.Width, node.Height));
            foreach (var port in node.Ports) diagnostic.Ports.Add(new TopologyLayoutPortDiagnostic(port.Id, port.Side, PortPoint(node, port)));
            report.Nodes.Add(diagnostic);
        }
        foreach (var edge in chart.Edges) {
            var points = EdgePoints(chart, edge, nodes);
            points = RenderedEdgeSamplePoints(chart, edge, nodes, points);
            var route = EdgeRouteDiagnostics(chart, edge, nodes);
            report.Edges.Add(new TopologyLayoutEdgeDiagnostic(edge.Id, edge.SourceNodeId, edge.TargetNodeId, points, route.Strategy, route.Corridor, route.ObstacleHits, route.LabelObstacleHits, route.RouteOverlapScore, route.FallbackReason));
        }
        for (var i = 0; i < report.Nodes.Count; i++) for (var j = i + 1; j < report.Nodes.Count; j++) {
            if (Intersects(report.Nodes[i].Bounds, report.Nodes[j].Bounds)) report.Collisions.Add(new TopologyLayoutCollisionDiagnostic("node-node", report.Nodes[i].Id, report.Nodes[j].Id, Intersection(report.Nodes[i].Bounds, report.Nodes[j].Bounds)));
        }
        return report;
    }

    private static ChartPoint PortPoint(TopologyNode node, TopologyNodePort port) {
        return port.Side switch {
            TopologyEdgePort.Top => new ChartPoint(node.X + node.Width * port.Offset, node.Y),
            TopologyEdgePort.Right => new ChartPoint(node.X + node.Width, node.Y + node.Height * port.Offset),
            TopologyEdgePort.Bottom => new ChartPoint(node.X + node.Width * port.Offset, node.Y + node.Height),
            TopologyEdgePort.Left => new ChartPoint(node.X, node.Y + node.Height * port.Offset),
            _ => new ChartPoint(node.X + node.Width / 2, node.Y + node.Height / 2)
        };
    }

    private static bool Intersects(ChartRect first, ChartRect second) => first.Left < second.Right && first.Right > second.Left && first.Top < second.Bottom && first.Bottom > second.Top;

    private static ChartRect Intersection(ChartRect first, ChartRect second) {
        var left = Math.Max(first.Left, second.Left);
        var top = Math.Max(first.Top, second.Top);
        return new ChartRect(left, top, Math.Max(0, Math.Min(first.Right, second.Right) - left), Math.Max(0, Math.Min(first.Bottom, second.Bottom) - top));
    }
}

/// <summary>Contains prepared topology geometry and collision diagnostics.</summary>
public sealed class TopologyLayoutDiagnosticReport {
    internal TopologyLayoutDiagnosticReport(double width, double height, TopologyLayoutMode layoutMode, TopologyLayoutDirection layoutDirection) {
        Width = width;
        Height = height;
        LayoutMode = layoutMode;
        LayoutDirection = layoutDirection;
    }
    /// <summary>Gets prepared viewport width.</summary>
    public double Width { get; }
    /// <summary>Gets prepared viewport height.</summary>
    public double Height { get; }
    /// <summary>Gets the applied layout mode.</summary>
    public TopologyLayoutMode LayoutMode { get; }
    /// <summary>Gets the applied layout direction.</summary>
    public TopologyLayoutDirection LayoutDirection { get; }
    /// <summary>Gets prepared group bounds.</summary>
    public List<TopologyLayoutBoundsDiagnostic> Groups { get; } = new();
    /// <summary>Gets prepared node bounds and ports.</summary>
    public List<TopologyLayoutNodeDiagnostic> Nodes { get; } = new();
    /// <summary>Gets prepared edge routes and router diagnostics.</summary>
    public List<TopologyLayoutEdgeDiagnostic> Edges { get; } = new();
    /// <summary>Gets detected geometry collisions.</summary>
    public List<TopologyLayoutCollisionDiagnostic> Collisions { get; } = new();
    /// <summary>Gets whether the prepared layout contains detected collisions.</summary>
    public bool HasCollisions => Collisions.Count > 0;
}

/// <summary>Describes one prepared bounds rectangle.</summary>
public class TopologyLayoutBoundsDiagnostic {
    internal TopologyLayoutBoundsDiagnostic(string id, ChartRect bounds) { Id = id; Bounds = bounds; }
    /// <summary>Gets the model identifier.</summary>
    public string Id { get; }
    /// <summary>Gets prepared bounds.</summary>
    public ChartRect Bounds { get; }
}

/// <summary>Describes one prepared node and its named ports.</summary>
public sealed class TopologyLayoutNodeDiagnostic : TopologyLayoutBoundsDiagnostic {
    internal TopologyLayoutNodeDiagnostic(string id, ChartRect bounds) : base(id, bounds) { }
    /// <summary>Gets prepared named port positions.</summary>
    public List<TopologyLayoutPortDiagnostic> Ports { get; } = new();
}

/// <summary>Describes one prepared named port.</summary>
public sealed class TopologyLayoutPortDiagnostic {
    internal TopologyLayoutPortDiagnostic(string id, TopologyEdgePort side, ChartPoint position) { Id = id; Side = side; Position = position; }
    /// <summary>Gets the node-local port id.</summary>
    public string Id { get; }
    /// <summary>Gets the boundary side.</summary>
    public TopologyEdgePort Side { get; }
    /// <summary>Gets the prepared port position.</summary>
    public ChartPoint Position { get; }
}

/// <summary>Describes one prepared edge route and routing quality metrics.</summary>
public sealed class TopologyLayoutEdgeDiagnostic {
    internal TopologyLayoutEdgeDiagnostic(string id, string sourceNodeId, string targetNodeId, IReadOnlyList<ChartPoint> points, string strategy, string corridor, int obstacleHits, int labelObstacleHits, double routeOverlapScore, string fallbackReason) {
        Id = id; SourceNodeId = sourceNodeId; TargetNodeId = targetNodeId; Points = new List<ChartPoint>(points); Strategy = strategy; Corridor = corridor; ObstacleHits = obstacleHits; LabelObstacleHits = labelObstacleHits; RouteOverlapScore = routeOverlapScore; FallbackReason = fallbackReason;
    }
    /// <summary>Gets the edge id.</summary>
    public string Id { get; }
    /// <summary>Gets the source node id.</summary>
    public string SourceNodeId { get; }
    /// <summary>Gets the target node id.</summary>
    public string TargetNodeId { get; }
    /// <summary>Gets sampled points along the prepared route as rendered by static and motion outputs.</summary>
    public IReadOnlyList<ChartPoint> Points { get; }
    /// <summary>Gets the selected router strategy.</summary>
    public string Strategy { get; }
    /// <summary>Gets the selected routing corridor.</summary>
    public string Corridor { get; }
    /// <summary>Gets node or group obstacle intersections.</summary>
    public int ObstacleHits { get; }
    /// <summary>Gets edge-label obstacle intersections.</summary>
    public int LabelObstacleHits { get; }
    /// <summary>Gets overlap score against other routes.</summary>
    public double RouteOverlapScore { get; }
    /// <summary>Gets a deterministic router fallback reason when applicable.</summary>
    public string FallbackReason { get; }
}

/// <summary>Describes one detected overlap between prepared layout items.</summary>
public sealed class TopologyLayoutCollisionDiagnostic {
    internal TopologyLayoutCollisionDiagnostic(string kind, string firstId, string secondId, ChartRect bounds) { Kind = kind; FirstId = firstId; SecondId = secondId; Bounds = bounds; }
    /// <summary>Gets the collision kind token.</summary>
    public string Kind { get; }
    /// <summary>Gets the first item id.</summary>
    public string FirstId { get; }
    /// <summary>Gets the second item id.</summary>
    public string SecondId { get; }
    /// <summary>Gets the overlap rectangle.</summary>
    public ChartRect Bounds { get; }
}
