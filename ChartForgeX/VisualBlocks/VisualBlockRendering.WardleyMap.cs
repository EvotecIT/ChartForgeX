using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public const int MaximumWardleyNodes = 256;
    public const int MaximumWardleyLinks = 512;

    public static void ValidateWardleyMap(WardleyMapBlock map) {
        if (map.Nodes.Count == 0) throw new InvalidOperationException("Wardley maps must contain at least one node.");
        if (map.Nodes.Count > MaximumWardleyNodes) throw new InvalidOperationException("Wardley maps must contain no more than " + MaximumWardleyNodes.ToString(CultureInfo.InvariantCulture) + " nodes.");
        if (map.Links.Count > MaximumWardleyLinks) throw new InvalidOperationException("Wardley maps must contain no more than " + MaximumWardleyLinks.ToString(CultureInfo.InvariantCulture) + " links.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in map.Nodes) {
            if (node.Id.Trim().Length == 0) throw new InvalidOperationException("Wardley map nodes must define ids.");
            if (node.Label.Trim().Length == 0) throw new InvalidOperationException("Wardley map nodes must define labels.");
            ValidateWardleyCoordinate(node.Visibility, "visibility");
            ValidateWardleyCoordinate(node.Evolution, "evolution");
            if (!ids.Add(node.Id)) throw new InvalidOperationException("Wardley map node ids must be unique: " + node.Id + ".");
        }

        foreach (var link in map.Links) {
            if (!ids.Contains(link.FromId)) throw new InvalidOperationException("Wardley map links reference unknown source node: " + link.FromId + ".");
            if (!ids.Contains(link.ToId)) throw new InvalidOperationException("Wardley map links reference unknown target node: " + link.ToId + ".");
        }

        foreach (var evolution in map.Evolutions) {
            if (!ids.Contains(evolution.NodeId)) throw new InvalidOperationException("Wardley map evolution trends reference unknown node: " + evolution.NodeId + ".");
            ValidateWardleyCoordinate(evolution.TargetEvolution, "evolution target");
        }

        foreach (var note in map.Notes) {
            if (note.Text.Trim().Length == 0) throw new InvalidOperationException("Wardley map notes must define text.");
            ValidateWardleyCoordinate(note.Visibility, "note visibility");
            ValidateWardleyCoordinate(note.Evolution, "note evolution");
        }

        foreach (var annotation in map.Annotations) {
            ValidateWardleyCoordinate(annotation.Visibility, "annotation visibility");
            ValidateWardleyCoordinate(annotation.Evolution, "annotation evolution");
        }

        foreach (var marker in map.Markers) {
            if (marker.Label.Trim().Length == 0) throw new InvalidOperationException("Wardley map markers must define labels.");
            ValidateWardleyCoordinate(marker.Visibility, "marker visibility");
            ValidateWardleyCoordinate(marker.Evolution, "marker evolution");
        }

        foreach (var pipeline in map.Pipelines) {
            if (!ids.Contains(pipeline.ParentId)) throw new InvalidOperationException("Wardley map pipelines reference unknown parent node: " + pipeline.ParentId + ".");
            foreach (var component in pipeline.Components) {
                if (component.Label.Trim().Length == 0) throw new InvalidOperationException("Wardley map pipeline components must define labels.");
                ValidateWardleyCoordinate(component.Evolution, "pipeline component evolution");
            }
        }
    }

    public static WardleyMapLayout BuildWardleyMapLayout(WardleyMapBlock map, ChartRect content, double y, double bottomPadding, double totalHeight) {
        var body = new ChartRect(content.X, y, content.Width, Math.Max(1, totalHeight - bottomPadding - y));
        var plot = new ChartRect(body.X + 56, body.Y + 16, Math.Max(1, body.Width - 72), Math.Max(1, body.Height - 70));
        var nodes = new List<WardleyNodePlacement>(map.Nodes.Count);
        var lookup = new Dictionary<string, WardleyNodePlacement>(StringComparer.Ordinal);
        for (var index = 0; index < map.Nodes.Count; index++) {
            var node = map.Nodes[index];
            var placement = new WardleyNodePlacement(node, index, ProjectWardleyX(plot, node.Evolution), ProjectWardleyY(plot, node.Visibility));
            nodes.Add(placement);
            lookup[node.Id] = placement;
        }

        return new WardleyMapLayout(body, plot, nodes, lookup);
    }

    public static double ProjectWardleyX(ChartRect plot, double evolution) => plot.X + Math.Max(0, Math.Min(1, evolution)) * plot.Width;

    public static double ProjectWardleyY(ChartRect plot, double visibility) => plot.Y + (1 - Math.Max(0, Math.Min(1, visibility))) * plot.Height;

    private static void ValidateWardleyCoordinate(double value, string label) {
        if (!IsFinite(value) || value < 0 || value > 1) throw new InvalidOperationException("Wardley map " + label + " values must be finite numbers from 0 to 1.");
    }

    public readonly struct WardleyMapLayout {
        public WardleyMapLayout(ChartRect body, ChartRect plot, IReadOnlyList<WardleyNodePlacement> nodes, IReadOnlyDictionary<string, WardleyNodePlacement> nodeLookup) {
            Body = body;
            Plot = plot;
            Nodes = nodes;
            NodeLookup = nodeLookup;
        }

        public ChartRect Body { get; }
        public ChartRect Plot { get; }
        public IReadOnlyList<WardleyNodePlacement> Nodes { get; }
        public IReadOnlyDictionary<string, WardleyNodePlacement> NodeLookup { get; }
    }

    public readonly struct WardleyNodePlacement {
        public WardleyNodePlacement(WardleyMapNode node, int index, double x, double y) {
            Node = node;
            Index = index;
            X = x;
            Y = y;
        }

        public WardleyMapNode Node { get; }
        public int Index { get; }
        public double X { get; }
        public double Y { get; }
    }
}
