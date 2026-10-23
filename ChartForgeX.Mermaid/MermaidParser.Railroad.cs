using System.Threading;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>Parses any of the four supported railroad frontend headers.</summary>
    public MermaidParseResult<MermaidRailroadDocument> ParseRailroad(string source) => ParseRailroad(source, null, default);
    /// <summary>Parses railroad source with captured resource bounds and cooperative cancellation.</summary>
    public MermaidParseResult<MermaidRailroadDocument> ParseRailroad(string source, MermaidParseOptions? options, CancellationToken cancellationToken = default) {
        var generic = Parse(source, options, cancellationToken); var result = new MermaidParseResult<MermaidRailroadDocument>();
        result.Diagnostics.AddRange(generic.Diagnostics);
        if (generic.Document is MermaidRailroadDocument railroad) result.Document = railroad;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a railroad grammar.");
        return result;
    }
    private static MermaidRailroadDocument ParseRailroad(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var normalized = Normalize(header.Text);
        var document = new MermaidRailroadDocument {
            SourceText = source, Kind = MermaidDiagramKind.Railroad, Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length), FrontMatter = frontMatter.Text,
            Dialect = normalized.Contains("ebnf") ? MermaidRailroadDialect.Ebnf : normalized.Contains("abnf") ? MermaidRailroadDialect.Abnf
                : normalized.Contains("peg") ? MermaidRailroadDialect.Peg : MermaidRailroadDialect.IntermediateRepresentation
        };
        MermaidRailroadParser.ParseStatements(document, lines, header.Line + 1, result); return document;
    }
}
