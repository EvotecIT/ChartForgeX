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
        var captionCount = placements.Count + block.Intersections.Count + block.TextNodes.Count;
        var lineBudget = Math.Min(radius * .24, Math.Max(18, radius * 1.4 / (captionCount + 1)));
        var denseTextBudget = block.TextNodes.Count > 17 ? Math.Min(lineBudget, radius / (block.TextNodes.Count + 1)) : lineBudget;
        var gap = Math.Min(3, denseTextBudget * .15);
        foreach (var placement in placements) {
            if (placement.Radius <= 0) continue;
            Add(new[] { placement.Set.Id }, placement.Set.Label, placement.X, placement.Y - placement.Radius * .52, placement.Radius * 1.16, 13.5, 800, placement.Set.TextColor ?? theme.Text);
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
            var width = Math.Max(42, radius * (ids.Count == 1 ? .95 : .72));
            var offset = ids.Count == 1 ? radius * .12 : ids.Count == 2 ? 5 : 18;
            Add(ids, text, center.X, center.Y + offset, width, defaultSize, weight, color, heightBudget);
        }

        void Add(IReadOnlyList<string> ids, string text, double x, double baseline, double width, double defaultSize, int weight, ChartColor color, double? heightBudget = null) {
            if (text.Length == 0) return;
            var textBudget = heightBudget ?? lineBudget;
            // Reserve the height-capped line around its baseline, not the whole
            // allocation below it. The final width fit can only reduce this box.
            var heightStyle = FitBodyTextStyle(theme, text, defaultSize, double.MaxValue, textBudget, weight);
            var lineTop = baseline - TextAscent(heightStyle);
            var lineBottom = lineTop + TextLayoutEngine.Measure("Mg", heightStyle).Height;
            foreach (var placement in placements) {
                if (!ContainsId(ids, placement.Set.Id)) continue;
                var verticalReach = Math.Max(Math.Abs(lineTop - placement.Y), Math.Abs(lineBottom - placement.Y));
                var horizontalReach = Math.Sqrt(Math.Max(0, placement.Radius * placement.Radius - verticalReach * verticalReach));
                width = Math.Min(width, Math.Max(1, 2 * (horizontalReach - Math.Abs(x - placement.X))));
            }
            var style = FitBodyTextStyle(theme, text, defaultSize, width, textBudget, weight);
            // Omit a caption that only fits below one source pixel; retain deliberately smaller authored typography.
            if (style.FontSize < Math.Min(1, BodyFontSize(theme, defaultSize))) return;
            var displayed = FitMeasuredText(text, style.FontSize, width, theme.FontFamily, weight);
            var metrics = TextLayoutEngine.Measure(displayed, style);
            var ascent = VisualBlockRendering.TextAscent(style);
            var candidates = new List<LabelCandidate> { new(0, 0, .5) };
            // Keep authored region anchors when clear. Nearby vertical lanes reserve the
            // complete measured line box rather than relying on a fixed baseline offset.
            for (var lane = 1; lane <= Math.Max(8, captionCount * 2); lane++) {
                var distance = (metrics.Height + gap) * lane / 2;
                candidates.Add(new LabelCandidate(0, distance, .5));
                candidates.Add(new LabelCandidate(0, -distance, .5));
            }
            var top = body.Top; var bottom = body.Bottom;
            foreach (var placement in placements) {
                if (!ContainsId(ids, placement.Set.Id)) continue;
                var horizontalReach = Math.Abs(x - placement.X) + metrics.Width / 2;
                var verticalReach = Math.Sqrt(Math.Max(0, placement.Radius * placement.Radius - horizontalReach * horizontalReach));
                top = Math.Max(top, placement.Y - verticalReach); bottom = Math.Min(bottom, placement.Y + verticalReach);
            }
            // Every corner of the text box remains in each referenced circle. Vertical
            // relocation must not turn a set caption into a caption of a different set.
            requests.Add(new LabelPlacementRequest(displayed, new ChartPoint(x, baseline - ascent), style, candidates) {
                Fallback = LabelFallbackRule.Drop,
                Bounds = new ChartRect(x - metrics.Width / 2, top, metrics.Width, Math.Max(0, bottom - top))
            });
            colors.Add(color);
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
