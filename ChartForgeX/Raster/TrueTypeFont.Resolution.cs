using System;
using System.Collections.Generic;
using System.IO;

namespace ChartForgeX.Raster;

internal sealed partial class TrueTypeFont {
    private static IEnumerable<string> CandidatePaths(string? fontFamily) {
        var kind = ClassifyFamily(fontFamily);
        if (kind == FontFamilyKind.Serif) {
            yield return "/System/Library/Fonts/Supplemental/Georgia.ttf";
            yield return "/System/Library/Fonts/Times.ttc";
            yield return "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf";
            yield return "/usr/share/fonts/truetype/liberation2/LiberationSerif-Regular.ttf";
        } else if (kind == FontFamilyKind.Monospace) {
            yield return "/System/Library/Fonts/Menlo.ttc";
            yield return "/System/Library/Fonts/Monaco.ttf";
            yield return "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf";
            yield return "/usr/share/fonts/truetype/liberation2/LiberationMono-Regular.ttf";
        } else if (kind == FontFamilyKind.Rounded) {
            yield return "/System/Library/Fonts/SFCompactRounded.ttf";
            yield return "/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf";
        } else if (kind == FontFamilyKind.Humanist) {
            yield return "/System/Library/Fonts/Supplemental/Candara.ttf";
            yield return "/System/Library/Fonts/Supplemental/Arial.ttf";
        } else if (kind == FontFamilyKind.Geometric) {
            yield return "/System/Library/Fonts/Avenir Next.ttc";
            yield return "/System/Library/Fonts/Avenir.ttc";
        }

        yield return "/System/Library/Fonts/SFNS.ttf";
        yield return "/System/Library/Fonts/SFCompact.ttf";
        yield return "/System/Library/Fonts/HelveticaNeue.ttc";
        yield return "/System/Library/Fonts/Geneva.ttf";
        yield return "/Library/Fonts/Arial.ttf";
        yield return "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        yield return "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf";
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (string.IsNullOrEmpty(windows)) yield break;
        if (kind == FontFamilyKind.Serif) {
            yield return Path.Combine(windows, "Fonts", "georgia.ttf");
            yield return Path.Combine(windows, "Fonts", "cambria.ttc");
            yield return Path.Combine(windows, "Fonts", "times.ttf");
        } else if (kind == FontFamilyKind.Monospace) {
            yield return Path.Combine(windows, "Fonts", "consola.ttf");
            yield return Path.Combine(windows, "Fonts", "cour.ttf");
        } else if (kind == FontFamilyKind.Rounded) {
            yield return Path.Combine(windows, "Fonts", "ARLRDBD.TTF");
        } else if (kind == FontFamilyKind.Humanist) {
            yield return Path.Combine(windows, "Fonts", "aptos.ttf");
            yield return Path.Combine(windows, "Fonts", "calibri.ttf");
        }

        yield return Path.Combine(windows, "Fonts", "arial.ttf");
        yield return Path.Combine(windows, "Fonts", "segoeui.ttf");
    }

    private static FontFamilyKind ClassifyFamily(string? fontFamily) {
        var family = fontFamily ?? string.Empty;
        if (family.Trim().Length == 0) return FontFamilyKind.SansSerif;
        if (ContainsAny(family, "monospace", "Consolas", "Menlo", "Courier", "Monaco", "DejaVu Sans Mono", "Liberation Mono", "Cascadia Mono")) return FontFamilyKind.Monospace;
        if (family.IndexOf(", serif", StringComparison.OrdinalIgnoreCase) >= 0 || family.Trim().Equals("serif", StringComparison.OrdinalIgnoreCase) || ContainsAny(family, "Georgia", "Cambria", "Times New Roman", "Charter", "DejaVu Serif", "Liberation Serif")) return FontFamilyKind.Serif;
        if (family.IndexOf("Rounded", StringComparison.OrdinalIgnoreCase) >= 0 || family.IndexOf("Nunito", StringComparison.OrdinalIgnoreCase) >= 0) return FontFamilyKind.Rounded;
        if (family.IndexOf("Aptos", StringComparison.OrdinalIgnoreCase) >= 0 || family.IndexOf("Calibri", StringComparison.OrdinalIgnoreCase) >= 0 || family.IndexOf("Candara", StringComparison.OrdinalIgnoreCase) >= 0) return FontFamilyKind.Humanist;
        if (family.IndexOf("Avenir", StringComparison.OrdinalIgnoreCase) >= 0 || family.IndexOf("Montserrat", StringComparison.OrdinalIgnoreCase) >= 0) return FontFamilyKind.Geometric;
        return FontFamilyKind.SansSerif;
    }

    private static bool ContainsAny(string value, params string[] candidates) {
        foreach (var candidate in candidates) {
            if (value.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        return false;
    }

    private enum FontFamilyKind {
        SansSerif,
        Serif,
        Monospace,
        Rounded,
        Humanist,
        Geometric
    }
}
