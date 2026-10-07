using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Svg;

internal sealed class SvgMarkupWriter {
    private readonly StringBuilder _builder;
    private readonly Stack<string> _elements = new();
    private string? _pendingElement;

    public SvgMarkupWriter() : this(1024) { }

    public SvgMarkupWriter(int capacity) {
        _builder = new StringBuilder(Math.Max(16, capacity));
    }

    /// <summary>Writes straight into <paramref name="target"/>; finish with <see cref="Complete"/> instead of <see cref="Build"/>.</summary>
    public SvgMarkupWriter(StringBuilder target) {
        _builder = target ?? throw new ArgumentNullException(nameof(target));
    }

    public SvgMarkupWriter StartElement(string name) {
        EnsureNoPendingStartTag();
        ValidateName(name, nameof(name));
        _builder.Append('<').Append(name);
        _elements.Push(name);
        _pendingElement = name;
        return this;
    }

    public SvgMarkupWriter EndStartElement() {
        EnsurePendingStartTag();
        _builder.Append('>');
        _pendingElement = null;
        return this;
    }

    public SvgMarkupWriter EndEmptyElement() {
        EnsurePendingStartTag();
        _builder.Append("/>");
        _elements.Pop();
        _pendingElement = null;
        return this;
    }

    public SvgMarkupWriter EndElement() {
        EnsureOpenElement();
        var name = _elements.Pop();
        if (_pendingElement != null) {
            _builder.Append('>');
            _pendingElement = null;
        }

        _builder.Append("</").Append(name).Append('>');
        return this;
    }

    public SvgMarkupWriter Attribute(string name, string? value) {
        if (value == null) return this;
        AppendAttributeName(name);
        _builder.Append("=\"");
        AppendEscapedAttribute(value);
        _builder.Append('"');
        return this;
    }

    /// <summary>
    /// Writes a paint attribute. A typed paint is a token the renderer resolves against its colour variables when the
    /// SVG is finished (see <see cref="Themes.SvgPaint.Resolve"/>), so it is written without escaping.
    /// </summary>
    public SvgMarkupWriter Paint(string name, Themes.SvgPaint paint) {
        if (!paint.IsRaw || paint.Value == null) return Attribute(name, paint.Value);
        AppendAttributeName(name);
        _builder.Append("=\"").Append(paint.Value).Append('"');
        return this;
    }

    public SvgMarkupWriter Attribute(string name, double value) {
        EnsureFinite(value, nameof(value));
        AppendAttributeName(name);
        var isDataValue = name == "data-cfx-value" || name == "data-cfx-y" || name == "data-cfx-x" || name == "data-cfx-category" || name == "data-cfx-base";
        _builder.Append("=\"").Append(isDataValue ? value.ToString("R", CultureInfo.InvariantCulture) : FormatNumber(value)).Append('"');
        return this;
    }

    public SvgMarkupWriter Attribute(string name, int value) {
        AppendAttributeName(name);
        _builder.Append("=\"").Append(value.ToString(CultureInfo.InvariantCulture)).Append('"');
        return this;
    }

    public SvgMarkupWriter Attribute(string name, bool value) {
        AppendAttributeName(name);
        _builder.Append(value ? "=\"true\"" : "=\"false\"");
        return this;
    }

    public SvgMarkupWriter OptionalAttribute(string name, double? value) =>
        value.HasValue ? Attribute(name, value.Value) : this;

    public SvgMarkupWriter OptionalAttribute(string name, int? value) =>
        value.HasValue ? Attribute(name, value.Value) : this;

    public SvgMarkupWriter Text(string? value) {
        EnsureTextCanBeWritten();
        if (value != null) AppendEscapedText(value);
        return this;
    }

    public SvgMarkupWriter Raw(string? value) {
        EnsureTextCanBeWritten();
        if (value != null) _builder.Append(value);
        return this;
    }

    public SvgMarkupWriter Comment(string? value) {
        EnsureTextCanBeWritten();
        value ??= string.Empty;
        if (value.IndexOf("--", StringComparison.Ordinal) >= 0 || value.EndsWith("-", StringComparison.Ordinal)) {
            throw new ArgumentException("SVG comments cannot contain '--' or end with '-'.", nameof(value));
        }

        _builder.Append("<!--").Append(value).Append("-->");
        return this;
    }

    public SvgMarkupWriter Line() {
        EnsureNoPendingStartTag();
        _builder.AppendLine();
        return this;
    }

    public string Build() {
        Complete();
        return _builder.ToString();
    }

    /// <summary>Checks that every element is closed, as <see cref="Build"/> does, without copying the markup.</summary>
    public void Complete() {
        if (_pendingElement != null) {
            throw new InvalidOperationException("Cannot build SVG markup while a start tag is still open.");
        }

        if (_elements.Count != 0) {
            throw new InvalidOperationException("Cannot build SVG markup while elements are still open.");
        }
    }

    public override string ToString() => _builder.ToString();

    internal static string EscapeText(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var builder = new StringBuilder(value.Length);
        AppendEscapedText(builder, value, escapeQuotes: false);
        return builder.ToString();
    }

    internal static string EscapeAttribute(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var builder = new StringBuilder(value.Length);
        AppendEscapedText(builder, value, escapeQuotes: true);
        return builder.ToString();
    }

    internal static string FormatNumber(double value) {
        EnsureFinite(value, nameof(value));
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void EnsureTextCanBeWritten() {
        if (_pendingElement != null) EndStartElement();
    }

    private void AppendAttributeName(string name) {
        EnsurePendingStartTag();
        ValidateName(name, nameof(name));
        _builder.Append(' ').Append(name);
    }

    private void AppendEscapedText(string value) =>
        AppendEscapedText(_builder, value, escapeQuotes: false);

    private void AppendEscapedAttribute(string value) =>
        AppendEscapedText(_builder, value, escapeQuotes: true);

    private static void AppendEscapedText(StringBuilder builder, string value, bool escapeQuotes) {
        var run = 0;
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            // Characters written unchanged are copied in runs; every other character takes the cases below.
            if (ch >= 0x20 && ch < 0x7F ? ch != '&' && ch != '<' && ch != '>' && (ch != '"' || !escapeQuotes)
                : ch is '\t' or '\n' or '\r' || ch >= 0xA0 && ch < 0xD800 || ch >= 0xE000 && ch < 0xFDD0 || ch > 0xFDEF && ch < 0xFFFE) continue;
            builder.Append(value, run, i - run);
            run = i + 1;
            if (char.IsHighSurrogate(ch)) {
                if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) {
                    var next = value[i + 1];
                    if (IsMarkupScalar(char.ConvertToUtf32(ch, next))) {
                        builder.Append(ch).Append(next);
                    } else {
                        builder.Append('\uFFFD');
                    }
                    i++;
                    run = i + 1;
                } else {
                    builder.Append('\uFFFD');
                }
                continue;
            }
            if (!IsMarkupScalar(ch)) {
                builder.Append('\uFFFD');
                continue;
            }
            switch (ch) {
                case '&':
                    builder.Append("&amp;");
                    break;
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                case '"' when escapeQuotes:
                    builder.Append("&quot;");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        builder.Append(value, run, value.Length - run);
    }

    internal static bool IsMarkupScalar(int scalar) =>
        scalar == '\t' ||
        scalar == '\n' ||
        scalar == '\r' ||
        scalar >= 0x20 &&
        scalar <= 0x10FFFF &&
        !(scalar >= 0xD800 && scalar <= 0xDFFF) &&
        !(scalar >= 0x7F && scalar <= 0x9F) &&
        !(scalar >= 0xFDD0 && scalar <= 0xFDEF) &&
        (scalar & 0xFFFF) != 0xFFFE &&
        (scalar & 0xFFFF) != 0xFFFF;

    private void EnsurePendingStartTag() {
        if (_pendingElement == null) {
            throw new InvalidOperationException("Attributes can only be written while a start tag is open.");
        }
    }

    private void EnsureNoPendingStartTag() {
        if (_pendingElement != null) {
            throw new InvalidOperationException("Close the current start tag before writing another element.");
        }
    }

    private void EnsureOpenElement() {
        if (_elements.Count == 0) {
            throw new InvalidOperationException("No SVG element is currently open.");
        }
    }

    internal static void ValidateName(string name, string parameterName) {

        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("SVG element and attribute names cannot be empty.", parameterName);
        }

        if (!IsNameStart(name[0])) {
            throw new ArgumentException("SVG names must start with a letter, underscore, or colon.", parameterName);
        }

        for (var i = 1; i < name.Length; i++) {
            if (!IsNameCharacter(name[i])) {
                throw new ArgumentException("SVG names can only contain letters, digits, underscores, hyphens, periods, and colons.", parameterName);
            }
        }
    }

    private static bool IsNameStart(char ch) => char.IsLetter(ch) || ch == '_' || ch == ':';

    private static bool IsNameCharacter(char ch) =>
        IsNameStart(ch) || char.IsDigit(ch) || ch == '-' || ch == '.';

    private static void EnsureFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(parameterName, "SVG numeric values must be finite.");
        }
    }
}
