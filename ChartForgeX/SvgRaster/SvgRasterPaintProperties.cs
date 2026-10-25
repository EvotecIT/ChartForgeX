using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.SvgRaster;

/// <summary>Existing definition-paint cascade shared by gradient stops and finite flood primitives.</summary>
internal static class SvgRasterPaintProperties {
    internal static void Resolve(SvgRasterElement element, SvgRasterStyleSheet styleSheet, IReadOnlyList<SvgRasterElement> ancestors,
        SvgRasterStyle style, string colorName, string opacityName, out string? color, out string? opacity) {
        var customProperties = style.CustomProperties;
        color = element.Get(colorName); opacity = element.Get(opacityName);
        var declarations = new List<SvgStyleDeclaration>(styleSheet.DeclarationsFor(element, ancestors));
        var inline = element.Get("style");
        if (!string.IsNullOrWhiteSpace(inline)) declarations.AddRange(SvgStyleDeclarationList.Parse(inline!).Declarations);
        foreach (var declaration in SvgStyleDeclarationList.InCascadeOrder(declarations)) {
            if (string.Equals(declaration.Name, colorName, StringComparison.Ordinal)) color = SvgRasterCssVariables.Resolve(declaration.Value, customProperties);
            else if (string.Equals(declaration.Name, opacityName, StringComparison.Ordinal)) opacity = SvgRasterCssVariables.Resolve(declaration.Value, customProperties);
        }
        if (color != null) color = SvgRasterCssVariables.Resolve(color, customProperties);
        if (opacity != null) opacity = SvgRasterCssVariables.Resolve(opacity, customProperties);
    }
    internal static ChartColor WithOpacity(ChartColor color, double opacity) =>
        ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity));
}
