using ChartForgeX.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveTopologyRenderer {
    // Legacy topology/report entrypoints use Automatic; caller-owned presentation callbacks retain their own SVG.
    internal static string AutomaticText(PreparedTopology prepared) {
        var emission = prepared.Visual.ExportSvgEmission(VisualSvgTextPresentation.Automatic);
        var writer = new HtmlMarkupWriter(); writer.RawTrusted(emission.Svg);
        HtmlCapturedFontAssets.Write(writer, emission.Fonts);
        return writer.Build();
    }
}
