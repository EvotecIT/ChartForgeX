using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Resolves model defaults once at the convenience-export boundary.</summary>
internal sealed partial class VisualExportRequest {
    private VisualExportRequest(VisualRenderContext context, VisualRenderOptions rasterOptions) {
        Context = context; RasterOptions = rasterOptions;
    }
    internal VisualRenderContext Context { get; }
    internal VisualRenderOptions RasterOptions { get; }

    internal static VisualExportRequest ForChart(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = chart.Options;
        var theme = FromTheme(options.Theme);
        ChartGuards.RenderCompatibility(chart, preparing: true);
        var family = VisualChartCompiler.Family(chart);
        // Numeric radial labels are formatted once during preparation, rather than while counting entries here.
        var showLegend = family == VisualChartFamily.NumericRadial ? ChartLegendVisibility.ForSeries(chart)
            : ChartLegendVisibility.ForPreparedContent(chart, VisualChartCompiler.LegendEntries(family, chart, theme.Resolve(VisualThemeMode.Light)).Count);
        var frame = new VisualFrame(options.ShowHeader && !options.HostOwnsFrame ? chart.Title : string.Empty,
            options.ShowHeader && !options.HostOwnsFrame ? chart.Subtitle : string.Empty,
            showLegend && !options.HostOwnsFrame, options.LegendPosition,
            options.ShowPlotBackground && !options.HostOwnsFrame, options.TransparentBackground || options.HostOwnsFrame,
            legendMaximumRows: options.LegendMaximumRows, legendMaximumHeightFraction: options.LegendMaximumHeightFraction,
            showCard: options.ShowCard && options.Theme.UseCard && !options.HostOwnsFrame);
        var padding = options.Padding;
        if (!options.HasExplicitPadding && (padding.Left + padding.Right >= options.Size.Width || padding.Top + padding.Bottom >= options.Size.Height))
            padding = new ChartPadding(Math.Min(padding.Left, options.Size.Width / 4), Math.Min(padding.Top, options.Size.Height / 4),
                Math.Min(padding.Right, options.Size.Width / 4), Math.Min(padding.Bottom, options.Size.Height / 4));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(options.Size.Width, options.Size.Height), padding),
            theme, frame: frame, font: new FontSpec {
                Family = options.Theme.FontFamily, FilePath = options.PngFontPath,
                FaceName = options.PngFontFaceName, CollectionIndex = options.PngFontCollectionIndex
            });
        return new VisualExportRequest(context, new VisualRenderOptions(options.PngOutputScale, options.PngSupersamplingScale,
            textHinting: options.PngTextHinting));
    }

    internal static VisualExportRequest ForGrid(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = ChartGridLayout.FromGrid(grid);
        var sourceTheme = grid.Theme ?? grid.Charts[0].Options.Theme;
        var theme = FromTheme(sourceTheme);
        var font = new FontSpec { Family = sourceTheme.FontFamily };
        var colors = theme.Resolve(VisualThemeMode.Light);
        TextStyle Heading(TextStyleOverride model, double size, ChartColor color, int weight) {
            var headingFont = font.Clone(); headingFont.Weight = weight;
            return model.Resolve(new TextStyle { Font = headingFont, FontSize = size, Color = color, LineHeight = 1 });
        }
        var title = Heading(grid.TitleStyle, theme.Typography.TitleSize, colors.Foreground, 700);
        var subtitle = Heading(grid.SubtitleStyle, theme.Typography.SubtitleSize, colors.MutedForeground, 400);
        var frame = new VisualFrame(grid.Title, grid.Subtitle, showLegend: true, showSurface: true,
            titleStyle: title, subtitleStyle: subtitle, showCard: true);
        // Preserve natural panel dimensions, but measure report headings through the same frame producer as export.
        var bodyHeight = layout.Height - layout.HeaderHeight;
        var height = (double)bodyHeight;
        if (grid.Title.Length > 0 || grid.Subtitle.Length > 0) {
            var measure = new VisualSceneBuilder(new VisualSize(layout.Width, layout.Height), font);
            var maximumHeadingHeight = 4 * (Math.Max(title.EffectiveFontSize * 1.3, measure.MeasureText("Mg", title).Height)
                + Math.Max(subtitle.EffectiveFontSize * 1.3, measure.MeasureText("Mg", subtitle).Height) + theme.Spacing);
            var probeSize = new VisualSize(layout.Width, bodyHeight + maximumHeadingHeight);
            var probe = new VisualRenderContext(new VisualLayoutOptions(probeSize, grid.Padding), theme, frame: frame, font: font);
            var content = VisualFrameLayout.Build(new VisualSceneBuilder(probeSize, font), probe, Array.Empty<VisualLegendEntry>());
            height += content.Top - grid.Padding;
        }
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(layout.Width, height), grid.Padding),
            theme, frame: frame, font: font);
        return new VisualExportRequest(context, new VisualRenderOptions(grid.PngOutputScale));
    }

    // Mutable model themes are input values. They never select a family renderer or alternate layout path.
    internal static VisualTheme FromTheme(ChartTheme source) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var mode = ChartColorMath.RelativeLuminance(source.CardBackground) < 0.5 ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var tokens = VisualTheme.Graphite().Resolve(mode).ToTokens();
        tokens.Background = source.Background; tokens.Surface = source.PlotBackground;
        tokens.ElevatedSurface = source.CardBackground; tokens.Foreground = source.Text;
        tokens.MutedForeground = source.MutedText; tokens.Border = source.CardBorder;
        tokens.Grid = source.Grid; tokens.Axis = source.Axis; tokens.Palette = source.Palette;
        tokens.Accent = source.Palette[0]; tokens.SequentialRamp = source.SequentialRamp;
        tokens.FontFamily = source.FontFamily;
        tokens.Status.Pass = Pair(source.Positive, tokens.Status.Pass);
        tokens.Status.Medium = Pair(source.Warning, tokens.Status.Medium);
        tokens.Status.Critical = Pair(source.Negative, tokens.Status.Critical);
        return new VisualTheme(tokens, tokens, new VisualTypography(source.FontFamily, source.TitleFontSize,
            source.SubtitleFontSize, source.TickLabelFontSize, source.LegendFontSize, source.DataLabelFontSize),
            seriesStrokeWidth: source.StrokeWidth, markerRadius: source.MarkerRadius, barRadius: source.PlotCornerRadius,
            cardRadius: source.CornerRadius, cardShadowOpacity: source.ShadowOpacity, cardShadowColor: source.ShadowColor);
    }

    private static VisualTokenColor Pair(ChartColor fill, VisualTokenColor canonical) => fill.Equals(canonical.Fill)
        ? canonical : new VisualTokenColor(fill, ChartColorMath.AccessibleTextOnBackground(fill));
}
