using System;

namespace ChartForgeX.Raster;

/// <summary>Describes one complete RGBA animation canvas and its display duration.</summary>
public readonly struct RasterAnimationFrame {
    /// <summary>Initializes a frame using a caller-owned RGBA pixel buffer.</summary>
    /// <param name="image">The complete canvas. All frames in an animation must have matching dimensions.</param>
    /// <param name="duration">A nonnegative display duration. Zero preserves a container's zero frame delay.</param>
    /// <remarks>The pixel buffer is borrowed while encoding. Do not change it until the encoding call returns.</remarks>
    public RasterAnimationFrame(RgbaImage image, TimeSpan duration) {
        ValidateImage(image);
        if (duration < TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Frame duration must be nonnegative.");
        }
        Image = image;
        Duration = duration;
    }

    /// <summary>Gets the complete frame canvas.</summary>
    public RgbaImage Image { get; }

    /// <summary>Gets the nonnegative frame display duration before the next frame or playback completion.</summary>
    /// <remarks>Zero is encoded as a zero delay without adding a minimum display duration.</remarks>
    public TimeSpan Duration { get; }

    internal static void ValidateImage(RgbaImage image) {
        if (image.Width <= 0 || image.Height <= 0 || image.Pixels == null ||
            image.Pixels.LongLength < checked((long)image.Width * image.Height * 4)) {
            throw new ArgumentException("An animation frame requires a valid RGBA image.", nameof(image));
        }
    }
}
