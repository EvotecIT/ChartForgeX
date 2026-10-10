using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

public sealed partial class Chart : IVisualRenderable {
    /// <summary>Prepares chart geometry, typography and semantics into a detached shared static scene.</summary>
    /// <remarks>Every chart family uses the same mutable-model validation before its scene is compiled.</remarks>
    public PreparedVisual Prepare(VisualRenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        ChartGuards.RenderCompatibility(this, preparing: true);
        var family = VisualChartCompiler.Family(this);
        var bubbleScale = Series.Any(series => series.Kind == ChartSeriesKind.Bubble) ? ChartBubbleSizeScale.Create(this) : null;
        var sourceFrame = context.Frame;
        var frameColors = context.Theme.Resolve(context.ThemeMode);
        var entries = VisualChartCompiler.LegendEntries(family, this, frameColors, bubbleScale);
        TextStyle RoleStyle(TextStyle? configured, TextStyleOverride model, double size, ChartColor color, int weight) {
            var fallback = configured ?? new TextStyle { Font = context.Font, FontSize = size, Color = color, LineHeight = 1 };
            if (configured == null) fallback.Font.Weight = weight;
            return model.Resolve(fallback);
        }
        var frame = new VisualFrame(sourceFrame.Title ?? Title, sourceFrame.Subtitle ?? Subtitle,
            sourceFrame.HasExplicitLegend ? sourceFrame.ShowLegend : ChartLegendVisibility.ForPreparedContent(this, entries.Count),
            sourceFrame.HasExplicitLegendPosition ? sourceFrame.LegendPosition : Options.LegendPosition,
            sourceFrame.ShowSurface, sourceFrame.TransparentBackground,
            RoleStyle(sourceFrame.TitleStyle, Options.TitleStyle, context.Theme.Typography.TitleSize, frameColors.Foreground, 700),
            RoleStyle(sourceFrame.SubtitleStyle, Options.SubtitleStyle, context.Theme.Typography.SubtitleSize, frameColors.MutedForeground, 400),
            RoleStyle(sourceFrame.LegendStyle, Options.LegendStyle, context.Theme.Typography.LegendSize, frameColors.Foreground, 400),
            sourceFrame.LegendMaximumRows, sourceFrame.LegendMaximumHeightFraction, sourceFrame.ShowCard, sourceFrame.LegendTitle);
        context = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, frame, context.Font);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var colors = context.Theme.Resolve(context.ThemeMode);
        var content = VisualFrameLayout.Build(builder, context, entries);
        VisualChartCompiler.Build(family, this, context, builder, content, bubbleScale);
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= !string.IsNullOrWhiteSpace(frame.Title) ? frame.Title
            : !string.IsNullOrWhiteSpace(Title) ? Title : Options.Labels.UntitledChart;
        accessibility.Description ??= Options.Labels.Describe(ChartAccessibleDescription.Facts(this));
        return new PreparedVisual(builder.Build(), accessibility,
            svgOptions: new VisualSvgOptions(colorVariables: Options.SvgColorVariables));
    }
}
