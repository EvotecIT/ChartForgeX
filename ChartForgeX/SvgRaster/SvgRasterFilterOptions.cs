using System;
using System.Threading;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

/// <summary>Explicitly enables the bounded source-sRGB native-filter subset on one rasterization.</summary>
/// <remarks>Finite two-child flood plus SourceGraphic standard blending or over/in/out/atop/xor reuses the same native compositor.
/// Standard blend modes operate in explicitly computed sRGB; implicit/linearRGB filter spaces and no-composite remain unsupported.
/// One explicit user-space SourceGraphic Gaussian or the finite SourceAlpha morphology-shadow chain is supported per filter. The same native
/// three-box approximation used by raster composition is used; this is not an exact Gaussian or browser-pixel contract.
/// Solid SourceGraphic descendants may use existing isolated opacity; their actual compositor canvases/copies share the document debit.
/// Nested filters, transforms, viewports, images and paint-server sources remain outside this finite subset.
/// A null options argument keeps the existing SFR002 behavior. Limits can tighten, never raise, existing raster ceilings.</remarks>
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
