using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

public sealed partial class VisualFrame {
    internal VisualFrame RequestValues(bool retainOwnedFonts = false) => new(Title, Subtitle, HasExplicitLegend ? ShowLegend : null,
        HasExplicitLegendPosition ? LegendPosition : null, ShowSurface, TransparentBackground,
        StyleValues(_titleStyle, retainOwnedFonts), StyleValues(_subtitleStyle, retainOwnedFonts), StyleValues(_legendStyle, retainOwnedFonts), LegendMaximumRows,
        LegendMaximumHeightFraction, ShowCard, LegendTitle);
    private static TextStyle? StyleValues(TextStyle? value, bool retainOwnedFonts) {
        if (value == null) return null;
        var copy = value.Clone(); copy.Font = retainOwnedFonts ? value.Font.WithoutCapturedText() : value.Font.RequestValues(); return copy;
    }
    // A finite comparison of this existing immutable request; no serialization or wire limits during preparation.
    internal bool SameRequest(VisualFrame other) => Title == other.Title && Subtitle == other.Subtitle
        && ShowLegend == other.ShowLegend && LegendPosition == other.LegendPosition
        && HasExplicitLegend == other.HasExplicitLegend && HasExplicitLegendPosition == other.HasExplicitLegendPosition
        && ShowSurface == other.ShowSurface && TransparentBackground == other.TransparentBackground && ShowCard == other.ShowCard
        && LegendTitle == other.LegendTitle && LegendMaximumRows == other.LegendMaximumRows
        && LegendMaximumHeightFraction == other.LegendMaximumHeightFraction
        && SameStyle(_titleStyle, other._titleStyle) && SameStyle(_subtitleStyle, other._subtitleStyle) && SameStyle(_legendStyle, other._legendStyle);
    private static bool SameStyle(TextStyle? a, TextStyle? b) => ReferenceEquals(a, b) || a != null && b != null
        && a.Font.SameRequest(b.Font) && a.FontSize == b.FontSize && a.Color.Equals(b.Color) && a.Alignment == b.Alignment
        && a.LineHeight == b.LineHeight && a.LetterSpacing == b.LetterSpacing && a.UnderlineStyle == b.UnderlineStyle && a.StrikethroughStyle == b.StrikethroughStyle
        && a.Baseline == b.Baseline && a.TextCase == b.TextCase && a.Hinting == b.Hinting && a.OpenTypeLanguageTag == b.OpenTypeLanguageTag;
}
