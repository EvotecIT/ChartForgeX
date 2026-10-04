using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Svg;
using ChartForgeX.Typography;

namespace ChartForgeX.SvgRaster;

internal sealed partial class SvgRasterStyleSheet {
    private readonly Dictionary<string, Dictionary<string, int>> _palettes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FontPaletteContext> _paletteContexts = new(StringComparer.Ordinal);

    internal FontPaletteContext? PaletteContext(string? name) {
        if (name == null || !_palettes.TryGetValue(name, out var families)) return null;
        if (!_paletteContexts.TryGetValue(name, out var context)) _paletteContexts[name] = context = new FontPaletteContext(families);
        return context;
    }

    private void ReadPaletteRule(string selector, string body) {
        const string prefix = "@font-palette-values";
        if (!selector.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || selector.Length == prefix.Length || !char.IsWhiteSpace(selector[prefix.Length])) return;
        var name = selector.Substring(prefix.Length).Trim();
        if (!TypographyPaletteCss.IsName(name) || body.Length > 16384 || !_palettes.ContainsKey(name) && _palettes.Count >= 256) return;
        SvgStyleDeclarationList declarations;
        try { declarations = SvgStyleDeclarationList.Parse(body); }
        catch (ArgumentException) { return; } catch (FormatException) { return; }
        string? families = null; var index = 0;
        foreach (var declaration in declarations.Declarations) {
            if (declaration.Name.Equals("font-family", StringComparison.OrdinalIgnoreCase)) families = declaration.Value;
            else if (declaration.Name.Equals("base-palette", StringComparison.OrdinalIgnoreCase) && int.TryParse(declaration.Value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed <= ushort.MaxValue) index = parsed;
        }
        if (families == null) return;
        if (!_palettes.TryGetValue(name, out var faces)) _palettes[name] = faces = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var family in TypographyPaletteCss.Families(families)) {
            if (faces.Count >= 128 && !faces.ContainsKey(family)) break;
            faces[family] = index;
        }
    }
}
