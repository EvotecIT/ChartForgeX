using System;
using System.Collections.Generic;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private const double CardNodeHalfHeight = 36;

    private static bool IsCardNode(GraphSceneNode node) {
        return EffectiveNodeShape(node) == GraphNodeShape.Box
            && node.Metadata.TryGetValue("topology.card", out var card)
            && string.Equals(card, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static double BoxHalfWidth(double size) {
        return size * 1.45;
    }

    private static double BoxHalfHeight(double size) {
        return Math.Min(size * 1.05, CardNodeHalfHeight);
    }

    private static double BoxHalfHeight(GraphSceneNode? node, double size) {
        return node != null && IsCardNode(node) ? BoxHalfHeight(size) : size * 1.05;
    }

    private static string CardText(string value, double size, bool secondary = false) {
        if (string.IsNullOrEmpty(value)) return value;
        var availableWidth = CardTextAvailableWidth(size);
        if (EstimatedCardTextWidth(value, secondary) <= availableWidth) return value;

        var elements = UnicodeScalarTextElements(value);
        for (var retained = elements.Count - 1; retained > 1; retained--) {
            var head = (int)Math.Ceiling(retained * 0.58);
            var tail = retained - head;
            var candidate = string.Concat(elements.GetRange(0, head)) + "…" + string.Concat(elements.GetRange(elements.Count - tail, tail));
            if (EstimatedCardTextWidth(candidate, secondary) <= availableWidth) return candidate;
        }

        return "…";
    }

    private static double CardTextAvailableWidth(double size) {
        return Math.Max(28, BoxHalfWidth(size) * 2 - 78);
    }

    private static double EstimatedCardTextWidth(string value, bool secondary) {
        var scale = secondary ? 0.8 : 1;
        var width = 0d;
        foreach (var element in UnicodeScalarTextElements(value)) {
            if (element.Length > 1) {
                width += 12;
                continue;
            }

            var character = element[0];
            width += character switch {
                ' ' => 3.5,
                '.' or ',' or ':' or ';' or '!' or '|' => 4,
                '-' or '_' or '/' or '\\' or '(' or ')' or '[' or ']' => 5,
                'I' or 'J' or 'L' or 'T' or 'f' or 'i' or 'j' or 'l' or 't' or '1' => 5.4,
                'M' or 'W' or 'Q' or 'O' or '@' or '%' or '&' => 10,
                '…' => 10,
                >= 'A' and <= 'Z' => 8.2,
                >= '0' and <= '9' => 7.2,
                _ => 6.8
            };
        }

        return width * scale;
    }

    private static List<string> UnicodeScalarTextElements(string value) {
        var elements = new List<string>(value.Length);
        for (var index = 0; index < value.Length;) {
            var length = index + 1 < value.Length && char.IsSurrogatePair(value[index], value[index + 1]) ? 2 : 1;
            elements.Add(value.Substring(index, length));
            index += length;
        }

        return elements;
    }
}
