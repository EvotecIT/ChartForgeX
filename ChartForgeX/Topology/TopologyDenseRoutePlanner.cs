using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Routes every obstacle-avoiding edge of a readable dense topology together, so routes know about each other:
/// each edge is searched on one grid built from the card, caption, and group-header edges, paying extra for corridors
/// other routes already use, and routes that still share a corridor are then moved onto parallel lanes.
/// </summary>
/// <remarks>
/// The plan is computed once per prepared chart and reused by layout, both renderers, and diagnostics. It is keyed on
/// the chart geometry because normalization can grow the viewport after an earlier routing pass.
/// </remarks>
internal static partial class TopologyDenseRoutePlanner {
    /// <summary>Distance between a card border and the point where a route ends.</summary>
    internal const double EndpointGap = 7;
    /// <summary>Routes are searched this far from cards, captions, and group headers.</summary>
    private const double Clearance = 10;
    /// <summary>Lanes moved apart keep at least this distance from cards, captions, and group headers.</summary>
    private const double LaneClearance = 5;
    /// <summary>Preferred straight run between a card and the first bend, so direction markers have room.</summary>
    private const double TerminalRun = 12;
    /// <summary>Route ends keep this distance from the corners of the card side they attach to.</summary>
    private const double SidePadding = 8;

    private static readonly ConditionalWeakTable<TopologyChart, PlanHolder> Plans = new();

    /// <summary>Returns true when the planner owns the route of this edge.</summary>
    public static bool Handles(TopologyChart chart, TopologyEdge edge) =>
        TopologyLayoutEngine.UsesReadableDenseLayout(chart) &&
        edge.Routing == TopologyEdgeRouting.ObstacleAvoidingOrthogonal && edge.Waypoints.Count == 0 &&
        !string.Equals(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal);

    /// <summary>
    /// Returns the planned route of an edge, or null when the planner does not own the edge or found no route that
    /// stays clear of every obstacle inside the content area.
    /// </summary>
    public static List<ChartPoint>? Route(TopologyChart chart, TopologyEdge edge) =>
        Handles(chart, edge) && Plan(chart) is { } plan && plan.TryGetValue(edge, out var points) && points != null ? new List<ChartPoint>(points) : null;

    /// <summary>Returns true when the planner found a route for this edge.</summary>
    public static bool IsPlanned(TopologyChart chart, TopologyEdge edge) =>
        Handles(chart, edge) && Plan(chart) is { } plan && plan.TryGetValue(edge, out var points) && points != null;

    /// <summary>
    /// Returns the current routes of a readable dense chart by edge, or null for other charts. An edge the planner owns
    /// but could not connect maps to null. Callers that need many routes fetch the plan once; the lists are shared and
    /// must not be changed.
    /// </summary>
    public static IReadOnlyDictionary<TopologyEdge, List<ChartPoint>?>? Plan(TopologyChart chart) {
        if (!TopologyLayoutEngine.UsesReadableDenseLayout(chart)) return null;
        var holder = Plans.GetOrCreateValue(chart);
        lock (holder) {
            var signature = Signature(chart);
            if (holder.Routes == null || holder.Signature != signature) {
                holder.Routes = Compute(chart);
                holder.Signature = signature;
            }

            return holder.Routes;
        }
    }

    // A plan is reused only while cards, groups, the viewport, the render options, and every edge's endpoints stay the
    // same. Identifiers are compared by reference, which keeps the check cheap; it runs on every route lookup.
    private static long Signature(TopologyChart chart) {
        unchecked {
            long hash = chart.Nodes.Count * 397L + chart.Groups.Count;
            hash = Mix(Mix(hash, chart.Viewport.Width), chart.Viewport.Height);
            hash = hash * 1_000_003L ^ (chart.RenderOptions == null ? 0 : RuntimeHelpers.GetHashCode(chart.RenderOptions));
            foreach (var node in chart.Nodes) hash = Mix(Mix(Mix(Mix(hash, node.X), node.Y), node.Width), node.Height);
            foreach (var group in chart.Groups) hash = Mix(Mix(Mix(Mix(hash, group.X), group.Y), group.Width), group.Height);
            foreach (var edge in chart.Edges) {
                hash = hash * 1_000_003L ^ RuntimeHelpers.GetHashCode(edge);
                hash = hash * 31 + (int)edge.SourcePort * 7 + (int)edge.TargetPort;
                hash = hash * 31 + (int)edge.Routing * 5 + edge.Waypoints.Count;
                hash = hash * 31 + RuntimeHelpers.GetHashCode(edge.SourceNodeId);
                hash = hash * 31 + RuntimeHelpers.GetHashCode(edge.TargetNodeId);
                hash = hash * 31 + (edge.SourcePortId == null ? 0 : RuntimeHelpers.GetHashCode(edge.SourcePortId));
                hash = hash * 31 + (edge.TargetPortId == null ? 0 : RuntimeHelpers.GetHashCode(edge.TargetPortId));
            }

            return hash;
        }
    }

    private static long Mix(long hash, double value) => unchecked(hash * 1_000_003L ^ BitConverter.DoubleToInt64Bits(value));

    private static Dictionary<TopologyEdge, List<ChartPoint>?> Compute(TopologyChart chart) {
        var routes = new Dictionary<TopologyEdge, List<ChartPoint>?>();
        var nodes = new Dictionary<string, TopologyNode>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (!nodes.ContainsKey(node.Id)) nodes.Add(node.Id, node);
        }

        var edges = chart.Edges.Where(edge => Handles(chart, edge) && nodes.ContainsKey(edge.SourceNodeId) && nodes.ContainsKey(edge.TargetNodeId)).ToList();
        if (edges.Count == 0) return routes;
        var scene = Scene.Create(chart);
        if (scene == null) return routes;

        var requests = new List<Request>(edges.Count);
        for (var i = 0; i < edges.Count; i++) {
            var edge = edges[i];
            var source = nodes[edge.SourceNodeId];
            var target = nodes[edge.TargetNodeId];
            var starts = Terminals(chart, scene, source, target, edge.SourcePort, edge.SourcePortId, (edge.LayoutInference & TopologyEdgeLayoutInference.SourcePort) != 0);
            var ends = Terminals(chart, scene, target, source, edge.TargetPort, edge.TargetPortId, (edge.LayoutInference & TopologyEdgeLayoutInference.TargetPort) != 0);
            routes[edge] = null;
            if (starts.Count == 0 || ends.Count == 0) continue;
            var distance = Math.Abs(Center(source).X - Center(target).X) + Math.Abs(Center(source).Y - Center(target).Y);
            requests.Add(new Request(edge, i, source, target, starts, ends, distance));
        }

        // Without a grid (more lines than the search can hold) only facing neighbours are joined; every other edge falls
        // back to the corridor candidates.
        var grid = Grid.Create(scene, requests);

        // Short routes are searched first: they have the fewest alternatives, and long routes can go around them.
        var planned = new List<PlannedRoute>(requests.Count);
        var sideUse = new Dictionary<(TopologyNode Node, TopologyEdgePort Side), int>();
        foreach (var request in requests.OrderBy(item => item.Distance).ThenBy(item => item.Order)) {
            var route = Direct(scene, request, sideUse) ?? (grid == null ? null : Search(grid, request, sideUse));
            if (route == null) continue;
            planned.Add(route);
            Count(sideUse, route.Start);
            Count(sideUse, route.End);
        }

        SeparateLanes(scene, planned);
        foreach (var route in planned) routes[route.Request.Edge] = route.Points;
        return routes;
    }

    private static void Count(Dictionary<(TopologyNode Node, TopologyEdgePort Side), int> sideUse, Terminal terminal) {
        sideUse.TryGetValue((terminal.Node, terminal.Side), out var used);
        sideUse[(terminal.Node, terminal.Side)] = used + 1;
    }

    private static ChartPoint Center(TopologyNode node) => new(node.X + node.Width / 2, node.Y + node.Height / 2);

    /// <summary>
    /// Lists the points where an edge may leave or enter a node. A named port is used as declared; a side the caller
    /// chose is kept unless a caption sits below it; otherwise every side that has no caption is offered and the search
    /// picks the cheapest, so a route never starts along the side of its own card.
    /// </summary>
    private static List<Terminal> Terminals(TopologyChart chart, Scene scene, TopologyNode node, TopologyNode other, TopologyEdgePort port, string? namedPortId, bool inferred) {
        var terminals = new List<Terminal>(4);
        var caption = TopologyNodeFootprint.Caption(chart, node);
        var hasCaption = caption.Height > 0.5;
        var captionLeft = node.X + node.Width / 2 - caption.Width / 2;
        var captionRight = node.X + node.Width / 2 + caption.Width / 2;
        var namedPort = string.IsNullOrWhiteSpace(namedPortId) ? null : node.Ports.FirstOrDefault(candidate => string.Equals(candidate.Id, namedPortId, StringComparison.Ordinal));
        TopologyEdgePort[] sides;
        if (namedPort != null) sides = new[] { namedPort.Side };
        else if (port != TopologyEdgePort.Auto && !inferred && !(port == TopologyEdgePort.Bottom && hasCaption)) sides = new[] { port };
        else sides = new[] { TopologyEdgePort.Top, TopologyEdgePort.Right, TopologyEdgePort.Bottom, TopologyEdgePort.Left };

        var preferred = namedPort != null ? namedPort.Side : port != TopologyEdgePort.Auto ? port : FacingSide(node, other);
        foreach (var side in sides) {
            // A caption sits below the card, so nothing leaves through the bottom of a captioned tile.
            if (side == TopologyEdgePort.Bottom && hasCaption) continue;
            var vertical = side is TopologyEdgePort.Top or TopologyEdgePort.Bottom;
            var offset = namedPort != null ? namedPort.Offset : 0.5;
            var x = node.X + node.Width * (vertical ? offset : 0.5);
            var y = node.Y + node.Height * (vertical ? 0.5 : offset);
            ChartPoint portPoint = side switch {
                TopologyEdgePort.Top => new ChartPoint(x, node.Y - EndpointGap),
                TopologyEdgePort.Bottom => new ChartPoint(x, node.Y + node.Height + EndpointGap),
                TopologyEdgePort.Left => new ChartPoint(node.X - EndpointGap, y),
                _ => new ChartPoint(node.X + node.Width + EndpointGap, y)
            };

            // A stub long enough for a direction marker is preferred; the shortest one that clears the card is also
            // offered, at a small cost, because two long stubs of neighbouring cards can overshoot each other.
            var offered = 0;
            foreach (var run in new[] { EndpointGap + TerminalRun, Clearance + 1 }) {
                ChartPoint stub = side switch {
                    TopologyEdgePort.Top => new ChartPoint(x, node.Y - run),
                    TopologyEdgePort.Bottom => new ChartPoint(x, node.Y + node.Height + run),
                    TopologyEdgePort.Left => new ChartPoint(Math.Min(node.X - run, hasCaption ? captionLeft - Clearance - 1 : double.PositiveInfinity), y),
                    _ => new ChartPoint(Math.Max(node.X + node.Width + run, hasCaption ? captionRight + Clearance + 1 : double.NegativeInfinity), y)
                };
                if (namedPort == null) stub = new ChartPoint(Math.Round(stub.X * 2) / 2, Math.Round(stub.Y * 2) / 2);
                // Keep the first leg exactly orthogonal after rounding: the end slides along the card side onto the stub axis.
                var end = vertical ? new ChartPoint(stub.X, portPoint.Y) : new ChartPoint(portPoint.X, stub.Y);
                if (!scene.Region.Contains(stub) || !scene.Region.Contains(end) || scene.Blocks(stub, end, node.Id)) continue;
                terminals.Add(new Terminal(node, side, end, stub, (side == preferred ? 0 : SidePenalty) + (offered++ == 0 ? 0 : ShortStubPenalty), namedPort != null));
            }
        }

        return terminals;
    }

    private static TopologyEdgePort FacingSide(TopologyNode node, TopologyNode other) {
        var dx = Center(other).X - Center(node).X;
        var dy = Center(other).Y - Center(node).Y;
        if (Math.Abs(dx) >= Math.Abs(dy)) return dx >= 0 ? TopologyEdgePort.Right : TopologyEdgePort.Left;
        return dy >= 0 ? TopologyEdgePort.Bottom : TopologyEdgePort.Top;
    }

    private sealed class PlanHolder {
        public long Signature;
        public Dictionary<TopologyEdge, List<ChartPoint>?>? Routes;
    }

    private sealed class Request {
        public Request(TopologyEdge edge, int order, TopologyNode source, TopologyNode target, List<Terminal> starts, List<Terminal> ends, double distance) {
            Edge = edge;
            Order = order;
            Source = source;
            Target = target;
            Starts = starts;
            Ends = ends;
            Distance = distance;
        }

        public TopologyEdge Edge { get; }
        public int Order { get; }
        public TopologyNode Source { get; }
        public TopologyNode Target { get; }
        public List<Terminal> Starts { get; }
        public List<Terminal> Ends { get; }
        public double Distance { get; }
    }

    private sealed class Terminal {
        public Terminal(TopologyNode node, TopologyEdgePort side, ChartPoint port, ChartPoint stub, double penalty, bool isFixed) {
            Node = node;
            Side = side;
            Port = port;
            Stub = stub;
            Penalty = penalty;
            IsFixed = isFixed;
        }

        public TopologyNode Node { get; }
        public TopologyEdgePort Side { get; }
        /// <summary>Where the route ends, <see cref="EndpointGap"/> outside the card.</summary>
        public ChartPoint Port { get; }
        /// <summary>The grid point straight out from the port where the search starts or finishes.</summary>
        public ChartPoint Stub { get; }
        public double Penalty { get; }
        /// <summary>A named port keeps its declared position when lanes are separated.</summary>
        public bool IsFixed { get; }
    }

    private sealed class PlannedRoute {
        public PlannedRoute(Request request, Terminal start, Terminal end, List<ChartPoint> points) {
            Request = request;
            Start = start;
            End = end;
            Points = points;
        }

        public Request Request { get; }
        public Terminal Start { get; }
        public Terminal End { get; }
        public List<ChartPoint> Points { get; set; }
    }

    /// <summary>The obstacles routes keep clear of and the area routes may use.</summary>
    private sealed class Scene {
        private Scene(List<Obstacle> obstacles, List<Box> groups, Box region) {
            Obstacles = obstacles;
            Groups = groups;
            Region = region;
        }

        public List<Obstacle> Obstacles { get; }
        /// <summary>Drawn group panels; routes may cross their borders but should not run along them.</summary>
        public List<Box> Groups { get; }
        public Box Region { get; }

        public static Scene? Create(TopologyChart chart) {
            var options = chart.RenderOptions!;
            var obstacles = new List<Obstacle>(chart.Nodes.Count * 2 + chart.Groups.Count);
            foreach (var node in chart.Nodes) {
                // Artwork nodes are backdrops that routes may pass over.
                if (node.DisplayMode == TopologyNodeDisplayMode.Artwork || (!node.DisplayMode.HasValue && TopologyRenderPrimitives.HasRenderableNodeArtwork(node))) continue;
                obstacles.Add(new Obstacle(new Box(node.X, node.Y, node.X + node.Width, node.Y + node.Height), node.Id));
                var caption = TopologyNodeFootprint.Caption(chart, node);
                if (caption.Height <= 0.5) continue;
                var center = node.X + node.Width / 2;
                obstacles.Add(new Obstacle(new Box(center - caption.Width / 2, node.Y + node.Height, center + caption.Width / 2, node.Y + node.Height + caption.Height), node.Id));
            }

            var groups = new List<Box>();
            if (options.IncludeGroups) {
                foreach (var group in chart.Groups) {
                    if (group.Width > 0 && group.Height > 0) groups.Add(new Box(group.X, group.Y, group.X + group.Width, group.Y + group.Height));
                }
            }

            if (TopologyGroupHeader.IsDrawn(options)) {
                foreach (var group in chart.Groups) {
                    var header = TopologyGroupHeader.Bounds(group, options, chart.TextMeasurement);
                    if (header.Width > 0 && header.Height > 0) obstacles.Add(new Obstacle(new Box(header.Left, header.Top, header.Right, header.Bottom), null));
                }
            }

            var pad = chart.Viewport.Padding;
            var titled = options.IncludeTitle && !(string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle));
            var top = pad + (titled ? TopologyRenderPrimitives.HeaderReservedHeight : 0);
            var bottom = chart.Viewport.Height - pad - TopologyRenderPrimitives.LegendReservedHeight(chart.Legend, chart.Viewport);
            if (bottom <= top || chart.Viewport.Width <= pad * 2) return null;
            return new Scene(obstacles, groups, new Box(pad, top, chart.Viewport.Width - pad, bottom));
        }

        /// <summary>Returns true when a segment comes closer than the search clearance to an obstacle of another node.</summary>
        public bool Blocks(ChartPoint a, ChartPoint b, string ownNodeId, string? otherNodeId = null) {
            foreach (var obstacle in Obstacles) {
                if (obstacle.NodeId != null && (string.Equals(obstacle.NodeId, ownNodeId, StringComparison.Ordinal) || string.Equals(obstacle.NodeId, otherNodeId, StringComparison.Ordinal))) continue;
                if (obstacle.Box.Expand(Clearance).Intersects(a, b)) return true;
            }

            return false;
        }
    }

    private readonly struct Obstacle {
        public Obstacle(Box box, string? nodeId) {
            Box = box;
            NodeId = nodeId;
        }

        public Box Box { get; }
        /// <summary>The node a card or caption belongs to; null for a group header.</summary>
        public string? NodeId { get; }
    }

    private readonly struct Box {
        public Box(double left, double top, double right, double bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public double Left { get; }
        public double Top { get; }
        public double Right { get; }
        public double Bottom { get; }

        public Box Expand(double by) => new(Left - by, Top - by, Right + by, Bottom + by);

        public bool Contains(ChartPoint point) => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

        /// <summary>Returns true when a horizontal or vertical segment touches the box.</summary>
        public bool Intersects(ChartPoint a, ChartPoint b) =>
            Math.Max(a.X, b.X) >= Left && Math.Min(a.X, b.X) <= Right && Math.Max(a.Y, b.Y) >= Top && Math.Min(a.Y, b.Y) <= Bottom;
    }
}
