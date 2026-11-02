using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    private static readonly LabelPlacementService VennTextPlacement = new();

    public static IReadOnlyList<VennCaption> VennCaptions(VennDiagramBlock block, IReadOnlyList<VennSetPlacement> placements, ChartRect body) {
        var theme = block.Options.ResolvedTheme;
        var result = new List<VennCaption>();
        var occupied = new List<LabelObstacle>();
        var radius = placements.Max(item => item.Radius);
        var centerY = placements.Average(item => item.Y);
        var captionCount = placements.Count + block.Intersections.Count + block.TextNodes.Count;
        var lineBudget = Math.Min(radius * .24, Math.Max(18, radius * 1.4 / (captionCount + 1)));
        var denseTextBudget = block.TextNodes.Count > 17 ? Math.Min(lineBudget, radius / (block.TextNodes.Count + 1)) : lineBudget;
        var gap = Math.Min(3, denseTextBudget * .15);
        foreach (var placement in placements) {
            if (placement.Radius <= 0) continue;
            // A lower set owns its outward lower region, not the pair-intersection lanes above it.
            var direction = placement.Y > centerY ? 1 : -1;
            Add(new[] { placement.Set.Id }, placement.Set.Label, placement.X, placement.Y + direction * placement.Radius * .52, placement.Radius * 1.16, 13.5, 800, placement.Set.TextColor ?? theme.Text);
        }
        foreach (var intersection in block.Intersections)
            AddRegion(intersection.SetIds, intersection.Label, 12.5, 700, intersection.TextColor ?? theme.Text);
        foreach (var text in block.TextNodes)
            AddRegion(text.SetIds, text.Label, 11.5, 600, text.TextColor ?? theme.MutedText, denseTextBudget);

        return result;

        void AddRegion(IReadOnlyList<string> ids, string text, double defaultSize, int weight, ChartColor color, double? heightBudget = null) {
            var circles = placements.Select(item => new VennCircle(item.X, item.Y, item.Radius)).ToArray();
            var indices = ids.Select(id => { for (var i = 0; i < placements.Count; i++) if (placements[i].Set.Id == id) return i; throw new InvalidOperationException("Unknown Venn set."); }).ToArray();
            var geometry = VennIntersection(circles, indices);
            if (geometry.Area <= 0) return;
            var center = geometry.Anchor;
            if (ids.Count == 2) {
                // Prefer the pair's exclusive side when a third set shares the center.
                var others = placements.Where(item => !ContainsId(ids, item.Set.Id)).ToArray();
                if (others.Length > 0) {
                    var dx = center.X - others.Average(item => item.X);
                    var dy = center.Y - others.Average(item => item.Y);
                    var distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance > 0) center = new ChartPoint(center.X + dx / distance * radius * .12, center.Y + dy / distance * radius * .12);
                }
            }
            var width = Math.Max(42, radius * (ids.Count == 1 ? .95 : .72));
            var offset = ids.Count == 1 ? radius * .12 : ids.Count == 2 ? 5 : 18;
            Add(ids, text, center.X, center.Y + offset, width, defaultSize, weight, color, heightBudget);
        }

        void Add(IReadOnlyList<string> ids, string text, double x, double baseline, double width, double defaultSize, int weight, ChartColor color, double? heightBudget = null) {
            if (text.Length == 0) return;
            text = SingleLineText(text);
            var textBudget = heightBudget ?? lineBudget;
            var style = FitBodyTextStyle(theme, text, defaultSize, width, textBudget, weight);
            PlacedLabel? placed = null;
            var maximum = style.FontSize;
            // Fit complete captions against both their circle chords and earlier labels.
            // A font that fills the lens can otherwise leave no collision-free lane.
            if (!Fits(maximum)) {
                var fitted = Math.Max(.001, ChartTextFitting.FitFontSize(0, maximum, Fits));
                if (!Fits(fitted)) return;
            }
            // Omit a caption that only fits below one source pixel; retain deliberately smaller authored typography.
            if (style.FontSize < Math.Min(1, BodyFontSize(theme, defaultSize))) return;
            var label = placed!;
            result.Add(new VennCaption(label.Text, label.Bounds, label.Request.Style, color));
            occupied.Add(new LabelObstacle("venn-caption-" + result.Count, label.Bounds));

            bool Fits(double size) {
                style.FontSize = size;
                var metrics = TextLayoutEngine.Measure(text, style);
                if (metrics.Width > width || metrics.Height > textBudget) return false;
                var top = body.Top; var bottom = body.Bottom;
                foreach (var placement in placements) {
                    if (!ContainsId(ids, placement.Set.Id)) continue;
                    var horizontalReach = Math.Abs(x - placement.X) + metrics.Width / 2;
                    if (horizontalReach > placement.Radius) return false;
                    var verticalReach = Math.Sqrt(Math.Max(0, placement.Radius * placement.Radius - horizontalReach * horizontalReach));
                    top = Math.Max(top, placement.Y - verticalReach); bottom = Math.Min(bottom, placement.Y + verticalReach);
                }
                if (text.Length == 0 || bottom - top < metrics.Height) return false;
                var ascent = VisualBlockRendering.TextAscent(style);
                var candidates = new List<LabelCandidate> { new(0, 0, .5) };
                // Reserve the measured line box when moving between vertical lanes.
                for (var lane = 1; lane <= Math.Max(8, captionCount * 2); lane++) {
                    var distance = (metrics.Height + gap) * lane / 2;
                    candidates.Add(new LabelCandidate(0, distance, .5));
                    candidates.Add(new LabelCandidate(0, -distance, .5));
                }
                // Include exact interval ends when a discrete lane would jump over it.
                candidates.Add(new LabelCandidate(0, top - (baseline - ascent), .5));
                candidates.Add(new LabelCandidate(0, bottom - metrics.Height - (baseline - ascent), .5));
                // Chords at the box's horizontal extremes keep all four corners inside
                // every referenced set, including after vertical relocation.
                var request = new LabelPlacementRequest(text, new ChartPoint(x, baseline - ascent), style, candidates) {
                    Fallback = LabelFallbackRule.Drop,
                    Bounds = new ChartRect(x - metrics.Width / 2, top, metrics.Width, bottom - top)
                };
                placed = VennTextPlacement.Place(new[] { request }, body, occupied, gap, TextLayoutEngine.Measure)[0];
                return !placed.IsDropped;
            }
        }

        bool ContainsId(IReadOnlyList<string> ids, string id) {
            foreach (var candidate in ids) if (candidate == id) return true;
            return false;
        }
    }

    public readonly struct VennCaption {
        public VennCaption(string text, ChartRect bounds, TextStyle style, ChartColor color) {
            Text = text; Bounds = bounds; Style = style; Color = color;
        }
        public string Text { get; }
        public ChartRect Bounds { get; }
        public TextStyle Style { get; }
        public ChartColor Color { get; }
        public double Baseline => Bounds.Top + VisualBlockRendering.TextAscent(Style);
    }
}
