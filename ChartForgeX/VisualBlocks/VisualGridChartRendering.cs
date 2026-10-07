using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

/// <summary>Embeds prepared chart pixels and geometry without changing the source model.</summary>
internal static class VisualGridChartRendering {
    internal static string Svg(Chart chart, string scope) {
        var prepared = Prepare(chart, out _);
        var prefix = VisualSvgOptions.NamespaceFromExternalId(scope);
        return prefix == null ? prepared.ToSvg() : prepared.ToSvg(prefix);
    }

    internal static RgbaImage Image(Chart chart, int density) {
        var prepared = Prepare(chart, out var raster);
        return prepared.ToRgba(new VisualRenderOptions(density, raster.Supersampling, raster.PixelBudget, raster.TextHinting));
    }

    private static PreparedVisual Prepare(Chart chart, out VisualRenderOptions raster) {
        chart = chart.PanelView();
        var request = VisualExportRequest.ForChart(chart);
        var context = request.Context;
        var frame = context.Frame;
        var embedded = new VisualFrame(frame.Title, frame.Subtitle, frame.ShowLegend, frame.LegendPosition,
            frame.ShowSurface, transparentBackground: true, titleStyle: frame.TitleStyle, subtitleStyle: frame.SubtitleStyle,
            legendStyle: frame.LegendStyle, legendMaximumRows: frame.LegendMaximumRows,
            legendMaximumHeightFraction: frame.LegendMaximumHeightFraction, showCard: frame.ShowCard);
        raster = request.RasterOptions;
        return chart.Prepare(new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, embedded, context.Font));
    }
}
