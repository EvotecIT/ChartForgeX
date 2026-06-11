namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>
    /// Parses Mermaid source as a Wardley map document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidWardleyDocument> ParseWardley(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidWardleyDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidWardleyDocument wardley) result.Document = wardley;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a Wardley map.");
        return result;
    }

    private static MermaidWardleyDocument ParseWardley(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidWardleyDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Wardley,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };
        MermaidWardleyParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }
}
