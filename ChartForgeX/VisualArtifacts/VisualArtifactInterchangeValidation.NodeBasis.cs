using System;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidatePreparedNodeBasis(VisualArtifactInterchangeNode node) {
        var basis = node.Topology?.PreparedNodeBasis;
        if (basis == null) return;
        basis.Validate();
        OptionalText(basis.IconId, "prepared node icon identity"); OptionalText(basis.Symbol, "prepared node symbol");
        var native = node.Topology!;
        if (node.Width != basis.Width || node.Height != basis.Height || native.Shape != basis.Shape || native.DisplayMode != basis.DisplayMode
            || native.Kind != basis.Kind || !ChartForgeX.Topology.TopologyPreparedNodeBasis.SameIconPolicy(native.IconShape, basis.IconShape) || node.IconId != basis.IconId || node.Symbol != basis.Symbol
            || native.ContentPadding != basis.ContentPadding || native.MaximumTextWidth != basis.MaximumTextWidth
            || native.CornerRadius != basis.CornerRadius || native.GlyphScale != basis.GlyphScale)
            throw new ArgumentException("Prepared node unit basis must agree with captured body and native shape-policy facts.");
        basis.ValidateDerived(node.X ?? 0, node.Y ?? 0, native.ContentPadding, native.MaximumTextWidth,
            native.TextStyle?.FontSize, native.SubtitleTextStyle?.FontSize);
    }
}
