using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartLabelScene {
    private TextStyle TextStyle(SvgRasterStyle style) => new() {
        FontSize = Math.Max(1, style.FontSize), LineHeight = 1,
        OpenTypeLanguageTag = style.OpenTypeLanguageTag,
        Font = new FontSpec {
            Family = style.FontFamily ?? _font.Family, Weight = TypographyFontResolver.FontSpecWeight(style.FontWeight),
            Italic = style.FontStyle != "normal", Variations = style.Variations ?? FontVariationSettings.Default,
            FilePath = _font.FilePath, CollectionIndex = _font.CollectionIndex, FaceName = _font.FaceName
        }
    };

    private ChartRect TextBox(SvgMarkupElement element, SvgRasterStyle style, SvgRasterMatrix matrix) {
        var resolved = TextStyle(style);
        var metrics = Measurements.Measure(element.Value, resolved);
        var face = TypographyFontResolver.ResolveFace(resolved.Font);
        var ascent = face.Font?.Ascent(resolved.FontSize) ?? resolved.FontSize * 0.82;
        var x = Number(element, "x") + Number(element, "dx");
        var y = Number(element, "y") + Number(element, "dy");
        x -= style.TextAnchor == "end" ? metrics.Width : style.TextAnchor == "middle" ? metrics.Width / 2 : 0;
        y += style.DominantBaseline is "middle" or "central" ? resolved.FontSize * 0.32
            : style.DominantBaseline is "hanging" or "text-before-edge" ? resolved.FontSize * 0.82
            : style.DominantBaseline is "text-after-edge" or "ideographic" ? -resolved.FontSize * 0.18 : 0;
        y += style.BaselineShift == "super" ? -resolved.FontSize * 0.35 : style.BaselineShift == "sub" ? resolved.FontSize * 0.22 : 0;
        y -= ascent;
        var halo = style.Stroke.IsNone ? 0 : style.StrokeWidth / 2;
        var box = TransformBox(new ChartRect(x - halo, y - halo, metrics.Width + halo * 2, metrics.Height + halo * 2), matrix);
        // Explicitly positioned spans represent multiline captions. Preserve their formatting and include their full footprint.
        foreach (var span in element.Elements().Where(e => e.LocalName == "tspan")) {
            if (span.Attribute("x") == null && span.Attribute("y") == null && span.Attribute("dy") == null) continue;
            var copy = span.Clone(); copy.Name = element.Name;
            if (copy.Attribute("x") == null) copy.SetAttributeValue("x", Number(element, "x"));
            if (copy.Attribute("y") == null) copy.SetAttributeValue("y", Number(element, "y"));
            var dy = copy.Attribute("dy");
            if (dy != null && dy.EndsWith("em", StringComparison.Ordinal)) {
                if (double.TryParse(dy.Substring(0, dy.Length - 2), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var em)) copy.SetAttributeValue("dy", F(em * resolved.FontSize));
            }
            box = Union(box, TextBox(copy, style, matrix));
        }
        return box;
    }

    private static ChartRect TransformBox(ChartRect box, SvgRasterMatrix matrix) {
        var points = new[] { matrix.Transform(new ChartPoint(box.Left, box.Top)), matrix.Transform(new ChartPoint(box.Right, box.Top)),
            matrix.Transform(new ChartPoint(box.Right, box.Bottom)), matrix.Transform(new ChartPoint(box.Left, box.Bottom)) };
        var left = points.Min(p => p.X); var top = points.Min(p => p.Y);
        return new ChartRect(left, top, points.Max(p => p.X) - left, points.Max(p => p.Y) - top);
    }

    private static bool CanMove(string role) => !role.Contains("axis") && !role.Contains("tick") && !role.Contains("column-label") && !role.Contains("row-label") && !role.Contains("legend")
        && !role.Contains("header") && role != "chart-title" && role != "chart-subtitle" && role != "donut-total-label" && role != "donut-title";
    private static string LabelRole(SvgMarkupElement element) => Role(element).Length != 0 ? Role(element)
        : element.Ancestors().Select(Role).FirstOrDefault(role => role.Length != 0) ?? "";
    private static int Priority(string role) => role is "chart-title" or "chart-subtitle" || role.Contains("header") ? 1000
        : role.Contains("legend") ? 900 : role.Contains("axis") || role.Contains("tick") || role.Contains("column-label") || role.Contains("row-label") ? 800
        : role.Contains("gauge-label") || role.Contains("annotation") || role.Contains("callout") ? 700
        : role.Contains("node") ? 600 : role.Contains("connector-label") || role.Contains("route-label") ? 300 : 500;

    private static IReadOnlyList<LabelCandidate> Candidates(Entry label, string role) {
        var candidates = new List<LabelCandidate> { new(0, 0) };
        if (!CanMove(role) || label.IsLegendItem) return candidates;
        // Funnel rows have a semantic stage: do not move their values or retention ratios to a different row.
        if (role.StartsWith("funnel-", StringComparison.Ordinal)) return candidates;
        if (role == "sankey-node-label" && label.Associated is { } node) {
            var bounds = node.Shape.Bounds;
            var above = bounds.Top - label.Box.Bottom - 8;
            var below = bounds.Bottom - label.Box.Top + 8;
            candidates.Add(new LabelCandidate(0, above)); candidates.Add(new LabelCandidate(0, below));
            candidates.Add(new LabelCandidate(bounds.Left - label.Box.Right - 8, 0));
            candidates.Add(new LabelCandidate(bounds.Right - label.Box.Left + 8, 0));
        }
        if (role.Contains("annotation")) { candidates.Add(new LabelCandidate(0, -4)); candidates.Add(new LabelCandidate(0, 4)); }
        var step = Math.Max(12, label.Box.Height + 4);
        // Maintain the current lane first, then the nearest above/below and side lanes.
        for (var i = 1; i <= 4; i++) { candidates.Add(new LabelCandidate(0, -step * i)); candidates.Add(new LabelCandidate(0, step * i)); }
        var side = Math.Max(20, label.Box.Width / 2 + 10);
        candidates.Add(new LabelCandidate(-side, 0)); candidates.Add(new LabelCandidate(side, 0));
        candidates.Add(new LabelCandidate(-side, -step)); candidates.Add(new LabelCandidate(side, -step));
        candidates.Add(new LabelCandidate(-side, step)); candidates.Add(new LabelCandidate(side, step));
        return candidates;
    }

    private void Apply(Entry entry, PlacedLabel result) {
        var element = entry.Element;
        var status = result.IsDropped ? "dropped" : result.IsEllipsized ? "ellipsis" : "placed";
        element.SetAttributeValue("data-cfx-label-original", entry.Text);
        element.SetAttributeValue("data-cfx-label-status", status);
        if (entry.Associated != null) {
            element.SetAttributeValue("data-cfx-label-mark", entry.Associated.Id);
            KeepAccessibleValue(entry.Associated.Element, entry.Text);
        }
        if (result.IsDropped) {
            element.SetAttributeValue("display", "none"); element.SetAttributeValue("aria-hidden", "true");
            foreach (var decoration in entry.Decorations) decoration.SetAttributeValue("display", "none");
            return;
        }
        if (Role(element) == "data-label" && entry.Associated is { Kind: "bar" or "horizontal-bar" } mark &&
            mark.SvgRoot?.Attribute("data-cfx-look") == "graphite" && mark.Shape.Contains(result.Bounds) &&
            entry.Ink?.ToHex() is "#4D525B" or "#B0B4BC" &&
            ChartColor.TryParse(Themes.SvgPaint.Resolve(mark.Element.Attribute("data-cfx-color") ?? "", null), out var fill)) {
            var state = mark.SvgRoot.Attribute("data-cfx-series-state-" + mark.Element.Attribute("data-cfx-series"));
            var role = state != null && state != "none" ? Themes.SvgColorRole.Status : Themes.SvgColorRole.Series;
            element.SetAttributeValue("fill", Themes.SvgPaint.Contrast(fill, role).Value);
        }
        element.SetAttributeValue("data-cfx-label-x", F(result.Bounds.X)); element.SetAttributeValue("data-cfx-label-y", F(result.Bounds.Y));
        element.SetAttributeValue("data-cfx-label-width", F(result.Bounds.Width)); element.SetAttributeValue("data-cfx-label-height", F(result.Bounds.Height));
        var dx = result.Bounds.X - entry.Box.X; var dy = result.Bounds.Y - entry.Box.Y;
        if (result.IsEllipsized) {
            element.Value = result.Text;
            if (entry.Matrix.TryInvert(out var inverse)) {
                var ascent = TypographyFontResolver.ResolveFace(entry.Style.Font).Font?.Ascent(entry.Style.FontSize) ?? entry.Style.FontSize * 0.82;
                var script = element.Attribute("baseline-shift");
                var shift = script == "super" ? -entry.Style.FontSize * 0.35 : script == "sub" ? entry.Style.FontSize * 0.22 : 0;
                var position = inverse.Transform(new ChartPoint(entry.ContentBox.X, entry.ContentBox.Y + ascent - shift));
                element.SetAttributeValue("text-anchor", "start"); element.SetAttributeValue("dominant-baseline", "auto");
                element.SetAttributeValue("x", F(position.X)); element.SetAttributeValue("y", F(position.Y));
            }
            foreach (var decoration in entry.Decorations.Where(e => e.LocalName == "rect")) {
                decoration.SetAttributeValue("width", F(Math.Max(0, Number(decoration, "width") + result.Bounds.Width - entry.Box.Width)));
                decoration.SetAttributeValue("height", F(Math.Max(0, Number(decoration, "height") + result.Bounds.Height - entry.Box.Height)));
            }
        }
        Translate(element, entry.ParentMatrix, dx, dy);
        foreach (var decoration in entry.Decorations) {
            if (IsLeader(decoration) && result.CandidateIndex > 0 && MoveLeader(entry, decoration, result, dx, dy)) continue;
            else if (decoration.Parent != element) Translate(decoration, entry.ParentMatrix, dx, dy);
        }
        if (result.HasLeaderLine && !entry.Decorations.Any(IsLeader) && entry.ParentMatrix.TryInvert(out var leaderInverse)) {
            var anchor = entry.Associated?.Shape.Bounds;
            var origin = anchor.HasValue ? new ChartPoint(Math.Max(anchor.Value.Left, Math.Min(anchor.Value.Right, entry.Box.Left + entry.Box.Width / 2)), Math.Max(anchor.Value.Top, Math.Min(anchor.Value.Bottom, entry.Box.Top + entry.Box.Height / 2)))
                : new ChartPoint(Number(element, "data-label-anchor-x", entry.Box.Left + entry.Box.Width / 2), Number(element, "data-label-anchor-y", entry.Box.Top + entry.Box.Height / 2));
            var start = leaderInverse.Transform(origin);
            var end = leaderInverse.Transform(result.LeaderEnd);
            var leaderRole = entry.Decorations.FirstOrDefault(IsLeader) is { } existingLeader ? Role(existingLeader) : "label-leader";
            // A new element in the label's namespace, named and declared as the XLinq writer wrote it.
            var line = element.CreateSibling("line", 11, out var declaration);
            line.AddAttribute("data-cfx-role", leaderRole); line.AddAttribute("data-cfx-label-decoration", "true");
            line.AddAttribute("x1", F(start.X)); line.AddAttribute("y1", F(start.Y)); line.AddAttribute("x2", F(end.X)); line.AddAttribute("y2", F(end.Y));
            line.AddAttribute("stroke", element.Attribute("fill") ?? "currentColor"); line.AddAttribute("stroke-width", "1"); line.AddAttribute("stroke-opacity", "0.65");
            if (declaration != null) line.AddAttribute("xmlns", declaration);
            element.AddBeforeSelf(line);
        }
    }
    private static bool IsLeader(SvgMarkupElement element) => Role(element).Contains("leader") || Role(element).Contains("connector");

    private static bool MoveLeader(Entry entry, SvgMarkupElement leader, PlacedLabel result, double dx, double dy) {
        var nested = leader.Parent == entry.Element;
        var parent = nested ? entry.Matrix : entry.ParentMatrix;
        var original = parent.Multiply(SvgRasterMatrix.ParseTransform(leader.Attribute("transform")));
        var moved = nested ? SvgRasterMatrix.Translate(dx, dy).Multiply(original) : original;
        if (!moved.TryInvert(out var inverse)) return false;
        if (leader.LocalName == "line") {
            var start = inverse.Transform(original.Transform(new ChartPoint(Number(leader, "x1"), Number(leader, "y1"))));
            var end = inverse.Transform(result.LeaderEnd);
            leader.SetAttributeValue("x1", F(start.X)); leader.SetAttributeValue("y1", F(start.Y));
            leader.SetAttributeValue("x2", F(end.X)); leader.SetAttributeValue("y2", F(end.Y));
            return true;
        }
        if (leader.LocalName != "path" || leader.Attribute("d") is not { } path) return false;
        var rings = Core.ChartMapPathParser.ParseRings(path);
        if (rings.Count != 1 || rings[0].Count < 2 || rings[0].Count > 3) return false;
        var points = rings[0];
        var drawing = new System.Text.StringBuilder();
        for (var i = 0; i < points.Count; i++) {
            var point = inverse.Transform(i == points.Count - 1 ? result.LeaderEnd : original.Transform(points[i]));
            drawing.Append(i == 0 ? "M " : " L ").Append(F(point.X)).Append(' ').Append(F(point.Y));
        }
        leader.SetAttributeValue("d", drawing.ToString());
        return true;
    }

    private static void Translate(SvgMarkupElement element, SvgRasterMatrix parent, double dx, double dy) {
        if (Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001 || !parent.TryInvert(out var inverse)) return;
        var zero = inverse.Transform(new ChartPoint(0, 0)); var translated = inverse.Transform(new ChartPoint(dx, dy));
        element.SetAttributeValue("transform", "translate(" + F(translated.X - zero.X) + " " + F(translated.Y - zero.Y) + ") " + (element.Attribute("transform") ?? ""));
    }
    private static void KeepAccessibleValue(SvgMarkupElement mark, string text) {
        var original = mark.Attribute("aria-label") ?? mark.Attribute("data-cfx-label") ?? "";
        if (original.IndexOf(text, StringComparison.Ordinal) < 0) mark.SetAttributeValue("aria-label", original.Length == 0 ? text : original + ", " + text);
        var labels = mark.Attribute("data-cfx-label-text") ?? "";
        if (labels.IndexOf(text, StringComparison.Ordinal) < 0) mark.SetAttributeValue("data-cfx-label-text", labels.Length == 0 ? text : labels + "; " + text);
    }
}
