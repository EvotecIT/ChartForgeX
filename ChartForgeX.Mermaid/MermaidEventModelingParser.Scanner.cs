using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidEventModelingParser {
    private static void ReadSuffix(MermaidEventModelingFrame frame, string text, MermaidParseResult<MermaidDocument> result) {
        var position = 0; var sources = new HashSet<string>(StringComparer.Ordinal); var extension = false;
        while (position < text.Length) {
            White(text, ref position); if (position == text.Length || At(text, position, "%%")) break;
            if (At(text, position, "->>")) {
                position += 3;
                while (true) {
                    White(text, ref position); var start = position;
                    while (position < text.Length && text[position] >= '0' && text[position] <= '9') position++;
                    var id = text.Substring(start, position - start);
                    if (!FrameId(id)) throw new ArgumentException("Every Event Modeling '->>' token requires a timeframe number.");
                    if (!sources.Add(id)) throw new ArgumentException("Event Modeling explicit source timeframe numbers must not repeat.");
                    MermaidResourceGuard.ReserveEdges(1, frame.Span); frame.ExplicitSources.Add(id);
                    White(text, ref position);
                    if (position < text.Length && text[position] == ',') { position++; extension = true; continue; }
                    if (position < text.Length && text[position] >= '0' && text[position] <= '9') { extension = true; continue; }
                    break;
                }
            } else if (At(text, position, "[[")) {
                if (frame.DataReference != null) throw new ArgumentException("Event Modeling frames accept one data reference.");
                var end = text.IndexOf("]]", position + 2, StringComparison.Ordinal);
                if (end < 0) throw new ArgumentException("Event Modeling data references must close with ']]'.");
                var id = text.Substring(position + 2, end - position - 2).Trim();
                if (!Identifier(id)) throw new ArgumentException("Event Modeling data references require a data-block identifier.");
                if (frame.InlineData != null) extension = true;
                frame.DataReference = id; position = end + 2;
            } else if (text[position] is '`' or '{') {
                if (frame.InlineData != null) throw new ArgumentException("Event Modeling frames accept one inline data body.");
                frame.DataType = ReadType(text, ref position); White(text, ref position);
                if (position >= text.Length || text[position] != '{') throw new ArgumentException("Event Modeling inline data requires a braced body.");
                var line = 1; var column = position;
                frame.InlineData = ReadBraced(new[] { text }, ref line, ref column, frame.Span);
                position = column;
            } else throw new ArgumentException("Unexpected or ambiguous Event Modeling timeframe suffix: " + text.Substring(position) + ".");
        }
        if (extension) MermaidParserUtilities.Add(result, frame.Span, MermaidDiagnosticSeverity.Warning,
            "Comma/space source lists and inline data before a reference are retained ChartForgeX extensions; maintained notation repeats '->> number' before data.", MermaidDiagnosticCodes.ApproximateMapping);
    }

    private static MermaidEventModelingDataBlock ReadDataBlock(string[] lines, ref int line, string text, MermaidSourceSpan span) {
        var position = 4; White(text, ref position); var start = position;
        while (position < text.Length && !char.IsWhiteSpace(text[position]) && text[position] is not ('`' or '{')) position++;
        var id = text.Substring(start, position - start);
        if (!Identifier(id)) throw new ArgumentException("Event Modeling data blocks require a plain identifier.");
        var type = ReadType(text, ref position); White(text, ref position);
        if (position >= text.Length || text[position] != '{') throw new ArgumentException("Event Modeling data blocks require a braced body.");
        // The source line can be indented; only the first line has been trimmed for declaration parsing.
        var rawPosition = MermaidParserUtilities.LeadingWhitespace(lines[line - 1]) + position;
        var initial = line; var content = ReadBraced(lines, ref line, ref rawPosition, span);
        var after = lines[line - 1].Substring(rawPosition).Trim();
        if (after.Length > 0 && !after.StartsWith("%%", StringComparison.Ordinal)) throw new ArgumentException("Unexpected text after an Event Modeling data body.");
        var length = initial == line ? rawPosition - span.Column + 1 : content.Length + text.Substring(0, position + 1).Length + 1;
        return new MermaidEventModelingDataBlock(id, type, content, new MermaidSourceSpan(span.Line, span.Column, length));
    }

    private static string ReadBraced(string[] lines, ref int line, ref int position, MermaidSourceSpan origin) =>
        MermaidBracedDataReader.Read(lines, ref line, ref position, origin, "Event Modeling");

    internal static HashSet<int> OpaqueDataLines(string[] lines, int firstLine) {
        var result = new HashSet<int>();
        for (var index = firstLine; index < lines.Length; index++) {
            MermaidResourceGuard.Checkpoint(); var text = lines[index].Trim();
            if (MermaidParser.SkipSharedDeclaration(lines, ref index)) continue;
            if (!Keyword(text, "data")) continue;
            var line = index + 1;
            try { ReadDataBlock(lines, ref line, text, new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(lines[index]) + 1, text.Length)); }
            catch (ArgumentException) { }
            for (var opaque = index; opaque < line; opaque++) result.Add(opaque);
            index = line - 1;
        }
        return result;
    }

    private static string? ReadType(string text, ref int position) {
        White(text, ref position); if (position >= text.Length || text[position] != '`') return null;
        var end = text.IndexOf('`', position + 1);
        if (end <= position + 1) throw new ArgumentException("Event Modeling data type markers must close their backtick.");
        var type = text.Substring(position + 1, end - position - 1); position = end + 1; return type;
    }
    private static List<string> SplitCommand(string text, int count) {
        var result = new List<string>(); var position = 0;
        while (result.Count < count && position < text.Length) {
            White(text, ref position); if (position == text.Length) break; var start = position;
            while (position < text.Length && !char.IsWhiteSpace(text[position])) position++;
            result.Add(text.Substring(start, position - start));
        }
        White(text, ref position); if (position < text.Length) result.Add(text.Substring(position)); return result;
    }
    private static void White(string text, ref int position) { while (position < text.Length && char.IsWhiteSpace(text[position])) position++; }
    private static bool At(string text, int position, string token) => position + token.Length <= text.Length && string.CompareOrdinal(text, position, token, 0, token.Length) == 0;
}
