namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>
    /// Parses Mermaid source as a packet diagram document.
    /// </summary>
    /// <param name="source">The Mermaid source text.</param>
    /// <returns>The parse result.</returns>
    public MermaidParseResult<MermaidPacketDocument> ParsePacket(string source) {
        var generic = Parse(source);
        var result = new MermaidParseResult<MermaidPacketDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidPacketDocument packet) result.Document = packet;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a packet diagram.");
        return result;
    }

    private static MermaidPacketDocument ParsePacket(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidPacketDocument {
            SourceText = source,
            Kind = MermaidDiagramKind.Packet,
            Header = header.Text,
            HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length),
            FrontMatter = frontMatter.Text
        };
        MermaidPacketParser.ParseStatements(document, lines, header.Line + 1, result);
        return document;
    }
}
