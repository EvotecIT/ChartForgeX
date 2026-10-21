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
        var output = new StringBuilder(value.Length);
        var depth = 0;
        for (var index = 0; index < value.Length; index++) {
            var character = value[index];
            if (character != '~') { output.Append(character); continue; }
            var opens = index > 0 && index + 1 < value.Length && char.IsLetterOrDigit(value[index - 1])
                && (char.IsLetterOrDigit(value[index + 1]) || value[index + 1] == '_');
            if (opens) { output.Append('<'); depth++; }
            else if (depth > 0) { output.Append('>'); depth--; }
            else output.Append(character);
        }
        return depth == 0 ? output.ToString() : value;
    }
}
