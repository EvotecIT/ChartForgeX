using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(document.Kind == MermaidDiagramKind.Swimlane || (document.Kind == MermaidDiagramKind.UseCase && document.Subgraphs.Count > 0) ? TopologyLayoutMode.Swimlane : TopologyLayoutMode.Layered, ToVisualLinkDirection(document.Direction));
        var inferredLayers = MermaidFlowchartLayering.Infer(document);
        var fitViewport = options.FitContent && !options.HasExplicitViewportSize;

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
            var sourceLabel = string.IsNullOrWhiteSpace(node.Text) ? node.Id : node.Text!;
            var label = MermaidFlowchartText.DisplayCaption(sourceLabel);
            var nodeWidth = ToNodeWidth(node.Shape, label, options.FitContent);
            // Mermaid shapes carry the node meaning; an implicit kind would add a glyph Mermaid never draws.
            chart.AddAutoNode(
                node.Id,
                label,
                TopologyNodeKind.Generic,
                TopologyHealthStatus.Unknown,
                groupId: node.SubgraphId,
                href: node.Href,
                tooltip: node.Tooltip,
                width: nodeWidth,
                height: node.Shape is MermaidFlowchartNodeShape.Circle or MermaidFlowchartNodeShape.DoubleCircle ? nodeWidth : ToNodeHeight(node.Shape, label),
                color: StyleValue(node.Styles, "stroke") ?? StyleValue(node.Styles, "color"),
                cssClass: "cfx-mermaid-node cfx-mermaid-shape-" + ToShapeToken(node.Shape));

            var target = chart.Nodes[chart.Nodes.Count - 1];
            target.Shape = node.Shape == MermaidFlowchartNodeShape.Rhombus ? TopologyNodeShape.Diamond :
                node.Shape == MermaidFlowchartNodeShape.Default ? TopologyNodeShape.Rectangle :
                (TopologyNodeShape)Enum.Parse(typeof(TopologyNodeShape), node.Shape.ToString());
            target.ShowStatusBadge = false;
            target.DisplayMode = TopologyNodeDisplayMode.Card;
            target.PreserveDisplayModeSize = true;
            target.BackgroundColor = StyleValue(node.Styles, "fill");
            target.Metadata["mermaid.id"] = node.Id;
            target.Metadata["mermaid.label"] = sourceLabel;
            target.Metadata["mermaid.shape"] = node.Shape.ToString();
            target.Metadata["mermaid.shapeToken"] = ToShapeToken(node.Shape);
            target.Metadata["mermaid.source.line"] = node.Span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.source.column"] = node.Span.Column.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.order"] = index.ToString(CultureInfo.InvariantCulture);
            if (inferredLayers.TryGetValue(node.Id, out var inferredLayer)) {
                target.Metadata["layer"] = inferredLayer.ToString(CultureInfo.InvariantCulture);
                target.Metadata["mermaid.layer"] = inferredLayer.ToString(CultureInfo.InvariantCulture);
            }
            if (node.SubgraphId != null) target.Metadata["mermaid.subgraph"] = node.SubgraphId;
            if (node.Classes.Count > 0) target.Metadata["mermaid.classes"] = string.Join(",", node.Classes);
            WriteStyleMetadata(target.Metadata, node.Styles, "mermaid.style.");
            WriteStyleMetadata(target.Metadata, node.Properties, "mermaid.property.");
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
                MermaidFlowchartOperators.Direction(edge.Operator),
                TopologyEdgeRouting.Orthogonal);

            var target = chart.Edges[chart.Edges.Count - 1];
            target.SourceMarker = MermaidFlowchartOperators.Marker(edge.Operator, true);
            target.TargetMarker = MermaidFlowchartOperators.Marker(edge.Operator, false);
            target.LineStyle = ToLineStyle(edge.Operator);
            if (document.Kind == MermaidDiagramKind.Flowchart || document.Kind == MermaidDiagramKind.Swimlane)
                target.MinimumRankSpan = MermaidFlowchartOperators.MinimumRankSpan(edge.Operator);
            if (document.Kind == MermaidDiagramKind.UseCase) target.Routing = TopologyEdgeRouting.Straight;
            var edgeColor = StyleValue(edge.Styles, "stroke");
            if (!string.IsNullOrWhiteSpace(edgeColor)) target.Color = edgeColor;
            if (edge.Styles.ContainsKey("stroke-dasharray")) target.LineStyle = TopologyEdgeLineStyle.Dashed;
            if (document.Kind == MermaidDiagramKind.UseCase && edge.Operator == "--|>") target.TargetMarker = TopologyMarkerKind.OpenTriangle;
            // UML relationship stereotypes need room between their two use-case surfaces.
            if (document.Kind == MermaidDiagramKind.UseCase && !string.IsNullOrWhiteSpace(edge.Label)) target.MinimumRankSpan = 2;
            target.Metadata["mermaid.operator"] = edge.Operator;
            target.Metadata["mermaid.source"] = edge.SourceId;
            target.Metadata["mermaid.target"] = edge.TargetId;
            target.Metadata["mermaid.source.line"] = edge.Span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.source.column"] = edge.Span.Column.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.order"] = index.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(edge.Label)) target.Metadata["mermaid.label"] = edge.Label!;
            WriteStyleMetadata(target.Metadata, edge.Styles, "mermaid.style.");
        }

        if (fitViewport) ApplyContentFittedViewport(chart, document.Direction, options);

        return MermaidPresentation.Apply(chart, document);
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
        var artifact = topology.ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid);
        artifact.Kind = VisualArtifactKind.Mermaid;
        artifact.PreserveNaturalSize = options.HasExplicitViewportSize;
        if (options.HasExplicitViewportSize) artifact.NaturalSize = new VisualArtifactSize(options.Width, options.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Json;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.direction"] = document.Direction.ToString();
        artifact.Metadata["mermaid.nodes"] = document.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.edges"] = document.Edges.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.subgraphs"] = document.Subgraphs.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.classDefinitions"] = document.ClassDefinitions.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.linkStyles"] = document.LinkStyles.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return MermaidPresentation.Apply(artifact, document);
    }

    /// <summary>
    /// Renders a Mermaid flowchart document to static SVG through ChartForgeX topology rendering.
    /// </summary>
    /// <param name="document">The parsed Mermaid flowchart document.</param>
    /// <param name="options">Optional conversion and rendering defaults.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this MermaidFlowchartDocument document, MermaidFlowchartRenderOptions? options = null) {
        options ??= new MermaidFlowchartRenderOptions();
        return document.ToTopologyChart(options).ToSvg(ToTopologyRenderOptions(options));
    }

    /// <summary>
    /// Renders a Mermaid flowchart document to static PNG through ChartForgeX topology rendering.
    /// </summary>
    /// <param name="document">The parsed Mermaid flowchart document.</param>
    /// <param name="options">Optional conversion and rendering defaults.</param>
    /// <returns>PNG bytes.</returns>
    public static byte[] ToPng(this MermaidFlowchartDocument document, MermaidFlowchartRenderOptions? options = null) {
        options ??= new MermaidFlowchartRenderOptions();
        return document.ToTopologyChart(options).ToPng(ToTopologyRenderOptions(options));
    }

    private static TopologyRenderOptions ToTopologyRenderOptions(MermaidFlowchartRenderOptions options) =>
        new TopologyRenderOptions { FitContentToViewport = options.HasExplicitViewportSize };

    private static string ResolveTitle(MermaidFlowchartDocument document, MermaidFlowchartRenderOptions options) =>
        MermaidHeading.Title(document, options.Title, null, document.Kind == MermaidDiagramKind.Swimlane ? "Mermaid swimlane" : document.Kind == MermaidDiagramKind.UseCase ? "Mermaid use case" : "Mermaid flowchart");

    private static TopologyLayoutDirection ToVisualLinkDirection(MermaidFlowchartDirection direction) {
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

    private static TopologyEdgeLineStyle ToLineStyle(string edgeOperator) {
        var prefixEnd = MermaidFlowchartOperators.ReadEnd(edgeOperator, 0);
        if (edgeOperator.IndexOf('.', 0, prefixEnd) >= 0) return TopologyEdgeLineStyle.Dotted;
        return TopologyEdgeLineStyle.Solid;
    }

    private static string? StyleValue(System.Collections.Generic.Dictionary<string, string> styles, string key) =>
        styles.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static void WriteStyleMetadata(System.Collections.Generic.Dictionary<string, string> metadata, System.Collections.Generic.Dictionary<string, string> styles, string prefix) {
        foreach (var item in styles) metadata[prefix + item.Key] = item.Value;
    }

    private static double ToNodeWidth(MermaidFlowchartNodeShape shape, string label, bool fitContent) {
        var shapeWidth = BaseNodeWidth(shape);
        if (!fitContent) return shapeWidth;
        var longestLine = label.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Max(line => line.Length);
        var labelWidth = 24 + longestLine * 7.1;
        if (shape == MermaidFlowchartNodeShape.Rhombus) labelWidth *= 1.5;
        return Clamp(Math.Max(shapeWidth, labelWidth), shapeWidth, 560);
    }

    private static double BaseNodeWidth(MermaidFlowchartNodeShape shape) {
        switch (shape) {
            case MermaidFlowchartNodeShape.Actor:
                return 110;
            case MermaidFlowchartNodeShape.Ellipse:
                return 160;
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

    private static void ApplyContentFittedViewport(
        TopologyChart chart,
        MermaidFlowchartDirection direction,
        MermaidFlowchartRenderOptions options) {
        if (chart.Nodes.Count == 0) return;
        var padding = options.Padding;
        var headerHeight = string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72;
        var layers = chart.Nodes
            .GroupBy(node => node.Metadata.TryGetValue("layer", out var value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var layer)
                    ? layer
                    : 0)
            .OrderBy(group => group.Key)
            .ToArray();
        var horizontal = direction is MermaidFlowchartDirection.LeftToRight or MermaidFlowchartDirection.RightToLeft;
        var corridors = RankSkippingCorridors(chart, layers);
        double requiredWidth;
        double requiredHeight;
        if (horizontal) {
            requiredWidth = padding * 2
                + layers.Sum(layer => layer.Max(node => node.Width))
                + Math.Max(0, layers.Length - 1) * 72;
            requiredHeight = padding * 2 + headerHeight
                + layers.Select((layer, rank) => layer.Sum(node => node.Height) + Math.Max(0, layer.Count() - 1) * 34 + corridors[rank] * CorridorExtent).Max();
        } else {
            requiredWidth = padding * 2
                + layers.Select((layer, rank) => layer.Sum(node => node.Width) + Math.Max(0, layer.Count() - 1) * 34 + corridors[rank] * CorridorExtent).Max();
            requiredHeight = padding * 2 + headerHeight
                + layers.Sum(layer => layer.Max(node => node.Height))
                + Math.Max(0, layers.Length - 1) * 58;
        }

        if (chart.Groups.Count > 0) {
            requiredWidth += horizontal ? chart.Groups.Count * 16 : 48;
            requiredHeight += horizontal ? 48 : chart.Groups.Count * 16;
        }

        chart.WithViewport(
            Clamp(requiredWidth, Math.Min(options.MinimumFittedWidth, options.MaximumFittedWidth), Math.Max(options.MinimumFittedWidth, options.MaximumFittedWidth)),
            Clamp(requiredHeight, Math.Min(options.MinimumFittedHeight, options.MaximumFittedHeight), Math.Max(options.MinimumFittedHeight, options.MaximumFittedHeight)),
            padding);
    }

    // Layered topology layout routes an edge that skips ranks through a reserved corridor beside each intermediate rank.
    private const double CorridorExtent = 84;

    private static int[] RankSkippingCorridors(TopologyChart chart, IReadOnlyList<IGrouping<int, TopologyNode>> layers) {
        var rankByNode = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var rank = 0; rank < layers.Count; rank++) {
            foreach (var node in layers[rank]) rankByNode[node.Id] = rank;
        }

        var corridors = new int[layers.Count];
        foreach (var edge in chart.Edges) {
            if (!rankByNode.TryGetValue(edge.SourceNodeId, out var source) || !rankByNode.TryGetValue(edge.TargetNodeId, out var target)) continue;
            for (var rank = Math.Min(source, target) + 1; rank < Math.Max(source, target); rank++) corridors[rank]++;
        }

        return corridors;
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));

    private static double ToNodeHeight(MermaidFlowchartNodeShape shape, string label) {
        // Cylinder captions occupy the body below its cap. Reserve both ordinary
        // native rows when a source caption explicitly requests a second line.
        if (shape == MermaidFlowchartNodeShape.Cylinder && label.IndexOf('\n') >= 0) return 82;
        switch (shape) {
            case MermaidFlowchartNodeShape.Actor: return 108;
            case MermaidFlowchartNodeShape.Ellipse: return 70;
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
    private double _minimumFittedWidth = 640;
    private double _minimumFittedHeight = 320;
    private double _maximumFittedWidth = 2800;
    private double _maximumFittedHeight = 1800;
    private bool _hasExplicitViewportSize;

    /// <summary>Gets or sets the rendered artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets an optional chart title override.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets an optional chart subtitle override.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets whether the viewport and node widths should fit the parsed graph content.</summary>
    public bool FitContent { get; set; } = true;

    /// <summary>Gets whether a caller explicitly assigned the viewport width or height.</summary>
    public bool HasExplicitViewportSize => _hasExplicitViewportSize;

    /// <summary>Gets or sets the topology viewport width.</summary>
    public double Width {
        get => _width;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Mermaid flowchart width must be finite and greater than zero.");
            _width = value;
            _hasExplicitViewportSize = true;
        }
    }

    /// <summary>Gets or sets the topology viewport height.</summary>
    public double Height {
        get => _height;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Mermaid flowchart height must be finite and greater than zero.");
            _height = value;
            _hasExplicitViewportSize = true;
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

    /// <summary>Gets or sets the minimum width used by content-fitted flowcharts.</summary>
    public double MinimumFittedWidth {
        get => _minimumFittedWidth;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Minimum fitted width must be finite and greater than zero.");
            _minimumFittedWidth = value;
        }
    }

    /// <summary>Gets or sets the minimum height used by content-fitted flowcharts.</summary>
    public double MinimumFittedHeight {
        get => _minimumFittedHeight;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Minimum fitted height must be finite and greater than zero.");
            _minimumFittedHeight = value;
        }
    }

    /// <summary>Gets or sets the maximum width used by content-fitted flowcharts.</summary>
    public double MaximumFittedWidth {
        get => _maximumFittedWidth;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum fitted width must be finite and greater than zero.");
            _maximumFittedWidth = value;
        }
    }

    /// <summary>Gets or sets the maximum height used by content-fitted flowcharts.</summary>
    public double MaximumFittedHeight {
        get => _maximumFittedHeight;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum fitted height must be finite and greater than zero.");
            _maximumFittedHeight = value;
        }
    }
}
