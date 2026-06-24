using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static IReadOnlyDictionary<string, Point> ComputePositions(GraphScene scene) {
        var positions = new Dictionary<string, Point>(StringComparer.Ordinal);
        var nodes = scene.Nodes;
        for (var i = 0; i < nodes.Count; i++) {
            var node = nodes[i];
            if (node.HasExplicitPosition) positions[node.Id] = new Point(node.X, node.Y);
        }

        var generated = nodes.Where(node => !node.HasExplicitPosition).ToArray();
        if (generated.Length == 0) return positions;

        var adjacency = BuildAdjacency(scene);
        var clusterMembership = BuildClusterMembership(scene);
        var components = ConnectedComponents(nodes, adjacency);
        var centers = ComponentCenters(components);
        for (var i = 0; i < components.Count; i++) {
            PlaceComponent(components[i], centers[i], adjacency, positions, clusterMembership);
        }

        if (generated.All(node => positions.ContainsKey(node.Id))) {
            SeparatePreparedOverlaps(positions, generated, 6);
            NormalizeGeneratedPositions(positions, generated);
            SeparatePreparedOverlaps(positions, generated, generated.Length >= 500 ? 3 : 8);
            NormalizeGeneratedPositions(positions, generated);
            AnchorGeneratedHubs(positions, components, centers, adjacency);
        }

        return positions;
    }

    private static Dictionary<string, List<string>> BuildAdjacency(GraphScene scene) {
        var adjacency = scene.Nodes.ToDictionary(node => node.Id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in scene.Edges) {
            if (!adjacency.ContainsKey(edge.SourceNodeId) || !adjacency.ContainsKey(edge.TargetNodeId)) continue;
            adjacency[edge.SourceNodeId].Add(edge.TargetNodeId);
            adjacency[edge.TargetNodeId].Add(edge.SourceNodeId);
        }

        return adjacency;
    }

    private static List<List<GraphSceneNode>> ConnectedComponents(IReadOnlyList<GraphSceneNode> nodes, IReadOnlyDictionary<string, List<string>> adjacency) {
        var byId = nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var components = new List<List<GraphSceneNode>>();
        foreach (var start in nodes.OrderByDescending(node => Degree(node.Id, adjacency)).ThenBy(node => node.Id, StringComparer.Ordinal)) {
            if (!visited.Add(start.Id)) continue;
            var component = new List<GraphSceneNode>();
            var queue = new Queue<string>();
            queue.Enqueue(start.Id);
            while (queue.Count > 0) {
                var id = queue.Dequeue();
                if (!byId.TryGetValue(id, out var node)) continue;
                component.Add(node);
                if (!adjacency.TryGetValue(id, out var neighbors)) continue;
                foreach (var neighbor in neighbors.OrderBy(value => value, StringComparer.Ordinal)) {
                    if (visited.Add(neighbor)) queue.Enqueue(neighbor);
                }
            }

            components.Add(component);
        }

        return components
            .OrderByDescending(component => component.Count)
            .ThenBy(component => component[0].Id, StringComparer.Ordinal)
            .ToList();
    }

    private static List<Point> ComponentCenters(IReadOnlyList<List<GraphSceneNode>> components) {
        var centers = new List<Point>(components.Count);
        for (var i = 0; i < components.Count; i++) {
            if (i == 0) {
                centers.Add(new Point(Width / 2, Height / 2));
                continue;
            }

            var angle = GoldenAngle(i - 1) - Math.PI / 2;
            var ring = Math.Min(260, 132 + 70 * Math.Sqrt(i));
            centers.Add(new Point(Width / 2 + Math.Cos(angle) * ring, Height / 2 + Math.Sin(angle) * ring * 0.68));
        }

        return centers;
    }

    private static void PlaceComponent(IReadOnlyList<GraphSceneNode> component, Point fallbackCenter, IReadOnlyDictionary<string, List<string>> adjacency, IDictionary<string, Point> positions, IReadOnlyDictionary<string, string> clusterMembership) {
        if (component.Count == 0) return;
        var explicitMembers = component.Where(node => node.HasExplicitPosition && positions.ContainsKey(node.Id)).ToArray();
        var center = explicitMembers.Length == 0
            ? fallbackCenter
            : new Point(explicitMembers.Average(node => positions[node.Id].X), explicitMembers.Average(node => positions[node.Id].Y));
        var generated = component.Where(node => !node.HasExplicitPosition).ToArray();
        if (generated.Length == 0) return;
        if (generated.Length == 1) {
            var generatedNode = generated[0];
            var explicitRadius = explicitMembers.Length == 0 ? 0 : explicitMembers.Max(PreparedNodeRadius);
            var spacing = explicitMembers.Length == 0 ? 36 : Math.Max(72, PreparedNodeRadius(generatedNode) + explicitRadius + 36);
            var angle = -Math.PI / 2 + StableUnit(generatedNode.Id + ":explicit-neighbor") * Math.PI;
            positions[generatedNode.Id] = new Point(center.X + Math.Cos(angle) * spacing, center.Y + Math.Sin(angle) * spacing * 0.74);
            return;
        }

        var depths = NodeDepths(component, adjacency);
        var hubs = ComponentHubs(component, adjacency);
        var communityCenters = CommunityCenters(component, center, clusterMembership, adjacency);
        var communityRanks = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in generated.OrderByDescending(node => hubs.Any(hub => string.Equals(hub.Id, node.Id, StringComparison.Ordinal))).ThenBy(node => depths[node.Id]).ThenByDescending(node => Degree(node.Id, adjacency)).ThenBy(node => node.Id, StringComparer.Ordinal)) {
            var hubIndex = Array.FindIndex(hubs, hub => string.Equals(hub.Id, node.Id, StringComparison.Ordinal));
            if (hubIndex >= 0) {
                var hubRadius = hubs.Length == 1
                    ? explicitMembers.Length == 0 ? 0 : Math.Max(72, PreparedNodeRadius(node) + explicitMembers.Max(PreparedNodeRadius) + 36)
                    : 24 + hubIndex * 8;
                var hubAngle = GoldenAngle(hubIndex);
                positions[node.Id] = new Point(center.X + Math.Cos(hubAngle) * hubRadius, center.Y + Math.Sin(hubAngle) * hubRadius * 0.72);
                continue;
            }

            var key = CommunityKey(node, clusterMembership);
            communityRanks.TryGetValue(key, out var rank);
            communityRanks[key] = rank + 1;
            var community = communityCenters[key];
            var depth = Math.Max(1, depths[node.Id]);
            var localAngle = GoldenAngle(rank) + StableOffset(node.Id + ":angle", 0.18);
            var localRadius = 18 + Math.Sqrt(rank + 1) * 14 + Math.Min(86, depth * 12) + StableOffset(node.Id + ":radius", 8);
            if (explicitMembers.Length > 0) localRadius = Math.Max(localRadius, PreparedNodeRadius(node) + explicitMembers.Max(PreparedNodeRadius) + 36);
            positions[node.Id] = new Point(community.X + Math.Cos(localAngle) * localRadius, community.Y + Math.Sin(localAngle) * localRadius * 0.74);
        }
    }

    private static GraphSceneNode[] ComponentHubs(IReadOnlyList<GraphSceneNode> component, IReadOnlyDictionary<string, List<string>> adjacency) {
        var maxDegree = Math.Max(1, component.Max(node => Degree(node.Id, adjacency)));
        var hubLimit = Math.Max(1, Math.Min(6, (int)Math.Ceiling(Math.Sqrt(component.Count) / 2)));
        var hubs = component
            .Where(node => Degree(node.Id, adjacency) >= Math.Max(2, maxDegree * 0.68))
            .OrderByDescending(node => Degree(node.Id, adjacency))
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .Take(hubLimit)
            .ToArray();
        return hubs.Length == 0
            ? component.OrderByDescending(node => Degree(node.Id, adjacency)).ThenBy(node => node.Id, StringComparer.Ordinal).Take(1).ToArray()
            : hubs;
    }

    private static void AnchorGeneratedHubs(IDictionary<string, Point> positions, IReadOnlyList<List<GraphSceneNode>> components, IReadOnlyList<Point> centers, IReadOnlyDictionary<string, List<string>> adjacency) {
        for (var i = 0; i < components.Count && i < centers.Count; i++) {
            if (components[i].Any(node => node.HasExplicitPosition)) continue;
            var hub = components[i]
                .Where(node => !node.HasExplicitPosition && Degree(node.Id, adjacency) > 0)
                .OrderByDescending(node => Degree(node.Id, adjacency))
                .ThenBy(node => node.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (hub == null) continue;
            positions[hub.Id] = centers[i];
        }
    }

    private static Dictionary<string, Point> CommunityCenters(IReadOnlyList<GraphSceneNode> component, Point center, IReadOnlyDictionary<string, string> clusterMembership, IReadOnlyDictionary<string, List<string>> adjacency) {
        var groups = component
            .GroupBy(node => CommunityKey(node, clusterMembership), StringComparer.Ordinal)
            .Select(group => new { Key = group.Key, Count = group.Count(), Degree = group.Sum(node => Degree(node.Id, adjacency)) })
            .OrderByDescending(group => group.Count)
            .ThenByDescending(group => group.Degree)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        var centers = new Dictionary<string, Point>(StringComparer.Ordinal);
        if (groups.Length == 1) {
            centers[groups[0].Key] = center;
            return centers;
        }

        var ring = Math.Min(Math.Min(Width, Height) * 0.39, Math.Max(112, Math.Sqrt(component.Count) * 24));
        for (var i = 0; i < groups.Length; i++) {
            var angle = -Math.PI / 2 + Math.PI * 2 * i / groups.Length;
            var sizeBias = Math.Min(1.18, Math.Max(0.86, Math.Sqrt(groups[i].Count / (double)Math.Max(1, component.Count / groups.Length))));
            centers[groups[i].Key] = new Point(center.X + Math.Cos(angle) * ring * sizeBias, center.Y + Math.Sin(angle) * ring * 0.72 * sizeBias);
        }

        return centers;
    }

    private static Dictionary<string, int> NodeDepths(IReadOnlyList<GraphSceneNode> component, IReadOnlyDictionary<string, List<string>> adjacency) {
        var componentIds = new HashSet<string>(component.Select(node => node.Id), StringComparer.Ordinal);
        var ordered = component.OrderByDescending(node => Degree(node.Id, adjacency)).ThenBy(node => node.Id, StringComparer.Ordinal).ToArray();
        var depths = component.ToDictionary(node => node.Id, _ => int.MaxValue, StringComparer.Ordinal);
        var queue = new Queue<string>();
        foreach (var node in ordered.Take(Math.Max(1, Math.Min(4, (int)Math.Ceiling(Math.Sqrt(component.Count) / 3))))) {
            depths[node.Id] = 0;
            queue.Enqueue(node.Id);
        }

        while (queue.Count > 0) {
            var id = queue.Dequeue();
            if (!adjacency.TryGetValue(id, out var neighbors)) continue;
            foreach (var neighbor in neighbors.OrderBy(value => value, StringComparer.Ordinal)) {
                if (!componentIds.Contains(neighbor) || depths[neighbor] <= depths[id] + 1) continue;
                depths[neighbor] = depths[id] + 1;
                queue.Enqueue(neighbor);
            }
        }

        foreach (var node in component) if (depths[node.Id] == int.MaxValue) depths[node.Id] = 2;
        return depths;
    }

    private static void NormalizeGeneratedPositions(IDictionary<string, Point> positions, IReadOnlyList<GraphSceneNode> generated) {
        if (generated.Count < 2) return;
        if (positions.Count > generated.Count) return;
        var points = generated.Select(node => positions[node.Id]).ToArray();
        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);
        var width = Math.Max(1, maxX - minX);
        var height = Math.Max(1, maxY - minY);
        var targetWidth = Width - 150;
        var targetHeight = Height - 130;
        var scale = Math.Min(1, Math.Min(targetWidth / width, targetHeight / height));
        if (generated.Count >= 24) {
            var minimumWidth = Width * (generated.Count >= 300 ? 0.62 : 0.48);
            var minimumHeight = Height * (generated.Count >= 300 ? 0.52 : 0.38);
            var expansion = Math.Min(targetWidth / width, targetHeight / height);
            if (width < minimumWidth || height < minimumHeight) scale = Math.Min(Math.Max(scale, Math.Min(2.4, expansion)), 1 + Math.Min(1.2, Math.Sqrt(generated.Count) / 18));
        }

        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;
        foreach (var node in generated) {
            var point = positions[node.Id];
            positions[node.Id] = new Point(Width / 2 + (point.X - centerX) * scale, Height / 2 + (point.Y - centerY) * scale);
        }
    }

    private static void SeparatePreparedOverlaps(IDictionary<string, Point> positions, IReadOnlyList<GraphSceneNode> generated, int passes) {
        if (generated.Count < 2) return;
        var cellSize = Math.Max(32, generated.Max(node => PreparedNodeRadius(node)) * 2 + 16);
        var maxPairs = generated.Count >= 3000 ? 120000 : generated.Count >= 1000 ? 180000 : 220000;
        for (var pass = 0; pass < passes; pass++) {
            var grid = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (var index = 0; index < generated.Count; index++) {
                var point = positions[generated[index].Id];
                var key = GridKey(point, cellSize);
                if (!grid.TryGetValue(key, out var bucket)) {
                    bucket = new List<int>();
                    grid[key] = bucket;
                }

                bucket.Add(index);
            }

            if (!SeparatePreparedOverlapPass(positions, generated, grid, cellSize, maxPairs, pass)) return;
        }
    }

    private static bool SeparatePreparedOverlapPass(IDictionary<string, Point> positions, IReadOnlyList<GraphSceneNode> generated, IReadOnlyDictionary<string, List<int>> grid, double cellSize, int maxPairs, int pass) {
        var pairs = 0;
        var moved = false;
        for (var i = 0; i < generated.Count; i++) {
            var first = generated[i];
            var firstPoint = positions[first.Id];
            var gx = (int)Math.Floor(firstPoint.X / cellSize);
            var gy = (int)Math.Floor(firstPoint.Y / cellSize);
            for (var ox = -1; ox <= 1; ox++) {
                for (var oy = -1; oy <= 1; oy++) {
                    if (!grid.TryGetValue((gx + ox).ToString(CultureInfo.InvariantCulture) + "," + (gy + oy).ToString(CultureInfo.InvariantCulture), out var bucket)) continue;
                    foreach (var j in bucket) {
                        if (j <= i) continue;
                        pairs++;
                        if (pairs > maxPairs) return moved;
                        moved |= SeparatePreparedPair(positions, generated, i, j, pass);
                    }
                }
            }
        }

        return moved;
    }

    private static bool SeparatePreparedPair(IDictionary<string, Point> positions, IReadOnlyList<GraphSceneNode> generated, int i, int j, int pass) {
        var first = generated[i];
        var second = generated[j];
        var a = positions[first.Id];
        var b = positions[second.Id];
        var minDistance = PreparedNodeRadius(first) + PreparedNodeRadius(second);
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance >= minDistance) return false;
        if (distance < 0.001) {
            var angle = GoldenAngle(i + j + pass);
            dx = Math.Cos(angle);
            dy = Math.Sin(angle);
            distance = 1;
        }

        var push = (minDistance - distance) / distance * 0.5;
        var fx = dx * push;
        var fy = dy * push;
        positions[first.Id] = new Point(a.X - fx, a.Y - fy);
        positions[second.Id] = new Point(b.X + fx, b.Y + fy);
        return true;
    }

    private static double PreparedNodeRadius(GraphSceneNode node) => Math.Max(14, node.Size + (node.Shape == GraphNodeShape.Box ? 16 : node.Shape == GraphNodeShape.Image ? 14 : 12));

    private static string GridKey(Point point, double cellSize) =>
        ((int)Math.Floor(point.X / cellSize)).ToString(CultureInfo.InvariantCulture) + "," + ((int)Math.Floor(point.Y / cellSize)).ToString(CultureInfo.InvariantCulture);

    private static string CommunityKey(GraphSceneNode node, IReadOnlyDictionary<string, string> clusterMembership) {
        var clusterId = NodeClusterId(node, clusterMembership);
        if (!string.IsNullOrWhiteSpace(clusterId)) return "cluster:" + clusterId;
        if (!string.IsNullOrWhiteSpace(node.GroupId)) return "group:" + node.GroupId;
        if (!string.IsNullOrWhiteSpace(node.Kind)) return "kind:" + node.Kind;
        return "graph";
    }

    private static Dictionary<string, string> BuildClusterMembership(GraphScene scene) {
        var membership = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var cluster in scene.Clusters.OrderBy(cluster => cluster.Id, StringComparer.Ordinal)) {
            foreach (var nodeId in cluster.NodeIds) {
                if (!membership.ContainsKey(nodeId)) membership[nodeId] = cluster.Id;
            }
        }

        foreach (var node in scene.Nodes) {
            if (!string.IsNullOrWhiteSpace(node.ClusterId)) membership[node.Id] = node.ClusterId!;
        }

        return membership;
    }

    private static string? NodeClusterId(GraphSceneNode node, IReadOnlyDictionary<string, string> clusterMembership) =>
        !string.IsNullOrWhiteSpace(node.ClusterId)
            ? node.ClusterId
            : clusterMembership.TryGetValue(node.Id, out var clusterId) ? clusterId : null;

    private static int Degree(string nodeId, IReadOnlyDictionary<string, List<string>> adjacency) => adjacency.TryGetValue(nodeId, out var neighbors) ? neighbors.Count : 0;

    private static double GoldenAngle(int index) => index * 2.39996322972865332;

    private static double StableOffset(string value, double amplitude) => (StableUnit(value) - 0.5) * 2 * amplitude;

    private static double StableUnit(string value) {
        unchecked {
            var hash = 2166136261u;
            foreach (var ch in value) {
                hash ^= ch;
                hash *= 16777619u;
            }

            return (hash & 0x00ffffff) / 16777215.0;
        }
    }
}
