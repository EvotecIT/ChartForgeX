using System;
using System.Text;

namespace ChartForgeX.Terminal;

internal static class TerminalTextSanitizer {
    private const char Escape = '\u001B';
    private const char Bell = '\u0007';

    public static string Transcript(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        return Normalize(value, "    ");
    }

    public static string OneLine(string value, string label, string tabReplacement, bool allowEmpty) {
        if (value == null) throw new ArgumentNullException(label);
        var normalized = Normalize(value, tabReplacement);
        if (normalized.IndexOf('\r') >= 0 || normalized.IndexOf('\n') >= 0) throw new ArgumentException("Value must be one line.", label);
        if (!allowEmpty && string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("Value must not be empty.", label);
        return normalized;
    }

    private static string Normalize(string value, string tabReplacement) {
        var output = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length;) {
            var character = value[index];
            if (character == Escape) {
                index = SkipEscapeSequence(value, index);
                continue;
            }

            if (character == '\t') {
                output.Append(tabReplacement);
                index++;
                continue;
            }

            if (character == '\r' || character == '\n' || character >= ' ' && character <= '\uD7FF' && (character < '\u007F' || character > '\u009F') || character >= '\uE000' && character <= '\uFFFD') {
                output.Append(character);
                index++;
                continue;
            }

            if (char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1])) {
                output.Append(character);
                output.Append(value[index + 1]);
                index += 2;
                continue;
            }

            index++;
        }

        return output.ToString();
    }

    private static int SkipEscapeSequence(string value, int escapeIndex) {
        var index = escapeIndex + 1;
        if (index >= value.Length) return index;
        if (value[index] == '[') {
            index++;
            while (index < value.Length) {
                var character = value[index++];
                if (character >= '\u0040' && character <= '\u007E') break;
            }
            return index;
        }

        if (value[index] == ']') {
            index++;
            while (index < value.Length) {
                if (value[index] == Bell) return index + 1;
                if (value[index] == Escape && index + 1 < value.Length && value[index + 1] == '\\') return index + 2;
                index++;
            }
            return index;
        }

        return Math.Min(value.Length, index + 1);
    }
}
