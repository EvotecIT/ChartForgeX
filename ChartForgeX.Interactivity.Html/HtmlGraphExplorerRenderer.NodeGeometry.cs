using System;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private const double CardNodeThreshold = 34;
    private const double CardNodeHalfHeight = 36;

    private static bool IsCardNode(GraphSceneNode node) {
        return EffectiveNodeShape(node) == GraphNodeShape.Box && SafeNodeSize(node) >= CardNodeThreshold;
    }

    private static double BoxHalfWidth(double size) {
        return size * 1.45;
    }

    private static double BoxHalfHeight(double size) {
        return Math.Min(size * 1.05, CardNodeHalfHeight);
    }

    private static string CardText(string value, double size, bool secondary = false) {
        if (string.IsNullOrEmpty(value)) return value;
        var availableWidth = Math.Max(28, BoxHalfWidth(size) * 2 - 74);
        var characterWidth = secondary ? 5.4 : 6.8;
        var maximumCharacters = Math.Max(6, (int)Math.Floor(availableWidth / characterWidth));
        if (value.Length <= maximumCharacters) return value;
        var retained = maximumCharacters - 1;
        var head = (int)Math.Ceiling(retained * 0.58);
        var tail = retained - head;
        return value.Substring(0, head) + "…" + value.Substring(value.Length - tail);
    }
}
