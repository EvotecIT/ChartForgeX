using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private sealed class ForceParticle {
        public ForceParticle(TopologyNode node, double x, double y, double preferredX, double preferredY) {
            Node = node;
            X = x;
            Y = y;
            PreferredX = preferredX;
            PreferredY = preferredY;
        }

        public TopologyNode Node { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public double PreferredX { get; }
        public double PreferredY { get; }
        public double Dx { get; set; }
        public double Dy { get; set; }
    }

    private sealed class ForceAnchor {
        public ForceAnchor(double x, double y, double width, double height, string strategy = "auto") {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Strategy = strategy;
        }

        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        public string Strategy { get; }
    }

    private static void ApplyForceDirected(TopologyChart chart) {
        if (chart.Nodes.Count == 0) return;

        var pad = Math.Max(24, chart.Viewport.Padding);
        var titleOffset = string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72;
        var legendOffset = LegendReservedHeight(chart.Legend, chart.Viewport);
        var left = pad;
        var right = Math.Max(left + 80, chart.Viewport.Width - pad);
        var top = pad + titleOffset;
        var bottom = Math.Max(top + 80, chart.Viewport.Height - pad - legendOffset);
        var centerX = (left + right) / 2;
        var centerY = (top + bottom) / 2;

        var groupAnchors = ForceGroupAnchors(chart, left, top, right, bottom);
        var groupHubIds = ForceGroupHubIds(chart);
        var groupParticleCounts = chart.Nodes
            .GroupBy(node => node.GroupId ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        ApplyForceGroupAnchorDiagnostics(chart, groupAnchors, groupParticleCounts);
        var groupParticleIndexes = ForceGroupParticleIndexes(chart.Nodes);
        var particles = chart.Nodes
            .Select((node, index) => CreateForceParticle(node, index, chart.Nodes.Count, groupAnchors, groupHubIds, groupParticleCounts, groupParticleIndexes, centerX, centerY, left, top, right, bottom))
            .ToList();
        var lookup = particles.ToDictionary(particle => particle.Node.Id, StringComparer.Ordinal);
        var springs = chart.Edges
            .Where(edge => lookup.ContainsKey(edge.SourceNodeId) && lookup.ContainsKey(edge.TargetNodeId))
            .Select(edge => (Edge: edge, Source: lookup[edge.SourceNodeId], Target: lookup[edge.TargetNodeId]))
            .ToList();

        var area = Math.Max(1, (right - left) * (bottom - top));
        var spacing = Math.Sqrt(area / Math.Max(1, particles.Count));
        var iterations = ForceIterationCount(particles.Count, springs.Count);
        var temperature = Math.Max(18, Math.Min(96, spacing * 0.9));

        for (var i = 0; i < iterations; i++) {
            foreach (var particle in particles) {
                particle.Dx = 0;
                particle.Dy = 0;
            }

            ApplyForceRepulsion(particles, spacing);
            ApplyForceSprings(springs, spacing);
            ApplyForceGroupGravity(particles, groupAnchors, groupParticleCounts, groupHubIds, centerX, centerY, Math.Min(right - left, bottom - top));
            ApplyForceCenterGravity(particles, centerX, centerY);
            MoveForceParticles(particles, groupAnchors, left, top, right, bottom, temperature);
            temperature *= 0.965;
        }

        foreach (var particle in particles) {
            particle.Node.X = particle.X - particle.Node.Width / 2;
            particle.Node.Y = particle.Y - particle.Node.Height / 2;
            particle.Node.Metadata["layout.force.x"] = particle.X.ToString("0.###", CultureInfo.InvariantCulture);
            particle.Node.Metadata["layout.force.y"] = particle.Y.ToString("0.###", CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(particle.Node.GroupId) &&
                groupHubIds.TryGetValue(particle.Node.GroupId!, out var hubId) &&
                string.Equals(hubId, particle.Node.Id, StringComparison.Ordinal)) {
                particle.Node.Metadata["layout.force.role"] = "hub";
            }
        }

        ApplyForceGroupBounds(chart, particles, groupAnchors, left, top, right, bottom);
        ApplyForceEdgeDefaults(chart);
    }

    private static ForceParticle CreateForceParticle(TopologyNode node, int index, int count, IReadOnlyDictionary<string, ForceAnchor> groupAnchors, IReadOnlyDictionary<string, string> groupHubIds, IReadOnlyDictionary<string, int> groupParticleCounts, IReadOnlyDictionary<string, int> groupParticleIndexes, double centerX, double centerY, double left, double top, double right, double bottom) {
        if (!IsUnset(node.X) || !IsUnset(node.Y)) return new ForceParticle(node, node.X + node.Width / 2, node.Y + node.Height / 2, node.X + node.Width / 2, node.Y + node.Height / 2);

        var groupId = node.GroupId ?? string.Empty;
        var anchor = !string.IsNullOrWhiteSpace(groupId) && groupAnchors.TryGetValue(groupId, out var groupAnchor)
            ? groupAnchor
            : new ForceAnchor(centerX, centerY, right - left, bottom - top);
        if (!string.IsNullOrWhiteSpace(node.GroupId) &&
            groupHubIds.TryGetValue(node.GroupId!, out var hubId) &&
            string.Equals(hubId, node.Id, StringComparison.Ordinal)) {
            return new ForceParticle(node, anchor.X, anchor.Y, anchor.X, anchor.Y);
        }

        var groupCount = groupParticleCounts.TryGetValue(groupId, out var localCount) ? localCount : count;
        if (groupCount >= 80 && groupParticleIndexes.TryGetValue(node.Id, out var groupIndex)) {
            var packed = ForcePackedSeed(node, groupIndex, groupCount, anchor, left, top, right, bottom);
            return new ForceParticle(node, packed.X, packed.Y, packed.X, packed.Y);
        }

        var seedA = StableUnit(node.Id + ":a");
        var seedB = StableUnit(node.Id + ":b");
        var angle = Math.PI * 2 * seedA + index * 2.399963229728653;
        var radius = Math.Sqrt(seedB) * Math.Max(28, Math.Min(right - left, bottom - top) * (count > 80 ? 0.2 : 0.14));
        var x = ClampForce(anchor.X + Math.Cos(angle) * radius, left + node.Width / 2, right - node.Width / 2);
        var y = ClampForce(anchor.Y + Math.Sin(angle) * radius, top + node.Height / 2, bottom - node.Height / 2);
        return new ForceParticle(node, x, y, x, y);
    }

    private static Dictionary<string, int> ForceGroupParticleIndexes(IEnumerable<TopologyNode> nodes) {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in nodes) {
            var groupId = node.GroupId ?? string.Empty;
            var index = counters.TryGetValue(groupId, out var current) ? current : 0;
            counters[groupId] = index + 1;
            result[node.Id] = index;
        }

        return result;
    }

    private static ChartForgeX.Primitives.ChartPoint ForcePackedSeed(TopologyNode node, int index, int count, ForceAnchor anchor, double left, double top, double right, double bottom) {
        var usableW = Math.Max(node.Width + 12, anchor.Width * 0.9);
        var usableH = Math.Max(node.Height + 12, anchor.Height * 0.82);
        var aspect = Math.Max(0.35, Math.Min(3.8, usableW / Math.Max(1, usableH)));
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count * aspect)));
        var minCellW = Math.Max(8, node.Width * 0.8);
        var minCellH = Math.Max(8, node.Height * 0.8);
        var maxColumnsByWidth = Math.Max(1, (int)Math.Floor(usableW / minCellW));
        var maxRowsByHeight = Math.Max(1, (int)Math.Floor(usableH / minCellH));
        var minColumnsForHeight = Math.Max(1, (int)Math.Ceiling(count / (double)maxRowsByHeight));
        columns = Math.Min(maxColumnsByWidth, Math.Max(columns, minColumnsForHeight));
        var rows = Math.Max(1, (int)Math.Ceiling(count / (double)columns));
        var cellW = usableW / columns;
        var cellH = usableH / rows;
        var row = index / columns;
        var col = index % columns;
        var jitterX = (StableUnit(node.Id + ":pack-x") - 0.5) * Math.Min(cellW * 0.42, 10);
        var jitterY = (StableUnit(node.Id + ":pack-y") - 0.5) * Math.Min(cellH * 0.42, 10);
        var x = anchor.X - usableW / 2 + (col + 0.5) * cellW + jitterX;
        var y = anchor.Y - usableH / 2 + (row + 0.5) * cellH + jitterY;
        return new ChartForgeX.Primitives.ChartPoint(
            ClampForce(x, left + node.Width / 2, right - node.Width / 2),
            ClampForce(y, top + node.Height / 2, bottom - node.Height / 2));
    }

    private static Dictionary<string, ForceAnchor> ForceGroupAnchors(TopologyChart chart, double left, double top, double right, double bottom) {
        var result = new Dictionary<string, ForceAnchor>(StringComparer.Ordinal);
        if (chart.Groups.Count == 0) return result;

        var orderedGroups = ForceOrderedGroups(chart);
        var nodeCounts = chart.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.GroupId))
            .GroupBy(node => node.GroupId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var explicitFallbackWidth = Math.Max(90, (right - left) / Math.Max(1, Math.Min(3, orderedGroups.Count)));
        var explicitFallbackHeight = Math.Max(72, (bottom - top) / Math.Max(1, Math.Min(2, orderedGroups.Count)));
        foreach (var group in chart.Groups) {
            if (HasExplicitGroupPlacement(group)) {
                var width = group.Width > 0 ? group.Width : explicitFallbackWidth;
                var height = group.Height > 0 ? group.Height : explicitFallbackHeight;
                result[group.Id] = new ForceAnchor(group.X + width / 2, group.Y + height / 2, width, height, "explicit");
            }
        }

        if (result.Count == 0 && orderedGroups.Count > 1 && orderedGroups.Count <= 4) {
            var totalWidth = Math.Max(1, right - left);
            var gap = orderedGroups.Count > 1 ? Math.Min(34d, Math.Max(4d, totalWidth * 0.08)) : 0d;
            var availableW = Math.Max(1, totalWidth - gap * (orderedGroups.Count - 1));
            var availableH = Math.Max(1, bottom - top);
            var weights = orderedGroups
                .Select(group => Math.Max(1.4, Math.Sqrt(nodeCounts.TryGetValue(group.Id, out var nodeCount) ? nodeCount : 1)))
                .ToList();
            var totalWeight = weights.Sum();
            var cursor = left;
            for (var i = 0; i < orderedGroups.Count; i++) {
                var width = availableW * weights[i] / totalWeight;
                var group = orderedGroups[i];
                result[group.Id] = new ForceAnchor(cursor + width / 2, top + availableH / 2, width, availableH, "weighted-row");
                cursor += width + gap;
            }

            return result;
        }

        var columns = Math.Min(4, Math.Max(1, (int)Math.Ceiling(Math.Sqrt(orderedGroups.Count * 1.6))));
        if (orderedGroups.Count <= 4) columns = orderedGroups.Count;
        var rows = Math.Max(1, (int)Math.Ceiling(orderedGroups.Count / (double)columns));
        var cellW = (right - left) / columns;
        var cellH = (bottom - top) / rows;
        for (var i = 0; i < orderedGroups.Count; i++) {
            var group = orderedGroups[i];
            if (result.ContainsKey(group.Id)) continue;
            var row = i / columns;
            var col = i % columns;
            var rowCount = Math.Min(columns, orderedGroups.Count - row * columns);
            var rowLeft = left + ((columns - rowCount) * cellW) / 2;
            result[group.Id] = new ForceAnchor(rowLeft + col * cellW + cellW / 2, top + row * cellH + cellH / 2, cellW, cellH, "grid");
        }

        return result;
    }

    private static void ApplyForceGroupAnchorDiagnostics(TopologyChart chart, IReadOnlyDictionary<string, ForceAnchor> anchors, IReadOnlyDictionary<string, int> groupParticleCounts) {
        foreach (var group in chart.Groups) {
            if (!anchors.TryGetValue(group.Id, out var anchor)) continue;
            group.Metadata["layout.force.anchor.strategy"] = anchor.Strategy;
            group.Metadata["layout.force.anchor.x"] = anchor.X.ToString("0.###", CultureInfo.InvariantCulture);
            group.Metadata["layout.force.anchor.y"] = anchor.Y.ToString("0.###", CultureInfo.InvariantCulture);
            group.Metadata["layout.force.anchor.width"] = anchor.Width.ToString("0.###", CultureInfo.InvariantCulture);
            group.Metadata["layout.force.anchor.height"] = anchor.Height.ToString("0.###", CultureInfo.InvariantCulture);
            group.Metadata["layout.force.anchor.node-count"] = (groupParticleCounts.TryGetValue(group.Id, out var count) ? count : 0).ToString(CultureInfo.InvariantCulture);
        }
    }

    private static List<TopologyGroup> ForceOrderedGroups(TopologyChart chart) {
        if (chart.Groups.Count <= 2) return chart.Groups.ToList();
        var nodeCounts = chart.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.GroupId))
            .GroupBy(node => node.GroupId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var ordered = chart.Groups
            .OrderByDescending(group => nodeCounts.TryGetValue(group.Id, out var count) ? count : 0)
            .ThenBy(group => group.Id, StringComparer.Ordinal)
            .ToList();

        if (ordered.Count == 3) return new List<TopologyGroup> { ordered[1], ordered[0], ordered[2] };
        if (ordered.Count == 4) return new List<TopologyGroup> { ordered[2], ordered[0], ordered[1], ordered[3] };
        return ordered;
    }

    private static Dictionary<string, string> ForceGroupHubIds(TopologyChart chart) {
        var degree = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            degree[edge.SourceNodeId] = degree.TryGetValue(edge.SourceNodeId, out var sourceDegree) ? sourceDegree + 1 : 1;
            degree[edge.TargetNodeId] = degree.TryGetValue(edge.TargetNodeId, out var targetDegree) ? targetDegree + 1 : 1;
        }

        return chart.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.GroupId))
            .GroupBy(node => node.GroupId!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(node => degree.TryGetValue(node.Id, out var value) ? value : 0)
                    .ThenByDescending(ForceHubKindPriority)
                    .ThenBy(node => node.Id, StringComparer.Ordinal)
                    .First().Id,
                StringComparer.Ordinal);
    }

    private static int ForceHubKindPriority(TopologyNode node) {
        return node.Kind switch {
            TopologyNodeKind.Hub => 5,
            TopologyNodeKind.Namespace => 4,
            TopologyNodeKind.Team => 3,
            TopologyNodeKind.Server => 2,
            TopologyNodeKind.Service => 2,
            _ => 0
        };
    }

    private static int ForceIterationCount(int nodeCount, int edgeCount) {
        if (nodeCount <= 48) return 220;
        if (nodeCount <= 120) return 170;
        if (nodeCount <= 320) return 120;
        if (nodeCount <= 900) return edgeCount > nodeCount ? 90 : 72;
        return 48;
    }

    private static void ApplyForceRepulsion(IReadOnlyList<ForceParticle> particles, double spacing) {
        if (particles.Count <= 650) {
            for (var i = 0; i < particles.Count; i++) {
                for (var j = i + 1; j < particles.Count; j++) {
                    RepelForcePair(particles[i], particles[j], spacing);
                }
            }

            return;
        }

        const int window = 42;
        var cellSize = Math.Max(42, spacing * 1.8);
        var cells = new Dictionary<long, List<int>>();
        for (var i = 0; i < particles.Count; i++) {
            var key = ForceCellKey(particles[i], cellSize);
            if (!cells.TryGetValue(key, out var bucket)) {
                bucket = new List<int>();
                cells[key] = bucket;
            }

            bucket.Add(i);
        }

        foreach (var cell in cells) {
            var cellX = (int)(cell.Key >> 32);
            var cellY = (int)cell.Key;
            foreach (var i in cell.Value) {
                for (var dx = -1; dx <= 1; dx++) {
                    for (var dy = -1; dy <= 1; dy++) {
                        var neighborKey = ForceCellKey(cellX + dx, cellY + dy);
                        if (!cells.TryGetValue(neighborKey, out var neighbor)) continue;
                        foreach (var j in neighbor) {
                            if (j <= i) continue;
                            RepelForcePair(particles[i], particles[j], spacing);
                        }
                    }
                }
            }
        }

        for (var i = 0; i < particles.Count; i++) {
            for (var offset = 7; offset <= window && offset < particles.Count; offset += 7) {
                var j = (i + offset) % particles.Count;
                if (j <= i) continue;
                var a = particles[i];
                var b = particles[j];
                var dx = b.X - a.X;
                var dy = b.Y - a.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance < 0.001) continue;
                var force = spacing * spacing / distance * 0.025;
                a.Dx -= dx / distance * force;
                a.Dy -= dy / distance * force;
                b.Dx += dx / distance * force;
                b.Dy += dy / distance * force;
            }
        }
    }

    private static long ForceCellKey(ForceParticle particle, double cellSize) {
        return ForceCellKey((int)Math.Floor(particle.X / cellSize), (int)Math.Floor(particle.Y / cellSize));
    }

    private static long ForceCellKey(int x, int y) {
        return ((long)x << 32) ^ (uint)y;
    }

    private static void RepelForcePair(ForceParticle a, ForceParticle b, double spacing) {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < 0.001) {
            var seed = StableUnit(a.Node.Id + "|" + b.Node.Id);
            dx = Math.Cos(seed * Math.PI * 2);
            dy = Math.Sin(seed * Math.PI * 2);
            distance = 1;
        }

        var padding = Math.Max(8, Math.Min(42, spacing * 0.35));
        var minDistance = (Math.Max(a.Node.Width, a.Node.Height) + Math.Max(b.Node.Width, b.Node.Height)) / 2 + padding;
        var force = spacing * spacing / distance * 0.18;
        if (distance < minDistance) force += (minDistance - distance) * 1.15;
        var fx = dx / distance * force;
        var fy = dy / distance * force;
        a.Dx -= fx;
        a.Dy -= fy;
        b.Dx += fx;
        b.Dy += fy;
    }

    private static void ApplyForceSprings(IEnumerable<(TopologyEdge Edge, ForceParticle Source, ForceParticle Target)> springs, double spacing) {
        foreach (var spring in springs) {
            var dx = spring.Target.X - spring.Source.X;
            var dy = spring.Target.Y - spring.Source.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < 0.001) continue;

            var desired = spring.Edge.Kind switch {
                TopologyEdgeKind.Membership => 104,
                TopologyEdgeKind.Dependency => 128,
                _ => Math.Max(118, spacing * 1.15)
            };
            var force = (distance - desired) * 0.095;
            var fx = dx / distance * force;
            var fy = dy / distance * force;
            spring.Source.Dx += fx;
            spring.Source.Dy += fy;
            spring.Target.Dx -= fx;
            spring.Target.Dy -= fy;
        }
    }

    private static void ApplyForceGroupGravity(IEnumerable<ForceParticle> particles, IReadOnlyDictionary<string, ForceAnchor> groupAnchors, IReadOnlyDictionary<string, int> groupParticleCounts, IReadOnlyDictionary<string, string> groupHubIds, double centerX, double centerY, double shortSide) {
        foreach (var particle in particles) {
            var groupId = particle.Node.GroupId ?? string.Empty;
            var anchor = !string.IsNullOrWhiteSpace(groupId) && groupAnchors.TryGetValue(groupId, out var groupAnchor)
                ? groupAnchor
                : new ForceAnchor(centerX, centerY, shortSide, shortSide);
            var targetX = anchor.X;
            var targetY = anchor.Y;
            var hub = groupHubIds.TryGetValue(groupId, out var hubId) && string.Equals(hubId, particle.Node.Id, StringComparison.Ordinal);
            var groupCount = groupParticleCounts.TryGetValue(groupId, out var count) ? count : 1;
            if (!hub && groupCount >= 80) {
                targetX = particle.PreferredX;
                targetY = particle.PreferredY;
            } else if (!hub && groupCount > 1) {
                var angle = StableUnit(particle.Node.Id + ":group-angle") * Math.PI * 2;
                var radius = Math.Min(shortSide * 0.12, Math.Max(58, Math.Sqrt(groupCount) * 22));
                radius *= 0.55 + StableUnit(particle.Node.Id + ":group-radius") * 0.55;
                targetX += Math.Cos(angle) * radius;
                targetY += Math.Sin(angle) * radius;
            }

            var strength = hub ? 0.22 : groupCount >= 80 ? 0.12 : 0.075;
            particle.Dx += (targetX - particle.X) * strength;
            particle.Dy += (targetY - particle.Y) * strength;
        }
    }

    private static void ApplyForceCenterGravity(IEnumerable<ForceParticle> particles, double centerX, double centerY) {
        foreach (var particle in particles) {
            particle.Dx += (centerX - particle.X) * 0.006;
            particle.Dy += (centerY - particle.Y) * 0.006;
        }
    }

    private static void MoveForceParticles(IEnumerable<ForceParticle> particles, IReadOnlyDictionary<string, ForceAnchor> groupAnchors, double left, double top, double right, double bottom, double temperature) {
        foreach (var particle in particles) {
            var length = Math.Sqrt(particle.Dx * particle.Dx + particle.Dy * particle.Dy);
            if (length > 0.001) {
                var step = Math.Min(length, temperature);
                particle.X += particle.Dx / length * step;
                particle.Y += particle.Dy / length * step;
            }

            var minX = left + particle.Node.Width / 2;
            var maxX = right - particle.Node.Width / 2;
            var minY = top + particle.Node.Height / 2;
            var maxY = bottom - particle.Node.Height / 2;
            if (!string.IsNullOrWhiteSpace(particle.Node.GroupId) &&
                groupAnchors.TryGetValue(particle.Node.GroupId!, out var anchor)) {
                var anchorMinX = anchor.X - anchor.Width * 0.48 + particle.Node.Width / 2;
                var anchorMaxX = anchor.X + anchor.Width * 0.48 - particle.Node.Width / 2;
                var anchorMinY = anchor.Y - anchor.Height * 0.46 + particle.Node.Height / 2;
                var anchorMaxY = anchor.Y + anchor.Height * 0.46 - particle.Node.Height / 2;
                ApplyForceAnchorClamp(ref minX, ref maxX, anchorMinX, anchorMaxX, anchor.X);
                ApplyForceAnchorClamp(ref minY, ref maxY, anchorMinY, anchorMaxY, anchor.Y);
            }

            particle.X = ClampForce(particle.X, minX, maxX);
            particle.Y = ClampForce(particle.Y, minY, maxY);
        }
    }

    private static void ApplyForceGroupBounds(TopologyChart chart, IReadOnlyList<ForceParticle> particles, IReadOnlyDictionary<string, ForceAnchor> groupAnchors, double left, double top, double right, double bottom) {
        foreach (var group in chart.Groups) {
            var groupParticles = particles
                .Where(particle => string.Equals(particle.Node.GroupId, group.Id, StringComparison.Ordinal))
                .ToList();
            if (groupParticles.Count == 0) continue;

            var minX = groupParticles.Min(particle => particle.X - particle.Node.Width / 2);
            var minY = groupParticles.Min(particle => particle.Y - particle.Node.Height / 2);
            var maxX = groupParticles.Max(particle => particle.X + particle.Node.Width / 2);
            var maxY = groupParticles.Max(particle => particle.Y + particle.Node.Height / 2);
            var groupLeft = left;
            var groupTop = top;
            var groupRight = right;
            var groupBottom = bottom;
            if (groupAnchors.TryGetValue(group.Id, out var anchor)) {
                groupLeft = Math.Max(groupLeft, anchor.X - anchor.Width / 2);
                groupTop = Math.Max(groupTop, anchor.Y - anchor.Height / 2);
                groupRight = Math.Min(groupRight, anchor.X + anchor.Width / 2);
                groupBottom = Math.Min(groupBottom, anchor.Y + anchor.Height / 2);
            }

            var availableWidth = Math.Max(1, groupRight - groupLeft);
            var availableHeight = Math.Max(1, groupBottom - groupTop);
            group.Width = Math.Min(availableWidth, Math.Max(Math.Min(150, availableWidth), maxX - minX + 68));
            group.Height = Math.Min(availableHeight, Math.Max(Math.Min(118, availableHeight), maxY - minY + 92));
            group.X = ClampForce(minX - 34, groupLeft, Math.Max(groupLeft, groupRight - group.Width));
            group.Y = ClampForce(minY - 58, groupTop, Math.Max(groupTop, groupBottom - group.Height));
            group.Metadata["layout.force.nodeCount"] = groupParticles.Count.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static void ApplyForceEdgeDefaults(TopologyChart chart) {
        foreach (var edge in chart.Edges) {
            if (edge.Waypoints.Count == 0 && edge.Routing == TopologyEdgeRouting.Orthogonal) edge.Routing = TopologyEdgeRouting.Straight;
            edge.Metadata["layout.force"] = "true";
        }
    }

    private static double StableUnit(string value) {
        unchecked {
            var hash = 2166136261u;
            foreach (var ch in value) {
                hash ^= ch;
                hash *= 16777619;
            }

            return (hash & 0xFFFFFF) / (double)0x1000000;
        }
    }

    private static double ClampForce(double value, double min, double max) {
        if (max < min) return min;
        return value < min ? min : value > max ? max : value;
    }

    private static void ApplyForceAnchorClamp(ref double min, ref double max, double anchorMin, double anchorMax, double anchorCenter) {
        if (anchorMax >= anchorMin) {
            min = Math.Max(min, anchorMin);
            max = Math.Min(max, anchorMax);
            return;
        }

        var center = ClampForce(anchorCenter, min, max);
        min = center;
        max = center;
    }
}
