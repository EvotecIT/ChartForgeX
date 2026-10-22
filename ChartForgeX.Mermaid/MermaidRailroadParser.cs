using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidRailroadParser {
    private readonly MermaidRailroadDocument _document;
    private readonly Scanner _scanner;
    private Token _token;
    private Token _last;
    private readonly HashSet<string> _ruleIds;

    private MermaidRailroadParser(MermaidRailroadDocument document, string source, int startLine) {
        _document = document; _scanner = new Scanner(source, startLine, document.Dialect);
        _ruleIds = new HashSet<string>(document.Dialect == MermaidRailroadDialect.Abnf ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    internal static void ParseStatements(MermaidRailroadDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var body = new StringBuilder(); var inDescription = false;
        for (var line = startLine; line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint(); var raw = lines[line - 1]; var text = raw.Trim(); var ignored = inDescription;
            if (inDescription && text.IndexOf('}') >= 0) inDescription = false;
            if (text.StartsWith("title ", StringComparison.Ordinal)) { document.Title = MermaidParserUtilities.Unquote(MermaidParserUtilities.StripInlineComment(text.Substring(6).Trim())); ignored = true; }
            if (text.StartsWith("accTitle:", StringComparison.Ordinal) || text.StartsWith("accDescr:", StringComparison.Ordinal)) ignored = true;
            if (text.StartsWith("accDescr", StringComparison.Ordinal) && text.IndexOf('{') >= 0) { inDescription = text.IndexOf('}') < 0; ignored = true; }
            body.Append(ignored ? new string(' ', raw.Length) : raw).Append('\n');
        }
        var parser = new MermaidRailroadParser(document, body.ToString(), startLine);
        try { parser.Advance(); parser.ReadRules(); }
        catch (RailroadSyntaxException exception) { MermaidParserUtilities.Add(result, exception.Span, MermaidDiagnosticSeverity.Error, exception.Message, MermaidDiagnosticCodes.InvalidStatement); }
        if (document.Rules.Count == 0 && !result.HasErrors) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "A railroad diagram requires at least one grammar rule.");
    }

    private void ReadRules() {
        while (_token.Kind != TokenKind.End) {
            MermaidResourceGuard.Checkpoint();
            if (_document.Dialect == MermaidRailroadDialect.Abnf && Is(";")) { _scanner.SkipToLineEnd(); Advance(); continue; }
            var name = Expect(TokenKind.Identifier, "A railroad rule requires an identifier.");
            ValidateRuleName(name);
            if (!_ruleIds.Add(name.Text)) throw Error(name, "Duplicate railroad rule identity '" + name.Text + "'.");
            if (_document.Dialect == MermaidRailroadDialect.Peg) Expect("<-");
            else if (_document.Dialect == MermaidRailroadDialect.Ebnf && Is("::=")) Advance();
            else Expect("=");
            var definition = _document.Dialect == MermaidRailroadDialect.IntermediateRepresentation ? ParsePrimitive(1) : ParseChoice(1);
            Token end;
            if (_document.Dialect == MermaidRailroadDialect.Abnf) {
                if (!Is(";")) throw Error(_token, "ABNF railroad rules require a terminating semicolon.");
                end = _token; _scanner.SkipToLineEnd(); Advance();
            } else end = Expect(";");
            MermaidResourceGuard.ReserveElements(1, name.Span);
            _document.Rules.Add(new MermaidRailroadRule(name.Text, definition, Span(name, end)));
            _document.Statements.Add(new MermaidRawStatement(_scanner.Source.Substring(name.Start, end.End - name.Start), Span(name, end)));
        }
    }

    private void ValidateRuleName(Token token) {
        if (_document.Dialect != MermaidRailroadDialect.Abnf) return;
        if (!AsciiLetter(token.Text[0]) || token.Text.Any(character => !AsciiLetter(character) && !(character is >= '0' and <= '9') && character != '-'))
            throw Error(token, "ABNF rule names require an ASCII letter followed by letters, digits or hyphens.");
    }

    private MermaidRailroadExpression Node(RailroadExpression expression, Token start, params MermaidRailroadExpression[] children) {
        var span = Span(start, _last); MermaidResourceGuard.CheckDepth(expression.Depth, span);
        MermaidResourceGuard.ReserveElements(1, span); MermaidResourceGuard.ReserveEdges(children.Length, span);
        return new MermaidRailroadExpression(expression, span, children);
    }
    private static RailroadExpression[] Native(IEnumerable<MermaidRailroadExpression> nodes) => nodes.Select(node => node.Expression).ToArray();
    private bool Is(string text) => _token.Text == text && _token.Kind == TokenKind.Symbol;
    private void Advance() { _last = _token; _token = _scanner.Next(); }
    private Token Expect(string text) { if (!Is(text)) throw Error(_token, "Expected '" + text + "' in railroad grammar."); var token = _token; Advance(); return token; }
    private Token Expect(TokenKind kind, string message) { if (_token.Kind != kind) throw Error(_token, message); var token = _token; Advance(); return token; }
    private static MermaidSourceSpan Span(Token first, Token last) => new(first.Span.Line, first.Span.Column, Math.Max(first.Span.Length, last.End - first.Start));
    private static RailroadSyntaxException Error(Token token, string message) => new(message, token.Span);
    private static bool AsciiLetter(char character) => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private sealed class RailroadSyntaxException : Exception {
        internal RailroadSyntaxException(string message, MermaidSourceSpan span) : base(message) { Span = span; }
        internal MermaidSourceSpan Span { get; }
    }
    private enum TokenKind { End, Identifier, String, Number, NumericValue, Special, Symbol }
    private readonly struct Token {
        internal Token(TokenKind kind, string text, int start, int end, int line, int column) { Kind = kind; Text = text; Start = start; End = end; Span = new MermaidSourceSpan(line, column, end - start); }
        internal TokenKind Kind { get; } internal string Text { get; } internal int Start { get; } internal int End { get; } internal MermaidSourceSpan Span { get; }
    }
}
