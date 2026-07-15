using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static class TypographyFontResolver {
    public static TrueTypeFont? Resolve(FontSpec font) {
        if (font.FilePath != null) {
            var requested = TrueTypeFont.TryLoadFromPath(font.FilePath, font.CollectionIndex, font.FaceName);
            if (requested != null) return requested;
        }

        return TrueTypeFont.TryLoadForFamily(font.Family, out _);
    }
}
