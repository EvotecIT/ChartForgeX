using System;
using System.Threading;
using ChartForgeX.Raster;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>One detached pixel/PNG resource shared by repeated native image placements.</summary>
internal sealed class VisualSceneImageData {
    private readonly Lazy<string> _dataUri;
    private readonly object _dataUriGate = new object();
    private string? _encodedDataUri;
    internal VisualSceneImageData(RgbaImage image, CancellationToken cancellation = default) {
        var valid = new RgbaImage(image.Width, image.Height, image.Pixels);
        cancellation.ThrowIfCancellationRequested();
        var pixels = new byte[checked(valid.Width * valid.Height * 4)];
        Array.Copy(valid.Pixels, pixels, pixels.Length);
        Image = new RgbaImage(valid.Width, valid.Height, pixels);
        var left = valid.Width; var top = valid.Height; var right = 0; var bottom = 0;
        // Capture an alpha extent once alongside the private pixel snapshot; placements reuse it.
        for (var y = 0; y < valid.Height; y++) {
            cancellation.ThrowIfCancellationRequested();
            for (var x = 0; x < valid.Width; x++) {
                if (pixels[(y * valid.Width + x) * 4 + 3] == 0) continue;
                left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x + 1); bottom = Math.Max(bottom, y + 1);
            }
        }
        cancellation.ThrowIfCancellationRequested();
        AlphaBounds = right > left && bottom > top ? new ChartRect(left, top, right - left, bottom - top) : (ChartRect?)null;
        _dataUri = new Lazy<string>(() => EncodeDataUri(null, null));
    }
    internal RgbaImage Image { get; }
    /// <summary>Gets the nonzero-alpha source pixel-cell extent, or null for an entirely transparent capture. This is not a per-pixel hit mask.</summary>
    internal ChartRect? AlphaBounds { get; }
    internal string DataUri => _dataUri.Value;

    /// <summary>Uses the same immutable PNG resource while bounding an actual SVG request before identity work.</summary>
    internal string BoundedDataUri(int maximumCharacters, Func<long, Exception> overflow) => EncodeDataUri(maximumCharacters, overflow);

    private string EncodeDataUri(int? maximumCharacters, Func<long, Exception>? overflow) {
        const string prefix = "data:image/png;base64,";
        lock (_dataUriGate) {
            if (_encodedDataUri != null) {
                if (maximumCharacters.HasValue && _encodedDataUri.Length > maximumCharacters.Value) throw overflow!(_encodedDataUri.Length);
                return _encodedDataUri;
            }
            byte[] png;
            if (maximumCharacters.HasValue) {
                var available = Math.Max(0L, (long)maximumCharacters.Value - prefix.Length);
                var maximumBytes = available / 4 * 3;
                png = PngWriter.WriteRgbaBounded(Image, maximumBytes,
                    observed => overflow!(prefix.Length + (observed + 2) / 3 * 4));
            } else png = PngWriter.WriteRgba(Image);
            var encoded = prefix + Convert.ToBase64String(png);
            if (maximumCharacters.HasValue && encoded.Length > maximumCharacters.Value) throw overflow!(encoded.Length);
            _encodedDataUri = encoded;
            return encoded;
        }
    }
}
