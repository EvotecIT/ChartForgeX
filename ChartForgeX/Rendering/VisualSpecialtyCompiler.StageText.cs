using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    /// <summary>Keeps stage text readable while retaining complete descriptions when a source slot cannot fit it.</summary>
    private static void StageText(VisualSceneBuilder builder, string text, ChartRect bounds, TextStyle style,
        string role, string id, TextAlignment alignment, SvgPaint paint, string family) {
        var measured = builder.MeasureText(text, style);
        var ratio = Math.Min(1, Math.Min(bounds.Width / Math.Max(1, measured.Width), bounds.Height / Math.Max(1, measured.Height)));
        style = style.Clone();
        // Preserve deliberately authored small type; fitting never turns an ordinary label into tiny glyphs.
        style.FontSize = Math.Max(Math.Min(10, style.EffectiveFontSize), style.EffectiveFontSize * ratio);
        style.Baseline = TextBaseline.Normal;
        if (bounds.Width <= 0 || bounds.Height < builder.MeasureText(text, style).Height)
            builder.AddDiagnostic(new VisualDiagnostic(family + ".label-hidden", "A " + family + " label did not fit at a readable size; complete text remains in descriptive regions."));
        VisualStateSceneTools.Text(builder, text, bounds, style, role, id, alignment, paint: paint);
    }
}
