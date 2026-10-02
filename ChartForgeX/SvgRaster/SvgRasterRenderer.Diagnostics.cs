using System;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static void ReportUnsupportedElement(SvgRasterElement element, SvgRasterStyle style, SvgRasterDefinitions definitions) {
        if (style.Displayed && style.VisibilityVisible && style.Opacity > 0 && !IsDefinitionElement(element.Name))
            definitions.Diagnostics?.Report("SFR001", "The SVG element is outside the supported raster subset.", element);
    }

    private static void ReportUnsupportedFilter(SvgRasterElement element, SvgRasterStyle style, SvgRasterDefinitions definitions) {
        if (style.Displayed && style.Opacity > 0 && !string.IsNullOrWhiteSpace(style.Filter) && !string.Equals(style.Filter, "none", StringComparison.OrdinalIgnoreCase))
            definitions.Diagnostics?.Report("SFR002", "SVG filters are not applied by the raster renderer.", element);
    }
}
