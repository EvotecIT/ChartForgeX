using System;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

/// <summary>Renders genuine core diagrams and delegates other blocks to their static pixel producer.</summary>
public sealed class PngVisualBlockRenderer {
    /// <summary>Renders a genuine diagram or producer-owned static block to PNG.</summary>
    public byte[] Render(IVisualBlock block) => PngWriter.WriteRgba(RenderImage(block));
    internal RgbaImage RenderImage(IVisualBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        return SvgVisualBlockRenderer.IsNativeBlock(block) ? RenderNativeImage(block) : block.RenderRgba();
    }
    internal RgbaImage RenderNativeImage(IVisualBlock block) => RenderCanvas(block).ToImage();
    internal RgbaCanvas RenderCanvas(IVisualBlock block, int? outputScale = null) {
        VisualBlockRendering.Validate(block);
        return RenderOwnedCanvas(block, () => new SvgVisualBlockRenderer().RenderLabelScene(block), outputScale);
    }

    /// <summary>Paints the block owner's shared marks and positioned text at the requested output density.</summary>
    internal static RgbaCanvas RenderOwnedCanvas(IVisualBlock block, Func<ChartLabelScene> labelScene, int? outputScale = null) {
        var options = block.Options;
        var theme = options.ResolvedTheme;
        var scene = labelScene();
        var target = new RgbaCanvas(options.Size.Width, options.Size.Height, 2, TypographyFontResolver.ResolveThemeFont(theme.FontFamily), outputScale ?? options.PngOutputScale);
        target.Clear(ChartColor.Transparent);
        scene.PaintMarks(target);
        scene.Paint(target);
        return target;
    }
}
