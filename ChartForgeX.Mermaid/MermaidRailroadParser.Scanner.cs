using System;
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
            if (character == '[' && _dialect == MermaidRailroadDialect.Peg) return Create(TokenKind.CharacterClass, CharacterClass(start, line, column));
            if (character == '%') {
                if (_dialect == MermaidRailroadDialect.Abnf && _offset + 1 < Source.Length
                    && (Source[_offset] is 'i' or 'I' or 's' or 'S') && Source[_offset + 1] == '"') {
                    var sensitive = Source[_offset] is 's' or 'S'; Move(); Move();
                    return Create(sensitive ? TokenKind.CaseSensitiveString : TokenKind.String, Quoted('"', start, line, column));
                }
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
                MermaidResourceGuard.Checkpoint(); var character = Source[_offset];
                var characterLine = _line; var characterColumn = _column; Move();
                if (character == quote) return text.ToString();
                if (_dialect == MermaidRailroadDialect.Abnf && (character < ' ' || character > '~'))
                    throw new RailroadSyntaxException("ABNF quoted strings contain printable US-ASCII characters only.", new MermaidSourceSpan(characterLine, characterColumn, 1));
                if (character != '\\' || _dialect == MermaidRailroadDialect.Abnf) { text.Append(character); continue; }
                if (_offset == Source.Length) break;
                var end = _offset; text.Append(ReadLiteralEscape(Source, ref end, _dialect == MermaidRailroadDialect.Peg, new MermaidSourceSpan(_line, Math.Max(1, _column - 1), 2)));
                while (_offset < end) Move();
            }
            throw new RailroadSyntaxException("Unclosed railroad literal.", new MermaidSourceSpan(line, column, Math.Max(1, _offset - start)));
        }
        private string CharacterClass(int start, int line, int column) {
            var content = _offset;
            while (_offset < Source.Length) {
                var character = Source[_offset]; Move();
                if (character == ']') return Source.Substring(content, _offset - content - 1);
                // Preserve class spelling for located semantic lowering; escaped ] is not a delimiter.
                if (character == '\\' && _offset < Source.Length) Move();
            }
            throw new RailroadSyntaxException("Unclosed PEG character class.", new MermaidSourceSpan(line, column, Math.Max(1, _offset - start)));
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
