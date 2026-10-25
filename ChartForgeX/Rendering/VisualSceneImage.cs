using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>Resolved image pixels, copied during compilation and shared by both exporters.</summary>
internal sealed class VisualSceneImage : VisualSceneNode {
    private readonly VisualSceneImageData _data;
    internal VisualSceneImage(RgbaImage image, ChartRect bounds, string? role, string? id, double opacity = 1)
        : this(new VisualSceneImageData(image), bounds, role, id, opacity) { }
    internal VisualSceneImage(VisualSceneImageData data, ChartRect bounds, string? role, string? id, double opacity = 1) : base(role, id) {
        _data = data; Bounds = bounds; Opacity = opacity;
    }
    internal RgbaImage Image => _data.Image;
    internal ChartRect? AlphaBounds => _data.AlphaBounds;
    internal ChartRect Bounds { get; }
    internal double Opacity { get; }
    internal string DataUri => _data.DataUri;
    internal string BoundedDataUri(int maximumCharacters, System.Func<long, System.Exception> overflow) =>
        _data.BoundedDataUri(maximumCharacters, overflow);
}
