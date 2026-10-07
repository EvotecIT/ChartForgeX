using System;
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
        var sourceFrame = context.Frame;
        var frameColors = context.Theme.Resolve(context.ThemeMode);
        TextStyle RoleStyle(TextStyle? configured, TextStyleOverride model, double size, ChartColor color, int weight) {
            var fallback = configured ?? new TextStyle { Font = context.Font, FontSize = size, Color = color, LineHeight = 1 };
            if (configured == null) fallback.Font.Weight = weight;
            return model.Resolve(fallback);
        }
        var frame = new VisualFrame(sourceFrame.Title ?? Title, sourceFrame.Subtitle ?? Subtitle,
            sourceFrame.ShowLegend, sourceFrame.LegendPosition, sourceFrame.ShowSurface, sourceFrame.TransparentBackground,
            RoleStyle(sourceFrame.TitleStyle, Options.TitleStyle, context.Theme.Typography.TitleSize, frameColors.Foreground, 600),
            RoleStyle(sourceFrame.SubtitleStyle, Options.SubtitleStyle, context.Theme.Typography.SubtitleSize, frameColors.MutedForeground, 400),
            RoleStyle(sourceFrame.LegendStyle, Options.LegendStyle, context.Theme.Typography.LegendSize, frameColors.Foreground, 400),
            sourceFrame.LegendMaximumRows, sourceFrame.LegendMaximumHeightFraction, sourceFrame.ShowCard);
        context = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, frame, context.Font);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var colors = context.Theme.Resolve(context.ThemeMode);
        var entries = VisualChartCompiler.LegendEntries(family, this, colors);
        var content = VisualFrameLayout.Build(builder, context, entries);
        VisualChartCompiler.Build(family, this, context, builder, content);
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= string.IsNullOrWhiteSpace(frame.Title) ? Title : frame.Title;
        accessibility.Description ??= string.IsNullOrWhiteSpace(frame.Subtitle) ? Subtitle : frame.Subtitle;
        return new PreparedVisual(builder.Build(), accessibility,
            svgOptions: new VisualSvgOptions(colorVariables: Options.SvgColorVariables));
    }
}
