using System;
using ChartForgeX.Effects;

namespace ChartForgeX.VisualArtifacts;

/// <summary>One captured complete-viewport mask, applied in list order after preparing editable source facts.</summary>
/// <remarks>The mutable portable record retains original unassociated RGBA bytes, including RGB at zero alpha.
/// Capture and replay detach its pixel array. JSON encodes those bytes as canonical base64 within the common envelope limits.</remarks>
public sealed class VisualArtifactInterchangeMask {
    /// <summary>Gets or sets the original bitmap width in device pixels.</summary>
    public int ImageWidth { get; set; }
    /// <summary>Gets or sets the original bitmap height in device pixels.</summary>
    public int ImageHeight { get; set; }
    /// <summary>Gets or sets the device-grid-aligned left origin in source logical coordinates.</summary>
    public double OriginX { get; set; }
    /// <summary>Gets or sets the device-grid-aligned top origin in source logical coordinates.</summary>
    public double OriginY { get; set; }
    /// <summary>Gets or sets the shared native device samples per logical unit.</summary>
    public int PixelsPerUnit { get; set; } = 1;
    /// <summary>Gets or sets the existing native alpha or sRGB luminance coverage policy.</summary>
    public VisualMaskMode Mode { get; set; }
    /// <summary>Gets or sets whether final alpha quantization uses the existing ordered dither.</summary>
    public bool OrderedDither { get; set; } = true;
    /// <summary>Gets or sets exactly ImageWidth*ImageHeight*4 row-major RGBA bytes.</summary>
    /// <remarks>This portable DTO is mutable. Snapshot/export/replay never retain the caller's array.</remarks>
    public byte[] Pixels { get; set; } = Array.Empty<byte>();
}
