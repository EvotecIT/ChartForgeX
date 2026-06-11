namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>
    /// Parses Mermaid source as a git graph diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidGitGraphDocument> ParseGitGraph(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidGitGraphDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidGitGraphDocument gitGraph) result.Document = gitGraph;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a git graph diagram.");
        return result;
    }

    private static MermaidGitGraphDocument ParseGitGraph(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, DiagramDescriptor descriptor, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidGitGraphDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.GitGraph,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text,
            Direction = descriptor.Direction.TrimEnd(':')
        };
        MermaidGitGraphParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }
}
