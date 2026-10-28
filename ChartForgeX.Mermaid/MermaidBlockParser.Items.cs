using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidBlockParser {
    private static bool TryParseItem(string token, MermaidSourceSpan span, ref int sequence, out MermaidBlockItem item, out string diagnostic) {
        item = null!; diagnostic = ""; token = token.Trim();
        var classes = new List<string>(); var suffix = MermaidStyleParser.Suffix(token);
        if (suffix >= 0) { classes.AddRange(MermaidStyleParser.Names(token.Substring(suffix + 3))); token = token.Substring(0, suffix); }
        if (!ReadSpan(ref token, out var columnSpan)) { diagnostic = "Block spans must be between one and twenty-four."; return false; }
        if (token.Equals("space", StringComparison.OrdinalIgnoreCase)) {
            item = new MermaidBlockItem("__cfx-space-" + (++sequence).ToString(CultureInfo.InvariantCulture), "", columnSpan, BlockLayoutShape.Rectangle, true, span);
            return true;
        }
        var shapeStart = FirstShapeIndex(token); var id = (shapeStart < 0 ? token : token.Substring(0, shapeStart)).Trim();
        if (!ValidId(id)) { diagnostic = "Block items require a single identifier before their label or shape."; return false; }
        var shape = BlockLayoutShape.Rectangle; var label = id; var directions = BlockLayoutArrowDirections.Right;
        if (shapeStart >= 0) {
            var body = token.Substring(shapeStart); string open = "", close = "";
            var pairs = new (string Open, string Close, BlockLayoutShape Shape)[] {
                ("(((", ")))", BlockLayoutShape.DoubleCircle),
                ("[[", "]]", BlockLayoutShape.Subroutine), ("[(", ")]", BlockLayoutShape.Database),
                ("([", "])", BlockLayoutShape.Stadium), ("((", "))", BlockLayoutShape.Circle),
                ("{{", "}}", BlockLayoutShape.Hexagon), ("[/", "/]", BlockLayoutShape.Parallelogram),
                ("[\\", "\\]", BlockLayoutShape.ParallelogramAlt), ("[/", "\\]", BlockLayoutShape.Trapezoid),
                ("[\\", "/]", BlockLayoutShape.TrapezoidAlt), ("[", "]", BlockLayoutShape.Rectangle),
                ("(", ")", BlockLayoutShape.Rounded), ("{", "}", BlockLayoutShape.Diamond), (">", "]", BlockLayoutShape.Asymmetric)
            };
            if (body.StartsWith("<[", StringComparison.Ordinal)) {
                var end = body.LastIndexOf("]>", StringComparison.Ordinal);
                if (end < 2) { diagnostic = "Directional block arrows require <[label]>(directions)."; return false; }
                open = "<["; close = "]>"; label = body.Substring(2, end - 2);
                var tail = body.Substring(end + 2).Trim(); directions = 0;
                if (!tail.StartsWith("(", StringComparison.Ordinal) || !tail.EndsWith(")", StringComparison.Ordinal)) {
                    diagnostic = "Directional block arrows require an explicit direction list."; return false;
                }
                foreach (var direction in tail.Substring(1, tail.Length - 2).Split(',')) {
                    switch (direction.Trim().ToLowerInvariant()) {
                        case "right": directions |= BlockLayoutArrowDirections.Right; break;
                        case "left": directions |= BlockLayoutArrowDirections.Left; break;
                        case "up": directions |= BlockLayoutArrowDirections.Up; break;
                        case "down": directions |= BlockLayoutArrowDirections.Down; break;
                        case "x": directions |= BlockLayoutArrowDirections.Horizontal; break;
                        case "y": directions |= BlockLayoutArrowDirections.Vertical; break;
                        default: diagnostic = "Unknown block arrow direction: " + direction + "."; return false;
                    }
                }
                shape = BlockLayoutShape.Arrow;
            } else {
                foreach (var pair in pairs) if (body.StartsWith(pair.Open, StringComparison.Ordinal) && body.EndsWith(pair.Close, StringComparison.Ordinal) && body.Length >= pair.Open.Length + pair.Close.Length) {
                    open = pair.Open; close = pair.Close; shape = pair.Shape; break;
                }
                if (open.Length == 0) { diagnostic = "Block shape delimiters are not balanced or supported."; return false; }
                label = body.Substring(open.Length, body.Length - open.Length - close.Length);
            }
            label = MermaidParserUtilities.Unquote(label.Trim()); if (label.Length == 0) label = id;
        }
        item = new MermaidBlockItem(id, label, columnSpan, shape, false, span) { ArrowDirections = directions };
        item.Classes.AddRange(classes); return true;
    }

    private static bool ValidId(string id) {
        if (id.Length == 0) return false;
        foreach (var character in id) if (char.IsWhiteSpace(character) || "[](){}<>:;|,\"'`".IndexOf(character) >= 0) return false;
        return true;
    }
    private static bool HasShape(string token) => FirstShapeIndex(token) >= 0;
    private static int FirstShapeIndex(string token) {
        for (var index = 0; index < token.Length; index++) if ("[({<>".IndexOf(token[index]) >= 0) return index;
        return -1;
    }
    private static bool ReadSpan(ref string token, out int span) {
        span = 1; var colon = -1;
        for (var index = 0; index < token.Length; index++) if (token[index] == ':' && IsTopLevel(token, index)) colon = index;
        if (colon < 0) return true;
        if (!int.TryParse(token.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out span) || span < 1 || span > 24) return false;
        token = token.Substring(0, colon).Trim(); return true;
    }
    private static bool IsTopLevel(string text, int position) {
        var depth = 0; var quote = '\0';
        for (var index = 0; index < position; index++) Advance(text[index], index, text, ref depth, ref quote);
        return depth == 0 && quote == '\0';
    }
    private static void Advance(char character, int index, string text, ref int depth, ref char quote) {
        if (quote != '\0') { if (character == quote && (index == 0 || text[index - 1] != '\\')) quote = '\0'; return; }
        if (character is '"' or '\'' or '`') { quote = character; return; }
        if (character is '[' or '(' or '{' or '<') depth++;
        else if (character is ']' or ')' or '}' || character == '>' && depth > 0) depth = Math.Max(0, depth - 1);
        else if (character == '>' && depth == 0 && index > 0 && text[index - 1] != '-' && text[index - 1] != '=') depth++;
    }
    private static List<(string Text, int Offset)> SplitTokens(string text) {
        var tokens = new List<(string, int)>(); var start = -1; var depth = 0; var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var character = text[index];
            if (char.IsWhiteSpace(character) && depth == 0 && quote == '\0') {
                if (start >= 0) tokens.Add((text.Substring(start, index - start), start)); start = -1;
            } else { if (start < 0) start = index; Advance(character, index, text, ref depth, ref quote); }
        }
        if (start >= 0) tokens.Add((text.Substring(start), start)); return tokens;
    }
    private static string InlineEdgeLabel(ref string token) {
        foreach (var separator in new[] { "--", "==", "-." }) {
            for (var i = 1; i + separator.Length < token.Length; i++) {
                if (!IsTopLevel(token, i) || string.CompareOrdinal(token, i, separator, 0, separator.Length) != 0) continue;
                var tail = token.Substring(i + separator.Length).Trim();
                if (tail.Length < 2 || tail[0] != '"' || tail[tail.Length - 1] != '"') continue;
                var label = MermaidParserUtilities.Unquote(tail); token = token.Substring(0, i).Trim(); return label;
            }
        }
        return "";
    }

    private static List<(string Text, string Arrow)> EdgeParts(string text) {
        var result = new List<(string, string)>(); var start = 0; var depth = 0; var quote = '\0'; var previous = "";
        var arrows = new[] { "-.->", "-->", "---", "==>", "===", "-.-", "~~~" };
        for (var index = 0; index < text.Length; index++) {
            string? found = null;
            if (depth == 0 && quote == '\0') foreach (var arrow in arrows)
                if (index + arrow.Length <= text.Length && string.CompareOrdinal(text, index, arrow, 0, arrow.Length) == 0) { found = arrow; break; }
            if (found == null) { Advance(text[index], index, text, ref depth, ref quote); continue; }
            result.Add((text.Substring(start, index - start).Trim(), previous)); previous = found;
            index += found.Length - 1; start = index + 1;
        }
        result.Add((text.Substring(start).Trim(), previous)); return result;
    }
    private static bool StartsWithKeyword(string text, string keyword) => text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)
        && (text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]) || text[keyword.Length] == ':');
}
