using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public const int MaximumWardleyNodes = 256;
    public const int MaximumWardleyLinks = 512;

    internal const double WardleyAccentOpacity = 155d / 255;
    internal const double WardleyPipelineOpacity = 145d / 255;
    internal const double WardleyStageLabelHeight = 16;

    public static void ValidateWardleyMap(WardleyMapBlock map) {
        if (map.Nodes.Count == 0) throw new InvalidOperationException("Wardley maps must contain at least one node.");
        if (map.Nodes.Count > MaximumWardleyNodes) throw new InvalidOperationException("Wardley maps must contain no more than " + MaximumWardleyNodes.ToString(CultureInfo.InvariantCulture) + " nodes.");
        if (map.Links.Count > MaximumWardleyLinks) throw new InvalidOperationException("Wardley maps must contain no more than " + MaximumWardleyLinks.ToString(CultureInfo.InvariantCulture) + " links.");

        if (map.StageBoundaries.Count != 0 && map.StageBoundaries.Count != (map.Stages.Count == 0 ? 4 : map.Stages.Count))
            throw new InvalidOperationException("Wardley stage boundaries must match the current stage count.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in map.Nodes) {
            if (node.Id.Trim().Length == 0) throw new InvalidOperationException("Wardley map nodes must define ids.");
            if (node.Label.Trim().Length == 0) throw new InvalidOperationException("Wardley map nodes must define labels.");
            ValidateWardleyCoordinate(node.Visibility, "visibility");
            ValidateWardleyCoordinate(node.Evolution, "evolution");
            ValidateWardleyOffset(node.LabelOffsetX); ValidateWardleyOffset(node.LabelOffsetY);
            if (!Enum.IsDefined(typeof(WardleyMapNodeKind), node.Kind)) throw new InvalidOperationException("Unknown Wardley node kind.");
            if (!string.IsNullOrWhiteSpace(node.Strategy) && node.Strategy != "build" && node.Strategy != "buy" && node.Strategy != "outsource" && node.Strategy != "market")
                throw new InvalidOperationException("Wardley sourcing strategy must be build, buy, outsource or market.");
            if (!ids.Add(node.Id)) throw new InvalidOperationException("Wardley map node ids must be unique: " + node.Id + ".");
        }

        var parentIds = new HashSet<string>(ids, StringComparer.Ordinal);
        foreach (var pipeline in map.Pipelines) {
            if (!parentIds.Contains(pipeline.ParentId)) throw new InvalidOperationException("Wardley map pipelines reference unknown parent node: " + pipeline.ParentId + ".");
            foreach (var child in pipeline.Components) {
                if (child.Id != null && !ids.Add(child.Id)) throw new InvalidOperationException("Wardley map endpoint ids must be unique: " + child.Id + ".");
                ValidateWardleyOffset(child.LabelOffsetX); ValidateWardleyOffset(child.LabelOffsetY);
            }
        }
        if (ids.Count > MaximumWardleyNodes) throw new InvalidOperationException("Wardley maps must contain no more than 256 identified endpoints.");

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
        var annotationInset = map.Annotations.Count == 0 ? 16 : Math.Max(16, WardleyAnnotationRadius(map.Options.ResolvedTheme) + 1);
        var stageLabelOffset = WardleyStageLabelOffset(map);
        var plot = new ChartRect(body.X + 56, body.Y + annotationInset, Math.Max(1, body.Width - 56 - annotationInset), Math.Max(1, body.Height - 36 - stageLabelOffset - annotationInset));
        var nodes = new List<WardleyNodePlacement>(map.Nodes.Count);
        var lookup = new Dictionary<string, WardleyNodePlacement>(StringComparer.Ordinal);
        for (var index = 0; index < map.Nodes.Count; index++) {
            var node = map.Nodes[index];
            var placement = new WardleyNodePlacement(node, index, ProjectWardleyX(plot, node.Evolution), ProjectWardleyY(plot, node.Visibility));
            nodes.Add(placement);
            lookup[node.Id] = placement;
        }

        foreach (var pipeline in map.Pipelines) {
            var parent = lookup[pipeline.ParentId];
            foreach (var child in pipeline.Components) if (child.Id != null) {
                // This is the existing declared pipeline child, projected at its parent's visibility.
                var node = new WardleyMapNode(child.Id, child.Label, parent.Node.Visibility, child.Evolution, WardleyMapNodeKind.PipelineComponent);
                lookup.Add(child.Id, new WardleyNodePlacement(node, parent.Index, ProjectWardleyX(plot, child.Evolution), parent.Y));
            }
        }
        return new WardleyMapLayout(body, plot, nodes, lookup);
    }

    public static double ProjectWardleyX(ChartRect plot, double evolution) => plot.X + Math.Max(0, Math.Min(1, evolution)) * plot.Width;

    public static double ProjectWardleyY(ChartRect plot, double visibility) => plot.Y + (1 - Math.Max(0, Math.Min(1, visibility))) * plot.Height;

    internal static double WardleyAnnotationRadius(ChartForgeX.Themes.ResolvedVisualTheme theme) => Math.Min(24, BodyFontSize(theme, 10));

    internal static double WardleyStageLabelOffset(WardleyMapBlock map) {
        var radius = map.Annotations.Count == 0 ? 0 : WardleyAnnotationRadius(map.Options.ResolvedTheme);
        // Reserve the complete fitted stage line below the circle and its stroke at
        // every radius, including the boundary of the ordinary plot inset.
        return Math.Max(18, radius + WardleyStageLabelHeight + 1);
    }

    /// <summary>Complete triangular head of an evolution overlay.</summary>
    internal static string WardleyEvolutionArrowPath(double x, double y, double direction = 1) => "M " + WardleyF(x) + " " + WardleyF(y) + (direction < 0 ? " l 7 -4 v 8 z" : " l -7 -4 v 8 z");

    /// <summary>Outlined accelerator/deaccelerator with its common 49-unit footprint.</summary>
    internal static string WardleyMarkerPath(double x, double y, WardleyMapMarkerKind kind) =>
        kind == WardleyMapMarkerKind.Accelerator
            ? "M " + WardleyF(x) + " " + WardleyF(y - 10) + " h 32 v -7 l 17 17 l -17 17 v -7 h -32 z"
            : "M " + WardleyF(x + 49) + " " + WardleyF(y - 10) + " h -32 v -7 l -17 17 l 17 17 v -7 h 32 z";

    private static string WardleyF(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void ValidateWardleyOffset(double? value) {
        if (value.HasValue && !IsFinite(value.Value)) throw new InvalidOperationException("Wardley label offsets must be finite.");
    }

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
