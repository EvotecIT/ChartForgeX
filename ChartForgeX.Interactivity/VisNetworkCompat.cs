using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Provides a typed compatibility envelope for common vis-network data and option concepts.
/// The converter keeps <see cref="GraphScene"/> as the reusable ChartForgeX graph contract.
/// </summary>
public sealed class VisNetworkGraph {
    /// <summary>Gets the vis-style node collection.</summary>
    public List<VisNetworkNode> Nodes { get; } = new();

    /// <summary>Gets the vis-style edge collection.</summary>
    public List<VisNetworkEdge> Edges { get; } = new();

    /// <summary>Gets group defaults keyed by group id.</summary>
    public Dictionary<string, VisNetworkGroupOptions> Groups { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets vis-style graph options.</summary>
    public VisNetworkOptions Options { get; } = new();

    /// <summary>
    /// Creates a new compatibility graph.
    /// </summary>
    /// <returns>A graph that can be converted to <see cref="GraphScene"/>.</returns>
    public static VisNetworkGraph Create() => new();

    /// <summary>
    /// Adds a node to the compatibility graph.
    /// </summary>
    public VisNetworkGraph AddNode(string id, string label, Action<VisNetworkNode>? configure = null) {
        var node = new VisNetworkNode { Id = id, Label = label };
        configure?.Invoke(node);
        Nodes.Add(node);
        return this;
    }

    /// <summary>
    /// Adds an edge to the compatibility graph.
    /// </summary>
    public VisNetworkGraph AddEdge(string id, string from, string to, string? label = null, Action<VisNetworkEdge>? configure = null) {
        var edge = new VisNetworkEdge { Id = id, From = from, To = to, Label = label };
        configure?.Invoke(edge);
        Edges.Add(edge);
        return this;
    }

    /// <summary>
    /// Converts this compatibility graph into the reusable ChartForgeX graph scene model.
    /// </summary>
    public GraphScene ToGraphScene(string id, string title) {
        var scene = GraphScene.Create(id, title);
        ApplyOptions(scene);
        var ids = VisNetworkGraphIdMap.Create(this);
        ApplyGroups(scene, ids);
        foreach (var node in Nodes) scene.Nodes.Add(node.ToGraphSceneNode(Groups, ids));
        for (var index = 0; index < Edges.Count; index++) scene.Edges.Add(Edges[index].ToGraphSceneEdge(index, ids));
        scene.Validate();
        return scene;
    }

    private void ApplyOptions(GraphScene scene) {
        scene.Options.Features = GraphSceneFeatures.Explorer | GraphSceneFeatures.Export;
        if (Options.Interaction.DragNodes) scene.Options.Enable(GraphSceneFeatures.DragNodes);
        else scene.Options.Disable(GraphSceneFeatures.DragNodes);
        if (Options.Interaction.NavigationButtons) scene.Options.Enable(GraphSceneFeatures.Viewport);
        else scene.Options.Disable(GraphSceneFeatures.Viewport);
        if (Options.Interaction.Hover) scene.Metadata["vis.interaction.hover"] = "true";
        if (Options.Manipulation.Enabled) {
            scene.Options.Enable(GraphSceneFeatures.Manipulation);
            scene.Options.Manipulation.CanAddNodes = Options.Manipulation.AddNode;
            scene.Options.Manipulation.CanEditNodes = Options.Manipulation.EditNode;
            scene.Options.Manipulation.CanDeleteNodes = Options.Manipulation.DeleteNode;
            scene.Options.Manipulation.CanAddEdges = Options.Manipulation.AddEdge;
            scene.Options.Manipulation.CanEditEdges = Options.Manipulation.EditEdge;
            scene.Options.Manipulation.CanDeleteEdges = Options.Manipulation.DeleteEdge;
            scene.Options.Manipulation.CanPersistPositions = Options.Manipulation.PersistPositions;
        }

        scene.Options.Physics.Solver = Options.Physics.Enabled ? Options.Physics.Solver : GraphPhysicsSolver.None;
        scene.Options.Physics.StabilizationIterations = Options.Physics.StabilizationIterations;
        scene.Options.Physics.LinkDistance = Options.Physics.LinkDistance;
        scene.Options.Physics.Repulsion = Options.Physics.Repulsion;
        scene.Options.Physics.Damping = Options.Physics.Damping;
        scene.Options.Physics.AdaptiveTimestep = Options.Physics.AdaptiveTimestep;
        if (Options.Layout.Hierarchical.Enabled) {
            scene.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
            scene.Options.Layout.Direction = Options.Layout.Hierarchical.Direction;
            scene.Options.Layout.LevelSeparation = Options.Layout.Hierarchical.LevelSeparation;
            scene.Options.Layout.NodeSpacing = Options.Layout.Hierarchical.NodeSpacing;
            scene.Options.Layout.ComponentSpacing = Options.Layout.Hierarchical.ComponentSpacing;
            scene.Options.Layout.InferLevelsFromEdges = Options.Layout.Hierarchical.InferLevelsFromEdges;
        }

        if (Options.Physics.Enabled && scene.Options.Physics.Solver != GraphPhysicsSolver.None && scene.Options.Physics.Solver != GraphPhysicsSolver.StaticPrepared) {
            scene.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization);
        }
    }

    private void ApplyGroups(GraphScene scene, VisNetworkGraphIdMap ids) {
        var groupIds = Nodes
            .Select(node => node.Group)
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Select(group => group!)
            .Concat(Groups.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(group => group, StringComparer.Ordinal);
        foreach (var groupId in groupIds) {
            Groups.TryGetValue(groupId, out var group);
            var members = Nodes.Where(node => string.Equals(node.Group, groupId, StringComparison.Ordinal)).Select(node => ids.NodeId(node.Id)).ToArray();
            if (members.Length == 0) continue;
            scene.AddCluster(ids.GroupId(groupId), group?.Label ?? groupId, members, cluster => {
                cluster.Kind = "group";
                cluster.Metadata["vis.group"] = groupId;
            });
        }
    }

    internal sealed class VisNetworkGraphIdMap {
        private readonly Dictionary<string, string> _nodeIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _groupIds = new(StringComparer.Ordinal);
        private readonly List<string> _edgeIds = new();

        private VisNetworkGraphIdMap() {
        }

        public static VisNetworkGraphIdMap Create(VisNetworkGraph graph) {
            var map = new VisNetworkGraphIdMap();
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in graph.Nodes) map._nodeIds[node.Id] = UniqueToken(node.Id, "node", nodeIds);
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < graph.Edges.Count; index++) {
                var rawId = string.IsNullOrWhiteSpace(graph.Edges[index].Id) ? "edge-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) : graph.Edges[index].Id;
                map._edgeIds.Add(UniqueToken(rawId, "edge", edgeIds));
            }

            var groupIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var groupId in graph.Nodes.Select(node => node.Group).Where(group => !string.IsNullOrWhiteSpace(group)).Select(group => group!).Concat(graph.Groups.Keys).Distinct(StringComparer.Ordinal)) {
                map._groupIds[groupId] = UniqueToken(groupId, "group", groupIds);
            }

            return map;
        }

        public string NodeId(string id) => _nodeIds[id];

        public string GroupId(string id) => _groupIds[id];

        public string EdgeId(int index) => _edgeIds[index];

        private static string UniqueToken(string value, string prefix, ISet<string> used) {
            var token = ToToken(value, prefix);
            var candidate = token;
            var suffix = 2;
            while (!used.Add(candidate)) {
                candidate = token + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
                suffix++;
            }

            return candidate;
        }

        private static string ToToken(string value, string prefix) {
            var trimmed = value.Trim();
            if (IsToken(trimmed)) return trimmed;
            var builder = new System.Text.StringBuilder(trimmed.Length);
            foreach (var ch in trimmed) builder.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.' ? ch : '-');
            var token = builder.ToString().Trim('-', '_', '.');
            return token.Length == 0 ? prefix : token;
        }

        private static bool IsToken(string value) {
            if (value.Length == 0) return false;
            foreach (var ch in value) {
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') continue;
                return false;
            }

            return true;
        }
    }
}

/// <summary>
/// Describes a vis-network-style node before conversion to <see cref="GraphSceneNode"/>.
/// </summary>
public sealed class VisNetworkNode {
    /// <summary>Gets or sets the node id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the node label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional group id.</summary>
    public string? Group { get; set; }

    /// <summary>Gets or sets the optional hierarchy level.</summary>
    public int? Level { get; set; }

    /// <summary>Gets or sets the node shape.</summary>
    public VisNetworkNodeShape? Shape { get; set; }

    /// <summary>Gets or sets the node size.</summary>
    public double? Size { get; set; }

    /// <summary>Gets or sets optional image URI for image nodes.</summary>
    public string? Image { get; set; }

    /// <summary>Gets or sets optional icon text for icon-like nodes.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets optional x-coordinate.</summary>
    public double? X { get; set; }

    /// <summary>Gets or sets optional y-coordinate.</summary>
    public double? Y { get; set; }

    /// <summary>Gets or sets whether the node is fixed in prepared or runtime layouts.</summary>
    public bool Fixed { get; set; }

    /// <summary>Gets node style hints.</summary>
    public GraphNodeStyle Style { get; } = new();

    internal GraphSceneNode ToGraphSceneNode(IReadOnlyDictionary<string, VisNetworkGroupOptions> groups, VisNetworkGraph.VisNetworkGraphIdMap ids) {
        groups.TryGetValue(Group ?? string.Empty, out var group);
        var groupId = string.IsNullOrWhiteSpace(Group) ? null : ids.GroupId(Group!);
        var node = new GraphSceneNode {
            Id = ids.NodeId(Id),
            Label = Label,
            GroupId = groupId,
            ClusterId = groupId,
            Kind = group?.Kind,
            Level = Level,
            Size = Size ?? group?.Size ?? 8,
            Shape = MapShape(Shape ?? group?.Shape ?? VisNetworkNodeShape.Dot),
            ImageUrl = Image ?? group?.Image,
            IconText = Icon ?? group?.Icon,
            Fixed = Fixed
        };
        if (X.HasValue && Y.HasValue) {
            node.X = X.Value;
            node.Y = Y.Value;
        }
        ApplyStyle(node.Style, group?.Style);
        node.Metadata["vis.node"] = "true";
        if (!string.Equals(node.Id, Id, StringComparison.Ordinal)) node.Metadata["vis.id"] = Id;
        if (!string.IsNullOrWhiteSpace(Group)) node.Metadata["vis.group"] = Group!;
        if (Level.HasValue) node.Metadata["vis.level"] = Level.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return node;
    }

    private void ApplyStyle(GraphNodeStyle target, GraphNodeStyle? groupStyle) {
        target.BackgroundColor = Style.BackgroundColor ?? groupStyle?.BackgroundColor;
        target.BorderColor = Style.BorderColor ?? groupStyle?.BorderColor;
        target.LabelColor = Style.LabelColor ?? groupStyle?.LabelColor;
        target.LabelBackgroundColor = Style.LabelBackgroundColor ?? groupStyle?.LabelBackgroundColor;
        target.Shadow = Style.Shadow || groupStyle?.Shadow == true;
    }

    private static GraphNodeShape MapShape(VisNetworkNodeShape shape) => shape switch {
        VisNetworkNodeShape.Box => GraphNodeShape.Box,
        VisNetworkNodeShape.Circle => GraphNodeShape.Circle,
        VisNetworkNodeShape.CircularImage => GraphNodeShape.Image,
        VisNetworkNodeShape.Database => GraphNodeShape.Database,
        VisNetworkNodeShape.Diamond => GraphNodeShape.Diamond,
        VisNetworkNodeShape.Ellipse => GraphNodeShape.Ellipse,
        VisNetworkNodeShape.Icon => GraphNodeShape.Circle,
        VisNetworkNodeShape.Image => GraphNodeShape.Image,
        VisNetworkNodeShape.Square => GraphNodeShape.Square,
        VisNetworkNodeShape.Star => GraphNodeShape.Star,
        VisNetworkNodeShape.Text => GraphNodeShape.Text,
        VisNetworkNodeShape.Triangle => GraphNodeShape.Triangle,
        VisNetworkNodeShape.TriangleDown => GraphNodeShape.TriangleDown,
        _ => GraphNodeShape.Circle
    };
}

/// <summary>
/// Describes a vis-network-style edge before conversion to <see cref="GraphSceneEdge"/>.
/// </summary>
public sealed class VisNetworkEdge {
    /// <summary>Gets or sets the edge id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the source node id, matching vis-network's from field.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Gets or sets the target node id, matching vis-network's to field.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional edge label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the optional edge kind.</summary>
    public string? Kind { get; set; }

    /// <summary>Gets or sets whether the edge should show an arrow at the target side.</summary>
    public bool ArrowsTo { get; set; }

    /// <summary>Gets or sets whether the edge should show an arrow at the source side.</summary>
    public bool ArrowsFrom { get; set; }

    /// <summary>Gets or sets whether the edge should show a middle arrow marker where adapters support it.</summary>
    public bool ArrowsMiddle { get; set; }

    /// <summary>Gets or sets whether the edge is dashed.</summary>
    public bool Dashes { get; set; }

    /// <summary>Gets or sets optional smooth edge behavior.</summary>
    public VisNetworkSmoothType Smooth { get; set; } = VisNetworkSmoothType.Dynamic;

    /// <summary>Gets or sets optional spring length.</summary>
    public double? Length { get; set; }

    /// <summary>Gets edge style hints.</summary>
    public GraphEdgeStyle Style { get; } = new();

    internal GraphSceneEdge ToGraphSceneEdge(int index, VisNetworkGraph.VisNetworkGraphIdMap ids) {
        var edge = new GraphSceneEdge {
            Id = ids.EdgeId(index),
            SourceNodeId = ids.NodeId(From),
            TargetNodeId = ids.NodeId(To),
            Label = Label,
            Kind = Kind,
            Directed = ArrowsTo || ArrowsFrom,
            SourceArrow = ArrowsFrom,
            TargetArrow = ArrowsTo,
            Dashed = Dashes || !string.IsNullOrWhiteSpace(Style.DashPattern),
            Length = Length ?? 0,
            Shape = MapShape(Smooth)
        };
        edge.Style.Color = Style.Color;
        edge.Style.LabelColor = Style.LabelColor;
        edge.Style.Width = Style.Width;
        edge.Style.DashPattern = Style.DashPattern;
        edge.Style.Physics = Style.Physics;
        edge.Style.Hidden = Style.Hidden;
        edge.Metadata["vis.edge"] = "true";
        if (!string.Equals(edge.Id, Id, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(Id)) edge.Metadata["vis.id"] = Id;
        if (!string.Equals(edge.SourceNodeId, From, StringComparison.Ordinal)) edge.Metadata["vis.from"] = From;
        if (!string.Equals(edge.TargetNodeId, To, StringComparison.Ordinal)) edge.Metadata["vis.to"] = To;
        edge.Metadata["vis.arrows.to"] = ArrowsTo ? "true" : "false";
        edge.Metadata["vis.arrows.from"] = ArrowsFrom ? "true" : "false";
        edge.Metadata["vis.arrows.middle"] = ArrowsMiddle ? "true" : "false";
        edge.Metadata["vis.smooth"] = Smooth.ToString();
        return edge;
    }

    private static GraphEdgeShape MapShape(VisNetworkSmoothType smooth) => smooth switch {
        VisNetworkSmoothType.Disabled => GraphEdgeShape.Line,
        VisNetworkSmoothType.Continuous => GraphEdgeShape.ContinuousCurve,
        VisNetworkSmoothType.SelfReference => GraphEdgeShape.SelfReference,
        _ => GraphEdgeShape.DynamicCurve
    };
}

/// <summary>
/// Describes group defaults similar to vis-network's groups option.
/// </summary>
public sealed class VisNetworkGroupOptions {
    /// <summary>Gets or sets the display label used for the generated group cluster.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the product-neutral kind applied to group members.</summary>
    public string? Kind { get; set; }

    /// <summary>Gets or sets the default group node shape.</summary>
    public VisNetworkNodeShape? Shape { get; set; }

    /// <summary>Gets or sets the default group node size.</summary>
    public double? Size { get; set; }

    /// <summary>Gets or sets the default group node image URI.</summary>
    public string? Image { get; set; }

    /// <summary>Gets or sets default group icon text.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets default group styling hints.</summary>
    public GraphNodeStyle Style { get; } = new();
}

/// <summary>
/// Describes vis-network-style top-level options supported by the ChartForgeX compatibility converter.
/// </summary>
public sealed class VisNetworkOptions {
    /// <summary>Gets layout options.</summary>
    public VisNetworkLayoutOptions Layout { get; } = new();

    /// <summary>Gets physics options.</summary>
    public VisNetworkPhysicsOptions Physics { get; } = new();

    /// <summary>Gets interaction options.</summary>
    public VisNetworkInteractionOptions Interaction { get; } = new();

    /// <summary>Gets manipulation options.</summary>
    public VisNetworkManipulationOptions Manipulation { get; } = new();
}

/// <summary>
/// Describes vis-network-style layout options.
/// </summary>
public sealed class VisNetworkLayoutOptions {
    /// <summary>Gets hierarchical layout options.</summary>
    public VisNetworkHierarchicalLayoutOptions Hierarchical { get; } = new();
}

/// <summary>
/// Describes vis-network-style hierarchical layout options.
/// </summary>
public sealed class VisNetworkHierarchicalLayoutOptions {
    /// <summary>Gets or sets whether hierarchical layout is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets hierarchy direction.</summary>
    public GraphLayoutDirection Direction { get; set; } = GraphLayoutDirection.TopToBottom;

    /// <summary>Gets or sets distance between hierarchy levels.</summary>
    public double LevelSeparation { get; set; } = 120;

    /// <summary>Gets or sets distance between nodes in a level.</summary>
    public double NodeSpacing { get; set; } = 92;

    /// <summary>Gets or sets distance between disconnected components.</summary>
    public double ComponentSpacing { get; set; } = 120;

    /// <summary>Gets or sets whether missing node levels can be inferred from directed edges.</summary>
    public bool InferLevelsFromEdges { get; set; } = true;
}

/// <summary>
/// Describes vis-network-style physics options supported by the converter.
/// </summary>
public sealed class VisNetworkPhysicsOptions {
    /// <summary>Gets or sets whether runtime physics is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the solver family.</summary>
    public GraphPhysicsSolver Solver { get; set; } = GraphPhysicsSolver.StaticPrepared;

    /// <summary>Gets or sets stabilization iterations.</summary>
    public int StabilizationIterations { get; set; } = 1000;

    /// <summary>Gets or sets default link distance.</summary>
    public double LinkDistance { get; set; } = 120;

    /// <summary>Gets or sets node repulsion.</summary>
    public double Repulsion { get; set; } = 4500;

    /// <summary>Gets or sets damping.</summary>
    public double Damping { get; set; } = 0.09;

    /// <summary>Gets or sets whether timestep may be adaptive.</summary>
    public bool AdaptiveTimestep { get; set; } = true;
}

/// <summary>
/// Describes vis-network-style interaction options supported by the converter.
/// </summary>
public sealed class VisNetworkInteractionOptions {
    /// <summary>Gets or sets whether nodes can be dragged.</summary>
    public bool DragNodes { get; set; } = true;

    /// <summary>Gets or sets whether hover-capable adapters should emit hover intent metadata.</summary>
    public bool Hover { get; set; } = true;

    /// <summary>Gets or sets whether navigation buttons are requested.</summary>
    public bool NavigationButtons { get; set; } = true;
}

/// <summary>
/// Describes vis-network-style manipulation options supported by the converter.
/// </summary>
public sealed class VisNetworkManipulationOptions {
    /// <summary>Gets or sets whether manipulation is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets whether adapters may add nodes.</summary>
    public bool AddNode { get; set; }

    /// <summary>Gets or sets whether adapters may edit nodes.</summary>
    public bool EditNode { get; set; }

    /// <summary>Gets or sets whether adapters may delete nodes.</summary>
    public bool DeleteNode { get; set; }

    /// <summary>Gets or sets whether adapters may add edges.</summary>
    public bool AddEdge { get; set; }

    /// <summary>Gets or sets whether adapters may edit edges.</summary>
    public bool EditEdge { get; set; }

    /// <summary>Gets or sets whether adapters may delete edges.</summary>
    public bool DeleteEdge { get; set; }

    /// <summary>Gets or sets whether adapters may persist positions.</summary>
    public bool PersistPositions { get; set; }
}

/// <summary>
/// Names common vis-network node shapes.
/// </summary>
public enum VisNetworkNodeShape {
    /// <summary>Use a small circular dot.</summary>
    Dot,

    /// <summary>Use a circular node.</summary>
    Circle,

    /// <summary>Use an elliptical node.</summary>
    Ellipse,

    /// <summary>Use a box node.</summary>
    Box,

    /// <summary>Use a database-like node.</summary>
    Database,

    /// <summary>Use a text-oriented node.</summary>
    Text,

    /// <summary>Use an image-backed node.</summary>
    Image,

    /// <summary>Use an image-backed circular node.</summary>
    CircularImage,

    /// <summary>Use a diamond node.</summary>
    Diamond,

    /// <summary>Use a square node.</summary>
    Square,

    /// <summary>Use a star node.</summary>
    Star,

    /// <summary>Use an upward triangle node.</summary>
    Triangle,

    /// <summary>Use a downward triangle node.</summary>
    TriangleDown,

    /// <summary>Use an icon-style node.</summary>
    Icon
}

/// <summary>
/// Names common vis-network smooth edge modes.
/// </summary>
public enum VisNetworkSmoothType {
    /// <summary>Do not smooth edges.</summary>
    Disabled,

    /// <summary>Use adapter-selected dynamic smoothing.</summary>
    Dynamic,

    /// <summary>Use continuous curve smoothing when supported.</summary>
    Continuous,

    /// <summary>Use self-reference smoothing for loop edges.</summary>
    SelfReference
}
