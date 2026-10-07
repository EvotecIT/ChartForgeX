using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ChartForgeX.Rendering;

/// <summary>
/// Parses rendered SVG into a <see cref="SvgMarkupDocument"/> holding the tree <c>XDocument.Load(reader,
/// LoadOptions.PreserveWhitespace)</c> builds: line ends and attribute white space normalized, character and entity
/// references decoded, empty elements told apart from elements written with an end tag.
/// </summary>
/// <remarks>
/// The direct parser takes well-formed markup with the five predefined entities, character references, comments on one
/// line and unambiguous namespace prefixes. Anything else (a document type, processing instructions, multi-line comments
/// or CDATA, a namespace bound to two prefixes, malformed or too deeply nested markup) goes through the framework: the
/// framework reader accepts or rejects it exactly as before, the framework writer normalizes it, and that normalized
/// text, whose names and node forms are already the writer's own, is parsed as it is.
/// </remarks>
internal sealed class SvgMarkupParser {
    private const string LineFeed = "\n";
    private readonly string _text;
    private readonly bool _normalized;
    private readonly StringBuilder _buffer = new();
    private readonly List<(string Name, string Value)> _attributes = new();
    private string[] _names = new string[256];
    private int _nameCount;
    private int _position;

    private SvgMarkupParser(string text, bool normalized) {
        _text = text;
        _normalized = normalized;
    }

    /// <summary>Parses rendered markup, through the framework when the direct parser does not take it.</summary>
    internal static SvgMarkupDocument Parse(string markup) {
        var document = new SvgMarkupParser(markup, normalized: false).Read();
        if (document != null) return document;
        // The framework accepts or rejects the markup as before; its own serialization is then taken as it is.
        using var source = new StringReader(markup);
        using var reader = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var loaded = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        var normalized = loaded.ToString(SaveOptions.DisableFormatting);
        var read = new SvgMarkupParser(normalized, normalized: true).Read()
            ?? throw new InvalidOperationException("The framework's serialization of the markup could not be read back.");
        // The writer writes a carriage return in text (&#13; in the source) as a line end, which reads back as a line
        // feed; the framework's own text values are restored so the tree holds the same text, not only the same markup.
        RestoreCarriageReturns(loaded.Root!, read.Root);
        return read;
    }

    private static void RestoreCarriageReturns(XElement source, SvgMarkupElement target) {
        var nodes = target.Nodes;
        var index = 0;
        foreach (var node in source.Nodes()) {
            // The two trees correspond node for node; should they not, the read-back values are kept.
            if (index >= nodes.Count) return;
            switch (node) {
                case XElement element when nodes[index] is SvgMarkupElement child:
                    RestoreCarriageReturns(element, child);
                    break;
                case XCData:
                    break;
                case XText text when nodes[index] is SvgMarkupText { IsCData: false } && text.Value.IndexOf('\r') >= 0:
                    target.ReplaceNode(index, new SvgMarkupText(text.Value));
                    break;
            }

            index++;
        }
    }

    /// <summary>Parses markup with the direct parser only; null when it would go through the framework.</summary>
    internal static SvgMarkupDocument? TryParseDirect(string markup) => new SvgMarkupParser(markup, normalized: false).Read();

    private SvgMarkupDocument? Read() {
        var nodes = new List<SvgMarkupNode>();
        if (!ReadOutsideRoot(nodes)) return null;
        if (_position >= _text.Length || _text[_position] != '<') return null;
        var root = ReadElement(null, null, 1);
        if (root == null) return null;
        nodes.Add(root);
        if (!ReadOutsideRoot(nodes) || _position != _text.Length) return null;
        return new SvgMarkupDocument(nodes, root);
    }

    // White space and comments before and after the root become document nodes, as in the framework's tree.
    private bool ReadOutsideRoot(List<SvgMarkupNode> nodes) {
        while (_position < _text.Length) {
            var start = _position;
            while (_position < _text.Length && IsWhiteSpace(_text[_position])) _position++;
            if (_position > start) {
                var text = NormalizeLineEnds(start, _position);
                nodes.Add(ReferenceEquals(text, LineFeed) ? SvgMarkupText.LineFeed : new SvgMarkupText(text));
            }
            if (_position + 3 < _text.Length && string.CompareOrdinal(_text, _position, "<!--", 0, 4) == 0) {
                var comment = ReadComment();
                if (comment == null) return false;
                nodes.Add(comment);
                continue;
            }

            if (_normalized && _position + 1 < _text.Length && _text[_position] == '<' && _text[_position + 1] == '?') {
                var instruction = ReadInstruction();
                if (instruction == null) return false;
                nodes.Add(instruction);
                continue;
            }

            break;
        }

        return true;
    }

    private SvgMarkupElement? ReadElement(Dictionary<string, string>? inheritedPrefixes, string? inheritedDefault, int depth) {
        // Nesting deeper than the scene accepts is left to the framework path, which reports it.
        // Normalized markup is already known to be well formed: report the depth as the scene's own check would.
        if (depth > SvgRaster.SvgRasterParser.MaximumElementDepth) {
            if (_normalized) throw new FormatException("SVG element nesting exceeds the supported depth of " + SvgRaster.SvgRasterParser.MaximumElementDepth + ".");
            return null;
        }
        _position++;
        var nameStart = _position;
        if (!ReadName()) return null;
        var name = Name(nameStart, _position - nameStart);
        var attributes = _attributes;
        attributes.Clear();
        var selfClosing = false;
        while (true) {
            var hadSpace = SkipWhiteSpace();
            if (_position >= _text.Length) return null;
            var c = _text[_position];
            if (c == '>') { _position++; break; }
            if (c == '/') {
                if (_position + 1 >= _text.Length || _text[_position + 1] != '>') return null;
                _position += 2;
                selfClosing = true;
                break;
            }

            if (!hadSpace) return null;
            var attributeStart = _position;
            if (!ReadName()) return null;
            var attributeName = Name(attributeStart, _position - attributeStart);
            SkipWhiteSpace();
            if (_position >= _text.Length || _text[_position] != '=') return null;
            _position++;
            SkipWhiteSpace();
            var value = ReadAttributeValue();
            if (value == null) return null;
            for (var i = 0; i < attributes.Count; i++) {
                if (string.Equals(attributes[i].Name, attributeName, StringComparison.Ordinal)) return null;
            }

            attributes.Add((attributeName, value));
        }

        // Namespace declarations of this element; a prefix must resolve and a namespace must have a single prefix (or
        // be the default) in scope, so the names are exactly the ones the framework writer would choose.
        var prefixes = inheritedPrefixes;
        var defaultNamespace = inheritedDefault;
        var declares = false;
        foreach (var (attributeName, value) in attributes) {
            // Values the framework reader checks (xml: attributes, reserved namespace names) are left to it.
            if (!_normalized && attributeName.StartsWith("xml", StringComparison.Ordinal) && !AcceptedReservedAttribute(attributeName, value)) return null;
            if (attributeName == "xmlns") {
                defaultNamespace = value;
                declares = true;
            } else if (attributeName.StartsWith("xmlns:", StringComparison.Ordinal)) {
                declares = true;
                var prefix = attributeName.Substring(6);
                if (value.Length == 0 || prefix is "xml" or "xmlns" || value is "http://www.w3.org/XML/1998/namespace" or "http://www.w3.org/2000/xmlns/") return null;
                prefixes = prefixes == null ? new Dictionary<string, string>(StringComparer.Ordinal) : new Dictionary<string, string>(prefixes, StringComparer.Ordinal);
                prefixes[prefix] = value;
            }
        }

        // Normalized markup was written by the framework, so its names are the writer's own and are not checked again.
        if (!_normalized) {
            if (declares && prefixes != null && !Unambiguous(prefixes, defaultNamespace)) return null;
            if (!Resolves(name, prefixes)) return null;
        }

        var element = new SvgMarkupElement(name, attributes.Count);
        foreach (var (attributeName, value) in attributes) {
            if (!_normalized && attributeName.IndexOf(':') > 0 && !attributeName.StartsWith("xmlns:", StringComparison.Ordinal)
                && !attributeName.StartsWith("xml:", StringComparison.Ordinal) && !Resolves(attributeName, prefixes)) return null;
            element.AddAttribute(attributeName, value);
        }

        if (selfClosing) return element;
        element.KeepEndTag();
        while (true) {
            if (_position >= _text.Length) return null;
            if (_text[_position] != '<') {
                var text = ReadText();
                if (text == null) return null;
                element.Add(ReferenceEquals(text, LineFeed) ? SvgMarkupText.LineFeed : new SvgMarkupText(text));
                continue;
            }

            if (_position + 1 >= _text.Length) return null;
            var next = _text[_position + 1];
            if (next == '/') {
                _position += 2;
                var endStart = _position;
                if (!ReadName()) return null;
                if (_position - endStart != name.Length || string.CompareOrdinal(_text, endStart, name, 0, name.Length) != 0) return null;
                SkipWhiteSpace();
                if (_position >= _text.Length || _text[_position] != '>') return null;
                _position++;
                return element;
            }

            if (next == '!') {
                SvgMarkupNode? node = _position + 3 < _text.Length && string.CompareOrdinal(_text, _position, "<!--", 0, 4) == 0 ? ReadComment()
                    : _position + 8 < _text.Length && string.CompareOrdinal(_text, _position, "<![CDATA[", 0, 9) == 0 ? ReadCData() : null;
                if (node == null) return null;
                element.Add(node);
                continue;
            }

            if (next == '?') {
                if (!_normalized) return null;
                var instruction = ReadInstruction();
                if (instruction == null) return null;
                element.Add(instruction);
                continue;
            }

            var child = ReadElement(prefixes, defaultNamespace, depth + 1);
            if (child == null) return null;
            element.Add(child);
        }
    }

    // The reserved attributes the direct parser takes: namespace declarations (a default namespace that is not one of the
    // reserved namespace names; prefixed declarations are checked above) and xml:space with one of its two values.
    private static bool AcceptedReservedAttribute(string name, string value) {
        if (name == "xmlns") return value is not ("http://www.w3.org/XML/1998/namespace" or "http://www.w3.org/2000/xmlns/");
        if (name.StartsWith("xmlns:", StringComparison.Ordinal)) return true;
        if (name == "xml:space") return value is "preserve" or "default";
        // Other names starting with "xml" (xml:lang, xmlfoo) are rare in rendered markup.
        return false;
    }

    private static bool Resolves(string name, Dictionary<string, string>? prefixes) {
        var colon = name.IndexOf(':');
        return colon < 0 || prefixes != null && prefixes.ContainsKey(name.Substring(0, colon));
    }

    private static bool Unambiguous(Dictionary<string, string> prefixes, string? defaultNamespace) {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(defaultNamespace)) seen.Add(defaultNamespace!);
        foreach (var uri in prefixes.Values) {
            if (!seen.Add(uri)) return false;
        }

        return true;
    }

    // Qualified XML names; non-ASCII name characters are checked with the framework's NCName rules.
    // Name characters above U+FFFF (surrogate pairs) are left to the framework reader; in normalized markup, which that
    // reader has already accepted, they are taken as they are.
    private bool ReadName() {
        if (_position >= _text.Length) return false;
        if (SurrogatePair(_position)) {
            if (!_normalized) return false;
            _position += 2;
        } else {
            if (!IsNameStart(_text[_position])) return false;
            _position++;
        }

        var colons = 0;
        while (_position < _text.Length) {
            var c = _text[_position];
            if (c == ':') {
                if (++colons > 1 || _position + 1 >= _text.Length || !(IsNameStart(_text[_position + 1]) || _normalized && SurrogatePair(_position + 1))) return false;
                _position++;
                continue;
            }

            if (SurrogatePair(_position)) {
                if (!_normalized) return false;
                _position += 2;
                continue;
            }

            if (!IsNameChar(c)) break;
            _position++;
        }

        return _position < _text.Length && (IsWhiteSpace(_text[_position]) || _text[_position] is '>' or '/' or '=');
    }

    private string? ReadAttributeValue() {
        if (_position >= _text.Length) return null;
        var quote = _text[_position];
        if (quote != '"' && quote != '\'') return null;
        _position++;
        // Most values need no decoding or normalization and are taken as they are.
        for (var end = _position; end < _text.Length; end++) {
            var c = _text[end];
            if (c == quote) {
                var plain = _text.Substring(_position, end - _position);
                _position = end + 1;
                return plain;
            }

            if (c < 0x20 || c == '&' || c == '<' || c >= 0xD800 && (c <= 0xDFFF || c >= 0xFFFE)) break;
        }

        _buffer.Clear();
        while (true) {
            if (_position >= _text.Length) return null;
            var c = _text[_position];
            if (c == quote) { _position++; return _buffer.ToString(); }
            if (c == '<') return null;
            if (c == '&') {
                if (!ReadReference()) return null;
                continue;
            }

            if (c == '\r') {
                // Line ends become one line feed, and literal white space becomes a space.
                _position++;
                if (_position < _text.Length && _text[_position] == '\n') _position++;
                _buffer.Append(' ');
                continue;
            }

            if (c is '\n' or '\t') {
                _position++;
                _buffer.Append(' ');
                continue;
            }

            if (!AppendCharacter(c)) return null;
        }
    }

    private string? ReadText() {
        // The renderers put a line end between elements; that text is always the same normalized string.
        if (_position + 2 < _text.Length && _text[_position] == '\r' && _text[_position + 1] == '\n' && _text[_position + 2] == '<') {
            _position += 2;
            return LineFeed;
        }

        if (_position + 1 < _text.Length && _text[_position] == '\n' && _text[_position + 1] == '<') {
            _position += 1;
            return LineFeed;
        }

        _buffer.Clear();
        while (_position < _text.Length) {
            var c = _text[_position];
            if (c == '<') break;
            if (c == '&') {
                if (!ReadReference()) return null;
                continue;
            }

            if (c == '\r') {
                _position++;
                if (_position < _text.Length && _text[_position] == '\n') _position++;
                _buffer.Append('\n');
                continue;
            }

            if (c == '>' && _position >= 2 && _text[_position - 1] == ']' && _text[_position - 2] == ']' && _buffer.Length >= 2) return null;
            if (!AppendCharacter(c)) return null;
        }

        return _buffer.ToString();
    }

    // A comment the framework writes back as it is read: on one line and without "--".
    private SvgMarkupOther? ReadComment() {
        var start = _position + 4;
        var end = _text.IndexOf("-->", start, StringComparison.Ordinal);
        if (end < 0) return null;
        var value = _text.Substring(start, end - start);
        if (value.IndexOf("--", StringComparison.Ordinal) >= 0 || value.EndsWith("-", StringComparison.Ordinal) || !_normalized && (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0)) return null;
        if (!AllowedCharacters(value)) return null;
        _position = end + 3;
        return new SvgMarkupOther("<!--" + value + "-->");
    }

    private SvgMarkupText? ReadCData() {
        var start = _position + 9;
        var end = _text.IndexOf("]]>", start, StringComparison.Ordinal);
        if (end < 0) return null;
        var raw = _text.Substring(start, end - start);
        if (!_normalized && (raw.IndexOf('\r') >= 0 || raw.IndexOf('\n') >= 0)) return null;
        if (!AllowedCharacters(raw)) return null;
        _position = end + 3;
        return new SvgMarkupText(raw.Replace("\r\n", "\n").Replace('\r', '\n'), isCData: true, raw: raw);
    }

    // Only normalized (framework-written) markup carries processing instructions here; they are kept as written.
    private SvgMarkupOther? ReadInstruction() {
        var end = _text.IndexOf("?>", _position + 2, StringComparison.Ordinal);
        if (end < 0) return null;
        var markup = _text.Substring(_position, end + 2 - _position);
        _position = end + 2;
        return new SvgMarkupOther(markup);
    }

    private static bool AllowedCharacters(string value) {
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (c < 0x20 && c != '\t' && c != '\n' && c != '\r' || c is '￾' or '￿') return false;
            if (char.IsHighSurrogate(c)) {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1])) return false;
                i++;
            } else if (char.IsLowSurrogate(c)) {
                return false;
            }
        }

        return true;
    }

    // Appends one literal character (or a surrogate pair) that XML allows.
    private bool AppendCharacter(char c) {
        if (c < 0x20 && c != '\t' && c != '\n' || c is '￾' or '￿') return false;
        if (char.IsHighSurrogate(c)) {
            if (_position + 1 >= _text.Length || !char.IsLowSurrogate(_text[_position + 1])) return false;
            _buffer.Append(c).Append(_text[_position + 1]);
            _position += 2;
            return true;
        }

        if (char.IsLowSurrogate(c)) return false;
        _buffer.Append(c);
        _position++;
        return true;
    }

    private bool ReadReference() {
        var end = _text.IndexOf(';', _position + 1);
        if (end < 0 || end - _position > 12) return false;
        var length = end - _position - 1;
        if (length <= 0) return false;
        if (_text[_position + 1] == '#') {
            var hex = length > 1 && _text[_position + 2] == 'x';
            var digitsStart = _position + (hex ? 3 : 2);
            if (digitsStart >= end) return false;
            var code = 0;
            for (var i = digitsStart; i < end; i++) {
                var digit = HexValue(_text[i]);
                if (digit < 0 || !hex && digit > 9) return false;
                code = code * (hex ? 16 : 10) + digit;
                if (code > 0x10FFFF) return false;
            }

            if (code is 0x9 or 0xA or 0xD || code >= 0x20 && code <= 0xD7FF || code >= 0xE000 && code <= 0xFFFD) _buffer.Append((char)code);
            else if (code >= 0x10000) _buffer.Append(char.ConvertFromUtf32(code));
            else return false;
        } else {
            var entity = EntityValue(end);
            if (entity == '\0') return false;
            _buffer.Append(entity);
        }

        _position = end + 1;
        return true;
    }

    private char EntityValue(int end) {
        var start = _position + 1;
        var length = end - start;
        if (length == 2 && _text[start + 1] == 't') return _text[start] == 'l' ? '<' : _text[start] == 'g' ? '>' : '\0';
        if (length == 3 && string.CompareOrdinal(_text, start, "amp", 0, 3) == 0) return '&';
        if (length == 4 && string.CompareOrdinal(_text, start, "quot", 0, 4) == 0) return '"';
        if (length == 4 && string.CompareOrdinal(_text, start, "apos", 0, 4) == 0) return '\'';
        return '\0';
    }

    private bool SkipWhiteSpace() {
        var start = _position;
        while (_position < _text.Length && IsWhiteSpace(_text[_position])) _position++;
        return _position > start;
    }

    private string NormalizeLineEnds(int start, int end) {
        if (end - start == 2 && _text[start] == '\r' && _text[start + 1] == '\n' || end - start == 1 && _text[start] == '\n') return LineFeed;
        _buffer.Clear();
        for (var i = start; i < end; i++) {
            var c = _text[i];
            if (c == '\r') {
                if (i + 1 < end && _text[i + 1] == '\n') i++;
                _buffer.Append('\n');
            } else {
                _buffer.Append(c);
            }
        }

        return _buffer.ToString();
    }

    // Names repeat on every element; one string per distinct name avoids a substring per element and attribute.
    private string Name(int start, int length) {
        var hash = 17;
        for (var i = start; i < start + length; i++) hash = unchecked(hash * 31 + _text[i]);
        var mask = _names.Length - 1;
        for (var slot = hash & mask; ; slot = (slot + 1) & mask) {
            var name = _names[slot];
            if (name == null) {
                name = _text.Substring(start, length);
                _names[slot] = name;
                if (++_nameCount * 2 > _names.Length) GrowNames();
                return name;
            }

            if (name.Length == length && string.CompareOrdinal(name, 0, _text, start, length) == 0) return name;
        }
    }

    private void GrowNames() {
        var old = _names;
        _names = new string[old.Length * 2];
        var mask = _names.Length - 1;
        foreach (var name in old) {
            if (name == null) continue;
            var hash = 17;
            foreach (var c in name) hash = unchecked(hash * 31 + c);
            var slot = hash & mask;
            while (_names[slot] != null) slot = (slot + 1) & mask;
            _names[slot] = name;
        }
    }

    private static bool IsWhiteSpace(char c) => c is ' ' or '\t' or '\n' or '\r';

    private bool SurrogatePair(int index) => char.IsHighSurrogate(_text[index]) && index + 1 < _text.Length && char.IsLowSurrogate(_text[index + 1]);

    private static bool IsNameStart(char c) => c < 0x80 ? c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' : XmlConvert.IsStartNCNameChar(c);

    private static bool IsNameChar(char c) => c < 0x80 ? c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '.' or '-' : XmlConvert.IsNCNameChar(c);

    private static int HexValue(char c) => c is >= '0' and <= '9' ? c - '0' : c is >= 'a' and <= 'f' ? c - 'a' + 10 : c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1;
}
