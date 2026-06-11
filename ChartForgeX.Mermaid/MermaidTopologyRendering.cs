using System;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX topology rendering adapters for Mermaid diagram families that map to graph-like previews.
/// </summary>
public static class MermaidTopologyRendering {
    /// <summary>Converts a Mermaid class diagram to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidClassDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var chart = CreateTopology(options, "mermaid-class", ResolveTitle(options, "Mermaid class diagram"), document.Header, TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight);
        foreach (var item in document.Classes) {
            chart.AddAutoNode(item.Id, item.Label ?? item.Id, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, subtitle: ClassSubtitle(item), width: 150, height: 82, symbol: "C", cssClass: "cfx-mermaid-class");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.id"] = item.Id;
            node.Metadata["mermaid.kind"] = "class";
            node.Metadata["mermaid.attributes"] = CountMembers(item, false).ToString(CultureInfo.InvariantCulture);
            node.Metadata["mermaid.methods"] = CountMembers(item, true).ToString(CultureInfo.InvariantCulture);
            if (item.Annotations.Count > 0) node.Metadata["mermaid.annotations"] = string.Join(",", item.Annotations);
        }

        for (var index = 0; index < document.Relationships.Count; index++) {
            var item = document.Relationships[index];
            chart.AddEdge(EdgeId(index), item.SourceId, item.TargetId, item.Label, ToClassEdgeKind(item.Connector), TopologyHealthStatus.Unknown, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal);
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.LineStyle = item.Connector.IndexOf(".", StringComparison.Ordinal) >= 0 ? TopologyEdgeLineStyle.Dotted : TopologyEdgeLineStyle.Solid;
            edge.Metadata["mermaid.connector"] = item.Connector;
        }

        return chart;
    }

    /// <summary>Converts a Mermaid state diagram to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidStateDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var direction = string.Equals(document.Direction, "LR", StringComparison.OrdinalIgnoreCase) ? TopologyLayoutDirection.LeftToRight : TopologyLayoutDirection.TopToBottom;
        var chart = CreateTopology(options, "mermaid-state", ResolveTitle(options, "Mermaid state diagram"), document.Header, TopologyLayoutMode.Layered, direction);
        foreach (var composite in document.States) {
            if (document.States.Exists(state => state.ParentId == composite.Id)) {
                chart.AddGroup(composite.Id, composite.Label ?? composite.Id, 0, 0, 220, 150, TopologyHealthStatus.Unknown, cssClass: "cfx-mermaid-state-composite");
            }
        }

        foreach (var item in document.States) {
            chart.AddAutoNode(item.Id, item.Label ?? item.Id, ToStateNodeKind(item), TopologyHealthStatus.Unknown, groupId: item.ParentId, subtitle: item.Kind, width: ToStateWidth(item), height: ToStateHeight(item), symbol: ToStateSymbol(item), cssClass: "cfx-mermaid-state");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.id"] = item.Id;
            node.Metadata["mermaid.kind"] = item.Kind ?? "state";
            if (item.ParentId != null) node.Metadata["mermaid.parent"] = item.ParentId;
        }

        for (var index = 0; index < document.Transitions.Count; index++) {
            var item = document.Transitions[index];
            chart.AddEdge(EdgeId(index), item.SourceId, item.TargetId, item.Label, TopologyEdgeKind.Dependency, TopologyHealthStatus.Unknown, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal);
        }

        return chart;
    }

    /// <summary>Converts a Mermaid ER diagram to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidEntityRelationshipDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var chart = CreateTopology(options, "mermaid-er", ResolveTitle(options, "Mermaid ER diagram"), document.Header, TopologyLayoutMode.RelationshipRadial, TopologyLayoutDirection.LeftToRight);
        foreach (var item in document.Entities) {
            chart.AddAutoNode(item.Id, item.Id, TopologyNodeKind.Database, TopologyHealthStatus.Unknown, subtitle: EntitySubtitle(item), width: 150, height: 82, symbol: "ER", cssClass: "cfx-mermaid-er-entity");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.id"] = item.Id;
            node.Metadata["mermaid.attributes"] = item.Attributes.Count.ToString(CultureInfo.InvariantCulture);
        }

        for (var index = 0; index < document.Relationships.Count; index++) {
            var item = document.Relationships[index];
            chart.AddEdge(EdgeId(index), item.SourceId, item.TargetId, item.Label, TopologyEdgeKind.Mapping, TopologyHealthStatus.Unknown, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal);
            chart.Edges[chart.Edges.Count - 1].Metadata["mermaid.cardinality"] = item.Connector;
        }

        return chart;
    }

    /// <summary>Converts a Mermaid mindmap to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidMindMapDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var title = document.Nodes.Count == 0 ? ResolveTitle(options, "Mermaid mindmap") : ResolveTitle(options, document.Nodes[0].Text);
        var chart = CreateTopology(options, "mermaid-mindmap", title, document.Header, TopologyLayoutMode.MindMap, TopologyLayoutDirection.LeftToRight);
        foreach (var item in document.Nodes) {
            chart.AddAutoNode(item.Id, item.Text, item.ParentId == null ? TopologyNodeKind.Hub : TopologyNodeKind.Branch, TopologyHealthStatus.Unknown, width: item.ParentId == null ? 150 : 120, height: 58, symbol: item.ParentId == null ? "M" : null, cssClass: "cfx-mermaid-mindmap-node");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.shape"] = item.Shape ?? "default";
            node.Metadata["mermaid.level"] = item.Level.ToString(CultureInfo.InvariantCulture);
            if (item.Classes.Count > 0) node.Metadata["mermaid.classes"] = string.Join(",", item.Classes);
            if (item.Icons.Count > 0) node.Metadata["mermaid.icons"] = string.Join(",", item.Icons);
            if (item.ParentId != null) chart.AddEdge("mindmap-edge-" + item.Id, item.ParentId, item.Id, null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Unknown, TopologyDirection.Forward, TopologyEdgeRouting.Curved);
        }

        return chart;
    }

    /// <summary>Converts a Mermaid tree view to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidTreeViewDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var title = document.Roots.Count == 1 ? ResolveTitle(options, document.Roots[0].Label) : ResolveTitle(options, "Mermaid tree view");
        var chart = CreateTopology(options, "mermaid-treeview", title, document.Header, TopologyLayoutMode.MindMap, TopologyLayoutDirection.LeftToRight);
        foreach (var item in document.Nodes) {
            var kind = item.Parent == null ? TopologyNodeKind.Hub : item.IsBranch ? TopologyNodeKind.Branch : TopologyNodeKind.Process;
            chart.AddAutoNode(item.Id, item.Label, kind, TopologyHealthStatus.Unknown, width: item.Parent == null ? 150 : 126, height: 54, symbol: item.Parent == null ? "T" : item.IsBranch ? "D" : null, cssClass: "cfx-mermaid-treeview-node");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.level"] = item.Level.ToString(CultureInfo.InvariantCulture);
            node.Metadata["mermaid.indent"] = item.Indent.ToString(CultureInfo.InvariantCulture);
            node.Metadata["mermaid.kind"] = item.IsBranch ? "branch" : "leaf";
            if (item.Parent != null) chart.AddEdge("treeview-edge-" + item.Id, item.Parent.Id, item.Id, null, TopologyEdgeKind.Ownership, TopologyHealthStatus.Unknown, TopologyDirection.Forward, TopologyEdgeRouting.Curved);
        }

        return chart;
    }

    /// <summary>Converts a Mermaid kanban board to a topology chart.</summary>
    public static TopologyChart ToTopologyChart(this MermaidKanbanDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var chart = CreateTopology(options, "mermaid-kanban", ResolveTitle(options, "Mermaid kanban"), document.Header, TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight);
        foreach (var column in document.Columns) {
            chart.AddAutoGroup(column.Id, column.Title, TopologyHealthStatus.Unknown, subtitle: column.Tasks.Count.ToString(CultureInfo.InvariantCulture) + " tasks", cssClass: "cfx-mermaid-kanban-column", symbol: "K");
            chart.Groups[chart.Groups.Count - 1].LayoutPolicy = TopologyGroupLayoutPolicy.Grid;
            foreach (var task in column.Tasks) {
                chart.AddAutoNode(task.Id, task.Title, TopologyNodeKind.Process, TaskStatus(task), groupId: column.Id, subtitle: TaskSubtitle(task), width: 132, height: 58, symbol: TaskSymbol(task), cssClass: "cfx-mermaid-kanban-task");
                var node = chart.Nodes[chart.Nodes.Count - 1];
                node.Metadata["mermaid.column"] = column.Id;
                foreach (var item in task.Metadata) node.Metadata["mermaid.meta." + item.Key] = item.Value;
            }
        }

        return chart;
    }

    /// <summary>Wraps a Mermaid class diagram in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidClassDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "classes", document.Classes.Count, "relationships", document.Relationships.Count);

    /// <summary>Wraps a Mermaid state diagram in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidStateDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "states", document.States.Count, "transitions", document.Transitions.Count);

    /// <summary>Wraps a Mermaid ER diagram in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidEntityRelationshipDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "entities", document.Entities.Count, "relationships", document.Relationships.Count);

    /// <summary>Wraps a Mermaid mindmap in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidMindMapDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "nodes", document.Nodes.Count, "roots", CountMindMapRoots(document));

    /// <summary>Wraps a Mermaid tree view in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidTreeViewDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "nodes", document.Nodes.Count, "roots", document.Roots.Count);

    /// <summary>Wraps a Mermaid kanban board in a visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidKanbanDocument document, MermaidTopologyRenderOptions? options = null) => ToArtifact(document, document.ToTopologyChart(options), "columns", document.Columns.Count, "tasks", CountKanbanTasks(document));

    /// <summary>Renders a Mermaid class diagram to SVG.</summary>
    public static string ToSvg(this MermaidClassDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders a Mermaid state diagram to SVG.</summary>
    public static string ToSvg(this MermaidStateDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders a Mermaid ER diagram to SVG.</summary>
    public static string ToSvg(this MermaidEntityRelationshipDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders a Mermaid mindmap to SVG.</summary>
    public static string ToSvg(this MermaidMindMapDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders a Mermaid tree view to SVG.</summary>
    public static string ToSvg(this MermaidTreeViewDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders a Mermaid kanban board to SVG.</summary>
    public static string ToSvg(this MermaidKanbanDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders a Mermaid class diagram to PNG.</summary>
    public static byte[] ToPng(this MermaidClassDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    /// <summary>Renders a Mermaid state diagram to PNG.</summary>
    public static byte[] ToPng(this MermaidStateDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    /// <summary>Renders a Mermaid ER diagram to PNG.</summary>
    public static byte[] ToPng(this MermaidEntityRelationshipDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    /// <summary>Renders a Mermaid mindmap to PNG.</summary>
    public static byte[] ToPng(this MermaidMindMapDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    /// <summary>Renders a Mermaid tree view to PNG.</summary>
    public static byte[] ToPng(this MermaidTreeViewDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    /// <summary>Renders a Mermaid kanban board to PNG.</summary>
    public static byte[] ToPng(this MermaidKanbanDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    private static TopologyChart CreateTopology(MermaidTopologyRenderOptions options, string defaultId, string title, string subtitle, TopologyLayoutMode layout, TopologyLayoutDirection direction) {
        var id = string.IsNullOrWhiteSpace(options.Id) ? defaultId : options.Id!.Trim();
        return TopologyChart.Create()
            .WithId(id)
            .WithTitle(title)
            .WithSubtitle(string.IsNullOrWhiteSpace(options.Subtitle) ? subtitle : options.Subtitle!)
            .WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(layout, direction);
    }

    private static VisualArtifact ToArtifact(MermaidDocument document, TopologyChart topology, string firstMetric, int firstValue, string secondMetric, int secondValue) {
        var artifact = VisualArtifact.Create(topology.Id ?? "mermaid-topology", VisualArtifactKind.Mermaid, topology);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = topology.Title ?? string.Empty;
        artifact.Subtitle = topology.Subtitle ?? string.Empty;
        artifact.NaturalSize = new VisualArtifactSize(topology.Viewport.Width, topology.Viewport.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid." + firstMetric] = firstValue.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid." + secondMetric] = secondValue.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return artifact;
    }

    private static string ResolveTitle(MermaidTopologyRenderOptions options, string fallback) => string.IsNullOrWhiteSpace(options.Title) ? fallback : options.Title!;

    private static string EdgeId(int index) => "mermaid-edge-" + index.ToString(CultureInfo.InvariantCulture);

    private static string? ClassSubtitle(MermaidClassNode node) {
        var prefix = node.Annotations.Count == 0 ? null : "<<" + node.Annotations[0] + ">>";
        var counts = CountMembers(node, false).ToString(CultureInfo.InvariantCulture) + " attrs, " + CountMembers(node, true).ToString(CultureInfo.InvariantCulture) + " methods";
        return string.IsNullOrWhiteSpace(prefix) ? counts : prefix + " - " + counts;
    }

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

    private static TopologyNodeKind ToStateNodeKind(MermaidStateNode node) {
        if (node.Kind == "start" || node.Kind == "end") return TopologyNodeKind.Hub;
        if (string.Equals(node.Kind, "choice", StringComparison.OrdinalIgnoreCase)) return TopologyNodeKind.Gateway;
        if (string.Equals(node.Kind, "fork", StringComparison.OrdinalIgnoreCase) || string.Equals(node.Kind, "join", StringComparison.OrdinalIgnoreCase)) return TopologyNodeKind.Process;
        return TopologyNodeKind.Process;
    }

    private static double ToStateWidth(MermaidStateNode node) => node.Kind == "start" || node.Kind == "end" ? 58 : 132;

    private static double ToStateHeight(MermaidStateNode node) => node.Kind == "start" || node.Kind == "end" ? 44 : 58;

    private static string? ToStateSymbol(MermaidStateNode node) => node.Kind == "start" ? "S" : node.Kind == "end" ? "E" : null;

    private static string EntitySubtitle(MermaidEntityNode entity) => entity.Attributes.Count == 0 ? null! : entity.Attributes.Count.ToString(CultureInfo.InvariantCulture) + " attributes";

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

    private static string? TaskSubtitle(MermaidKanbanTask task) {
        if (task.Metadata.TryGetValue("assigned", out var assigned) && !string.IsNullOrWhiteSpace(assigned)) return assigned;
        if (task.Metadata.TryGetValue("ticket", out var ticket) && !string.IsNullOrWhiteSpace(ticket)) return ticket;
        return null;
    }

    private static string? TaskSymbol(MermaidKanbanTask task) => task.Metadata.TryGetValue("priority", out var priority) && !string.IsNullOrWhiteSpace(priority) ? priority.Substring(0, 1).ToUpperInvariant() : null;

    private static TopologyHealthStatus TaskStatus(MermaidKanbanTask task) {
        if (!task.Metadata.TryGetValue("priority", out var priority)) return TopologyHealthStatus.Unknown;
        if (priority.IndexOf("very high", StringComparison.OrdinalIgnoreCase) >= 0) return TopologyHealthStatus.Critical;
        if (priority.IndexOf("high", StringComparison.OrdinalIgnoreCase) >= 0) return TopologyHealthStatus.Warning;
        if (priority.IndexOf("low", StringComparison.OrdinalIgnoreCase) >= 0) return TopologyHealthStatus.Healthy;
        return TopologyHealthStatus.Unknown;
    }
}

/// <summary>
/// Defines rendering defaults for topology-backed Mermaid diagram families.
/// </summary>
public sealed class MermaidTopologyRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public double Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public double Height { get; set; } = 560;

    /// <summary>Gets or sets the topology viewport padding.</summary>
    public double Padding { get; set; } = 28;
}
