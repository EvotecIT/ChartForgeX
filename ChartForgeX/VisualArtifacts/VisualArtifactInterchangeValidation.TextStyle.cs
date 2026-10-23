using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateDetailTextStyle(TextStyleOverride? style) {
        if (style == null) return;
        OptionalText(style.FontFamily, "detail font family");
        OptionalText(style.FontWeight, "detail font weight");
        OptionalText(style.OpenTypeLanguageTag, "detail OpenType language");
        // Native setters and immutable variation settings own numeric, enum and axis-tag validation.
        // The transport adds its existing text budget, without another typography validation policy.
    }
}
