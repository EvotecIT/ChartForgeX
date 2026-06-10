using System;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid flowcharts.
/// </summary>
public static class MermaidFlowchartRendering {
    /// <summary>
    /// Converts a Mermaid flowchart document into a renderer-independent topology chart.
    /// </summary>
    /// <param name="document">The parsed Mermaid flowchart document.</param>
    /// <param name="options">Optional conversion and rendering defaults.</param>
    /// <returns>A topology chart that can render to ChartForgeX SVG, HTML, PNG, and raster outputs.</returns>
    public static TopologyChart ToTopologyChart(this MermaidFlowchartDocument document, MermaidFlowchartRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidFlowchartRenderOptions();

        var artifactId = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-flowchart" : options.Id!.Trim();
        var chart = TopologyChart.Create()
            .WithId(artifactId)
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(TopologyLayoutMode.Layered, ToTopologyDirection(document.Direction));

        for (var index = 0; index < document.Subgraphs.Count; index++) {
            var subgraph = document.Subgraphs[index];
            chart.AddAutoGroup(
                subgraph.Id,
                subgraph.Title,
                TopologyHealthStatus.Unknown,
                cssClass: "cfx-mermaid-subgraph");
            var target = chart.Groups[chart.Groups.Count - 1];
            target.Metadata["mermaid.id"] = subgraph.Id;
            target.Metadata["mermaid.title"] = subgraph.Title;
            target.Metadata["mermaid.nodes"] = string.Join(",", subgraph.NodeIds);
            target.Metadata["mermaid.source.line"] = subgraph.Span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.order"] = index.ToString(CultureInfo.InvariantCulture);
        }

        for (var index = 0; index < document.Nodes.Count; index++) {
            var node = document.Nodes[index];
            var label = string.IsNullOrWhiteSpace(node.Text) ? node.Id : node.Text!;
            chart.AddAutoNode(
                node.Id,
                label,
                ToNodeKind(node.Shape),
                TopologyHealthStatus.Unknown,
                groupId: node.SubgraphId,
                href: node.Href,
                tooltip: node.Tooltip,
                width: ToNodeWidth(node.Shape),
                height: ToNodeHeight(node.Shape),
                color: StyleValue(node.Styles, "stroke") ?? StyleValue(node.Styles, "color"),
                cssClass: "cfx-mermaid-node cfx-mermaid-shape-" + ToShapeToken(node.Shape));

            var target = chart.Nodes[chart.Nodes.Count - 1];
            target.DisplayMode = ToDisplayMode(node.Shape);
            target.BackgroundColor = StyleValue(node.Styles, "fill");
            target.Metadata["mermaid.id"] = node.Id;
            target.Metadata["mermaid.shape"] = node.Shape.ToString();
            target.Metadata["mermaid.shapeToken"] = ToShapeToken(node.Shape);
            target.Metadata["mermaid.source.line"] = node.Span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.source.column"] = node.Span.Column.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.order"] = index.ToString(CultureInfo.InvariantCulture);
            if (node.SubgraphId != null) target.Metadata["mermaid.subgraph"] = node.SubgraphId;
            if (node.Classes.Count > 0) target.Metadata["mermaid.classes"] = string.Join(",", node.Classes);
            WriteStyleMetadata(target.Metadata, node.Styles, "mermaid.style.");
        }

        for (var index = 0; index < document.Edges.Count; index++) {
            var edge = document.Edges[index];
            var edgeId = "mermaid-edge-" + index.ToString(CultureInfo.InvariantCulture);
            chart.AddEdge(
                edgeId,
                edge.SourceId,
                edge.TargetId,
                edge.Label,
                TopologyEdgeKind.Dependency,
                TopologyHealthStatus.Unknown,
                ToTopologyDirection(edge.Operator),
                TopologyEdgeRouting.Orthogonal);

            var target = chart.Edges[chart.Edges.Count - 1];
            target.LineStyle = ToLineStyle(edge.Operator);
            var edgeColor = StyleValue(edge.Styles, "stroke");
            if (!string.IsNullOrWhiteSpace(edgeColor)) target.Color = edgeColor;
            if (edge.Styles.ContainsKey("stroke-dasharray")) target.LineStyle = TopologyEdgeLineStyle.Dashed;
            target.Metadata["mermaid.operator"] = edge.Operator;
            target.Metadata["mermaid.source"] = edge.SourceId;
            target.Metadata["mermaid.target"] = edge.TargetId;
            target.Metadata["mermaid.source.line"] = edge.Span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.source.column"] = edge.Span.Column.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.order"] = index.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(edge.Label)) target.Metadata["mermaid.label"] = edge.Label!;
            WriteStyleMetadata(target.Metadata, edge.Styles, "mermaid.style.");
        }

        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid flowchart document in a product-neutral visual artifact envelope backed by a topology chart.
    /// </summary>
    /// <param name="document">The parsed Mermaid flowchart document.</param>
    /// <param name="options">Optional conversion and rendering defaults.</param>
    /// <returns>A visual artifact envelope.</returns>
    public static VisualArtifact ToVisualArtifact(this MermaidFlowchartDocument document, MermaidFlowchartRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidFlowchartRenderOptions();
        var topology = document.ToTopologyChart(options);
        var artifact = VisualArtifact.Create(topology.Id ?? "mermaid-flowchart", VisualArtifactKind.Mermaid, topology);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = topology.Title ?? string.Empty;
        artifact.Subtitle = topology.Subtitle ?? string.Empty;
        artifact.NaturalSize = new VisualArtifactSize(topology.Viewport.Width, topology.Viewport.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.direction"] = document.Direction.ToString();
        artifact.Metadata["mermaid.nodes"] = document.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.edges"] = document.Edges.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.subgraphs"] = document.Subgraphs.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.classDefinitions"] = document.ClassDefinitions.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.linkStyles"] = document.LinkStyles.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid flowchart document to static SVG through ChartForgeX topology rendering.
    /// </summary>
    /// <param name="document">The parsed Mermaid flowchart document.</param>
    /// <param name="options">Optional conversion and rendering defaults.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this MermaidFlowchartDocument document, MermaidFlowchartRenderOptions? options = null) =>
        document.ToTopologyChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid flowchart document to static PNG through ChartForgeX topology rendering.
    /// </summary>
    /// <param name="document">The parsed Mermaid flowchart document.</param>
    /// <param name="options">Optional conversion and rendering defaults.</param>
    /// <returns>PNG bytes.</returns>
    public static byte[] ToPng(this MermaidFlowchartDocument document, MermaidFlowchartRenderOptions? options = null) =>
        document.ToTopologyChart(options).ToPng();

    private static string ResolveTitle(MermaidFlowchartDocument document, MermaidFlowchartRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        var frontMatterTitle = FindFrontMatterValue(document.FrontMatter, "title");
        return string.IsNullOrWhiteSpace(frontMatterTitle) ? "Mermaid flowchart" : frontMatterTitle!;
    }

    private static string ResolveSubtitle(MermaidFlowchartDocument document, MermaidFlowchartRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static string? FindFrontMatterValue(string? frontMatter, string key) {
        if (string.IsNullOrWhiteSpace(frontMatter)) return null;
        var lines = frontMatter!.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var prefix = key + ":";
        foreach (var line in lines) {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var value = trimmed.Substring(prefix.Length).Trim();
            return value.Trim('"', '\'');
        }

        return null;
    }

    private static TopologyLayoutDirection ToTopologyDirection(MermaidFlowchartDirection direction) {
        switch (direction) {
            case MermaidFlowchartDirection.LeftToRight:
                return TopologyLayoutDirection.LeftToRight;
            case MermaidFlowchartDirection.RightToLeft:
                return TopologyLayoutDirection.RightToLeft;
            case MermaidFlowchartDirection.BottomToTop:
                return TopologyLayoutDirection.BottomToTop;
            case MermaidFlowchartDirection.None:
            case MermaidFlowchartDirection.TopToBottom:
            case MermaidFlowchartDirection.TopDown:
            default:
                return TopologyLayoutDirection.TopToBottom;
        }
    }

    private static TopologyDirection ToTopologyDirection(string edgeOperator) {
        if (edgeOperator.IndexOf("<", StringComparison.Ordinal) >= 0 && edgeOperator.IndexOf(">", StringComparison.Ordinal) >= 0) return TopologyDirection.Bidirectional;
        if (edgeOperator.IndexOf("<", StringComparison.Ordinal) >= 0) return TopologyDirection.Backward;
        if (edgeOperator.IndexOf(">", StringComparison.Ordinal) >= 0 || edgeOperator.IndexOf("x", StringComparison.Ordinal) >= 0 || edgeOperator.IndexOf("o", StringComparison.Ordinal) >= 0) return TopologyDirection.Forward;
        return TopologyDirection.None;
    }

    private static TopologyEdgeLineStyle ToLineStyle(string edgeOperator) {
        if (edgeOperator.IndexOf(".", StringComparison.Ordinal) >= 0) return TopologyEdgeLineStyle.Dotted;
        if (edgeOperator.IndexOf("=", StringComparison.Ordinal) >= 0) return TopologyEdgeLineStyle.Solid;
        return TopologyEdgeLineStyle.Solid;
    }

    private static string? StyleValue(System.Collections.Generic.Dictionary<string, string> styles, string key) =>
        styles.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static void WriteStyleMetadata(System.Collections.Generic.Dictionary<string, string> metadata, System.Collections.Generic.Dictionary<string, string> styles, string prefix) {
        foreach (var item in styles) metadata[prefix + item.Key] = item.Value;
    }

    private static TopologyNodeKind ToNodeKind(MermaidFlowchartNodeShape shape) {
        switch (shape) {
            case MermaidFlowchartNodeShape.Cylinder:
                return TopologyNodeKind.Database;
            case MermaidFlowchartNodeShape.Circle:
            case MermaidFlowchartNodeShape.DoubleCircle:
                return TopologyNodeKind.Hub;
            case MermaidFlowchartNodeShape.Rhombus:
            case MermaidFlowchartNodeShape.Hexagon:
                return TopologyNodeKind.Process;
            default:
                return TopologyNodeKind.Generic;
        }
    }

    private static TopologyNodeDisplayMode ToDisplayMode(MermaidFlowchartNodeShape shape) {
        switch (shape) {
            case MermaidFlowchartNodeShape.Circle:
            case MermaidFlowchartNodeShape.DoubleCircle:
                return TopologyNodeDisplayMode.Tile;
            case MermaidFlowchartNodeShape.Rounded:
            case MermaidFlowchartNodeShape.Stadium:
            case MermaidFlowchartNodeShape.Asymmetric:
                return TopologyNodeDisplayMode.Pill;
            default:
                return TopologyNodeDisplayMode.Card;
        }
    }

    private static double ToNodeWidth(MermaidFlowchartNodeShape shape) {
        switch (shape) {
            case MermaidFlowchartNodeShape.Circle:
            case MermaidFlowchartNodeShape.DoubleCircle:
                return 88;
            case MermaidFlowchartNodeShape.Stadium:
            case MermaidFlowchartNodeShape.Asymmetric:
                return 150;
            default:
                return 132;
        }
    }

    private static double ToNodeHeight(MermaidFlowchartNodeShape shape) {
        switch (shape) {
            case MermaidFlowchartNodeShape.Circle:
            case MermaidFlowchartNodeShape.DoubleCircle:
                return 88;
            case MermaidFlowchartNodeShape.Stadium:
            case MermaidFlowchartNodeShape.Asymmetric:
                return 52;
            default:
                return 68;
        }
    }

    private static string ToShapeToken(MermaidFlowchartNodeShape shape) {
        switch (shape) {
            case MermaidFlowchartNodeShape.DoubleCircle:
                return "double-circle";
            case MermaidFlowchartNodeShape.ParallelogramAlt:
                return "parallelogram-alt";
            case MermaidFlowchartNodeShape.TrapezoidAlt:
                return "trapezoid-alt";
            default:
                return shape.ToString().ToLowerInvariant();
        }
    }
}

/// <summary>
/// Defines conversion defaults for rendering Mermaid flowcharts through ChartForgeX.
/// </summary>
public sealed class MermaidFlowchartRenderOptions {
    private double _width = 1200;
    private double _height = 700;
    private double _padding = 32;

    /// <summary>Gets or sets the rendered artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets an optional chart title override.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets an optional chart subtitle override.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the topology viewport width.</summary>
    public double Width {
        get => _width;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Mermaid flowchart width must be finite and greater than zero.");
            _width = value;
        }
    }

    /// <summary>Gets or sets the topology viewport height.</summary>
    public double Height {
        get => _height;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Mermaid flowchart height must be finite and greater than zero.");
            _height = value;
        }
    }

    /// <summary>Gets or sets the topology viewport padding.</summary>
    public double Padding {
        get => _padding;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Mermaid flowchart padding must be finite and non-negative.");
            _padding = value;
        }
    }
}
