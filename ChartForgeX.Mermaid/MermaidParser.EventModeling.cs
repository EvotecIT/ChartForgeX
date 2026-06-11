namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>
    /// Parses Mermaid source as an Event Modeling document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidEventModelingDocument> ParseEventModeling(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidEventModelingDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidEventModelingDocument eventModeling) result.Document = eventModeling;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not an Event Modeling diagram.");
        return result;
    }

    private static MermaidEventModelingDocument ParseEventModeling(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidEventModelingDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.EventModeling,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };
        MermaidEventModelingParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }
}
