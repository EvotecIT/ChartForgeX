using System.Threading;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>Parses a typed native ZenUML occurrence tree with captured source bounds.</summary>
    public MermaidParseResult<MermaidZenUmlDocument> ParseZenUml(string source) => ParseZenUml(source, null, default);

    /// <summary>Parses typed ZenUML occurrences with captured resource bounds and cancellation.</summary>
    public MermaidParseResult<MermaidZenUmlDocument> ParseZenUml(string source, MermaidParseOptions? options, CancellationToken cancellationToken = default) {
        var parsed = Parse(source, options, cancellationToken);
        var result = new MermaidParseResult<MermaidZenUmlDocument>();
        result.Diagnostics.AddRange(parsed.Diagnostics);
        if (parsed.Document is MermaidZenUmlDocument document) result.Document = document;
        else if (!parsed.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a ZenUML diagram.");
        return result;
    }

    private static MermaidZenUmlDocument ParseZenUml(string source, string[] lines, FrontMatterResult frontMatter,
        HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidZenUmlDocument {
            SourceText = source, Kind = MermaidDiagramKind.ZenUml, Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length), FrontMatter = frontMatter.Text
        };
        MermaidZenUmlParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }
}
