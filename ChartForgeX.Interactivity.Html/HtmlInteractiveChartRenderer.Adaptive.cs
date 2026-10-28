using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    internal static void ValidateAdaptiveOptions(HtmlChartAdaptiveLayoutOptions options) {
        if (options.NarrowWidth <= 0) throw new ArgumentOutOfRangeException(nameof(options.NarrowWidth));
        if (options.NarrowHeight.HasValue && options.NarrowHeight.Value <= 0) throw new ArgumentOutOfRangeException(nameof(options.NarrowHeight));
        if (options.Breakpoint <= 0) throw new ArgumentOutOfRangeException(nameof(options.Breakpoint));
    }

    internal static VisualRenderContext AdaptiveDrawingContext(VisualRenderContext context, HtmlChartAdaptiveLayoutOptions options) {
        var height = options.NarrowHeight ?? context.Layout.Size.Height;
        var padding = context.Layout.PaddingEdges;
        // Native preparation owns text fitting and geometry in both drawings. Keep valid outer insets
        // when an automatic large-canvas padding would consume the narrow canvas.
        if (padding.Left + padding.Right >= options.NarrowWidth || padding.Top + padding.Bottom >= height)
            padding = new ChartPadding(Math.Min(padding.Left, options.NarrowWidth / 4d), Math.Min(padding.Top, height / 4d),
                Math.Min(padding.Right, options.NarrowWidth / 4d), Math.Min(padding.Bottom, height / 4d));
        return new VisualRenderContext(new VisualLayoutOptions(new VisualSize(options.NarrowWidth, height), padding),
            context.Theme, context.ThemeMode, context.Frame, context.Font).WithNativeValueSource(context.CaptureNativeValueSource);
    }
}
