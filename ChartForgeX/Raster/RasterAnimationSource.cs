using System;
using System.Threading;

namespace ChartForgeX.Raster;

/// <summary>A repeatable bounded frame producer with known dimensions and count.</summary>
/// <remarks>GIF makes a palette pass and an encoding pass. Each index must return identical pixels and timing on both passes. Buffers are borrowed only until the next frame is requested.</remarks>
public sealed class RasterAnimationSource {
    private readonly Func<int, CancellationToken, RasterAnimationFrame> _renderFrame;
    /// <summary>Creates a deterministic frame source. Rendering is deferred until encoding or reading.</summary>
    public RasterAnimationSource(int width, int height, int frameCount, Func<int, CancellationToken, RasterAnimationFrame> renderFrame,
        long additionalWorkingBytes = 0) {
        if (width < 1 || width > 65535) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1 || height > 65535) throw new ArgumentOutOfRangeException(nameof(height));
        if (frameCount < 1) throw new ArgumentOutOfRangeException(nameof(frameCount));
        if (additionalWorkingBytes < 0) throw new ArgumentOutOfRangeException(nameof(additionalWorkingBytes));
        Width = width; Height = height; FrameCount = frameCount;
        AdditionalWorkingBytes = additionalWorkingBytes;
        _renderFrame = renderFrame ?? throw new ArgumentNullException(nameof(renderFrame));
    }
    /// <summary>Gets the complete canvas width in pixels.</summary>
    public int Width { get; }
    /// <summary>Gets the complete canvas height in pixels.</summary>
    public int Height { get; }
    /// <summary>Gets the known number of frames.</summary>
    public int FrameCount { get; }
    /// <summary>Gets the producer's conservative retained asset and working-buffer estimate, excluding returned frame pixels.</summary>
    public long AdditionalWorkingBytes { get; }
    /// <summary>Renders and validates one frame.</summary>
    public RasterAnimationFrame GetFrame(int index, CancellationToken cancellationToken = default) {
        if (index < 0 || index >= FrameCount) throw new ArgumentOutOfRangeException(nameof(index));
        cancellationToken.ThrowIfCancellationRequested();
        var frame = _renderFrame(index, cancellationToken);
        RasterAnimationFrame.ValidateImage(frame.Image);
        if (frame.Image.Width != Width || frame.Image.Height != Height) throw new InvalidOperationException("Frame source returned a different canvas size.");
        if (frame.Duration < TimeSpan.Zero) throw new InvalidOperationException("Frame source returned a negative duration.");
        cancellationToken.ThrowIfCancellationRequested();
        return frame;
    }
}
