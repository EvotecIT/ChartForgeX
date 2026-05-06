using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Svg;

internal sealed class SvgStyleDeclarationList {
    private readonly List<SvgStyleDeclaration> _declarations = new();

    public SvgStyleDeclarationList() { }

    public SvgStyleDeclarationList(IEnumerable<SvgStyleDeclaration>? declarations) {
        if (declarations == null) return;
        foreach (var declaration in declarations) Set(declaration.Name, declaration.Value);
    }

    public IReadOnlyList<SvgStyleDeclaration> Declarations => _declarations;

    public bool IsEmpty => _declarations.Count == 0;

    public static SvgStyleDeclarationList Parse(string style) {
        if (style == null) throw new ArgumentNullException(nameof(style));

        var list = new SvgStyleDeclarationList();
        foreach (var declaration in SplitDeclarations(style)) {
            var separator = declaration.IndexOf(':');
            if (separator <= 0) throw new FormatException("SVG style declaration is missing a property name or ':'.");

            var name = declaration.Substring(0, separator);
            var value = declaration.Substring(separator + 1);
            list.Set(name, value);
        }

        return list;
    }

    public string? Get(string name) {
        for (var i = _declarations.Count - 1; i >= 0; i--) {
            if (string.Equals(_declarations[i].Name, name, StringComparison.Ordinal)) return _declarations[i].Value;
        }

        return null;
    }

    public SvgStyleDeclarationList Set(string name, string? value) {
        if (value == null) return Remove(name);
        var declaration = new SvgStyleDeclaration(name, value);
        for (var i = 0; i < _declarations.Count; i++) {
            if (!string.Equals(_declarations[i].Name, declaration.Name, StringComparison.Ordinal)) continue;
            _declarations[i] = declaration;
            return this;
        }

        _declarations.Add(declaration);
        return this;
    }

    public SvgStyleDeclarationList Remove(string name) {
        SvgStyleDeclaration.ValidateName(name);
        for (var i = 0; i < _declarations.Count; i++) {
            if (!string.Equals(_declarations[i].Name, name.Trim(), StringComparison.Ordinal)) continue;
            _declarations.RemoveAt(i);
            break;
        }

        return this;
    }

    public string ToMarkup() =>
        string.Join(";", _declarations.Select(declaration => declaration.ToMarkup()));

    public override string ToString() => ToMarkup();

    private static IEnumerable<string> SplitDeclarations(string style) {
        var start = 0;
        var depth = 0;
        var quote = '\0';
        for (var i = 0; i < style.Length; i++) {
            var ch = style[i];
            if (quote != '\0') {
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch == '\'' || ch == '"') {
                quote = ch;
                continue;
            }

            if (ch == '(') depth++;
            else if (ch == ')' && depth > 0) depth--;
            else if (ch == ';' && depth == 0) {
                var declaration = style.Substring(start, i - start).Trim();
                if (declaration.Length > 0) yield return declaration;
                start = i + 1;
            }
        }

        var tail = style.Substring(start).Trim();
        if (tail.Length > 0) yield return tail;
    }
}
