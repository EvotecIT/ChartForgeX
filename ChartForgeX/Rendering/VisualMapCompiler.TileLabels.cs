using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    /// <summary>Fits a complete tile identity inside its contour without reducing ordinary type below eight pixels.</summary>
    private static void TileLabel(VisualSceneBuilder builder, string code, ChartRect bounds, List<ChartPoint> contour,
        double clearance, TextStyle style, string id, SvgPaint paint) {
        const string role = "tile-map-label";
        builder.AddRegion(new VisualSemanticRegion(id, role, bounds, code));
        using (builder.PushGroup(id + "-source", role + "-source", new Dictionary<string, string> { ["aria-label"] = code, ["data-cfx-label"] = code })) {
            var shape = new LabelMarkShape(new[] { contour }, true, 0);
            style = style.Clone(); style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
            style.Alignment = TextAlignment.Center;
            if (!FitsCode()) {
                var preferred = style.FontSize; var minimum = Math.Min(8, preferred);
                style.FontSize = minimum;
                if (!FitsCode()) {
                    builder.AddDiagnostic(new VisualDiagnostic("map.label-overflow", "A tile region code was omitted because it does not fit at a readable size; complete identity remains in descriptive regions."));
                    return;
                }
                // A fixed search bounds work even for very large authored sizes. Quarter-pixel
                // steps retain the readable caption policy and stay within the measured contour.
                var lower = minimum; var upper = preferred;
                for (var step = 0; step < 16; step++) {
                    style.FontSize = lower + (upper - lower) / 2;
                    if (FitsCode()) lower = style.FontSize;
                    else upper = style.FontSize;
                }
                style.FontSize = Math.Max(minimum, Math.Floor(lower * 4) / 4);
            }
            var metrics = builder.MeasureText(code, style);
            builder.Text(code, bounds.Left + bounds.Width / 2,
                bounds.Top + (bounds.Height - metrics.Height) / 2 + builder.TextAscent(style), style, role, id, paint);

            bool FitsCode() => ChartTextFitting.FitEnd(code, Fits, "").Length == code.Length;
            bool Fits(string text) {
                var measured = builder.MeasureText(text, style);
                return shape.Contains(new ChartRect(bounds.Left + (bounds.Width - measured.Width) / 2 - clearance,
                    bounds.Top + (bounds.Height - measured.Height) / 2 - clearance,
                    measured.Width + clearance * 2, measured.Height + clearance * 2));
            }
        }
    }
}
