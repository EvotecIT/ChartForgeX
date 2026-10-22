using System.Collections.Generic;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidRailroadParser {
    private MermaidRailroadExpression ParseChoice(int depth) {
        MermaidResourceGuard.CheckDepth(depth, _token.Span); var start = _token; var alternatives = new List<MermaidRailroadExpression> { ParseSequence(depth) };
        var separator = _document.Dialect == MermaidRailroadDialect.Ebnf ? "|" : "/";
        while (Is(separator)) { Advance(); alternatives.Add(ParseSequence(depth)); }
        return alternatives.Count == 1 ? alternatives[0] : Node(RailroadExpression.Choice(Native(alternatives), _document.Dialect == MermaidRailroadDialect.Peg), start, alternatives.ToArray());
    }
    private MermaidRailroadExpression ParseSequence(int depth) {
        MermaidResourceGuard.CheckDepth(depth, _token.Span); var start = _token; var elements = new List<MermaidRailroadExpression>();
        while (CanStartExpression()) {
            elements.Add(_document.Dialect == MermaidRailroadDialect.Abnf ? ParseAbnfElement(depth) : ParseTerm(depth));
            if (_document.Dialect == MermaidRailroadDialect.Ebnf && Is(",")) { Advance(); if (!CanStartExpression()) throw Error(_token, "An EBNF concatenation comma requires another element."); }
        }
        if (elements.Count == 0) throw Error(_token, "A railroad expression must not be empty.");
        return elements.Count == 1 ? elements[0] : Node(RailroadExpression.Sequence(Native(elements)), start, elements.ToArray());
    }
    private bool CanStartExpression() => _token.Kind is TokenKind.Identifier or TokenKind.String or TokenKind.Special
        || _document.Dialect == MermaidRailroadDialect.Abnf && (_token.Kind is TokenKind.Number or TokenKind.NumericValue || Is("*"))
        || Is("(") || Is("[") && _document.Dialect != MermaidRailroadDialect.Peg
        || _document.Dialect == MermaidRailroadDialect.Ebnf && Is("{")
        || _document.Dialect == MermaidRailroadDialect.Peg && (Is("!") || Is("&") || Is("."));

    private MermaidRailroadExpression ParseTerm(int depth) {
        MermaidResourceGuard.CheckDepth(depth, _token.Span); var start = _token;
        var predicate = _document.Dialect == MermaidRailroadDialect.Peg && (Is("!") || Is("&")) ? _token.Text : null;
        if (predicate != null) Advance();
        var expression = ParsePrimary(depth); var postfixes = 0;
        while (Is("?") || Is("*") || Is("+") || _document.Dialect == MermaidRailroadDialect.Ebnf && Is("-")) {
            if (_document.Dialect == MermaidRailroadDialect.Peg && postfixes++ > 0) throw Error(_token, "PEG permits one suffix operator on a primary expression.");
            var operation = _token.Text; Advance();
            if (operation == "-") { var excluded = ParsePrimary(depth); expression = Node(RailroadExpression.Except(expression.Expression, excluded.Expression), start, expression, excluded); }
            else expression = Node(operation == "?" ? RailroadExpression.Optional(expression.Expression) : RailroadExpression.Repeat(expression.Expression, operation == "+" ? 1 : 0), start, expression);
        }
        return predicate == null ? expression : Node(RailroadExpression.Lookahead(expression.Expression, predicate == "&"), start, expression);
    }
    private MermaidRailroadExpression ParsePrimary(int depth) {
        MermaidResourceGuard.CheckDepth(depth, _token.Span); var start = _token;
        if (_token.Kind == TokenKind.String) { var value = _token.Text; Advance(); return Node(RailroadExpression.Terminal(value, _document.Dialect != MermaidRailroadDialect.Abnf), start); }
        if (_token.Kind == TokenKind.Identifier) { ValidateRuleName(_token); var name = _token.Text; Advance(); return Node(RailroadExpression.Reference(name), start); }
        if (_token.Kind == TokenKind.Special && _document.Dialect == MermaidRailroadDialect.Ebnf) { var text = _token.Text; Advance(); return Node(RailroadExpression.Special(text), start); }
        if (Is("." ) && _document.Dialect == MermaidRailroadDialect.Peg) { Advance(); return Node(RailroadExpression.AnyCharacter(), start); }
        if (Is("(")) { Advance(); var expression = ParseChoice(depth + 1); Expect(")"); return expression; }
        if (Is("[") && _document.Dialect != MermaidRailroadDialect.Peg) { Advance(); var expression = ParseChoice(depth + 1); Expect("]"); return Node(RailroadExpression.Optional(expression.Expression), start, expression); }
        if (Is("{") && _document.Dialect == MermaidRailroadDialect.Ebnf) { Advance(); var expression = ParseChoice(depth + 1); Expect("}"); return Node(RailroadExpression.Repeat(expression.Expression, 0), start, expression); }
        if (_token.Kind == TokenKind.NumericValue && _document.Dialect == MermaidRailroadDialect.Abnf) return ParseAbnfNumeric();
        throw Error(_token, "Expected a literal, rule reference or supported grammar group.");
    }
}
