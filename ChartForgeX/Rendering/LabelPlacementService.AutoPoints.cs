using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

public sealed partial class LabelPlacementService {
    /// <summary>Tries bounded full-caption lanes after the authored Auto point's ordinary candidates.</summary>
    /// <remarks>These are candidates only: the canonical placement loop still checks the frame and every mark/label.
    /// No data values or axis transformations are interpreted here; mapped rectangles carry orientation and scale.</remarks>
    private static IEnumerable<LabelCandidate> AutoPointCandidates(LabelPlacementRequest request, double width, double height,
        LabelSpatialIndex occupied) {
        var mark = request.AutoPointMarkBounds!.Value;
        var anchor = request.Anchor;
        var spacing = request.AutoPointSpacing;
        // A sloping stroke at a first/last observation may block the first lane without
        // requiring shortening. Use measured half/full text-height lanes, as for callouts.
        foreach (var fraction in new[] { .5, 1d }) {
            var above = mark.Top - anchor.Y - spacing - height * fraction;
            var below = mark.Bottom - anchor.Y + spacing + height * fraction;
            foreach (var alignment in new[] { .5, 0, 1 }) {
                yield return new LabelCandidate(0, above, alignment, 1);
                yield return new LabelCandidate(0, below, alignment, 0);
                yield return new LabelCandidate(mark.Left - anchor.X - spacing - width * fraction, 0, 1, alignment);
                yield return new LabelCandidate(mark.Right - anchor.X + spacing + width * fraction, 0, 0, alignment);
            }
            // Keep a full caption beside an endpoint when the inward lane meets a contour.
            foreach (var side in new[] { -1d, 1d }) {
                var offset = side < 0 ? mark.Left - anchor.X - spacing : mark.Right - anchor.X + spacing;
                yield return new LabelCandidate(offset, above, side < 0 ? 1 : 0, 1);
                yield return new LabelCandidate(offset, below, side < 0 ? 1 : 0, 0);
            }
        }
        // A point can lie inside a companion bar. Its own marker-relative lanes cannot
        // escape that solid obstacle; use its actual enclosing bounds, independent of
        // series order, axis reversal, logarithmic mapping or primary/secondary axes.
        var containing = occupied.ContainingMarks(anchor, request.AssociatedMarkId);
        if (!containing.HasValue) yield break;
        var outer = containing.Value;
        // A sloping contour can cross the first solid-mark edge lane as well. A
        // bounded two measured-height/width retreat keeps full text ahead of ellipsis.
        foreach (var fraction in new[] { 0d, .5, 1, 2 }) {
            foreach (var alignment in new[] { .5, 0, 1 }) {
                yield return new LabelCandidate(0, outer.Top - anchor.Y - spacing - height * fraction, alignment, 1);
                yield return new LabelCandidate(0, outer.Bottom - anchor.Y + spacing + height * fraction, alignment, 0);
                yield return new LabelCandidate(outer.Left - anchor.X - spacing - width * fraction, 0, 1, alignment);
                yield return new LabelCandidate(outer.Right - anchor.X + spacing + width * fraction, 0, 0, alignment);
            }
        }
    }
}
