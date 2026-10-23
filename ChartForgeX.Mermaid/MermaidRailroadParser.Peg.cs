using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidRailroadParser {
    /// <summary>Lowers a PEG character set to symbolic ranges without expanding or executing it.</summary>
    private MermaidRailroadExpression ParsePegCharacterClass() {
        var token = _token; Advance(); var text = token.Text; var offset = 0;
        var alternatives = new List<MermaidRailroadExpression>();
        while (offset < text.Length) {
            MermaidResourceGuard.Checkpoint(); var first = PegClassCharacter(text, ref offset, token);
            var last = first;
            if (offset + 1 < text.Length && text[offset] == '-') {
                offset++; last = PegClassCharacter(text, ref offset, token);
                if (first > last) throw Error(token, "PEG character-range bounds must be in increasing order.");
            }
            alternatives.Add(Node(RailroadExpression.CharacterRange(first, last), token));
        }
        if (alternatives.Count == 0) {
            // [] never matches, including at end-of-input. !. alone would match at end-of-input.
            var any = Node(RailroadExpression.AnyCharacter(), token);
            var excluded = Node(RailroadExpression.AnyCharacter(), token);
            return Node(RailroadExpression.Except(any.Expression, excluded.Expression), token, any, excluded);
        }
        return alternatives.Count == 1 ? alternatives[0] : Node(RailroadExpression.Choice(Native(alternatives), ordered: true), token, alternatives.ToArray());
    }

    /// <summary>Reads one literal code point so non-BMP class entries remain one symbolic range.</summary>
    private static int PegClassCharacter(string text, ref int offset, Token token) {
        var first = PegClassUnit(text, ref offset, token);
        if (!char.IsSurrogate(first)) return first;
        if (char.IsHighSurrogate(first) && offset < text.Length) {
            var second = PegClassUnit(text, ref offset, token);
            if (char.IsLowSurrogate(second)) return char.ConvertToUtf32(first, second);
        }
        throw Error(token, "A PEG character class requires complete Unicode code points, not unpaired surrogates.");
    }

    private static char PegClassUnit(string text, ref int offset, Token token) {
        var character = text[offset++];
        return character == '\\' ? ReadLiteralEscape(text, ref offset, peg: true, token.Span) : character;
    }

    /// <summary>Shares literal escape decoding between quoted text and PEG character classes.</summary>
    private static char ReadLiteralEscape(string text, ref int offset, bool peg, MermaidSourceSpan span) {
        if (offset == text.Length) throw new RailroadSyntaxException("An escape requires a following character.", span);
        var escaped = text[offset++];
        switch (escaped) {
            case 'n': return '\n'; case 'r': return '\r'; case 't': return '\t';
            case 'b': return '\b'; case 'f': return '\f';
            case '\\': return '\\'; case '\'': return '\''; case '"': return '"';
            case '[' when peg: return '['; case ']' when peg: return ']'; case '-' when peg: return '-';
            case 'u':
                if (offset + 4 > text.Length || !ushort.TryParse(text.Substring(offset, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                    throw new RailroadSyntaxException("A Unicode literal escape requires four hexadecimal digits.", span);
                offset += 4; return (char)code;
            default:
                if (peg && escaped is >= '0' and <= '7') {
                    // Ford PEG's Char production allows three octal digits beginning 0..2,
                    // or one/two octal digits. Counts remain lexical, never grammar execution.
                    var value = escaped - '0'; var remaining = escaped <= '2' ? 2 : 1;
                    while (remaining-- > 0 && offset < text.Length && text[offset] is >= '0' and <= '7') value = value * 8 + text[offset++] - '0';
                    return (char)value;
                }
                throw new RailroadSyntaxException("Unsupported literal escape '\\" + escaped + "'.", span);
        }
    }
}
