namespace ChartForgeX.Mermaid;

/// <summary>Shares quoted-text boundaries between flowchart statement and label readers.</summary>
internal static class MermaidFlowchartText {
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
