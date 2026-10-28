using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void FitOversizedDataLabels(VisualSceneBuilder builder, ChartRect plot,
        List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        if (!labels.Any(request => request.Style.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize)) return;
        var placement = new LabelPlacementService();
        var occupied = new List<LabelObstacle>(obstacles);
        var ordinal = 0;
        // Match the final placement order. A later oversized caption must fit around
        // the earlier captions as well as marks, rather than being dropped afterwards.
        foreach (var request in labels.OrderByDescending(request => request.Priority)) {
            if (request.Style.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize) {
                var fitted = VisualTextFitting.FitOversized(builder, request.Style,
                    request.Bounds?.Height ?? plot.Height, style => {
                        // Test the complete caption against the real mark-relative candidates.
                        // Shortening remains the final scene's policy after typography fits.
                        var probe = new LabelPlacementRequest(request.Text, request.Anchor, style, request.Candidates) {
                            AssociatedMarkId = request.AssociatedMarkId, Bounds = request.Bounds,
                            Padding = request.Padding, Fallback = LabelFallbackRule.Drop,
                            AutoPointMarkBounds = request.AutoPointMarkBounds, AutoPointSpacing = request.AutoPointSpacing
                        };
                        return !placement.Place(new[] { probe }, plot, occupied, 2, builder.MeasureText)[0].IsDropped;
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
            var placed = placement.Place(new[] { request }, plot, occupied, 2, builder.MeasureText)[0];
            if (!placed.IsDropped) occupied.Add(new LabelObstacle("fitted-caption-" + ordinal, placed.Bounds));
            ordinal++;
        }
    }
}
