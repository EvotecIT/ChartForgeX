using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    private static void Text(VisualSceneBuilder builder, string text, ChartRect bounds, SequencePreparedLayout layout,
        SequenceTextRole role, ChartColor color, bool center = false, double ratio = 1, string? nativeRole = null) {
        if (!layout.Presentation.Customized) { Text(builder, text, bounds, layout.FontSize * ratio, color, center); return; }
        var style = layout.Presentation.Style(role, color, ratio);
        style.Alignment = (role == SequenceTextRole.Message ? layout.Presentation.Layout?.MessageAlignment
            : role == SequenceTextRole.Note ? layout.Presentation.Layout?.NoteAlignment : null) ?? (center ? TextAlignment.Center : TextAlignment.Left);
        var x = style.Alignment == TextAlignment.Center ? bounds.X + bounds.Width / 2 : style.Alignment == TextAlignment.Right ? bounds.Right : bounds.X;
        var actualColor = style.Color;
        var actualAlignment = style.Alignment;
        // These stored fields do not participate in measurement/shaping. Keep them
        // canonical across explicit context faces and captured role-face restoration.
        style.Color = ChartColor.Black;
        style.Alignment = TextAlignment.Left;
        builder.Text(text, x, bounds.Y + builder.TextAscent(style), style,
            role: nativeRole ?? "sequence-label", color: actualColor, alignment: actualAlignment);
    }
}
