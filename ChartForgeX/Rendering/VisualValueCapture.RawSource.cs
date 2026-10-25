using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualValueCapture {
    /// <summary>Reflow keeps the original complete value source; raw rows are never inferred from filtered paint.</summary>
    internal static bool TryRawSource(PreparedVisual drawing, out IVisualRawValuePlan? plan, out PreparedVisual complete) {
        var visibility = drawing.Scene.VisibilityCapture;
        complete = visibility?.ReflowSource == null ? drawing : visibility.CompleteValueBasis;
        plan = complete.Scene.ValueCapture?.Plan as IVisualRawValuePlan;
        return plan?.CapturesRawSource == true;
    }
}
