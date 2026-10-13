using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void FitOversizedDataLabels(VisualSceneBuilder builder, ChartRect plot,
        List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var placement = new LabelPlacementService();
        foreach (var request in labels) {
            if (request.Style.EffectiveFontSize <= VisualTextFitting.MaximumOrdinaryFontSize) continue;
            var fitted = VisualTextFitting.FitOversized(builder, request.Style,
                request.Bounds?.Height ?? plot.Height, style => {
                    // Test the complete caption against the real mark-relative candidates.
                    // Shortening remains the final scene's policy after typography fits.
                    var probe = new LabelPlacementRequest(request.Text, request.Anchor, style, request.Candidates) {
                        AssociatedMarkId = request.AssociatedMarkId, Bounds = request.Bounds,
                        Padding = request.Padding, Fallback = LabelFallbackRule.Drop
                    };
                    return !placement.Place(new[] { probe }, plot, obstacles, 2, builder.MeasureText)[0].IsDropped;
                });
            request.Style.FontSize = fitted.FontSize;
            request.MeasuredSize = null;
            var ink = ((CartesianLabels)labels).ContainedInk;
            if (ink.TryGetValue(request, out var inside)) {
                var style = inside.Style.Clone();
                style.FontSize = fitted.FontSize;
                ink[request] = (inside.Bounds, style, inside.Paint);
            }
        }
    }
}
