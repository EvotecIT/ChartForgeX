using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    // Mermaid 12's half-arrow tokens alter terminal paint, never the source/target identities.
    private static bool HalfArrow(VisualSceneBuilder builder, string notation, ChartPoint[] points, ChartColor color, double stroke, bool tangentTerminals = false) {
        if (!SequenceHalfArrowNotation.TryRead(notation, out var top, out var solid, out var reverse)) return false;
        var from = reverse ? points[1] : points[points.Length - 2];
        var to = reverse ? points[0] : points[points.Length - 1];
        var direction = to.X >= from.X ? 1 : -1;
        using var rotation = SequenceTerminalRotation(builder,from,to,tangentTerminals);
        var back = to.X - direction * 9;
        var wing = to.Y + (top ? -5 : 5);
        if (solid) builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(to.X, to.Y),
            ChartPathCommand.LineTo(back, wing), ChartPathCommand.LineTo(back, to.Y) }), color, role: "sequence-half-call-arrow", close: true);
        else builder.Line(back, wing, to.X, to.Y, color, stroke, "sequence-half-async-arrow");
        return true;
    }
}
