using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartLabelScene {
    /// <summary>Paints the geometry of the placed chart scene on the destination device grid.</summary>
    internal void PaintMarks(RgbaCanvas canvas) {
        // Text and grouped legend decorations are painted by the same label layer in both renderer paths, so they and
        // their content are left out of the mark layer.
        var layer = SvgRasterParser.FromMarkupRoot(_document.Root, e => !(e.LocalName == "text" || e.Attribute("data-cfx-label-decoration") == "true"), filterDescendants: true);
        SvgRasterRenderer.PaintChartMarks(canvas, layer);
    }
}