using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX.Typography;

namespace ChartForgeX.Mermaid;

/// <summary>Bounded safe emphasis lowering; it does not evaluate HTML, math, links, images or code.</summary>
internal static class MermaidInlineCaption {
    internal static TextContent Content(string source, bool markdownByDefault = false) {
        var wrapped = MermaidLabelText.IsMarkdown(source);
        if (!wrapped && !markdownByDefault) return TextContent.Plain(MermaidLabelText.DisplayCaption(source));
        var text = wrapped ? source.Substring(1, source.Length - 2) : source;
        text = Regex.Replace(text, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (Unsupported(text) || HasActiveNotation(text)) return TextContent.Plain(MermaidLabelText.DisplayCaption(source));
        var removed = new bool[text.Length]; var bold = new int[text.Length + 1]; var italic = new int[text.Length + 1];
        var buckets = new Dictionary<(char Marker, int Modulo, bool Closes), Stack<Entry>>(); var matched = false;
        for (var index = 0; index < text.Length;) {
            MermaidResourceGuard.Checkpoint();
            if (text[index] == '\\' && index + 1 < text.Length && IsEscapable(text[index + 1])) {
                removed[index] = true; index += 2; continue;
            }
            var marker = text[index];
            if (marker != '*' && marker != '_') { index++; continue; }
            var start = index; while (index < text.Length && text[index] == marker) index++;
            var count = index - start; var before = start == 0 ? '\n' : text[start - 1]; var after = index == text.Length ? '\n' : text[index];
            var left = !char.IsWhiteSpace(after) && (!Punctuation(after) || char.IsWhiteSpace(before) || Punctuation(before));
            var right = !char.IsWhiteSpace(before) && (!Punctuation(before) || char.IsWhiteSpace(after) || Punctuation(after));
            var opens = left && (marker != '_' || !right || Punctuation(before));
            var closes = right && (marker != '_' || !left || Punctuation(after));
            var remaining = count; var consumed = 0;
            while (closes && remaining > 0) {
                Delimiter? selected = null;
                for (var modulo = 0; modulo < 3; modulo++) foreach (var canClose in new[] { false, true }) {
                    var key = (marker, modulo, canClose);
                    if (!buckets.TryGetValue(key, out var stack)) continue;
                    while (stack.Count > 0 && (stack.Peek().Generation != stack.Peek().Value.Generation || stack.Peek().Value.Remaining == 0)) stack.Pop();
                    if (stack.Count == 0) continue;
                    var candidate = stack.Peek().Value;
                    if ((candidate.Closes || opens) && (candidate.Remaining + remaining) % 3 == 0
                        && (candidate.Remaining % 3 != 0 || remaining % 3 != 0)) continue;
                    if (selected == null || candidate.Start > selected.Start) selected = candidate;
                }
                if (selected == null) break;
                var used = selected.Remaining >= 2 && remaining >= 2 ? 2 : 1;
                var openAt = selected.Start + selected.Remaining - used; var closeAt = start + consumed;
                for (var offset = 0; offset < used; offset++) { removed[openAt + offset] = true; removed[closeAt + offset] = true; }
                var differences = used == 2 ? bold : italic;
                differences[openAt + used]++; differences[closeAt]--;
                selected.Remaining -= used; selected.Generation++; remaining -= used; consumed += used; matched = true;
                foreach (var bucket in buckets.Values)
                    while (bucket.Count > 0 && bucket.Peek().Value.Start > selected.Start) bucket.Pop();
                if (selected.Remaining > 0) Add(selected);
            }
            if (opens && remaining > 0) Add(new Delimiter(start + consumed, remaining, marker, closes));
        }
        var output = new List<TextContentRun>(); var current = new StringBuilder();
        var weight = 0; var slant = 0; var previous = TextInlineEmphasis.Normal;
        for (var index = 0; index < text.Length; index++) {
            weight += bold[index]; slant += italic[index]; if (removed[index]) continue;
            var emphasis = (weight > 0 ? TextInlineEmphasis.Bold : TextInlineEmphasis.Normal)
                | (slant > 0 ? TextInlineEmphasis.Italic : TextInlineEmphasis.Normal);
            if (current.Length > 0 && emphasis != previous) Flush();
            previous = emphasis; current.Append(text[index]);
        }
        Flush();
        if (!wrapped && !matched) return TextContent.Plain(MermaidLabelText.DisplayCaption(source));
        return TextContent.FromRuns(output);

        void Add(Delimiter delimiter) {
            var key = (delimiter.Marker, delimiter.Remaining % 3, delimiter.Closes);
            if (!buckets.TryGetValue(key, out var stack)) { stack = new Stack<Entry>(); buckets.Add(key, stack); }
            stack.Push(new Entry(delimiter, delimiter.Generation));
        }
        void Flush() {
            if (current.Length == 0) return;
            // Decode source entities only after delimiters were recognized, so encoded markup stays literal text.
            output.Add(new TextContentRun(MermaidLabelText.DecodeEntities(current.ToString()), previous)); current.Clear();
        }
    }
    private static bool HasActiveNotation(string text) => Regex.IsMatch(text, "(?<!<)</?[a-zA-Z][^>]*>(?!>)")
        || text.IndexOf("$$", StringComparison.Ordinal) >= 0;
    internal static void Diagnose(string source, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, string family, bool markdownByDefault = false) {
        var text = MermaidLabelText.IsMarkdown(source) ? source.Substring(1, source.Length - 2) : source;
        var withoutBreaks = Regex.Replace(text, "<br\\s*/?>", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (HasActiveNotation(withoutBreaks))
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                family + " HTML and math captions are retained and displayed as literal text.", MermaidDiagnosticCodes.UnsupportedStatement);
        if ((markdownByDefault || MermaidLabelText.IsMarkdown(source)) && Unsupported(text))
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                family + " code, links and images remain literal caption text; only native strong/emphasis and line breaks are supported.", MermaidDiagnosticCodes.UnsupportedStatement);
    }
    internal static bool Unsupported(string text) => text.IndexOf('`') >= 0 || text.IndexOf("](", StringComparison.Ordinal) >= 0
        || text.IndexOf("![", StringComparison.Ordinal) >= 0;
    private static bool IsEscapable(char character) => "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~".IndexOf(character) >= 0;
    private static bool Punctuation(char value) {
        var category = CharUnicodeInfo.GetUnicodeCategory(value);
        return category is UnicodeCategory.ConnectorPunctuation or UnicodeCategory.DashPunctuation or UnicodeCategory.OpenPunctuation
            or UnicodeCategory.ClosePunctuation or UnicodeCategory.InitialQuotePunctuation or UnicodeCategory.FinalQuotePunctuation
            or UnicodeCategory.OtherPunctuation or UnicodeCategory.MathSymbol or UnicodeCategory.CurrencySymbol
            or UnicodeCategory.ModifierSymbol or UnicodeCategory.OtherSymbol;
    }
    private sealed class Delimiter {
        internal Delimiter(int start, int remaining, char marker, bool closes) { Start = start; Remaining = remaining; Marker = marker; Closes = closes; }
        internal int Start { get; }
        internal int Remaining { get; set; }
        internal char Marker { get; }
        internal bool Closes { get; }
        internal int Generation { get; set; }
    }
    private readonly struct Entry {
        internal Entry(Delimiter value, int generation) { Value = value; Generation = generation; }
        internal Delimiter Value { get; }
        internal int Generation { get; }
    }
}
