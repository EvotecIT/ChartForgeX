using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualSankeyCompiler {
    /// <summary>Uses the captured shared flow key; ribbon visibility never changes its observed color domain.</summary>
    private static ChartRect ColorScale(VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartFlowColorSurface surface, VisualThemeColors colors) =>
        surface.AllocateKey(context, builder, plot, colors, "sankey");
}
