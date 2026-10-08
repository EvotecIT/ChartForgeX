using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    private const int MaximumCachedPlans = 8;
    private static readonly List<CachedPlan> CachedPlans = new();
    private static readonly object CacheGate = new();

    /// <summary>
    /// Everything the route search, the crossing and lane passes and the trunk pass read, taken after the scene,
    /// terminals and authored routes are derived: obstacles and their nodes, group panels, the content area, every
    /// request with its order, distance and terminals (nodes, sides, ports, stubs, penalties, node boxes), the authored
    /// route samples and, when incoming trunks are shared, each edge's trunk grouping inputs.
    /// </summary>
    /// <remarks>
    /// The plan is a deterministic function of these values, so two charts with equal keys get equal routes. Hosts
    /// render a chart more than once (light and dark drawings each prepare their own chart); those renders share one
    /// plan. Numbers are compared bit for bit and texts ordinally; the hash only selects candidates.
    /// </remarks>
    private sealed class PlanKey : IEquatable<PlanKey> {
        private readonly long[] _numbers;
        private readonly string[] _texts;
        private readonly int _hash;

        private PlanKey(long[] numbers, string[] texts) {
            _numbers = numbers;
            _texts = texts;
            unchecked {
                var hash = (int)2166136261;
                foreach (var number in numbers) hash = (hash ^ (int)number ^ (int)(number >> 32)) * 16777619;
                foreach (var text in texts) hash = (hash ^ StringComparer.Ordinal.GetHashCode(text)) * 16777619;
                _hash = hash;
            }
        }

        public static PlanKey Create(TopologyChart chart, Scene scene, List<TopologyEdge> edges, List<Request> requests, List<List<ChartPoint>> fixedRoutes) {
            var numbers = new List<long>(1024);
            var texts = new List<string>(256);
            Add(numbers, scene.Region);
            numbers.Add(scene.Obstacles.Count);
            foreach (var obstacle in scene.Obstacles) {
                Add(numbers, obstacle.Box);
                numbers.Add(obstacle.NodeId == null ? 0 : 1);
                texts.Add(obstacle.NodeId ?? string.Empty);
            }

            numbers.Add(scene.Groups.Count);
            foreach (var group in scene.Groups) Add(numbers, group);
            numbers.Add(edges.Count);
            numbers.Add(requests.Count);
            foreach (var request in requests) {
                numbers.Add(request.Order);
                Add(numbers, request.Distance);
                texts.Add(request.Source.Id);
                texts.Add(request.Target.Id);
                Add(numbers, texts, request.Starts);
                Add(numbers, texts, request.Ends);
            }

            numbers.Add(fixedRoutes.Count);
            foreach (var route in fixedRoutes) {
                numbers.Add(route.Count);
                foreach (var point in route) Add(numbers, point);
            }

            var options = chart.RenderOptions!;
            numbers.Add(options.ShareIncomingTrunks ? 1 : 0);
            if (options.ShareIncomingTrunks) {
                numbers.Add(options.EnableHtmlInteractions && options.EnableHtmlForceGraphControls &&
                    chart.LayoutMode is TopologyLayoutMode.ForceDirected or TopologyLayoutMode.RelationshipRadial ? 1 : 0);
                var highlight = TopologyHighlightState.From(chart, options);
                foreach (var edge in edges) {
                    texts.Add(TopologyRenderPrimitives.EffectiveEdgeDash(edge) ?? string.Empty);
                    texts.Add(TrunkInputs(edge, options, highlight));
                }
            }

            return new PlanKey(numbers.ToArray(), texts.ToArray());
        }

        private static void Add(List<long> numbers, List<string> texts, List<Terminal> terminals) {
            numbers.Add(terminals.Count);
            foreach (var terminal in terminals) {
                texts.Add(terminal.Node.Id);
                Add(numbers, terminal.Node.X);
                Add(numbers, terminal.Node.Y);
                Add(numbers, terminal.Node.Width);
                Add(numbers, terminal.Node.Height);
                numbers.Add((long)terminal.Side);
                Add(numbers, terminal.Port);
                Add(numbers, terminal.Stub);
                Add(numbers, terminal.Penalty);
                numbers.Add(terminal.IsFixed ? 1 : 0);
            }
        }

        private static void Add(List<long> numbers, Box box) {
            Add(numbers, box.Left);
            Add(numbers, box.Top);
            Add(numbers, box.Right);
            Add(numbers, box.Bottom);
        }

        private static void Add(List<long> numbers, ChartPoint point) {
            Add(numbers, point.X);
            Add(numbers, point.Y);
        }

        private static void Add(List<long> numbers, double value) => numbers.Add(BitConverter.DoubleToInt64Bits(value));

        public bool Equals(PlanKey? other) {
            if (other == null || other._hash != _hash || other._numbers.Length != _numbers.Length || other._texts.Length != _texts.Length) return false;
            for (var i = 0; i < _numbers.Length; i++) {
                if (_numbers[i] != other._numbers[i]) return false;
            }

            for (var i = 0; i < _texts.Length; i++) {
                if (!string.Equals(_texts[i], other._texts[i], StringComparison.Ordinal)) return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as PlanKey);

        public override int GetHashCode() => _hash;
    }

    /// <summary>A computed plan by handled-edge index, kept as copies so no chart shares route lists with another.</summary>
    private sealed class CachedPlan {
        public CachedPlan(PlanKey key, List<TopologyEdge> edges, Dictionary<TopologyEdge, List<ChartPoint>?> routes,
            Dictionary<TopologyEdge, List<ChartPoint>> paintRoutes, Dictionary<TopologyEdge, TopologyEdge> trunkOwners) {
            Key = key;
            var index = new Dictionary<TopologyEdge, int>(edges.Count);
            for (var i = 0; i < edges.Count; i++) index[edges[i]] = i;
            Routes = new ChartPoint[]?[edges.Count];
            PaintRoutes = new ChartPoint[]?[edges.Count];
            TrunkOwners = new int[edges.Count];
            for (var i = 0; i < edges.Count; i++) {
                Routes[i] = routes.TryGetValue(edges[i], out var route) && route != null ? route.ToArray() : null;
                PaintRoutes[i] = paintRoutes.TryGetValue(edges[i], out var paint) ? paint.ToArray() : null;
                TrunkOwners[i] = trunkOwners.TryGetValue(edges[i], out var owner) ? index[owner] : -1;
            }
        }

        public PlanKey Key { get; }
        public ChartPoint[]?[] Routes { get; }
        public ChartPoint[]?[] PaintRoutes { get; }
        public int[] TrunkOwners { get; }

        public Dictionary<TopologyEdge, List<ChartPoint>?> Restore(List<TopologyEdge> edges,
            Dictionary<TopologyEdge, List<ChartPoint>> paintRoutes, Dictionary<TopologyEdge, TopologyEdge> trunkOwners) {
            var routes = new Dictionary<TopologyEdge, List<ChartPoint>?>(edges.Count);
            for (var i = 0; i < edges.Count; i++) {
                routes[edges[i]] = Routes[i] == null ? null : new List<ChartPoint>(Routes[i]!);
                if (PaintRoutes[i] != null) paintRoutes[edges[i]] = new List<ChartPoint>(PaintRoutes[i]!);
                if (TrunkOwners[i] >= 0) trunkOwners[edges[i]] = edges[TrunkOwners[i]];
            }

            return routes;
        }
    }

    /// <summary>Drops every cached plan; for tests and benchmarks that measure planning itself.</summary>
    internal static void ClearPlanCache() {
        lock (CacheGate) CachedPlans.Clear();
    }

    /// <summary>The number of cached plans; for tests.</summary>
    internal static int CachedPlanCount {
        get { lock (CacheGate) return CachedPlans.Count; }
    }

    private static CachedPlan? FindCachedPlan(PlanKey key) {
        lock (CacheGate) {
            for (var i = 0; i < CachedPlans.Count; i++) {
                if (!CachedPlans[i].Key.Equals(key)) continue;
                var plan = CachedPlans[i];
                // Most recently used plans stay at the end.
                CachedPlans.RemoveAt(i);
                CachedPlans.Add(plan);
                return plan;
            }
        }

        return null;
    }

    private static void StoreCachedPlan(CachedPlan plan) {
        lock (CacheGate) {
            for (var i = 0; i < CachedPlans.Count; i++) {
                if (CachedPlans[i].Key.Equals(plan.Key)) return;
            }

            if (CachedPlans.Count >= MaximumCachedPlans) CachedPlans.RemoveAt(0);
            CachedPlans.Add(plan);
        }
    }

    /// <summary>
    /// The inputs of <see cref="TrunkKey"/> that come from the edge and the render options; the end side it also uses
    /// is planned from the rest of the key. Keep the two in step.
    /// </summary>
    private static string TrunkInputs(TopologyEdge edge, TopologyRenderOptions options, TopologyHighlightState highlight) =>
        string.Join("|", edge.TargetNodeId.Length + ":" + edge.TargetNodeId, (int)edge.Kind, (int)edge.Status,
            (int)edge.Direction, (int)edge.Emphasis, edge.IsMuted, edge.Color?.Length + ":" + edge.Color,
            edge.StrokeWidth?.ToString("R", CultureInfo.InvariantCulture), edge.Opacity?.ToString("R", CultureInfo.InvariantCulture),
            (int?)edge.TargetMarker, edge.TargetPortId?.Length + ":" + edge.TargetPortId,
            options.SelectedEdgeIds.Contains(edge.Id), highlight.IsEdgeHighlighted(edge));
}
