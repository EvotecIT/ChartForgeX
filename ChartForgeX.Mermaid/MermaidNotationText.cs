using System;
using System.Text;

namespace ChartForgeX.Mermaid;

/// <summary>Normalizes the text forms shared by native class, state, and ER notation.</summary>
internal static class MermaidNotationText {
    internal static string Note(string value) => MermaidParserUtilities.Unquote(value.Trim()).Replace("\\n", "\n");

    internal static string ClassId(string value) {
        value = value.Trim().Trim('"', '`');
        var generic = value.IndexOf('~');
        return generic > 0 ? value.Substring(0, generic) : value;
    }

    internal static string Generics(string value) {
        if (value.IndexOf('~') < 0) return value;
        var output = new StringBuilder(value.Length); var quote = '\0';
        for (var index = 0; index < value.Length; index++) {
            if ((index & 255) == 0) MermaidResourceGuard.Checkpoint();
            var character = value[index];
            if (quote != '\0') {
                output.Append(character);
                if (character == quote && !MermaidFlowchartText.IsEscaped(value, index)) quote = '\0';
            } else if (character is '"' or '\'' or '`') { quote = character; output.Append(character); }
            else if (character == '~' && index > 0 && TypeCharacter(value[index - 1])) {
                var end = index;
                if (ReadArguments(value, ref end, 0, out var arguments, out _)) { output.Append(arguments); index = end - 1; }
                else output.Append(character);
            } else output.Append(character);
        }
        return output.ToString();
    }

    // Read only balanced type spans. A following member name or return type is outside
    // the span; quoted literals and leading package visibility '~' remain authored text.
    internal static bool TryGenericArguments(string text, ref int position, out int nestingDepth) =>
        ReadArguments(text, ref position, 0, out _, out nestingDepth);

    private static bool ReadArguments(string text, ref int position, int depth, out string normalized, out int nestingDepth) {
        normalized = string.Empty; nestingDepth = 1;
        if (depth >= 128) return false;
        var start = position; var output = new StringBuilder("<"); position++;
        while (true) {
            while (position < text.Length && char.IsWhiteSpace(text[position])) output.Append(text[position++]);
            var name = position;
            if (position < text.Length && text[position] == '?') { position = start; return false; }
            while (position < text.Length && TypeCharacter(text[position])) {
                if ((position & 255) == 0) MermaidResourceGuard.Checkpoint();
                output.Append(text[position++]);
            }
            if (position == name) { position = start; return false; }
            if (position < text.Length && text[position] == '~') {
                var nested = position;
                if (ReadArguments(text, ref nested, depth + 1, out var generic, out var nestedDepth)) {
                    output.Append(generic); position = nested; nestingDepth = Math.Max(nestingDepth, nestedDepth + 1);
                }
            }
            while (position < text.Length && char.IsWhiteSpace(text[position])) output.Append(text[position++]);
            if (position < text.Length && text[position] == '~') {
                position++; output.Append('>'); normalized = output.ToString(); return true;
            }
            if (position >= text.Length || text[position] != ',') { position = start; return false; }
            output.Append(text[position++]);
        }
    }

    private static bool TypeCharacter(char value) => char.IsLetterOrDigit(value) || value is '_' or '.' or '$' or '?';
}
