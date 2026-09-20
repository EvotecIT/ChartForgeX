using System;
using System.IO;
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
    // SVG hosts select a real bold face, whereas raster drawing currently synthesizes
    // bold from the regular face. Layout must reserve the larger of both advances.
    internal static TrueTypeFont? ResolveBoldMeasurementFace(string family) {
        TrueTypeFont.TryLoadForFamily(family, out var path);
        if (path == null) return null;
        var name = Path.GetFileName(path);
        string? boldName = null;
        if (name.Equals("Arial.ttf", StringComparison.OrdinalIgnoreCase)) boldName = "Arial Bold.ttf";
        else if (name.EndsWith("-Regular.ttf", StringComparison.OrdinalIgnoreCase)) boldName = name.Substring(0, name.Length - "-Regular.ttf".Length) + "-Bold.ttf";
        else if (name.StartsWith("DejaVu", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)) boldName = Path.GetFileNameWithoutExtension(name) + "-Bold.ttf";
        if (boldName != null) {
            var face = TrueTypeFont.TryLoadFromPath(Path.Combine(Path.GetDirectoryName(path)!, boldName));
            if (face != null) return face;
        }
        // Windows uses short filenames for the Arial faces.
        if (name.Equals("arial.ttf", StringComparison.OrdinalIgnoreCase))
            return TrueTypeFont.TryLoadFromPath(Path.Combine(Path.GetDirectoryName(path)!, "arialbd.ttf"));
        return null;
    }
}
