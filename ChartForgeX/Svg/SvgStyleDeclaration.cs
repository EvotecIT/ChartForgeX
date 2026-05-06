using System;

namespace ChartForgeX.Svg;

internal readonly struct SvgStyleDeclaration {
    public SvgStyleDeclaration(string name, string value) {
        name = NormalizeName(name);
        ValidateName(name);
        Name = name;
        Value = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
    }

    public string Name { get; }

    public string Value { get; }

    public string ToMarkup() => Name + ":" + Value;

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
