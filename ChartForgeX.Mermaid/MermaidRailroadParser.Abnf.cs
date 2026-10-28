using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidRailroadParser {
    private MermaidRailroadExpression ParseAbnfElement(int depth) {
        MermaidResourceGuard.CheckDepth(depth, _token.Span); var start = _token; int? minimum = null; int? maximum = null;
        if (_token.Kind == TokenKind.Number) { minimum = RepeatCount(_token); Advance(); maximum = minimum; }
        if (Is("*")) { minimum ??= 0; maximum = null; Advance(); if (_token.Kind == TokenKind.Number) { maximum = RepeatCount(_token); Advance(); } }
        if (minimum.HasValue && maximum.HasValue && minimum > maximum) throw Error(start, "ABNF repeat bounds must be non-negative and ordered.");
        var expression = ParsePrimary(depth);
        return minimum.HasValue ? Node(RailroadExpression.Repeat(expression.Expression, minimum.Value, maximum), start, expression) : expression;
    }
    private static int RepeatCount(Token token) {
        if (!int.TryParse(token.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var count)) throw Error(token, "A railroad repeat count must fit a non-negative 32-bit integer; repeats are not expanded.");
        return count;
    }
    private MermaidRailroadExpression ParseAbnfNumeric() {
        var token = _token; var text = token.Text; Advance();
        if (text.Length < 3) throw Error(token, "ABNF numeric literals require a radix and at least one value.");
        var radix = char.ToLowerInvariant(text[1]) switch { 'x' => 16, 'd' => 10, 'b' => 2, _ => 0 };
        if (radix == 0) throw Error(token, "ABNF requires %x, %d or %b numeric values, or an adjacent %i/%s double-quoted literal.");
        var value = text.Substring(2);
        if (value.IndexOf('-') >= 0) {
            if (value.IndexOf('.') >= 0) throw Error(token, "An ABNF numeric range cannot also be a numeric sequence.");
            var range = value.Split('-');
            if (range.Length != 2) throw Error(token, "An ABNF numeric range requires exactly two bounds.");
            var first = CodePoint(range[0], radix, token); var last = CodePoint(range[1], radix, token);
            if (first > last) throw Error(token, "ABNF character-range bounds must be in increasing order.");
            return Node(RailroadExpression.CharacterRange(first, last), token);
        }
        var sequence = value.Split('.').Select(part => { var scalar = CodePoint(part, radix, token); return Node(RailroadExpression.CharacterRange(scalar, scalar), token); }).ToArray();
        return sequence.Length == 1 ? sequence[0] : Node(RailroadExpression.Sequence(Native(sequence)), token, sequence);
    }
    private static int CodePoint(string text, int radix, Token token) {
        if (text.Length == 0) throw Error(token, "ABNF numeric values must not be empty.");
        var result = 0;
        foreach (var character in text) {
            var digit = character is >= '0' and <= '9' ? character - '0' : character is >= 'a' and <= 'f' ? character - 'a' + 10 : character is >= 'A' and <= 'F' ? character - 'A' + 10 : -1;
            if (digit < 0 || digit >= radix || result > (0x10ffff - digit) / radix) throw Error(token, "An ABNF numeric value must be a valid code point in its declared radix, no greater than U+10FFFF.");
            result = result * radix + digit;
        }
        return result;
    }
}
