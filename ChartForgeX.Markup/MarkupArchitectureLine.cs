using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Markup;

/// <summary>One whitespace-separated token of an architecture markup line, with its position in the raw line.</summary>
internal readonly struct MarkupArchitectureToken {
    public MarkupArchitectureToken(string text, int start, bool quoted) { Text = text; Start = start; Quoted = quoted; }
    public string Text { get; }
    public int Start { get; }
    public bool Quoted { get; }
    public int Column => Start + 1;
}

/// <summary>Splits architecture markup lines into tokens while keeping quoted text and raw offsets.</summary>
internal static class MarkupArchitectureLine {
    /// <summary>Removes a <c>//</c> comment or a leading <c>#</c> comment outside quotes.</summary>
    public static string StripComment(string line) {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("#", StringComparison.Ordinal)) return string.Empty;
        var quoted = false;
        for (var i = 0; i < line.Length; i++) {
            if (quoted && line[i] == '\\') { i++; continue; }
            if (line[i] == '"') quoted = !quoted;
            if (quoted || line[i] != '/' || i + 1 >= line.Length || line[i + 1] != '/') continue;
            if (i == 0 || char.IsWhiteSpace(line[i - 1])) return line.Substring(0, i);
        }

        return line;
    }

    public static List<MarkupArchitectureToken> Tokenize(string line, out string? error) {
        error = null;
        var tokens = new List<MarkupArchitectureToken>();
        var i = 0;
        while (i < line.Length) {
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i >= line.Length) break;
            var start = i;
            if (line[i] == '"') {
                var text = new StringBuilder();
                i++;
                var closed = false;
                while (i < line.Length) {
                    if (line[i] == '\\' && i + 1 < line.Length && (line[i + 1] == '"' || line[i + 1] == '\\')) { text.Append(line[i + 1]); i += 2; continue; }
                    if (line[i] == '"') { closed = true; i++; break; }
                    text.Append(line[i++]);
                }

                if (!closed) error = "Quoted text that starts at column " + (start + 1) + " is not closed.";
                tokens.Add(new MarkupArchitectureToken(text.ToString(), start, true));
                continue;
            }

            var value = new StringBuilder();
            while (i < line.Length && !char.IsWhiteSpace(line[i])) {
                if (line[i] == '"') {
                    // key:"quoted value" keeps the quoted part in the same token.
                    i++;
                    while (i < line.Length && line[i] != '"') {
                        if (line[i] == '\\' && i + 1 < line.Length && (line[i + 1] == '"' || line[i + 1] == '\\')) { value.Append(line[i + 1]); i += 2; continue; }
                        value.Append(line[i++]);
                    }

                    if (i < line.Length) i++;
                    else error = "Quoted text that starts at column " + (start + 1) + " is not closed.";
                    continue;
                }

                value.Append(line[i++]);
            }

            tokens.Add(new MarkupArchitectureToken(value.ToString(), start, false));
        }

        return tokens;
    }

    /// <summary>Splits a <c>key:value</c> or <c>key=value</c> attribute token.</summary>
    public static bool TryAttribute(MarkupArchitectureToken token, out string key, out string value) {
        key = value = string.Empty;
        if (token.Quoted) return false;
        var text = token.Text;
        var separator = text.IndexOfAny(new[] { ':', '=' });
        if (separator <= 0) return false;
        key = VisualMarkupFenceOptions.NormalizeKey(text.Substring(0, separator));
        value = text.Substring(separator + 1);
        return key.Length > 0;
    }

    /// <summary>Returns the bracket balance of a JSON payload, ignoring brackets inside strings.</summary>
    public static int JsonBalance(string text, int balance) {
        var quoted = false;
        for (var i = 0; i < text.Length; i++) {
            var ch = text[i];
            if (quoted) {
                if (ch == '\\') { i++; continue; }
                if (ch == '"') quoted = false;
                continue;
            }

            if (ch == '"') quoted = true;
            else if (ch == '{' || ch == '[') balance++;
            else if (ch == '}' || ch == ']') balance--;
        }

        return balance;
    }
}
