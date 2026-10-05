using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartLabelScene {
    /// <summary>Paints the geometry of the placed chart scene on the destination device grid.</summary>
    internal void PaintMarks(RgbaCanvas canvas) {
        var layer = new XElement(_document.Root!);
        // Text and grouped legend decorations are painted by the same label layer in both renderer paths.
        foreach (var element in layer.Descendants().Where(e => e.Name.LocalName == "text" ||
                     (string?)e.Attribute("data-cfx-label-decoration") == "true").ToArray()) element.Remove();
        SvgRasterRenderer.PaintChartMarks(canvas, SvgRasterParser.FromDocumentRoot(layer));
    }
}
