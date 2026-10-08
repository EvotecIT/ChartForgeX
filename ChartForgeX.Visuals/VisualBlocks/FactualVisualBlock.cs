using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

/// <summary>Base for static factual producers owned by the optional Visuals package.</summary>
/// <typeparam name="TSelf">The concrete factual block type.</typeparam>
public abstract class FactualVisualBlock<TSelf> : VisualBlock<TSelf>, IFactualVisualPixels where TSelf : FactualVisualBlock<TSelf> {
    /// <summary>Renders factual SVG without introducing optional types into the core renderer.</summary>
    public override string RenderSvg(string idScope) => new SvgFactBlockRenderer().Render(this, idScope);
    /// <summary>Renders factual pixels using the configured static output scale.</summary>
    public override RgbaImage RenderRgba() => new PngFactBlockRenderer().RenderImage(this);
    RgbaImage IFactualVisualPixels.RenderAtScale(int outputScale) => new PngFactBlockRenderer().RenderCanvas(this, outputScale).ToImage();
}

/// <summary>Preserves composition density without changing a factual model's configured export scale.</summary>
internal interface IFactualVisualPixels {
    RgbaImage RenderAtScale(int outputScale);
}
