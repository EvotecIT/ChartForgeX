using ChartForgeX.Mermaid;

namespace ChartForgeX.Markup.Mermaid;

/// <summary>
/// Provides per-diagram Mermaid rendering defaults for visual markup parsing.
/// </summary>
public sealed class MermaidVisualMarkupRenderOptions {
    /// <summary>
    /// Gets or sets default rendering options for Mermaid flowchart artifacts.
    /// </summary>
    public MermaidFlowchartRenderOptions? Flowchart { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid sequence artifacts.
    /// </summary>
    public MermaidSequenceRenderOptions? Sequence { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid pie artifacts.
    /// </summary>
    public MermaidPieRenderOptions? Pie { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid timeline artifacts.
    /// </summary>
    public MermaidTimelineRenderOptions? Timeline { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid XY chart artifacts.
    /// </summary>
    public MermaidXYChartRenderOptions? XYChart { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid Sankey artifacts.
    /// </summary>
    public MermaidSankeyRenderOptions? Sankey { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid radar artifacts.
    /// </summary>
    public MermaidRadarRenderOptions? Radar { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid treemap artifacts.
    /// </summary>
    public MermaidTreemapRenderOptions? Treemap { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid Gantt artifacts.
    /// </summary>
    public MermaidGanttRenderOptions? Gantt { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid class diagram artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? Class { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid state diagram artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? State { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid entity relationship artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? EntityRelationship { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid mindmap artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? MindMap { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid kanban artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? Kanban { get; set; }
}
