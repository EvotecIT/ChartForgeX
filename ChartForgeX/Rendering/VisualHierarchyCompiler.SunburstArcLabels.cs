using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    /// <summary>Fits a complete captured primary run along an upright native circular lane, without fragment shaping.</summary>
    private static bool SunburstCurvedLabel(Chart chart, VisualSceneBuilder builder, ChartSunburstModel model,
        ChartSunburstNode node, SunburstSectorPaintGeometry geometry, ChartColor fill, SvgPaint fillPaint,
        HierarchyCaptionSurface? surface, TextStyle style, string original, string? secondary, bool probing, out string reason) {
        reason = "unfit-geometry";
        if (node.ViewDepth == 0 || geometry.InnerRadius <= 0) { reason = "central-caption"; return false; }
        if (secondary != null) { reason = "secondary-caption"; return false; }
        var captured = builder.PrepareText(original, style);
        if (!VisualArcTextRuns.TryCreate(captured, out var runs, out reason)) return false;
        var contours = ChartSlicePathGeometry.Contours(model.CenterX, model.CenterY, geometry.OuterRadius,
            geometry.InnerRadius, geometry.StartAngle, geometry.Sweep, chart.Options.Sunburst.CornerRadius, 8);
        var mid = geometry.StartAngle + geometry.Sweep / 2;
        var direction = Math.Cos(mid + Math.PI / 2) >= 0 ? 1d : -1d;
        var fullInk = captured.Face.Font!.MeasureGlyphInk(captured.Lines[0].Glyphs, captured.Size, captured.Face.SynthesizeItalic);
        if (!fullInk.HasValue) { reason = "empty-ink"; return false; }
        var inkMiddle = fullInk.Value.Y - captured.Ascent + fullInk.Value.Height / 2;
        var thickness = geometry.OuterRadius - geometry.InnerRadius;
        foreach (var fraction in new[] { .5, .35, .65 }) {
            var radius = geometry.InnerRadius + thickness * fraction + direction * inkMiddle;
            if (radius <= 0 || captured.Metrics.Width + SunburstCaptionClearance * 2 > radius * geometry.Sweep) continue;
            var placed = new List<(VisualArcTextRun Run, double X, double Y, double Degrees, double Angle)>();
            foreach (var run in runs) {
                var angle = mid + direction * (run.Offset + run.Width / 2 - captured.Metrics.Width / 2) / radius;
                var degrees = (angle + direction * Math.PI / 2) * 180 / Math.PI;
                while (degrees > 180) degrees -= 360;
                while (degrees < -180) degrees += 360;
                if (Math.Abs(degrees) > 90 + .000001) { placed.Clear(); break; }
                var x = model.CenterX + Math.Cos(angle) * radius;
                var y = model.CenterY + Math.Sin(angle) * radius;
                if (run.Ink.HasValue && !Fits(run.Ink.Value, run.Width, x, y, degrees)) { placed.Clear(); break; }
                placed.Add((run, x, y, degrees, angle));
            }
            if (placed.Count != runs.Count) continue;
            if (probing) { reason = string.Empty; return true; }
            var paint = VisualChartPaint.ExplicitDataLabelColor(chart, node.Index) ? VisualChartPaint.Text(style)
                : surface?.Paint(fill, fillPaint, style.Color) ?? SvgPaint.Contrast(fill, fillPaint);
            using (builder.PushGroup(Id("node-label", node.Index), "sunburst-label", new Dictionary<string, string> {
                ["aria-label"] = original, ["data-cfx-full-label"] = original,
                ["data-cfx-caption-placement"] = "arc", ["data-cfx-caption-radius"] = N(radius)
            })) {
                for (var index = 0; index < placed.Count; index++) {
                    var part = placed[index];
                    using (builder.PushGroup(null, "sunburst-label-cluster", new Dictionary<string, string> {
                        ["data-cfx-cluster-index"] = N(index), ["data-cfx-arc-angle"] = N(part.Angle),
                        ["data-cfx-glyph-count"] = N(part.Run.Text.Lines[0].Glyphs.Count)
                    }))
                    using (builder.PushRotation(part.Degrees, part.X, part.Y))
                        builder.PreparedText(part.Run.Text, part.X - part.Run.Width / 2, part.Y, style.Color,
                            TextAlignment.Left, "sunburst-label-run", paint: paint);
                }
            }
            reason = string.Empty; return true;
        }
        reason = "unfit-geometry"; return false;

        bool Fits(ChartRect ink, double width, double x, double y, double degrees) {
            var angle = -degrees * Math.PI / 180; var cosine = Math.Cos(angle); var sine = Math.Sin(angle);
            var local = contours.Select(contour => contour.Select(point => new ChartPoint(
                (point.X - x) * cosine - (point.Y - y) * sine,
                (point.X - x) * sine + (point.Y - y) * cosine)).ToList()).ToList();
            return new LabelMarkShape(local, true, 0).Contains(new ChartRect(
                ink.X - width / 2 - SunburstCaptionClearance, ink.Y - SunburstCaptionClearance,
                ink.Width + SunburstCaptionClearance * 2, ink.Height + SunburstCaptionClearance * 2));
        }
    }
}
