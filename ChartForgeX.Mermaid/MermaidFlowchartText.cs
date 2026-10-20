namespace ChartForgeX.Mermaid;

/// <summary>Shares quoted-text boundaries between flowchart statement and label readers.</summary>
internal static class MermaidFlowchartText {
    // Mermaid's HTML line-break spelling is display text, never executable markup.
    // The document and node metadata retain the exact source caption.
    internal static string DisplayCaption(string source) {
        var text = source;
        if (IsMarkdown(text)) {
            text = text.Substring(1, text.Length - 2);
            // Native captions currently have one text style. Preserve readable content while
            // reporting the loss of Markdown emphasis at the parser boundary.
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<![\\\w])(\*\*|__)(?=\S)([^\r\n]+?)(?<=\S)\1(?!\w)", "$2");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<![\\\w])(\*|_)(?=\S)([^\r\n]+?)(?<=\S)\1(?!\w)", "$2");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\\([\\`*_{}\[\]()#+.!-])", "$1");
        }
        text = System.Text.RegularExpressions.Regex.Replace(text, "<br\\s*/?>", "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        // Mermaid's #name; and #decimal; escapes are text. Decode once, after recognizing
        // authored breaks, so encoded markup remains literal rather than becoming markup.
        text = System.Text.RegularExpressions.Regex.Replace(text, @"#([a-zA-Z]+|[0-9]+);", match =>
            "&" + (char.IsDigit(match.Groups[1].Value[0]) ? "#" : "") + match.Groups[1].Value + ";");
        return System.Net.WebUtility.HtmlDecode(text);
    }

    internal static bool IsMarkdown(string text) => text.Length >= 2 && text[0] == '`' && text[text.Length - 1] == '`';

    /// <summary>Maps an offset within a logical statement back to the original physical line.</summary>
    internal static MermaidSourceSpan AtOffset(string text, MermaidSourceSpan statement, int offset, int length) {
        var line = statement.Line;
        var column = statement.Column;
        for (var index = 0; index < offset; index++) {
            if (text[index] == '\n') { line++; column = 1; }
            else column++;
        }
        return new MermaidSourceSpan(line, column, length);
    }

    internal static bool ReadQuotedCharacter(string text, ref int index, ref char quote, bool metadata = false) {
        var ch = text[index];
        if (quote != '\0') {
            if (ch == quote) {
                if (metadata && quote == '\'' && index + 1 < text.Length && text[index + 1] == '\'') index++;
                else if (metadata && quote == '\'' || !IsEscaped(text, index)) quote = '\0';
            }
            return true;
        }
        if (ch == '"' || ch == '\'' && metadata && StartsMetadataScalar(text, index)) {
            quote = ch;
            return true;
        }
        return false;
    }

    private static bool StartsMetadataScalar(string text, int index) {
        while (index > 0 && char.IsWhiteSpace(text[index - 1])) index--;
        return index == 0 || text[index - 1] == ':' || text[index - 1] == '{' || text[index - 1] == ',';
    }

    internal static bool IsEscaped(string text, int index) {
        var slashes = 0;
        while (index > 0 && text[--index] == '\\') slashes++;
        return (slashes & 1) != 0;
    }
}
