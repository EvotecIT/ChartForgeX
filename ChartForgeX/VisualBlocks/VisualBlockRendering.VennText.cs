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
        var requests = new List<LabelPlacementRequest>();
        var colors = new List<ChartColor>();
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

        var result = new List<VennCaption>();
        var placed = VennTextPlacement.Place(requests, body, obstacles: null, gap: gap, measureText: TextLayoutEngine.Measure);
        for (var index = 0; index < placed.Count; index++) {
            var label = placed[index];
            if (label.IsDropped) continue;
            result.Add(new VennCaption(label.Text, label.Bounds, label.Request.Style, colors[index]));
        }
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
            var textBudget = heightBudget ?? lineBudget;
            var style = FitBodyTextStyle(theme, text, defaultSize, width, textBudget, weight);
            var displayed = string.Empty;
            var metrics = default(TextMetrics);
            var top = body.Top; var bottom = body.Bottom;
            var maximum = style.FontSize;
            // Fit the complete box at the actual candidate font. Its feasible vertical
            // interval comes from the circle chords, not from the source baseline.
            if (!Fits(maximum)) {
                var fitted = Math.Max(.001, ChartTextFitting.FitFontSize(0, maximum, Fits));
                if (!Fits(fitted)) return;
            }
            // Omit a caption that only fits below one source pixel; retain deliberately smaller authored typography.
            if (style.FontSize < Math.Min(1, BodyFontSize(theme, defaultSize))) return;
            var ascent = VisualBlockRendering.TextAscent(style);
            var candidates = new List<LabelCandidate> { new(0, 0, .5) };
            // Keep authored region anchors when clear. Nearby vertical lanes reserve the
            // complete measured line box rather than relying on a fixed baseline offset.
            for (var lane = 1; lane <= Math.Max(8, captionCount * 2); lane++) {
                var distance = (metrics.Height + gap) * lane / 2;
                candidates.Add(new LabelCandidate(0, distance, .5));
                candidates.Add(new LabelCandidate(0, -distance, .5));
            }
            // A discrete lane may jump over a short feasible interval. Its exact ends
            // are valid candidates under the same canonical collision and membership checks.
            candidates.Add(new LabelCandidate(0, top - (baseline - ascent), .5));
            candidates.Add(new LabelCandidate(0, bottom - metrics.Height - (baseline - ascent), .5));
            // Every corner of the text box remains in each referenced circle. Vertical
            // relocation must not turn a set caption into a caption of a different set.
            requests.Add(new LabelPlacementRequest(displayed, new ChartPoint(x, baseline - ascent), style, candidates) {
                Fallback = LabelFallbackRule.Drop,
                Bounds = new ChartRect(x - metrics.Width / 2, top, metrics.Width, Math.Max(0, bottom - top))
            });
            colors.Add(color);

            bool Fits(double size) {
                style.FontSize = size;
                displayed = FitMeasuredText(text, size, width, theme.FontFamily, weight);
                metrics = TextLayoutEngine.Measure(displayed, style);
                top = body.Top; bottom = body.Bottom;
                foreach (var placement in placements) {
                    if (!ContainsId(ids, placement.Set.Id)) continue;
                    var horizontalReach = Math.Abs(x - placement.X) + metrics.Width / 2;
                    if (horizontalReach > placement.Radius) return false;
                    var verticalReach = Math.Sqrt(Math.Max(0, placement.Radius * placement.Radius - horizontalReach * horizontalReach));
                    top = Math.Max(top, placement.Y - verticalReach); bottom = Math.Min(bottom, placement.Y + verticalReach);
                }
                return displayed.Length > 0 && bottom - top >= metrics.Height;
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
