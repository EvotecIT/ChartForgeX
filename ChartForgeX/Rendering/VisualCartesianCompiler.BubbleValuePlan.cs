using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Actual Bubble size facts specialize the same complete component owner used by native tuples.</summary>
    private sealed class BubbleValuePlan : ComponentValuePlan {
        internal BubbleValuePlan(ChartVisibilitySource source, VisualRenderContext context, VisualSceneTextCapture text,
            ChartCartesianProjection axes, ChartBubbleSizeScale sizes)
            : base(source, context, text, axes, "bubble-components", new VisualComponentSizePolicy(sizes)) { }
    }
}
