namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>Parses an inert native Agentflow document with the default captured source policy.</summary>
    public MermaidParseResult<MermaidAgentflowDocument> ParseAgentflow(string source) {
        var parsed = Parse(source);
        var result = new MermaidParseResult<MermaidAgentflowDocument>();
        result.Diagnostics.AddRange(parsed.Diagnostics);
        if (parsed.Document is MermaidAgentflowDocument document) result.Document = document;
        else if (!parsed.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not an Agentflow diagram.");
        return result;
    }

    private static MermaidAgentflowDocument ParseAgentflow(string source, string[] lines, FrontMatterResult frontMatter,
        HeaderLine header, DiagramDescriptor descriptor, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidAgentflowDocument {
            SourceText = source, Kind = MermaidDiagramKind.Agentflow, Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text, Direction = descriptor.Direction
        };
        MermaidAgentflowParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }
}
