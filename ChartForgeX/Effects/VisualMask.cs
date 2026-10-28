using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Effects;

/// <summary>An immutable fixed-density mask snapshot in the original child's logical coordinates.</summary>
/// <remarks>Masking changes alpha once and retains unassociated source RGB wherever output alpha is nonzero.
/// Samples must share the layer's density and device grid. Outside these bounds coverage is zero.
/// Native luminance uses sRGB; SVG source masks resolve their own computed color-interpolation separately.</remarks>
public sealed class VisualMask {
    private readonly VisualSceneImageData _pixels;
    /// <summary>Copies the required RGBA pixels after validating the complete borrowed allocation and snapshot budget.</summary>
    /// <param name="image">Borrowed RGBA image; its required pixels are copied, never retained by reference.</param>
    /// <param name="origin">Mask's top-left in original child coordinates, aligned to the selected device grid.</param>
    /// <param name="pixelsPerUnit">Positive device samples per logical unit.</param>
    /// <param name="mode">Alpha alone, or sRGB luminance multiplied by mask alpha.</param>
    /// <param name="orderedDither">Uses the existing deterministic Bayer phase for final alpha only.</param>
    public VisualMask(RgbaImage image, ChartPoint origin = default, int pixelsPerUnit = 1,
        VisualMaskMode mode = VisualMaskMode.Alpha, bool orderedDither = true) {
        var valid = ValidateInput(image, origin, pixelsPerUnit, mode);
        var bytes = checked((long)valid.Width * valid.Height * 4);
        RasterEffectPreflight.ValidateCapture(null, valid.Pixels.LongLength, bytes, checked(bytes * 3), out _, out _);
        _pixels = new VisualSceneImageData(valid);
        Bounds = new ChartRect(origin.X, origin.Y, (double)valid.Width / pixelsPerUnit, (double)valid.Height / pixelsPerUnit);
        PixelsPerUnit = pixelsPerUnit; Mode = mode; OrderedDither = orderedDither;
    }
    /// <summary>Gets the finite mask coverage bounds in original child logical coordinates.</summary>
    public ChartRect Bounds { get; }
    /// <summary>Gets the mask's device density.</summary>
    public int PixelsPerUnit { get; }
    /// <summary>Gets the selected mask channel policy.</summary>
    public VisualMaskMode Mode { get; }
    /// <summary>Gets whether only final alpha quantization uses ordered dither.</summary>
    public bool OrderedDither { get; }
    /// <summary>Returns a caller-owned mutable pixel copy. Repeated exports are outside the original operation's pixel budget.</summary>
    public RgbaImage ToImage() => new(_pixels.Image.Width, _pixels.Image.Height, (byte[])_pixels.Image.Pixels.Clone());
    internal RgbaImage Image => _pixels.Image;

    /// <summary>Shares the finite input/grid contract with typed portable mask validation without copying pixels.</summary>
    internal static RgbaImage ValidateInput(RgbaImage image, ChartPoint origin, int pixelsPerUnit, VisualMaskMode mode) {
        if (pixelsPerUnit <= 0) throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
        RasterEffectPlan.ValidateMaskMode(mode);
        var valid = new RgbaImage(image.Width, image.Height, image.Pixels);
        _ = new RasterEffectRegion(0, 0, valid.Width, valid.Height);
        _ = RasterEffectGrid.Coordinate(origin.X, pixelsPerUnit, nameof(origin));
        _ = RasterEffectGrid.Coordinate(origin.Y, pixelsPerUnit, nameof(origin));
        return valid;
    }
}
