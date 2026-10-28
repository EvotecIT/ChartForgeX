using System;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX topology rendering adapters for Mermaid diagram families that map to graph-like previews.
/// </summary>
public static partial class MermaidTopologyRendering {
    /// <summary>Converts a Mermaid class diagram to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidClassDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var chart = CreateTopology(options, "mermaid-class", ResolveTitle(document, options, "Mermaid class diagram"), TopologyLayoutMode.Layered, DiagramDirection(document.Direction, TopologyLayoutDirection.LeftToRight));
        AddClassNamespaceGroups(chart, document);
        foreach (var item in document.Classes) {
            chart.AddAutoNode(item.Id, MermaidLabelText.DisplayCaption(item.Label ?? item.Id), TopologyNodeKind.Generic, TopologyHealthStatus.Unknown, groupId: item.Namespace, subtitle: item.Annotations.Count == 0 ? null : string.Join(", ", item.Annotations), width: MemberWidth(item), height: 82 + item.Members.Count * 18 + (item.Annotations.Count > 0 ? 20 : 0), cssClass: "cfx-mermaid-class");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.LabelContent = MermaidInlineCaption.Content(item.Label ?? item.Id);
            node.Metadata["mermaid.id"] = item.Id;
            node.ShowStatusBadge = false;
            node.Shape = TopologyNodeShape.Rectangle;
            node.PreserveDisplayModeSize = true;
            node.FitLabelToContent = true;
            node.MaximumLabelCharacters = node.Label.Length;
            node.Metadata["mermaid.source.label"] = item.Label ?? item.Id;
            if (document.GenericDeclarations.TryGetValue(item.Id, out var declaration)) node.Metadata["mermaid.generic.source"] = declaration;
            node.Metadata["mermaid.kind"] = "class";
            node.Metadata["mermaid.attributes"] = CountMembers(item, false).ToString(CultureInfo.InvariantCulture);
            node.Metadata["mermaid.methods"] = CountMembers(item, true).ToString(CultureInfo.InvariantCulture);
            if (item.Annotations.Count > 0) node.Metadata["mermaid.annotations"] = string.Join(",", item.Annotations);
        }

        for (var index = 0; index < document.Relationships.Count; index++) {
            var item = document.Relationships[index];
            chart.AddEdge(EdgeId(index), item.SourceId, item.TargetId, DisplayRelationshipCaption(item.Label), ToClassEdgeKind(item.Connector), TopologyHealthStatus.Unknown, ClassDirection(item.Connector), TopologyEdgeRouting.Orthogonal);
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.LabelContent = RelationshipContent(item.Label);
            edge.LineStyle = item.Connector.IndexOf(".", StringComparison.Ordinal) >= 0 ? TopologyEdgeLineStyle.Dashed : TopologyEdgeLineStyle.Solid;
            edge.MinimumRankSpan = 2;
            edge.SourceLabel = item.SourceMultiplicity;
            edge.TargetLabel = item.TargetMultiplicity;
            edge.SourceMarker = ClassMarker(item.Connector, true);
            edge.TargetMarker = ClassMarker(item.Connector, false);
            edge.Metadata["mermaid.connector"] = item.Connector;
            if (item.Label != null) edge.Metadata["mermaid.source.label"] = item.Label;
        }

        ApplyClassNotation(chart, document);
        ApplyClassStyles(chart, document);
        AddNotes(chart, document.Notes);
        return MermaidPresentation.Apply(chart, document, options.Presentation);
    }

    /// <summary>Converts a Mermaid state diagram to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidStateDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        return CreateStateTopology(document, options);
    }

    /// <summary>Converts a Mermaid ER diagram to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidEntityRelationshipDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var chart = CreateTopology(options, "mermaid-er", ResolveTitle(document, options, "Mermaid ER diagram"), TopologyLayoutMode.Layered, DiagramDirection(document.Direction, TopologyLayoutDirection.LeftToRight));
        foreach (var item in document.Entities) {
            chart.AddAutoNode(item.Id, MermaidLabelText.EntityCaption(item.Label ?? item.Id), TopologyNodeKind.Generic, TopologyHealthStatus.Unknown, width: EntityWidth(item), height: 82 + item.Attributes.Count * 18, cssClass: "cfx-mermaid-er-entity");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.LabelContent = MermaidInlineCaption.Content(item.Label ?? item.Id, markdownByDefault: true);
            node.Metadata["mermaid.id"] = item.Id;
            node.ShowStatusBadge = false;
            node.Shape = TopologyNodeShape.Rectangle;
            node.PreserveDisplayModeSize = true;
            node.FitLabelToContent = true;
            node.MaximumLabelCharacters = node.Label.Length;
            node.Metadata["mermaid.source.label"] = item.Label ?? item.Id;
            foreach (var attribute in item.Attributes) {
                var detail = new TopologyNodeDetail { Content = EntityAttributeContent(attribute) };
                detail.Metadata["mermaid.attribute.type"] = attribute.Type;
                detail.Metadata["mermaid.attribute.name"] = attribute.Name;
                if (attribute.Key != null) detail.Metadata["mermaid.attribute.key"] = attribute.Key;
                if (attribute.Comment != null) detail.Metadata["mermaid.attribute.comment"] = attribute.Comment;
                node.Details.Add(detail);
            }
            node.Metadata["mermaid.attributes"] = item.Attributes.Count.ToString(CultureInfo.InvariantCulture);
        }

        for (var index = 0; index < document.Relationships.Count; index++) {
            var item = document.Relationships[index];
            chart.AddEdge(EdgeId(index), item.SourceId, item.TargetId, DisplayRelationshipCaption(item.Label, entity: true), TopologyEdgeKind.Mapping, TopologyHealthStatus.Unknown, VisualLinkDirection.None, TopologyEdgeRouting.Orthogonal);
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.LabelContent = RelationshipContent(item.Label, entity: true);
            edge.MinimumRankSpan = 2;
            edge.SourceMarker = CardinalityMarker(item.Connector, true);
            edge.TargetMarker = CardinalityMarker(item.Connector, false);
            edge.LineStyle = item.Connector.IndexOf("..", StringComparison.Ordinal) >= 0 ? TopologyEdgeLineStyle.Dashed : TopologyEdgeLineStyle.Solid;
            edge.Metadata["mermaid.cardinality"] = item.Connector;
            edge.Metadata["mermaid.source.label"] = item.Label;
        }

        ApplyEntityGroups(chart, document);
        ApplyEntityStyles(chart, document);
        return MermaidPresentation.Apply(chart, document, options.Presentation);
    }

    /// <summary>Converts a Mermaid tree view to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidTreeViewDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var title = document.Roots.Count == 1 ? ResolveTitle(document, options, document.Roots[0].Label) : ResolveTitle(document, options, "Mermaid tree view");
        var chart = CreateTopology(options, "mermaid-treeview", title, TopologyLayoutMode.MindMap, TopologyLayoutDirection.LeftToRight);
        foreach (var item in document.Nodes) {
            var kind = item.Parent == null ? TopologyNodeKind.Hub : item.IsBranch ? TopologyNodeKind.Branch : TopologyNodeKind.Process;
            chart.AddAutoNode(item.Id, item.Label, kind, TopologyHealthStatus.Unknown, width: item.Parent == null ? 150 : 126, height: 54, symbol: item.Parent == null ? "T" : item.IsBranch ? "D" : null, cssClass: "cfx-mermaid-treeview-node");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.level"] = item.Level.ToString(CultureInfo.InvariantCulture);
            node.Metadata["mermaid.indent"] = item.Indent.ToString(CultureInfo.InvariantCulture);
            node.Metadata["mermaid.kind"] = item.IsBranch ? "branch" : "leaf";
            if (item.Parent != null) chart.AddEdge("treeview-edge-" + item.Id, item.Parent.Id, item.Id, null, TopologyEdgeKind.Ownership, TopologyHealthStatus.Unknown, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved);
        }

        return MermaidPresentation.Apply(chart, document, options.Presentation);
    }

    /// <summary>Wraps a Mermaid class diagram in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidClassDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "classes", document.Classes.Count, "relationships", document.Relationships.Count);

    /// <summary>Wraps a Mermaid state diagram in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidStateDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "states", document.States.Count, "transitions", document.Transitions.Count);

    /// <summary>Wraps a Mermaid ER diagram in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidEntityRelationshipDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "entities", document.Entities.Count, "relationships", document.Relationships.Count);

    /// <summary>Wraps a Mermaid mindmap in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidMindMapDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "nodes", document.Nodes.Count, "roots", CountMindMapRoots(document));


    /// <summary>Wraps a Mermaid kanban board in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidKanbanDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "columns", document.Columns.Count, "tasks", CountKanbanTasks(document));

    /// <summary>Renders a Mermaid class diagram to SVG.</summary>
    public static string ToSvg(this MermaidClassDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();

    /// <summary>Renders a Mermaid state diagram to SVG.</summary>
    public static string ToSvg(this MermaidStateDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();

    /// <summary>Renders a Mermaid ER diagram to SVG.</summary>
    public static string ToSvg(this MermaidEntityRelationshipDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();

    /// <summary>Renders a Mermaid mindmap to SVG.</summary>
    public static string ToSvg(this MermaidMindMapDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();


    /// <summary>Renders a Mermaid kanban board to SVG.</summary>
    public static string ToSvg(this MermaidKanbanDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();

    /// <summary>Renders a Mermaid class diagram to PNG.</summary>
    public static byte[] ToPng(this MermaidClassDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();

    /// <summary>Renders a Mermaid state diagram to PNG.</summary>
    public static byte[] ToPng(this MermaidStateDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();

    /// <summary>Renders a Mermaid ER diagram to PNG.</summary>
    public static byte[] ToPng(this MermaidEntityRelationshipDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();

    /// <summary>Renders a Mermaid mindmap to PNG.</summary>
    public static byte[] ToPng(this MermaidMindMapDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();


    /// <summary>Renders a Mermaid kanban board to PNG.</summary>
    public static byte[] ToPng(this MermaidKanbanDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();

    private static TopologyLayoutDirection DiagramDirection(string? direction, TopologyLayoutDirection fallback) => direction?.ToUpperInvariant() switch {
        "LR" => TopologyLayoutDirection.LeftToRight, "RL" => TopologyLayoutDirection.RightToLeft,
        "TB" => TopologyLayoutDirection.TopToBottom, "BT" => TopologyLayoutDirection.BottomToTop, _ => fallback
    };

    private static TopologyChart CreateTopology(MermaidTopologyRenderOptions options, string defaultId, string title, TopologyLayoutMode layout, TopologyLayoutDirection direction) {
        var id = string.IsNullOrWhiteSpace(options.Id) ? defaultId : options.Id!.Trim();
        return TopologyChart.Create()
            .WithId(id)
            .WithTitle(title)
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(layout, direction);
    }

    private static VisualArtifact ToArtifact(MermaidDocument document, TopologyChart topology, string firstMetric, int firstValue, string secondMetric, int secondValue) {
        var artifact = topology.ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid);
        artifact.Kind = VisualArtifactKind.Mermaid;
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Json;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid." + firstMetric] = firstValue.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid." + secondMetric] = secondValue.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return MermaidPresentation.Apply(artifact, document);
    }

    private static string ResolveTitle(MermaidDocument document, MermaidTopologyRenderOptions options, string fallback) => MermaidHeading.Title(document, options.Title, null, fallback);

    private static string EdgeId(int index) => "mermaid-edge-" + index.ToString(CultureInfo.InvariantCulture);

    private static int CountMembers(MermaidClassNode node, bool methods) {
        var count = 0;
        foreach (var member in node.Members) if (member.IsMethod == methods) count++;
        return count;
    }

    private static TopologyEdgeKind ToClassEdgeKind(string connector) {
        if (connector.IndexOf("<|", StringComparison.Ordinal) >= 0 || connector.IndexOf("|>", StringComparison.Ordinal) >= 0) return TopologyEdgeKind.Dependency;
        if (connector.IndexOf("*", StringComparison.Ordinal) >= 0 || connector.IndexOf("o", StringComparison.Ordinal) >= 0) return TopologyEdgeKind.Ownership;
        return TopologyEdgeKind.Link;
    }

    private static bool IsPseudoState(MermaidStateNode node) => node.Kind == "start" || node.Kind == "end";

    // Start is a filled dot and end a ringed dot, as in Mermaid; a choice is a decision diamond.
    private static TopologyNodeShape ToStateShape(MermaidStateNode node) =>
        node.Kind == "end" ? TopologyNodeShape.DoubleCircle :
        node.Kind == "start" ? TopologyNodeShape.Circle :
        IsSynchronizationState(node) ? TopologyNodeShape.SynchronizationBar :
        string.Equals(node.Kind, "choice", StringComparison.OrdinalIgnoreCase) ? TopologyNodeShape.Diamond : TopologyNodeShape.Rounded;

    private static bool IsSynchronizationState(MermaidStateNode node) => string.Equals(node.Kind, "fork", StringComparison.OrdinalIgnoreCase) || string.Equals(node.Kind, "join", StringComparison.OrdinalIgnoreCase);

    private static bool IsStateControl(MermaidStateNode node) => IsPseudoState(node) || IsSynchronizationState(node) || string.Equals(node.Kind, "choice", StringComparison.OrdinalIgnoreCase);

    private static double ToStateWidth(MermaidStateNode node, TopologyLayoutDirection direction) => IsPseudoState(node) ? 22 :
        IsSynchronizationState(node) ? direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft ? 8 : 96 :
        string.Equals(node.Kind, "choice", StringComparison.OrdinalIgnoreCase) ? 30 : 132;

    private static double ToStateHeight(MermaidStateNode node, TopologyLayoutDirection direction) => IsPseudoState(node) ? 22 :
        IsSynchronizationState(node) ? direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft ? 96 : 8 :
        string.Equals(node.Kind, "choice", StringComparison.OrdinalIgnoreCase) ? 30 : 58;

    private static int CountMindMapRoots(MermaidMindMapDocument document) {
        var count = 0;
        foreach (var node in document.Nodes) if (node.ParentId == null) count++;
        return count;
    }

    private static int CountKanbanTasks(MermaidKanbanDocument document) {
        var count = 0;
        foreach (var column in document.Columns) count += column.Tasks.Count;
        return count;
    }


}

/// <summary>
/// Defines rendering defaults for topology-backed Mermaid diagram families.
/// </summary>
public sealed class MermaidTopologyRenderOptions : MermaidDiagramRenderOptions {
    private double _padding=28;
    internal bool HasExplicitPadding { get; private set; }
    /// <summary>Gets or sets the natural preview width.</summary>
    public double Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public double Height { get; set; } = 560;

    /// <summary>Gets or sets the topology viewport padding.</summary>
    public double Padding {
        get=>_padding;
        set {
            if(double.IsNaN(value)||double.IsInfinity(value)||value<0)throw new System.ArgumentOutOfRangeException(nameof(value));
            _padding=value;HasExplicitPadding=true;
        }
    }
}
