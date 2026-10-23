namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides per-diagram Mermaid rendering defaults for source rendering and markup adapters.
/// </summary>
public sealed class MermaidRenderOptions {
    /// <summary>Gets or sets shared host presentation; explicit family fields take precedence.</summary>
    public MermaidPresentationOptions? Presentation { get; set; }

    /// <summary>Gets or sets whether source warnings prevent native artifact creation.</summary>
    public MermaidWarningPolicy WarningPolicy { get; set; }

    /// <summary>Gets or sets the source-resolution context used by source rendering and markup adapters.</summary>
    public MermaidParseOptions? ParseOptions { get; set; }

    /// <summary>Gets or sets minimum natural dimensions and presentation for all railroad frontends.</summary>
    public MermaidRailroadRenderOptions? Railroad { get; set; }

    /// <summary>Gets or sets rendering defaults for swimlane diagrams.</summary>
    public MermaidFlowchartRenderOptions? Swimlane { get; set; }
    /// <summary>Gets or sets rendering defaults for use case diagrams.</summary>
    public MermaidFlowchartRenderOptions? UseCase { get; set; }
    /// <summary>Gets or sets rendering defaults for Cynefin domain maps.</summary>
    public MermaidTopologyRenderOptions? Cynefin { get; set; }

    /// <summary>Gets or sets native static Agentflow presentation defaults.</summary>
    public MermaidTopologyRenderOptions? Agentflow { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid flowchart artifacts.
    /// </summary>
    public MermaidFlowchartRenderOptions? Flowchart { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid sequence artifacts.
    /// </summary>
    public MermaidSequenceRenderOptions? Sequence { get; set; }

    /// <summary>Gets or sets native sequence presentation for typed ZenUML occurrences.</summary>
    public MermaidSequenceRenderOptions? ZenUml { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid pie artifacts.
    /// </summary>
    public MermaidPieRenderOptions? Pie { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid journey artifacts.
    /// </summary>
    public MermaidJourneyRenderOptions? Journey { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid git graph artifacts.
    /// </summary>
    public MermaidGitGraphRenderOptions? GitGraph { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid timeline artifacts.
    /// </summary>
    public MermaidTimelineRenderOptions? Timeline { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid quadrant chart artifacts.
    /// </summary>
    public MermaidQuadrantRenderOptions? Quadrant { get; set; }

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
    /// Gets or sets default rendering options for Mermaid packet artifacts.
    /// </summary>
    public MermaidPacketRenderOptions? Packet { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid block artifacts.
    /// </summary>
    public MermaidBlockRenderOptions? Block { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid Venn artifacts.
    /// </summary>
    public MermaidVennRenderOptions? Venn { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid Ishikawa artifacts.
    /// </summary>
    public MermaidIshikawaRenderOptions? Ishikawa { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid Wardley map artifacts.
    /// </summary>
    public MermaidWardleyRenderOptions? Wardley { get; set; }

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
    /// Gets or sets default rendering options for Mermaid requirement artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? Requirement { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid architecture artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? Architecture { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid C4 artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? C4 { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid mindmap artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? MindMap { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid tree view artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? TreeView { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid Event Modeling artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? EventModeling { get; set; }

    /// <summary>
    /// Gets or sets default rendering options for Mermaid kanban artifacts.
    /// </summary>
    public MermaidTopologyRenderOptions? Kanban { get; set; }
    /// <summary>Captures an independent configuration for a host or long-lived markup parser.</summary>
    public MermaidRenderOptions Clone() => new() {
        Presentation = MermaidPresentationOptions.Merge(null, Presentation), WarningPolicy = WarningPolicy,
        ParseOptions = ParseOptions?.Clone(),
        Railroad = Railroad?.Snapshot<MermaidRailroadRenderOptions>(null),
            Swimlane = Swimlane?.Snapshot<MermaidFlowchartRenderOptions>(null),
            UseCase = UseCase?.Snapshot<MermaidFlowchartRenderOptions>(null),
            Cynefin = Cynefin?.Snapshot<MermaidTopologyRenderOptions>(null),
            Agentflow = Agentflow?.Snapshot<MermaidTopologyRenderOptions>(null),
            Flowchart = Flowchart?.Snapshot<MermaidFlowchartRenderOptions>(null),
            Sequence = Sequence?.Snapshot<MermaidSequenceRenderOptions>(null),
            ZenUml = ZenUml?.Snapshot<MermaidSequenceRenderOptions>(null),
            Pie = Pie?.Snapshot<MermaidPieRenderOptions>(null),
            Journey = Journey?.Snapshot<MermaidJourneyRenderOptions>(null),
            GitGraph = GitGraph?.Snapshot<MermaidGitGraphRenderOptions>(null),
            Timeline = Timeline?.Snapshot<MermaidTimelineRenderOptions>(null),
            Quadrant = Quadrant?.Snapshot<MermaidQuadrantRenderOptions>(null),
            XYChart = XYChart?.Snapshot<MermaidXYChartRenderOptions>(null),
            Sankey = Sankey?.Snapshot<MermaidSankeyRenderOptions>(null),
            Radar = Radar?.Snapshot<MermaidRadarRenderOptions>(null),
            Treemap = Treemap?.Snapshot<MermaidTreemapRenderOptions>(null),
            Gantt = Gantt?.Snapshot<MermaidGanttRenderOptions>(null),
            Packet = Packet?.Snapshot<MermaidPacketRenderOptions>(null),
            Block = Block?.Snapshot<MermaidBlockRenderOptions>(null),
            Venn = Venn?.Snapshot<MermaidVennRenderOptions>(null),
            Ishikawa = Ishikawa?.Snapshot<MermaidIshikawaRenderOptions>(null),
            Wardley = Wardley?.Snapshot<MermaidWardleyRenderOptions>(null),
            Class = Class?.Snapshot<MermaidTopologyRenderOptions>(null),
            State = State?.Snapshot<MermaidTopologyRenderOptions>(null),
            EntityRelationship = EntityRelationship?.Snapshot<MermaidTopologyRenderOptions>(null),
            Requirement = Requirement?.Snapshot<MermaidTopologyRenderOptions>(null),
            Architecture = Architecture?.Snapshot<MermaidTopologyRenderOptions>(null),
            C4 = C4?.Snapshot<MermaidTopologyRenderOptions>(null),
            MindMap = MindMap?.Snapshot<MermaidTopologyRenderOptions>(null),
            TreeView = TreeView?.Snapshot<MermaidTopologyRenderOptions>(null),
            EventModeling = EventModeling?.Snapshot<MermaidTopologyRenderOptions>(null),
            Kanban = Kanban?.Snapshot<MermaidTopologyRenderOptions>(null),
    };

    internal T Resolve<T>(T? family) where T : MermaidDiagramRenderOptions, new() =>
        (family ?? new T()).Snapshot<T>(Presentation);
}
