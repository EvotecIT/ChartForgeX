using System.Collections.Generic;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidRailroadParser {
    private MermaidRailroadExpression ParsePrimitive(int depth) {
        MermaidResourceGuard.CheckDepth(depth, _token.Span); var start = Expect(TokenKind.Identifier, "A railroad IR expression requires a supported constructor.");
        Expect("(");
        if (start.Text is "terminal" or "nonterminal" or "special") {
            var literal = Expect(TokenKind.String, "This railroad constructor requires one quoted string."); Expect(")");
            if (start.Text == "nonterminal" && string.IsNullOrWhiteSpace(literal.Text) || start.Text == "special" && string.IsNullOrWhiteSpace(literal.Text)) throw Error(literal, "A railroad reference or special description must not be empty.");
            return Node(start.Text == "terminal" ? RailroadExpression.Terminal(literal.Text) : start.Text == "nonterminal" ? RailroadExpression.Reference(literal.Text) : RailroadExpression.Special(literal.Text), start);
        }
        if (start.Text is "optional" or "zeroOrMore" or "oneOrMore") {
            var inner = ParsePrimitive(depth + 1); Expect(")");
            return Node(start.Text == "optional" ? RailroadExpression.Optional(inner.Expression) : RailroadExpression.Repeat(inner.Expression, start.Text == "oneOrMore" ? 1 : 0), start, inner);
        }
        if (start.Text is "sequence" or "choice") {
            var children = new List<MermaidRailroadExpression> { ParsePrimitive(depth + 1) };
            while (Is(",")) { Advance(); children.Add(ParsePrimitive(depth + 1)); }
            Expect(")");
            return Node(start.Text == "sequence" ? RailroadExpression.Sequence(Native(children)) : RailroadExpression.Choice(Native(children)), start, children.ToArray());
        }
        throw Error(start, "Unsupported railroad IR constructor '" + start.Text + "'.");
    }
}
