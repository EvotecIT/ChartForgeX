using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private enum CynefinTokenKind { End, Newline, Word, Quoted, Arrow, Colon, Invalid }
    private readonly struct CynefinToken {
        internal CynefinToken(CynefinTokenKind kind, string value, int start, int end) { Kind = kind; Value = value; Start = start; End = end; }
        internal CynefinTokenKind Kind { get; }
        internal string Value { get; }
        internal int Start { get; }
        internal int End { get; }
    }

    // Independent native reader for the pinned STRING/EOL rules. No external parser runtime
    // or generated grammar is copied into production. Offsets address the original UTF-16 source.
    private sealed class CynefinReader {
        private readonly string _source;
        private readonly string[] _lines;
        private readonly List<int> _starts;
        private readonly MermaidParseResult<MermaidDocument> _result;
        private int _position;
        private CynefinToken? _peek;
        internal CynefinReader(string source, string[] lines, List<int> starts, int position, MermaidParseResult<MermaidDocument> result) {
            _source = source; _lines = lines; _starts = starts; _position = position; _result = result;
        }
        internal CynefinToken Peek { get { if (!_peek.HasValue) _peek = Read(); return _peek.Value; } }
        internal CynefinToken Take() { var token = Peek; _peek = null; return token; }
        internal MermaidSourceSpan Span(CynefinToken token) {
            var line = _starts.BinarySearch(token.Start); if (line < 0) line = ~line - 1;
            return new MermaidSourceSpan(line + 1, token.Start - _starts[line] + 1, token.End - token.Start);
        }
        internal void SkipLine() { while (Peek.Kind is not (CynefinTokenKind.Newline or CynefinTokenKind.End)) Take(); }
        internal CynefinToken Title(int start) {
            // The TITLE terminal consumes text before %% even when that text contains quotes.
            _peek = null; var valueStart = _position;
            while (_position < _source.Length && _source[_position] is not ('\r' or '\n') && !At("%%")) _position++;
            var end = _position;
            return new CynefinToken(CynefinTokenKind.Word, _source.Substring(valueStart, end - valueStart), start, end);
        }
        private bool At(string value) => _position + value.Length <= _source.Length && string.CompareOrdinal(_source, _position, value, 0, value.Length) == 0;
        private CynefinToken Read() {
            while (_position < _source.Length) {
                MermaidResourceGuard.Checkpoint();
                // Shared prepasses blank only whole declarations, retaining original offsets.
                var line = _starts.BinarySearch(_position);
                if (line >= 0 && line < _lines.Length && _lines[line].Length == 0) {
                    while (_position < _source.Length && _source[_position] is not ('\r' or '\n')) _position++;
                    if (_position == _source.Length) break;
                }
                var ch = _source[_position];
                if (ch is ' ' or '\t') { _position++; continue; }
                if (At("%%")) { while (_position < _source.Length && _source[_position] is not ('\r' or '\n')) _position++; continue; }
                var start = _position++;
                if (ch is '\r' or '\n') { if (ch == '\r' && _position < _source.Length && _source[_position] == '\n') _position++; return new CynefinToken(CynefinTokenKind.Newline, string.Empty, start, _position); }
                if (ch == ':') return new CynefinToken(CynefinTokenKind.Colon, ":", start, _position);
                if (ch == '-' && At("->")) { _position += 2; return new CynefinToken(CynefinTokenKind.Arrow, "-->", start, _position); }
                if (ch is '\'' or '"') return Quoted(start, ch);
                while (_position < _source.Length && !char.IsWhiteSpace(_source[_position]) && _source[_position] is not (':' or '\'' or '"') && !At("-->") && !At("%%")) _position++;
                return new CynefinToken(CynefinTokenKind.Word, _source.Substring(start, _position - start), start, _position);
            }
            return new CynefinToken(CynefinTokenKind.End, string.Empty, _source.Length, _source.Length);
        }
        private CynefinToken Quoted(int start, char quote) {
            var value = new StringBuilder();
            while (_position < _source.Length) {
                MermaidResourceGuard.Checkpoint();
                var ch = _source[_position++];
                if (ch == quote) {
                    var token = new CynefinToken(CynefinTokenKind.Quoted, value.ToString(), start, _position);
                    try { XmlConvert.VerifyXmlChars(token.Value); }
                    catch (XmlException) { MermaidParserUtilities.Add(_result, Span(token), MermaidDiagnosticSeverity.Error,
                        "Cynefin string contains a control character that native text output cannot preserve.", MermaidDiagnosticCodes.UnsupportedStatement); }
                    return token;
                }
                if (ch == '\\') {
                    if (_position == _source.Length || _source[_position] is '\r' or '\n') break;
                    ch = _source[_position++];
                    // Pinned CommonValueConverter delegates STRING conversion to these escape
                    // semantics. Unknown escapes remove only the backslash; no Unicode decoding.
                    ch = ch switch { 'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', 'v' => '\v', '0' => '\0', _ => ch };
                }
                value.Append(ch);
            }
            var invalid = new CynefinToken(CynefinTokenKind.Invalid, string.Empty, start, _position);
            MermaidParserUtilities.Add(_result, Span(invalid), MermaidDiagnosticSeverity.Error, "Cynefin quoted strings must close and cannot escape a physical newline.");
            return invalid;
        }
    }
}
