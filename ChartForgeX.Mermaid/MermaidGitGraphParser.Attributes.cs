using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGitGraphParser {
    /// <summary>Splits command boundaries without treating quoted IDs, messages or tags as statements.</summary>
    private static IEnumerable<MermaidRawStatement> SplitStatements(string raw, int line) {
        var text = MermaidParserUtilities.StripInlineComment(raw);
        var start = 0;
        var quote = '\0';
        var escaped = false;
        for (var index = 0; index <= text.Length; index++) {
            if (index < text.Length) {
                var current = text[index];
                if (quote != '\0') {
                    if (escaped) escaped = false;
                    else if (current == '\\') escaped = true;
                    else if (current == quote) quote = '\0';
                    continue;
                }
                if (current == '"' || current == '\'') { quote = current; continue; }
                if (current != ';') continue;
            }
            var fragment = text.Substring(start, index - start);
            var trimmed = fragment.Trim();
            if (!MermaidParserUtilities.IsSkippable(trimmed))
                yield return new MermaidRawStatement(trimmed, new MermaidSourceSpan(line, start + MermaidParserUtilities.LeadingWhitespace(fragment) + 1, trimmed.Length));
            start = index + 1;
        }
    }

    private static bool TryReadNameAndAttributes(string text, string command, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result, out string name, out Dictionary<string, string> attributes) {
        var position = 0;
        attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!TryReadValue(text, ref position, quotedOnly: false, out name) || name.Length == 0) {
            Invalid(result, span, "Mermaid gitGraph " + command + " statements require a branch name.");
            return false;
        }
        return TryReadAttributes(text, position, command, span, result, out attributes);
    }

    /// <summary>Consumes every command argument so invalid tokens cannot silently change into a valid graph.</summary>
    private static bool TryReadAttributes(string text, int position, string command, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result, out Dictionary<string, string> attributes) {
        attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        while (position < text.Length) {
            while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
            if (position == text.Length) break;
            var start = position;
            while (position < text.Length && char.IsLetter(text[position])) position++;
            var key = text.Substring(start, position - start);
            if (position == text.Length || text[position] != ':' || !SupportsAttribute(command, key)) {
                Invalid(result, span, "Invalid Mermaid gitGraph " + command + " argument: " + text.Substring(start) + ".");
                return false;
            }
            position++;
            while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
            var quotedValue = position < text.Length && (text[position] == '"' || text[position] == '\'');
            if ((key == "type" || key == "order") && quotedValue) {
                Invalid(result, span, "Mermaid gitGraph " + key + " requires an unquoted value.");
                return false;
            }
            if (!TryReadValue(text, ref position, quotedOnly: key != "type" && key != "order", out var value)) {
                Invalid(result, span, "Mermaid gitGraph " + key + " requires a complete " + (key == "type" || key == "order" ? "value." : "quoted string."));
                return false;
            }
            // The native block has one tag caption; retain every authored tag in that caption.
            attributes[key] = key == "tag" && attributes.TryGetValue(key, out var previous) && previous.Length > 0
                ? previous + ", " + value : value;
        }
        return true;
    }

    private static bool SupportsAttribute(string command, string key) => command switch {
        "branch" => key == "order",
        "commit" => key == "id" || key == "type" || key == "tag" || key == "msg",
        "merge" => key == "id" || key == "type" || key == "tag",
        "cherry-pick" => key == "id" || key == "tag" || key == "parent",
        _ => false
    };

    private static bool TryReadValue(string text, ref int position, bool quotedOnly, out string value) {
        value = string.Empty;
        while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
        if (position == text.Length) return false;
        var quote = text[position];
        if (quote == '"' || quote == '\'') {
            position++;
            var contents = new StringBuilder();
            while (position < text.Length) {
                var current = text[position++];
                if (current == quote) { value = contents.ToString(); return true; }
                if (current == '\\') {
                    if (position == text.Length) return false;
                    current = text[position++];
                    contents.Append(current switch {
                        'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', 'v' => '\v', '0' => '\0',
                        _ => current
                    });
                } else contents.Append(current);
            }
            return false;
        }
        if (quotedOnly) return false;
        var bareStart = position;
        while (position < text.Length && !char.IsWhiteSpace(text[position])) position++;
        value = text.Substring(bareStart, position - bareStart);
        return value.IndexOf('"') < 0 && value.IndexOf('\'') < 0 && value.IndexOf(':') < 0;
    }

    private static bool TryReadCommitType(Dictionary<string, string> attributes, GitGraphCommitType fallback,
        MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, out GitGraphCommitType type) {
        type = fallback;
        if (!attributes.TryGetValue("type", out var value)) return true;
        switch (value) {
            case "NORMAL": type = GitGraphCommitType.Normal; return true;
            case "HIGHLIGHT": type = GitGraphCommitType.Highlight; return true;
            case "REVERSE": type = GitGraphCommitType.Reverse; return true;
            default: Invalid(result, span, "Mermaid gitGraph commit type must be NORMAL, REVERSE or HIGHLIGHT."); return false;
        }
    }

    private static bool IsImmediateParent(MermaidGitGraphCommit commit, string id) {
        foreach (var parent in commit.ParentIds) if (string.Equals(parent, id, StringComparison.Ordinal)) return true;
        return false;
    }

    private static void Invalid(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, string message) =>
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
}
