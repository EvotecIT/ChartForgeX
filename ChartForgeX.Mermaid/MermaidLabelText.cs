using System;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

/// <summary>Shares safe native caption lowering; source text stays in family models and metadata.</summary>
internal static class MermaidLabelText {
    // Mermaid's HTML line-break spelling is display text, never executable markup.
    // The document and node metadata retain the exact source caption.
    internal static string DisplayCaption(string source) {
        var text = source;
        if (IsMarkdown(text)) {
            text = text.Substring(1, text.Length - 2);
            // This literal fallback is retained by families whose native captions still use one style.
            // Supported structured consumers lower through MermaidInlineCaption instead.
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<![\\\w])(\*\*|__)(?=\S)([^\r\n]+?)(?<=\S)\1(?!\w)", "$2");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<![\\\w])(\*|_)(?=\S)([^\r\n]+?)(?<=\S)\1(?!\w)", "$2");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\\([\\`*_{}\[\]()#+.!-])", "$1");
        }
        text = System.Text.RegularExpressions.Regex.Replace(text, "<br\\s*/?>", "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        // Mermaid's #name; and #decimal; escapes are text. Decode once, after recognizing
        // authored breaks, so encoded markup remains literal rather than becoming markup.
        return DecodeEntities(text);
    }

    internal static string DecodeEntities(string text) {
        text = System.Text.RegularExpressions.Regex.Replace(text, @"#([a-zA-Z]+|[0-9]+);", match =>
            "&" + (char.IsDigit(match.Groups[1].Value[0]) ? "#" : "") + match.Groups[1].Value + ";");
        return System.Net.WebUtility.HtmlDecode(text);
    }

    internal static bool IsMarkdown(string text) => text.Length >= 2 && text[0] == '`' && text[text.Length - 1] == '`';

    /// <summary>ER names support unwrapped Markdown; the other families keep their authored wrapper boundary.</summary>
    internal static string EntityCaption(string source) => DisplayCaption(IsMarkdown(source) ? source : "`" + source + "`");

    internal static void Diagnose(string? source, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, string family, bool entity = false) {
        if (source == null) return;
        if (IsMarkdown(source) || entity && EntityCaption(source) != DisplayCaption(source))
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                family + " Markdown emphasis renders as readable plain text with the native caption style.", MermaidDiagnosticCodes.ApproximateMapping);
        var withoutBreaks = Regex.Replace(source, "<br\\s*/?>", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (Regex.IsMatch(withoutBreaks, "(?<!<)</?[a-zA-Z][^>]*>(?!>)") || source.IndexOf("$$", StringComparison.Ordinal) >= 0)
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                family + " HTML and math captions are retained and displayed as literal text.", MermaidDiagnosticCodes.UnsupportedStatement);
    }
}
