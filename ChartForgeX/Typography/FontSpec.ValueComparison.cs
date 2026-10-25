using System;

namespace ChartForgeX.Typography;

public sealed partial class FontSpec {
    // A finite request-only copy deliberately retains no capture continuation writer or owned runtime context.
    internal FontSpec RequestValues() => new() { Family = Family, FilePath = FilePath, CollectionIndex = CollectionIndex,
        FaceName = FaceName, Weight = Weight, Italic = Italic, Variations = Variations, ColorPaletteIndex = ColorPaletteIndex };
    // Same finite native resolver inputs used by the existing captured text request key, without serializing during preparation.
    internal bool SameRequest(FontSpec other) => string.Equals(Family, other.Family, StringComparison.Ordinal)
        && string.Equals(FilePath, other.FilePath, StringComparison.Ordinal) && CollectionIndex == other.CollectionIndex
        && string.Equals(FaceName, other.FaceName, StringComparison.Ordinal) && Weight == other.Weight && Italic == other.Italic
        && ColorPaletteIndex == other.ColorPaletteIndex && Variations.Key == other.Variations.Key;
}
