using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

/// <summary>Renders genuine core diagrams and delegates other blocks to their static pixel producer.</summary>
public sealed partial class PngVisualBlockRenderer {
    /// <summary>Renders a genuine diagram or producer-owned static block to PNG.</summary>
    public byte[] Render(IVisualBlock block) => PngWriter.WriteRgba(RenderImage(block));
    internal RgbaImage RenderImage(IVisualBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        return SvgVisualBlockRenderer.IsNativeBlock(block) ? RenderNativeImage(block) : block.RenderRgba();
    }
    internal RgbaImage RenderNativeImage(IVisualBlock block) => RenderCanvas(block).ToImage();
    internal RgbaCanvas RenderCanvas(IVisualBlock block, int? outputScale = null) {
        VisualBlockRendering.Validate(block);
        return RenderOwnedCanvas(block, canvas => {
            if (block is PacketLayoutBlock packet) DrawPacketLayout(canvas, packet);
            else if (block is BlockLayoutBlock blockLayout) DrawBlockLayout(canvas, blockLayout);
            else if (block is GitGraphBlock gitGraph) DrawGitGraph(canvas, gitGraph);
            else if (block is VennDiagramBlock venn) DrawVennDiagram(canvas, venn);
            else if (block is FishboneDiagramBlock fishbone) DrawFishboneDiagram(canvas, fishbone);
            else if (block is WardleyMapBlock wardleyMap) DrawWardleyMap(canvas, wardleyMap);
            else throw new NotSupportedException("Unsupported static block producer: " + block.GetType().FullName);
        }, () => new SvgVisualBlockRenderer().RenderLabelScene(block), outputScale);
    }

    internal static RgbaCanvas RenderOwnedCanvas(IVisualBlock block, Action<RgbaCanvas> renderContent, Func<ChartLabelScene> labelScene, int? outputScale = null) {
        var options = block.Options;
        var theme = options.Theme;
        if (theme.UseGraphiteLayout) {
            var scene = labelScene();
            var target = new RgbaCanvas(options.Size.Width, options.Size.Height, 2, TypographyFontResolver.ResolveThemeFont(theme.FontFamily), outputScale ?? options.PngOutputScale);
            target.Clear(options.HostOwnsFrame || options.TransparentBackground || options.ShowCard && theme.UseCard ? ChartColor.Transparent : VisualBlockRendering.SurfaceBackground(options));
            scene.PaintMarks(target);
            scene.Paint(target);
            return target;
        }
        using var emphasis = RgbaCanvas.OpenEmphasisScope();
        var canvas = new RgbaCanvas(options.Size.Width, options.Size.Height, 2, TypographyFontResolver.ResolveThemeFont(theme.FontFamily), outputScale ?? options.PngOutputScale);
        canvas.Clear(VisualBlockRendering.SurfaceBackground(options));
        if (options.ShowCard && theme.UseCard) {
            canvas.FillRoundedRectVerticalGradient(0, 0, options.Size.Width, options.Size.Height, theme.CornerRadius, ChartSurfacePolish.GradientTop(theme.CardBackground), ChartSurfacePolish.GradientBottom(theme.CardBackground));
            canvas.StrokeRoundedRect(0.5, 0.5, Math.Max(1, options.Size.Width - 1), Math.Max(1, options.Size.Height - 1), theme.CornerRadius, theme.CardBorder, 1);
            if (theme.CardBackground.A > 0) canvas.StrokeRoundedRect(ChartVisualPrimitives.CardInnerHighlightInset, ChartVisualPrimitives.CardInnerHighlightInset, Math.Max(1, options.Size.Width - ChartVisualPrimitives.CardInnerHighlightInset * 2), Math.Max(1, options.Size.Height - ChartVisualPrimitives.CardInnerHighlightInset * 2), Math.Max(0, theme.CornerRadius - ChartVisualPrimitives.CardInnerHighlightInset), ApplyOpacity(ChartColor.White, ChartVisualPrimitives.CardInnerHighlightOpacity), 1);
        }

        renderContent(canvas);
        return canvas;
    }

    internal static void DrawHeading(RgbaCanvas canvas, IVisualBlock block, ref double y, double x, double width) {
        var theme = block.Options.Theme;
        if (block.Title.Length > 0) {
            canvas.DrawTextEmphasized(x, y, FitText(canvas, block.Title, theme.TitleFontSize, width), theme.Text, theme.TitleFontSize);
            y += theme.TitleFontSize + 8;
        }

        if (block.Subtitle.Length > 0) {
            canvas.DrawText(x, y, FitText(canvas, block.Subtitle, theme.SubtitleFontSize, width), theme.MutedText, theme.SubtitleFontSize);
            y += theme.SubtitleFontSize + 13;
        } else if (block.Title.Length > 0) {
            y += 8;
        }
    }

    internal static void DrawAlignedText(RgbaCanvas canvas, string text, double x, double y, double width, TextAlignment alignment, ChartColor color, double fontSize, bool emphasized) {
        var fitted = FitText(canvas, text, fontSize, Math.Max(1, width));
        var textWidth = emphasized ? canvas.MeasureTextEmphasizedWidth(fitted, fontSize) : canvas.MeasureTextWidth(fitted, fontSize);
        var textX = alignment == TextAlignment.Center ? x + (width - textWidth) / 2 : alignment == TextAlignment.Right ? x + width - textWidth : x;
        if (emphasized) canvas.DrawTextEmphasized(textX, y, fitted, color, fontSize);
        else canvas.DrawText(textX, y, fitted, color, fontSize);
    }

    internal static void DrawCenteredText(RgbaCanvas canvas, string text, double x, double y, double size, ChartColor color, bool emphasized, double? maxWidth = null) {
        var fitted = FitText(canvas, text, size, Math.Max(1, maxWidth ?? x * 2));
        var width = emphasized ? canvas.MeasureTextEmphasizedWidth(fitted, size) : canvas.MeasureTextWidth(fitted, size);
        if (emphasized) canvas.DrawTextEmphasized(x - width / 2, y, fitted, color, size);
        else canvas.DrawText(x - width / 2, y, fitted, color, size);
    }

    internal static void DrawCenteredTextMiddle(RgbaCanvas canvas, string text, double x, double y, double size, ChartColor color, bool emphasized, double? maxWidth = null) {
        var fitted = FitText(canvas, text, size, Math.Max(1, maxWidth ?? x * 2));
        var width = emphasized ? canvas.MeasureTextEmphasizedWidth(fitted, size) : canvas.MeasureTextWidth(fitted, size);
        var height = canvas.MeasureTextHeight(size);
        if (emphasized) canvas.DrawTextEmphasized(x - width / 2, y - height / 2, fitted, color, size);
        else canvas.DrawText(x - width / 2, y - height / 2, fitted, color, size);
    }

    internal static ChartColor ApplyOpacity(ChartColor color, double opacity) {
        var alpha = (byte)Math.Max(0, Math.Min(255, Math.Round(color.A * Math.Max(0, Math.Min(1, opacity)))));
        return ChartColor.FromRgba(color.R, color.G, color.B, alpha);
    }

    internal static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    internal static string FitText(RgbaCanvas canvas, string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || canvas.MeasureTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (canvas.MeasureTextWidth(suffix, fontSize) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (canvas.MeasureTextWidth(value.Substring(0, mid) + suffix, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, Typography.TextElementBoundary.Snap(value, low)) + suffix;
    }
}
