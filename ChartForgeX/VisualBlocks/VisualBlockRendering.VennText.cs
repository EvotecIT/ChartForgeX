using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    private static readonly LabelPlacementService VennTextPlacement = new();

    public static IReadOnlyList<VennCaption> VennCaptions(VennDiagramBlock block, IReadOnlyList<VennSetPlacement> placements, ChartRect body) {
        var theme = block.Options.Theme;
        var requests = new List<LabelPlacementRequest>();
        var colors = new List<ChartColor>();
        var radius = placements[0].Radius;
        var captionCount = placements.Count + block.Intersections.Count + block.TextNodes.Count;
        var lineBudget = Math.Min(radius * .24, Math.Max(18, radius * 1.4 / (captionCount + 1)));
        foreach (var placement in placements)
            Add(new[] { placement.Set.Id }, placement.Set.Label, placement.X, placement.Y - radius * .52, radius * 1.16, 13.5, 800, placement.Set.TextColor ?? theme.Text);
        foreach (var intersection in block.Intersections)
            AddRegion(intersection.SetIds, intersection.Label, 12.5, 700, intersection.TextColor ?? theme.Text);
        foreach (var text in block.TextNodes)
            AddRegion(text.SetIds, text.Label, 11.5, 600, text.TextColor ?? theme.MutedText);

        var result = new List<VennCaption>();
        var placed = VennTextPlacement.Place(requests, body, gap: 3);
        for (var index = 0; index < placed.Count; index++) {
            var label = placed[index];
            if (label.IsDropped) continue;
            result.Add(new VennCaption(label.Text, label.Bounds, label.Request.Style, colors[index]));
        }
        return result;

        void AddRegion(IReadOnlyList<string> ids, string text, double defaultSize, int weight, ChartColor color) {
            var center = VennRegionCenter(placements, ids);
            var width = Math.Max(42, radius * (ids.Count == 1 ? .95 : .72));
            var offset = ids.Count == 1 ? radius * .12 : ids.Count == 2 ? 5 : 18;
            Add(ids, text, center.X, center.Y + offset, width, defaultSize, weight, color);
        }

        void Add(IReadOnlyList<string> ids, string text, double x, double baseline, double width, double defaultSize, int weight, ChartColor color) {
            if (text.Length == 0) return;
            foreach (var placement in placements) {
                if (!ContainsId(ids, placement.Set.Id)) continue;
                var verticalReach = Math.Abs(baseline - placement.Y) + lineBudget;
                var horizontalReach = Math.Sqrt(Math.Max(0, radius * radius - verticalReach * verticalReach));
                width = Math.Min(width, Math.Max(1, 2 * (horizontalReach - Math.Abs(x - placement.X))));
            }
            var style = FitBodyTextStyle(theme, text, defaultSize, width, lineBudget, weight);
            var displayed = FitMeasuredText(text, style.FontSize, width, theme.FontFamily, weight);
            var metrics = ChartLabelScene.MeasureText(displayed, style);
            var ascent = TypographyFontResolver.ResolveFace(style.Font).Font?.Ascent(style.FontSize) ?? style.FontSize * .82;
            var candidates = new List<LabelCandidate> { new(0, 0, .5) };
            // Keep authored region anchors when clear. Nearby vertical lanes reserve the
            // complete measured line box rather than relying on a fixed baseline offset.
            for (var lane = 1; lane <= 8; lane++) {
                var distance = (metrics.Height + 3) * lane / 2;
                candidates.Add(new LabelCandidate(0, distance, .5));
                candidates.Add(new LabelCandidate(0, -distance, .5));
            }
            var top = body.Top; var bottom = body.Bottom;
            foreach (var placement in placements) {
                if (!ContainsId(ids, placement.Set.Id)) continue;
                var horizontalReach = Math.Abs(x - placement.X) + metrics.Width / 2;
                var verticalReach = Math.Sqrt(Math.Max(0, radius * radius - horizontalReach * horizontalReach));
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
        public double Baseline => Bounds.Top + (TypographyFontResolver.ResolveFace(Style.Font).Font?.Ascent(Style.FontSize) ?? Style.FontSize * .82);
    }
}
