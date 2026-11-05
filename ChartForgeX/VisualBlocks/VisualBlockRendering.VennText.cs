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
            Add(new[] { placement.Set.Id }, placement.Set.Label, placement.X, placement.Y + direction * placement.Radius * .52, placement.Radius * 1.16, 13.5, 800, placement.Set.TextColor ?? theme.Text,
                alternates: ExclusiveSide(placement));
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

        // Horizontal positions that slide a set caption away from the circles it overlaps, into its exclusive region.
        IReadOnlyList<double>? ExclusiveSide(VennSetPlacement placement) {
            var others = placements.Where(item => item.Index != placement.Index && item.Radius > 0
                && Math.Abs(item.X - placement.X) + Math.Abs(item.Y - placement.Y) < item.Radius + placement.Radius).ToArray();
            if (others.Length == 0) return null;
            var dx = placement.X - others.Average(item => item.X);
            if (Math.Abs(dx) < 1e-9) return null;
            var step = Math.Sign(dx) * placement.Radius * .1;
            return Enumerable.Range(1, 6).Select(index => placement.X + step * index).ToArray();
        }

        void Add(IReadOnlyList<string> ids, string text, double x, double baseline, double width, double defaultSize, int weight, ChartColor color, double? heightBudget = null,
            IReadOnlyList<double>? alternates = null) {
            if (text.Length == 0) return;
            text = SingleLineText(text);
            var textBudget = heightBudget ?? lineBudget;
            var style = FitBodyTextStyle(theme, text, defaultSize, width, textBudget, weight);
            PlacedLabel? placed = null;
            var maximum = style.FontSize;
            var anchors = new List<double> { x };
            if (alternates != null) anchors.AddRange(alternates);
            // Captions keep clear of the outlines of sets they do not belong to. When that would
            // shrink the text noticeably, readable size wins and only member chords constrain it.
            var clearOutlines = true;
            // Fit complete captions against both their circle chords and earlier labels.
            // A font that fills the lens can otherwise leave no collision-free lane.
            if (!Fits(maximum)) {
                var fitted = Math.Max(.001, ChartTextFitting.FitFontSize(0, maximum, Fits));
                if (!Fits(fitted) || fitted < maximum * .8) {
                    clearOutlines = false;
                    if (!Fits(maximum)) {
                        fitted = Math.Max(.001, ChartTextFitting.FitFontSize(0, maximum, Fits));
                        if (!Fits(fitted)) return;
                    }
                }
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
                // Prefer sliding along the intended line over moving to another lane.
                foreach (var anchor in anchors) if (FitsAt(anchor, metrics, true)) return true;
                foreach (var anchor in anchors) if (FitsAt(anchor, metrics, false)) return true;
                return false;
            }

            bool FitsAt(double anchorX, TextMetrics metrics, bool baselineOnly) {
                var top = body.Top; var bottom = body.Bottom;
                foreach (var placement in placements) {
                    if (!ContainsId(ids, placement.Set.Id)) continue;
                    var horizontalReach = Math.Abs(anchorX - placement.X) + metrics.Width / 2;
                    if (horizontalReach > placement.Radius) return false;
                    var verticalReach = Math.Sqrt(Math.Max(0, placement.Radius * placement.Radius - horizontalReach * horizontalReach));
                    top = Math.Max(top, placement.Y - verticalReach); bottom = Math.Min(bottom, placement.Y + verticalReach);
                }
                if (text.Length == 0 || bottom - top < metrics.Height) return false;
                var ascent = VisualBlockRendering.TextAscent(style);
                var origin = baseline - ascent;
                var offsets = new List<double> { 0 };
                if (!baselineOnly) {
                    // Reserve the measured line box when moving between vertical lanes.
                    for (var lane = 1; lane <= Math.Max(8, captionCount * 2); lane++) {
                        var distance = (metrics.Height + gap) * lane / 2;
                        offsets.Add(distance); offsets.Add(-distance);
                    }
                    // Include exact interval ends when a discrete lane would jump over it.
                    offsets.Add(top - origin); offsets.Add(bottom - metrics.Height - origin);
                }
                var candidates = new List<LabelCandidate>();
                foreach (var offset in offsets) {
                    var box = new ChartRect(anchorX - metrics.Width / 2, origin + offset, metrics.Width, metrics.Height);
                    // A cleared caption also keeps a gap inside its own outlines rather than touching a chord end.
                    if (clearOutlines && (CrossesOtherSet(ids, box) || !InsideOwnSets(ids, box))) continue;
                    candidates.Add(new LabelCandidate(0, offset, .5));
                }
                if (candidates.Count == 0) return false;
                // Chords at the box's horizontal extremes keep all four corners inside
                // every referenced set, including after vertical relocation.
                var request = new LabelPlacementRequest(text, new ChartPoint(anchorX, origin), style, candidates) {
                    Fallback = LabelFallbackRule.Drop,
                    Bounds = new ChartRect(anchorX - metrics.Width / 2, top, metrics.Width, bottom - top)
                };
                placed = VennTextPlacement.Place(new[] { request }, body, occupied, gap, TextLayoutEngine.Measure)[0];
                return !placed.IsDropped;
            }
        }

        bool InsideOwnSets(IReadOnlyList<string> ids, ChartRect box) {
            foreach (var placement in placements) {
                if (!ContainsId(ids, placement.Set.Id)) continue;
                var inner = Math.Max(0, placement.Radius - gap);
                foreach (var x in new[] { box.Left, box.Right })
                    foreach (var y in new[] { box.Top, box.Bottom })
                        if ((x - placement.X) * (x - placement.X) + (y - placement.Y) * (y - placement.Y) > inner * inner) return false;
            }
            return true;
        }

        // A caption box that touches a set it does not belong to would sit on that set's outline or inside it.
        bool CrossesOtherSet(IReadOnlyList<string> ids, ChartRect box) {
            foreach (var placement in placements) {
                if (placement.Radius <= 0 || ContainsId(ids, placement.Set.Id)) continue;
                var nearestX = Math.Max(box.Left, Math.Min(placement.X, box.Right));
                var nearestY = Math.Max(box.Top, Math.Min(placement.Y, box.Bottom));
                var dx = placement.X - nearestX; var dy = placement.Y - nearestY;
                if (dx * dx + dy * dy < (placement.Radius + gap) * (placement.Radius + gap)) return true;
            }
            return false;
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
