namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>
    /// Parses Mermaid source as a block diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidBlockDocument> ParseBlock(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidBlockDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidBlockDocument block) result.Document = block;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a block diagram.");
        return result;
    }

    private static MermaidBlockDocument ParseBlock(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidBlockDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Block,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };
        MermaidBlockParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }
}
