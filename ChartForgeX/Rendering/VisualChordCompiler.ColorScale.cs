using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualChordCompiler {
    /// <summary>Uses the captured shared flow key before weighted geometry is prepared.</summary>
    private static ChartRect ColorScale(VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartFlowColorSurface surface, VisualThemeColors colors) =>
        surface.AllocateKey(context, builder, plot, colors, "chord");
}
