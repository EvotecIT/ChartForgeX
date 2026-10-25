using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateViewportPadding(ChartPadding? padding) {
        if (!padding.HasValue) return;
        var value = padding.Value;
        NonNegativeOptional(value.Left, "topology viewport left padding"); NonNegativeOptional(value.Top, "topology viewport top padding");
        NonNegativeOptional(value.Right, "topology viewport right padding"); NonNegativeOptional(value.Bottom, "topology viewport bottom padding");
    }
}
