using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidC4Parser {
    private static void ParseLayoutConfig(MermaidC4Document document, CallStatement call, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var shapes = Count(call.Arguments.Count > 0 ? Clean(call.Arguments[0]) : null);
        var boundaries = Count(call.Arguments.Count > 1 ? Clean(call.Arguments[1]) : null);
        if (shapes.HasValue) document.MaximumShapesPerRow = shapes;
        if (boundaries.HasValue) document.MaximumBoundariesPerRow = boundaries;
        if (call.Arguments.Count > 2 || HasNamedUnsupported(call.Arguments, 0, "c4shapeinrow", "c4boundaryinrow"))
            Retained("Unknown C4 layout settings remain retained: ");
        // The pinned layout updater consumes the two signature positions, including named-value objects.
        // Integer-prefix parsing follows its source owner; a larger-than-native count is equivalent to no wrap.
        int? Count(string? value) {
            if (value == null || value.Length == 0) return null;
            value = value.TrimStart();
            if (value.Length == 0) { Retained("An unparseable C4 row limit was ignored: "); return null; }
            var index = 0; var negative = false;
            if (value[index] == '+' || value[index] == '-') { negative = value[index] == '-'; index++; }
            var radix = 10;
            if (index + 1 < value.Length && value[index] == '0' && (value[index + 1] == 'x' || value[index + 1] == 'X')) { radix = 16; index += 2; }
            var firstDigit = index; var count = 0;
            for (; index < value.Length; index++) {
                var digit = value[index] >= '0' && value[index] <= '9' ? value[index] - '0'
                    : radix == 16 && value[index] >= 'a' && value[index] <= 'f' ? value[index] - 'a' + 10
                    : radix == 16 && value[index] >= 'A' && value[index] <= 'F' ? value[index] - 'A' + 10 : -1;
                if (digit < 0 || digit >= radix) break;
                count = count > (int.MaxValue - digit) / radix ? int.MaxValue : count * radix + digit;
            }
            if (index > firstDigit && !negative && count > 0) return count;
            Retained("A nonpositive or unparseable C4 row limit was ignored, retaining its previous value: "); return null;
        }
        void Retained(string message) => Retain(document, result, document.Statements[document.Statements.Count - 1].Text, span, message);
    }

    private static void ParseElementPresentation(MermaidC4Document document, Dictionary<string, MermaidC4Element> elements,
        Dictionary<string, MermaidC4Boundary> boundaries, CallStatement call, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (call.Arguments.Count == 0) { Error("UpdateElementStyle requires an existing element alias."); return; }
        var alias = Clean(call.Arguments[0]); elements.TryGetValue(alias, out var element); boundaries.TryGetValue(alias, out var boundary);
        if (element == null && boundary == null) { Error("Unknown C4 element style target '" + alias + "'."); return; }
        var style = new MermaidStyleDefinition(alias, span);
        Color("fill", OptionalArg(call.Arguments, 1, 0, "bgColor")); Color("color", OptionalArg(call.Arguments, 1, 1, "fontColor")); Color("stroke", OptionalArg(call.Arguments, 1, 2, "borderColor"));
        document.ElementStyles.Add(style);
        var shadow = PresentationArg(call.Arguments, 1, 3, "shadowing");
        var shape = PresentationArg(call.Arguments, 1, 4, "shape"); var sprite = PresentationArg(call.Arguments, 1, 5, "sprite");
        var technology = PresentationArg(call.Arguments, 1, 6, "techn"); var legend = PresentationArg(call.Arguments, 1, 7, "legendText");
        var legendSprite = PresentationArg(call.Arguments, 1, 8, "legendSprite");
        if (element != null) {
            if (shape != null) element.Shape = shape; if (sprite != null) element.Sprite = sprite;
            if (technology != null) element.Technology = technology;
            if (legend != null) element.LegendText = legend; if (legendSprite != null) element.LegendSprite = legendSprite;
        } else if (boundary != null) {
            if (shape != null) boundary.Shape = shape; if (sprite != null) boundary.Sprite = sprite;
            if (legend != null) boundary.LegendText = legend; if (legendSprite != null) boundary.LegendSprite = legendSprite;
            if (!string.IsNullOrEmpty(shape) || !string.IsNullOrEmpty(technology)) Retained("C4 boundary shape and technology updates remain metadata: ");
        }
        if (!string.IsNullOrEmpty(shape) && !MermaidC4Shapes.IsKeyword(shape)) Retained("Unsupported C4 shape token remains metadata: ");
        if (!string.IsNullOrEmpty(sprite) && !MermaidC4Shapes.IsKeyword(sprite)) Retained("External C4 sprites are not loaded; source metadata remains retained: ");
        if (!string.IsNullOrEmpty(legendSprite) && !MermaidC4Shapes.IsKeyword(legendSprite)) Retained("External C4 legend sprites are not loaded; source metadata remains retained: ");
        if (!string.IsNullOrEmpty(shadow) && !string.Equals(shadow, "false", StringComparison.OrdinalIgnoreCase)) Retained("C4 shadowing is retained without a native shadow layer: ");
        if (call.Arguments.Count > 10 || HasNamedUnsupported(call.Arguments, "bgcolor", "fontcolor", "bordercolor", "shadowing", "shape", "sprite", "techn", "legendtext", "legendsprite"))
            Retained("Unsupported C4 element style arguments remain retained: ");
        void Color(string key, string? value) { if (value == null) return; if (MermaidStyleValues.IsColor(value)) style.Styles[key] = value; else Error("Invalid C4 style color: " + value + "."); }
        void Error(string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message);
        void Retained(string message) => Retain(document, result, document.Statements[document.Statements.Count - 1].Text, span, message);
    }

    // Empty strings can clear a previous shape/sprite/technology; only an absent argument is null.
    private static string? PresentationArg(IReadOnlyList<string> args, int firstOptional, int position, string name) {
        for (var index = firstOptional; index < args.Count; index++)
            if (TrySplitNamedArgument(args[index], out var candidate, out var value) && NormalizeName(candidate) == NormalizeName(name)) return Clean(value);
        var current = 0;
        for (var index = firstOptional; index < args.Count; index++) {
            if (TrySplitNamedArgument(args[index], out _, out _)) continue;
            if (current++ == position) return Clean(args[index]);
        }
        return null;
    }
}
