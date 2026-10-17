using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

/// <summary>Renders factual blocks through their shared marks and positioned text.</summary>
public sealed class PngFactBlockRenderer {
    /// <summary>Renders a factual block to PNG.</summary>
    public byte[] Render(IVisualBlock block) => PngWriter.WriteRgba(RenderImage(block));
    internal RgbaImage RenderImage(IVisualBlock block) => RenderCanvas(block).ToImage();
    internal RgbaCanvas RenderCanvas(IVisualBlock block, int? outputScale = null) {
        VisualFactBlockRendering.Validate(block);
        return PngVisualBlockRenderer.RenderOwnedCanvas(block, () => new SvgFactBlockRenderer().RenderLabelScene(block), outputScale);
    }
}
