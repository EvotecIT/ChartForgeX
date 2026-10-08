using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Composition;

public sealed partial class VisualCanvas : IStaticVisualSource {
    /// <summary>Renders the canvas through its static SVG owner using a deterministic embedding scope.</summary>
    public string RenderSvg(string idScope) => new SvgVisualCanvasRenderer().Render(this, idScope);

    /// <summary>Renders the canvas through its native raster owner at the configured output scale.</summary>
    public RgbaImage RenderRgba() => new PngVisualCanvasRenderer().RenderImage(this);
}
