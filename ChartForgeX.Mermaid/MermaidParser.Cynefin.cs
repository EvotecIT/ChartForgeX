using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    /// <summary>Parses source as a source-preserving Cynefin domain document.</summary>
    public MermaidParseResult<MermaidCynefinDocument> ParseCynefin(string source) {
        var generic = Parse(source); var result = new MermaidParseResult<MermaidCynefinDocument>();
        foreach (var diagnostic in generic.Diagnostics) result.Diagnostics.Add(diagnostic);
        if (generic.Document is MermaidCynefinDocument document) result.Document = document;
        else if (!generic.HasErrors) Add(result, 1, 1, 0, MermaidDiagnosticSeverity.Error, "Mermaid source is not a Cynefin diagram.");
        return result;
    }

    // The pinned family grammar accepts adjacent quoted items and domain blocks, while a
    // transition/title must end at a physical newline or EOF. Raw source remains authoritative.
    private static MermaidCynefinDocument ParseCynefin(string source, string[] lines, FrontMatterResult frontMatter, HeaderLine header, MermaidParseResult<MermaidDocument> result) {
        var document = new MermaidCynefinDocument { SourceText = source, Kind = MermaidDiagramKind.Cynefin,
            Header = header.Text, HeaderSpan = new MermaidSourceSpan(header.Line, header.Column, header.Text.Length), FrontMatter = frontMatter.Text };
        const string keyword = "cynefin-beta";
        if (!header.Text.StartsWith(keyword, StringComparison.Ordinal) || header.Text.Length > keyword.Length &&
            header.Text[keyword.Length] != ':' && !char.IsWhiteSpace(header.Text[keyword.Length])) {
            MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Cynefin requires the case-sensitive cynefin-beta header.");
            return document;
        }
        var starts = MermaidParserUtilities.OriginalLineStarts(source);
        var offset = starts[header.Line - 1] + header.Column - 1 + keyword.Length;
        if (offset < source.Length && source[offset] == ':') offset++;
        var reader = new CynefinReader(source, lines, starts, offset, result);
        string? current = null;
        while (reader.Peek.Kind != CynefinTokenKind.End) {
            MermaidResourceGuard.Checkpoint();
            var token = reader.Take();
            if (token.Kind == CynefinTokenKind.Newline) continue;
            if (token.Kind == CynefinTokenKind.Word && token.Value == "title") {
                current = null;
                var title = reader.Title(token.Start);
                document.Title = Regex.Replace(title.Value.Trim(), "[\\t ]{2,}", " ", RegexOptions.CultureInvariant);
                Retain(title); continue;
            }
            if (token.Kind == CynefinTokenKind.Word && IsCynefinDomain(token.Value)) {
                if (reader.Peek.Kind != CynefinTokenKind.Arrow) {
                    current = token.Value;
                    // The reference database uses Map.set for each domain block, retaining
                    // first insertion order while replacing the prior block's item list.
                    document.Domains[current] = new List<string>(); Retain(token); continue;
                }
                current = null; reader.Take(); var target = reader.Take();
                if (target.Kind != CynefinTokenKind.Word || !IsCynefinDomain(target.Value)) { Error(target, "Cynefin transitions require a domain after '-->'."); reader.SkipLine(); continue; }
                string? label = null; var end = target.End;
                if (reader.Peek.Kind == CynefinTokenKind.Colon) {
                    reader.Take(); var text = reader.Take(); end = text.End;
                    if (text.Kind != CynefinTokenKind.Quoted) { Error(text, "Cynefin transition labels require a single or double quoted string."); reader.SkipLine(); continue; }
                    label = text.Value.Length == 0 ? null : text.Value;
                }
                if (reader.Peek.Kind != CynefinTokenKind.Newline && reader.Peek.Kind != CynefinTokenKind.End) { Error(reader.Take(), "Cynefin transitions must end at a newline or end of source."); reader.SkipLine(); continue; }
                var statement = new CynefinToken(CynefinTokenKind.Word, string.Empty, token.Start, end); Retain(statement);
                // Self loops are syntactically valid and retained as source, but the pinned
                // family database explicitly excludes them from rendered relationships.
                if (token.Value != target.Value) {
                    MermaidResourceGuard.ReserveEdges(1, reader.Span(statement));
                    document.Transitions.Add(new MermaidFlowchartEdge(token.Value, target.Value, "-->", reader.Span(statement)) { Label = label });
                }
                continue;
            }
            if (token.Kind == CynefinTokenKind.Quoted && current != null) { document.Domains[current].Add(token.Value); Retain(token); continue; }
            if (token.Kind != CynefinTokenKind.Invalid) Error(token, "Cynefin statements require a domain, quoted item, title, or domain transition.");
            current = null; reader.SkipLine();
        }
        return document;

        void Retain(CynefinToken token) {
            MermaidResourceGuard.ReserveElements(1, reader.Span(token));
            document.RawStatements.Add(new MermaidRawStatement(source.Substring(token.Start, token.End - token.Start), reader.Span(token)));
        }
        void Error(CynefinToken token, string message) => MermaidParserUtilities.Add(result, reader.Span(token), MermaidDiagnosticSeverity.Error, message);
    }

    private static bool IsCynefinDomain(string value) => value is "complex" or "complicated" or "chaotic" or "clear" or "confusion";

    // Shared configuration/accessibility scans must not consume literal lines inside a
    // family string. Title quotes are literal title characters, not string delimiters.
    private static HashSet<int> CynefinQuotedContinuationLines(string[] lines, int header) {
        var opaque = new HashSet<int>(); var quote = '\0';
        for (var line = header; line < lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            if (quote != '\0') opaque.Add(line);
            else if (line > header && SkipSharedDeclaration(lines, ref line)) continue;
            var text = lines[line]; var start = line == header ? text.IndexOf("cynefin-beta", StringComparison.Ordinal) + "cynefin-beta".Length : 0;
            for (var index = Math.Max(0, start); index < text.Length; index++) {
                var ch = text[index];
                if (quote != '\0') { if (ch == '\\') index++; else if (ch == quote) quote = '\0'; }
                else if (ch == '%' && index + 1 < text.Length && text[index + 1] == '%') break;
                else if (ch is '\'' or '"') quote = ch;
                else if (ch == 't' && (index == 0 || char.IsWhiteSpace(text[index - 1])) && text.Substring(index).StartsWith("title", StringComparison.Ordinal) &&
                    (index + 5 == text.Length || text[index + 5] is ' ' or '\t')) break;
            }
        }
        return opaque;
    }
}
