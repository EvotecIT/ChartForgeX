using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMatrixCompiler {
    /// <summary>Whether every column caption is a number or a date, so thinning them loses no column identity.</summary>
    private static bool OrderedColumnCaptions(IReadOnlyList<string> captions) => captions.Count > 1 && captions.All(caption => caption.Trim().Length > 0
        && (double.TryParse(caption, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            || DateTime.TryParse(caption, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out _)));

    /// <summary>Draws measured wrapped captions while keeping the original category in descriptive exports.</summary>
    private static void MatrixColumnText(VisualSceneBuilder builder, string caption, string displayed, ChartRect bounds,
        TextStyle style, string id) {
        var measured = builder.MeasureText(displayed, style);
        if (measured.Width > bounds.Width + .000001 || measured.Height > bounds.Height + .000001) {
            // Explicit horizontal captions retain the ordinary measured shortening policy.
            VisualStateSceneTools.Text(builder, caption, bounds, style, "heatmap-column-label", id, TextAlignment.Center);
            return;
        }
        builder.AddRegion(new VisualSemanticRegion(id, "heatmap-column-label", bounds, caption));
        style = style.Clone(); style.Alignment = TextAlignment.Center;
        var baseline = bounds.Top + (bounds.Height - measured.Height) / 2 + builder.TextAscent(style);
        using (builder.PushClip(bounds)) builder.Text(displayed, bounds.Left + bounds.Width / 2, baseline, style,
            "heatmap-column-label", id, VisualChartPaint.Text(style));
    }
}
