using System;
using System.Threading;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

/// <summary>Explicitly enables bounded native-filter source memberships on one rasterization.</summary>
/// <remarks>Ordered SourceGraphic/SourceAlpha and earlier named results support finite color matrices, component transfer,
/// flood, Gaussian blur, morphology, aligned offset, standard blend, over/in/out/atop/xor/arithmetic composition and ordered merge.
/// Named results retain premultiplied RGBA16 and their computed sRGB/linearRGB space. Only RGB transfers at consumer space changes;
/// every primitive clamps, alpha stays coverage, and zero-alpha RGB is discarded. Existing exact byte/scalar/typed recipe paths remain.
/// Regions require explicit aligned numeric user-space filter lengths and positive axis transforms. Primitive output regions clip results.
/// The Gaussian reuses the native three-box approximation; this is not an exact Gaussian or browser-byte identity contract.
/// Solid SourceGraphic descendants may use existing isolated opacity; nested filters, transforms, viewports, images and paint servers retain diagnostics.
/// Backdrop/paint built-ins, authored filter resolution and unrepresented primitives remain unsupported. Null options retain ordinary SFR002 behavior.
/// Stage count shares existing source, work, cumulative byte and cancellation limits; one applied sequence is one filter application.</remarks>
public sealed class SvgRasterFilterOptions {
    /// <summary>Creates immutable, per-document native-filter import limits and a cooperative cancellation token.</summary>
    /// <param name="maximumApplications">Maximum applied instances, including use instances and embedded SVG documents; 1 through 16.</param>
    /// <param name="maximumWorkSamples">Counted effect/capture sample visits, at most one billion; not a duration bound.</param>
    /// <param name="maximumLiveBytes">Conservative cumulative declared pixel arrays, at most 256 MiB; not process RSS.</param>
    /// <param name="cancellationToken">Checked at document/element boundaries, capture rows, existing kernel rows and before publishing pixels.</param>
    public SvgRasterFilterOptions(int maximumApplications = 16, long maximumWorkSamples = 1_000_000_000,
        long maximumLiveBytes = 256L * 1024 * 1024, CancellationToken cancellationToken = default) {
        if (maximumApplications < 1 || maximumApplications > 16) throw new ArgumentOutOfRangeException(nameof(maximumApplications));
        if (maximumWorkSamples < 1 || maximumWorkSamples > RasterEffectPreflight.MaximumWorkSamples) throw new ArgumentOutOfRangeException(nameof(maximumWorkSamples));
        if (maximumLiveBytes < 1 || maximumLiveBytes > RasterEffectPreflight.MaximumLiveBytes) throw new ArgumentOutOfRangeException(nameof(maximumLiveBytes));
        MaximumApplications = maximumApplications; MaximumWorkSamples = maximumWorkSamples;
        MaximumLiveBytes = maximumLiveBytes; CancellationToken = cancellationToken;
    }
    /// <summary>Gets the document-wide instance limit; a reused definition is charged for each painted instance.</summary>
    public int MaximumApplications { get; }
    /// <summary>Gets the conservative cumulative explicit sample-visit ceiling.</summary>
    public long MaximumWorkSamples { get; }
    /// <summary>Gets the conservative declared-pixel-array ceiling; encoder, XML, font and geometry allocations retain their own owners.</summary>
    public long MaximumLiveBytes { get; }
    /// <summary>Gets cancellation observed by the opt-in import path.</summary>
    public CancellationToken CancellationToken { get; }
}
