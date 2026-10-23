using System;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidRailroadParser {
    private sealed class Scanner {
        private readonly MermaidRailroadDialect _dialect; private int _offset; private int _line; private int _column = 1;
        internal Scanner(string source, int line, MermaidRailroadDialect dialect) { Source = source; _line = line; _dialect = dialect; }
        internal string Source { get; }
        internal void SkipToLineEnd() { while (_offset < Source.Length && Source[_offset] != '\n') Move(); }
        internal Token Next() {
            SkipTrivia(); var start = _offset; var line = _line; var column = _column;
            if (_offset == Source.Length) return new Token(TokenKind.End, "", start, start, line, column);
            var character = Source[_offset]; Move();
            if (AsciiLetter(character) || character == '_') {
                while (_offset < Source.Length && (char.IsLetterOrDigit(Source[_offset]) || Source[_offset] is '_' or '-')) Move();
                return Create(TokenKind.Identifier, Source.Substring(start, _offset - start));
            }
            if (char.IsDigit(character)) { while (_offset < Source.Length && char.IsDigit(Source[_offset])) Move(); return Create(TokenKind.Number, Source.Substring(start, _offset - start)); }
            if (character is '\'' or '"') return Create(TokenKind.String, Quoted(character, start, line, column));
            if (character == '%') {
                while (_offset < Source.Length && (char.IsLetterOrDigit(Source[_offset]) || Source[_offset] is '.' or '-')) Move();
                return Create(TokenKind.NumericValue, Source.Substring(start, _offset - start));
            }
            if (character == ':' && Peek(':') && _offset + 1 < Source.Length && Source[_offset + 1] == '=') { Move(); Move(); return Create(TokenKind.Symbol, "::="); }
            if (character == '<' && Peek('-')) { Move(); return Create(TokenKind.Symbol, "<-"); }
            if (character == '?' && _dialect == MermaidRailroadDialect.Ebnf && (start == 0 || char.IsWhiteSpace(Source[start - 1]) || Source[start - 1] is '=' or '(' or '[' or '{' or '|' or ',')) {
                var close = Source.IndexOf('?', _offset); var end = Source.IndexOf(';', _offset);
                if (close >= 0 && (end < 0 || close < end) && Source.Substring(_offset, close - _offset).Trim().Length > 0) {
                    var text = Source.Substring(_offset, close - _offset).Trim(); while (_offset <= close) Move(); return Create(TokenKind.Special, text);
                }
            }
            if ("=;|/,()[]{}?*+-!&.".IndexOf(character) >= 0) return Create(TokenKind.Symbol, character.ToString());
            throw new RailroadSyntaxException("Unsupported character in railroad grammar: '" + character + "'.", new MermaidSourceSpan(line, column, _offset - start));
            Token Create(TokenKind kind, string text) => new(kind, text, start, _offset, line, column);
        }
        private string Quoted(char quote, int start, int line, int column) {
            if (_dialect == MermaidRailroadDialect.Abnf && quote != '"') throw new RailroadSyntaxException("The pinned ABNF frontend accepts double-quoted literals only.", new MermaidSourceSpan(line, column, 1));
            var text = new StringBuilder();
            while (_offset < Source.Length) {
                MermaidResourceGuard.Checkpoint(); var character = Source[_offset]; Move();
                if (character == quote) return text.ToString();
                if (character != '\\' || _dialect == MermaidRailroadDialect.Abnf) { text.Append(character); continue; }
                if (_offset == Source.Length) break;
                var escaped = Source[_offset]; Move();
                switch (escaped) {
                    case 'n': text.Append('\n'); break; case 'r': text.Append('\r'); break; case 't': text.Append('\t'); break;
                    case 'b': text.Append('\b'); break; case 'f': text.Append('\f'); break;
                    case '\\': text.Append('\\'); break; case '\'': text.Append('\''); break; case '"': text.Append('"'); break;
                    case 'u':
                        if (_offset + 4 > Source.Length || !ushort.TryParse(Source.Substring(_offset, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                            throw new RailroadSyntaxException("A Unicode literal escape requires four hexadecimal digits.", new MermaidSourceSpan(_line, _column, Math.Min(4, Source.Length - _offset)));
                        text.Append((char)code); for (var index = 0; index < 4; index++) Move(); break;
                    default: throw new RailroadSyntaxException("Unsupported literal escape '\\" + escaped + "'.", new MermaidSourceSpan(_line, Math.Max(1, _column - 2), 2));
                }
            }
            throw new RailroadSyntaxException("Unclosed railroad literal.", new MermaidSourceSpan(line, column, Math.Max(1, _offset - start)));
        }
        private void SkipTrivia() {
            while (_offset < Source.Length) {
                MermaidResourceGuard.Checkpoint(); var c = Source[_offset];
                if (char.IsWhiteSpace(c)) { Move(); continue; }
                if (Starts("%%{")) { SkipBlock("}%%"); continue; }
                if (Starts("%%") || _dialect == MermaidRailroadDialect.Peg && c == '#') { SkipToLineEnd(); continue; }
                if ((_dialect == MermaidRailroadDialect.Ebnf || _dialect == MermaidRailroadDialect.IntermediateRepresentation) && Starts("/*")) { SkipBlock("*/"); continue; }
                if (_dialect == MermaidRailroadDialect.Ebnf && Starts("(*")) { SkipBlock("*)"); continue; }
                break;
            }
        }
        private void SkipBlock(string terminator) {
            var line = _line; var column = _column; var close = Source.IndexOf(terminator, _offset + 2, StringComparison.Ordinal);
            if (close < 0) throw new RailroadSyntaxException("Unclosed railroad comment or directive.", new MermaidSourceSpan(line, column, 2));
            while (_offset < close + terminator.Length) Move();
        }
        private bool Starts(string text) => _offset + text.Length <= Source.Length && string.CompareOrdinal(Source, _offset, text, 0, text.Length) == 0;
        private bool Peek(char character) => _offset < Source.Length && Source[_offset] == character;
        private void Move() { if ((_offset & 1023) == 0) MermaidResourceGuard.Checkpoint(); if (Source[_offset++] == '\n') { _line++; _column = 1; } else _column++; }
    }
}
