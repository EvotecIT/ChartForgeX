using System;

namespace ChartForgeX.Svg;

internal readonly struct SvgStyleDeclaration {
    public SvgStyleDeclaration(string name, string value, bool important = false) {
        name = NormalizeName(name);
        ValidateName(name);
        Name = name;
        Value = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
        Important = important;
    }

    public string Name { get; }

    public string Value { get; }

    public bool Important { get; }

    public string ToMarkup() => Name + ":" + Value + (Important ? " !important" : string.Empty);

    /// <summary>Separates a terminal CSS priority from its value, outside strings and functions.</summary>
    internal static SvgStyleDeclaration Parse(string name, string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        value = value.Trim();
        var bang = -1;
        var depth = 0;
        var quote = '\0';
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (ch == '\\') { i++; continue; }
            if (quote != '\0') { if (ch == quote) quote = '\0'; continue; }
            if (ch == '\'' || ch == '"') quote = ch;
            else if (ch == '(' || ch == '[' || ch == '{') depth++;
            else if ((ch == ')' || ch == ']' || ch == '}') && depth > 0) depth--;
            else if (ch == '!' && depth == 0) bang = i;
        }

        var important = bang >= 0 && string.Equals(value.Substring(bang + 1).Trim(), "important", StringComparison.OrdinalIgnoreCase);
        return new SvgStyleDeclaration(name, important ? value.Substring(0, bang).TrimEnd() : value, important);
    }

    public override string ToString() => ToMarkup();

    internal static void ValidateName(string name) {
        name = NormalizeName(name);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("SVG style property names cannot be empty.", nameof(name));
        for (var i = 0; i < name.Length; i++) {
            var ch = name[i];
            if (char.IsWhiteSpace(ch) || ch == ':' || ch == ';' || ch == '{' || ch == '}') {
                throw new ArgumentException("SVG style property names cannot contain whitespace, ':', ';', '{', or '}'.", nameof(name));
            }
        }
    }

    internal static string NormalizeName(string name) =>
        name?.Trim() ?? throw new ArgumentNullException(nameof(name));
}
