namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>
    /// Parses Mermaid source as a sequence diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidSequenceDocument> ParseSequence(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidSequenceDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidSequenceDocument sequence) result.Document = sequence;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a sequence diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a flowchart document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidFlowchartDocument> ParseFlowchart(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidFlowchartDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidFlowchartDocument flowchart) result.Document = flowchart;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a flowchart diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a pie chart document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidPieDocument> ParsePie(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidPieDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidPieDocument pie) result.Document = pie;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a pie chart diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a user journey document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidJourneyDocument> ParseJourney(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidJourneyDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidJourneyDocument journey) result.Document = journey;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a journey diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a class diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidClassDocument> ParseClass(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidClassDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidClassDocument classDiagram) result.Document = classDiagram;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a class diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a state diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidStateDocument> ParseState(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidStateDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidStateDocument state) result.Document = state;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a state diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as an entity relationship diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidEntityRelationshipDocument> ParseEntityRelationship(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidEntityRelationshipDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidEntityRelationshipDocument er) result.Document = er;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not an entity relationship diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a Gantt diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidGanttDocument> ParseGantt(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidGanttDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidGanttDocument gantt) result.Document = gantt;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a Gantt diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a timeline diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidTimelineDocument> ParseTimeline(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidTimelineDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidTimelineDocument timeline) result.Document = timeline;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a timeline diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a quadrant chart document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidQuadrantDocument> ParseQuadrant(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidQuadrantDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidQuadrantDocument quadrant) result.Document = quadrant;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a quadrant chart diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a requirement diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidRequirementDocument> ParseRequirement(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidRequirementDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidRequirementDocument requirement) result.Document = requirement;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a requirement diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a mindmap document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidMindMapDocument> ParseMindMap(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidMindMapDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidMindMapDocument mindMap) result.Document = mindMap;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a mindmap diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as an XY chart document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidXYChartDocument> ParseXYChart(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidXYChartDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidXYChartDocument xyChart) result.Document = xyChart;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not an XY chart diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a Sankey diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidSankeyDocument> ParseSankey(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidSankeyDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidSankeyDocument sankey) result.Document = sankey;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a Sankey diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a radar diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidRadarDocument> ParseRadar(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidRadarDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidRadarDocument radar) result.Document = radar;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a radar diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a treemap diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidTreemapDocument> ParseTreemap(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidTreemapDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidTreemapDocument treemap) result.Document = treemap;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a treemap diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as a kanban diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidKanbanDocument> ParseKanban(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidKanbanDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidKanbanDocument kanban) result.Document = kanban;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a kanban diagram.");
        return result;
    }

    /// <summary>
    /// Parses Mermaid source as an architecture diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidArchitectureDocument> ParseArchitecture(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidArchitectureDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidArchitectureDocument architecture) result.Document = architecture;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not an architecture diagram.");
        return result;
    }
}
