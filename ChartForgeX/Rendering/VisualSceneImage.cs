using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>Resolved image pixels, copied during compilation and shared by both exporters.</summary>
internal sealed class VisualSceneImage : VisualSceneNode {
    private readonly Lazy<string> _dataUri;
    internal VisualSceneImage(RgbaImage image, ChartRect bounds, string? role, string? id, double opacity = 1) : base(role, id) {
        var pixels = new byte[checked(image.Width * image.Height * 4)];
        Array.Copy(image.Pixels, pixels, pixels.Length);
        Image = new RgbaImage(image.Width, image.Height, pixels); Bounds = bounds; Opacity = opacity;
        _dataUri = new Lazy<string>(() => "data:image/png;base64," + Convert.ToBase64String(PngWriter.WriteRgba(Image)));
    }
    internal RgbaImage Image { get; }
    internal ChartRect Bounds { get; }
    internal double Opacity { get; }
    internal string DataUri => _dataUri.Value;
}
