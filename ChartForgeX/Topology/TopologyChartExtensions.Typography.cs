using System;
using ChartForgeX.Core;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Resolves the host font used for PNG text and opt-in installed-font layout measurement.
    /// SVG viewers resolve the theme font family independently and may choose a different face.
    /// </summary>
    /// <param name="chart">The topology whose theme supplies the font family.</param>
    /// <returns>The resolved outline font or built-in fallback information.</returns>
    public static PngFontInfo GetPngFontInfo(this TopologyChart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return TrueTypeFont.ResolveInfo(null, null, null, (chart.Theme ?? TopologyTheme.Light()).FontFamily);
    }
}
