namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>
    /// Parses Mermaid source as a C4 diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidC4Document> ParseC4(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidC4Document>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidC4Document c4) result.Document = c4;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a C4 diagram.");
        return result;
    }

    private static MermaidC4Document ParseC4(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidC4Document {
            SourceText = source,
            Kind = MermaidDiagramKind.C4,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text,
            DiagramType = header.Text
        };
        MermaidC4Parser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }
}
