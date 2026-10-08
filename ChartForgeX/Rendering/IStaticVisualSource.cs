using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>A producer-owned static visual that can be embedded without loading an optional producer into the core.</summary>
/// <remarks>Native prepared scenes implement the shared preparation pipeline. This export boundary also serves
/// immediate raster compositions and existing static producers; it does not infer a scene from serialized SVG.</remarks>
public interface IStaticVisualSource {
    /// <summary>Renders static SVG with an embedding identity scope. An empty scope uses deterministic producer identity.</summary>
    string RenderSvg(string idScope);
    /// <summary>Renders static pixels using the producer's configured output scale.</summary>
    RgbaImage RenderRgba();
}
