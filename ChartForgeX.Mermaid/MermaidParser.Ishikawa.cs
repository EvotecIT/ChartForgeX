using System;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>
    /// Parses Mermaid source as an Ishikawa diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidIshikawaDocument> ParseIshikawa(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidIshikawaDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidIshikawaDocument ishikawa) result.Document = ishikawa;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not an Ishikawa diagram.");
        return result;
    }

    private static MermaidIshikawaDocument ParseIshikawa(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidIshikawaDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Ishikawa,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };

        MermaidIshikawaParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }
}
