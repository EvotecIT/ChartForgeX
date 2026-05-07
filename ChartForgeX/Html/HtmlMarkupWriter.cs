using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Html;

internal sealed class HtmlMarkupWriter {
    private readonly StringBuilder _builder;
    private readonly Stack<string> _elements = new();
    private string? _pendingElement;

    public HtmlMarkupWriter() : this(1024) { }

    public HtmlMarkupWriter(int capacity) {
        _builder = new StringBuilder(Math.Max(16, capacity));
    }

    public HtmlMarkupWriter Doctype() {
        EnsureNoPendingStartTag();
        _builder.Append("<!doctype html>");
        return this;
    }

    public HtmlMarkupWriter StartElement(string name) {
        EnsureNoPendingStartTag();
        ValidateName(name, nameof(name));
        _builder.Append('<').Append(name);
        _elements.Push(name);
        _pendingElement = name;
        return this;
    }

    public HtmlMarkupWriter EndStartElement() {
        EnsurePendingStartTag();
        _builder.Append('>');
        _pendingElement = null;
        return this;
    }

    public HtmlMarkupWriter EndVoidElement() {
        EnsurePendingStartTag();
        _builder.Append('>');
        _elements.Pop();
        _pendingElement = null;
        return this;
    }

    public HtmlMarkupWriter EndElement() {
        EnsureOpenElement();
        var name = _elements.Pop();
        if (_pendingElement != null) {
            _builder.Append('>');
            _pendingElement = null;
        }

        _builder.Append("</").Append(name).Append('>');
        return this;
    }

    public HtmlMarkupWriter Attribute(string name, string? value) {
        if (value == null) return this;
        AppendAttributeName(name);
        _builder.Append("=\"");
        AppendEscapedAttribute(value);
        _builder.Append('"');
        return this;
    }

    public HtmlMarkupWriter Attribute(string name, int value) {
        AppendAttributeName(name);
        _builder.Append("=\"").Append(value.ToString(CultureInfo.InvariantCulture)).Append('"');
        return this;
    }

    public HtmlMarkupWriter Attribute(string name, double value) {
        EnsureFinite(value, nameof(value));
        AppendAttributeName(name);
        _builder.Append("=\"").Append(FormatNumber(value)).Append('"');
        return this;
    }

    public HtmlMarkupWriter Attribute(string name, bool value) {
        AppendAttributeName(name);
        _builder.Append(value ? "=\"true\"" : "=\"false\"");
        return this;
    }

    public HtmlMarkupWriter Attribute(string name, bool? value) =>
        value.HasValue ? Attribute(name, value.Value) : this;

    public HtmlMarkupWriter BooleanAttribute(string name, bool enabled = true) {
        if (!enabled) return this;
        AppendAttributeName(name);
        return this;
    }

    public HtmlMarkupWriter Text(string? value) {
        EnsureTextCanBeWritten();
        if (value != null) AppendEscapedText(value, escapeQuotes: true);
        return this;
    }

    public HtmlMarkupWriter RawTrusted(string? value) {
        EnsureTextCanBeWritten();
        if (value != null) _builder.Append(value);
        return this;
    }

    public HtmlMarkupWriter Line() {
        EnsureNoPendingStartTag();
        _builder.AppendLine();
        return this;
    }

    public string Build() {
        if (_pendingElement != null) {
            throw new InvalidOperationException("Cannot build HTML markup while a start tag is still open.");
        }

        if (_elements.Count != 0) {
            throw new InvalidOperationException("Cannot build HTML markup while elements are still open.");
        }

        return _builder.ToString();
    }

    public override string ToString() => _builder.ToString();

    internal static string EscapeText(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var builder = new StringBuilder(value.Length);
        AppendEscapedText(builder, value, escapeQuotes: true);
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

    internal static void ValidateName(string name, string parameterName) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("HTML element and attribute names cannot be empty.", parameterName);
        }

        if (!IsNameStart(name[0])) {
            throw new ArgumentException("HTML names must start with a letter, underscore, or colon.", parameterName);
        }

        for (var i = 1; i < name.Length; i++) {
            if (!IsNameCharacter(name[i])) {
                throw new ArgumentException("HTML names can only contain letters, digits, underscores, hyphens, periods, and colons.", parameterName);
            }
        }
    }

    private void EnsureTextCanBeWritten() {
        if (_pendingElement != null) EndStartElement();
    }

    private void AppendAttributeName(string name) {
        EnsurePendingStartTag();
        ValidateName(name, nameof(name));
        _builder.Append(' ').Append(name);
    }

    private void AppendEscapedText(string value, bool escapeQuotes) =>
        AppendEscapedText(_builder, value, escapeQuotes);

    private void AppendEscapedAttribute(string value) =>
        AppendEscapedText(_builder, value, escapeQuotes: true);

    private static void AppendEscapedText(StringBuilder builder, string value, bool escapeQuotes) {
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
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
    }

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
            throw new InvalidOperationException("No HTML element is currently open.");
        }
    }

    private static bool IsNameStart(char ch) => char.IsLetter(ch) || ch == '_' || ch == ':';

    private static bool IsNameCharacter(char ch) =>
        IsNameStart(ch) || char.IsDigit(ch) || ch == '-' || ch == '.';

    private static void EnsureFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(parameterName, "HTML numeric values must be finite.");
        }
    }
}
