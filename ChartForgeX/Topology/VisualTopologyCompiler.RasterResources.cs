using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private readonly SvgRasterResourceContext _artworkResources = new();

    private bool TryCaptureArtworkImage(TopologyNode node, string href, ChartRect bounds, string? aspect, out VisualSceneImageData resource) {
        _artworkResources.ItemId = node.Id;
        var size = ArtworkSamplingSize(bounds);
        return SvgRasterRenderer.TryCaptureImageResource(href, size.Width, size.Height, aspect, _artworkResources, out resource);
    }

    private bool TryCaptureArtworkFragment(TopologyNode node, string body, string? viewBox, string? aspect, ChartRect bounds,
        out VisualSceneImageData resource) {
        _artworkResources.ItemId = node.Id;
        var size = ArtworkSamplingSize(bounds);
        return SvgRasterRenderer.TryCaptureFragment(body, viewBox, aspect, size.Width, size.Height, _artworkResources, out resource);
    }
}
