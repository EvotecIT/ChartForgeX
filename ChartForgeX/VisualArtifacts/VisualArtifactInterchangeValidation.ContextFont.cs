using System;
using ChartForgeX.Rendering;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    // Only public portable access reaches this owner; native preparation retains immutable roots without wire budgets.
    private static void ValidateContextFont(PreparedDiagramFont? source, VisualArtifactInterchangeEnvelope envelope, VisualFrame? frame) {
        if (source == null) return;
        if (envelope.Family != VisualArtifactInterchangeFamily.Topology && envelope.Family != VisualArtifactInterchangeFamily.Flow)
            throw new ArgumentException("Captured context fonts require the Topology or Flow family.");
        Text(source.ThemeFamily, "captured font theme-family basis"); Font(source.Font);
        var resources = OwnedFontResources.Capture(source, frame);
        foreach (var root in resources.Roots) Face(root);
        if (source.Text == null) return;
        foreach (var request in source.Text.Requests) {
            Font(request.Style.Font); OptionalText(request.Style.OpenTypeLanguageTag, "captured font language");
            if (request.Face.Font == null) throw new NotSupportedException("A portable captured font request requires its actual native face.");
            Face(request.Face.Font);
            foreach (var decision in request.Clusters.Decisions) Face(decision.Face);
        }
        void Font(FontSpec value) {
            Text(value.Family, "captured font family"); OptionalText(value.FaceName, "captured font requested face");
            ValidateDetailTextStyle(new TextStyleOverride { FontFamily = value.Family, FontWeight = value.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Italic = value.Italic, ColorPaletteIndex = value.ColorPaletteIndex, Variations = value.Variations });
        }
        void Face(TrueTypeFont value) {
            OptionalText(value.DisplayName, "captured font actual face"); OptionalText(value.SelectedFamily, "captured font selected family");
            OptionalText(value.LanguageTag, "captured face language");
            foreach (var family in value.FallbackFamilies) Text(family, "captured fallback family");
            if (value.PaletteContext != null) throw new NotSupportedException("Captured portable fonts require the existing finite palette-index contract.");
        }
    }
}
