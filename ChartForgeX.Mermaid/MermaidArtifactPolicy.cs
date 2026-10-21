using System;
using System.Threading;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Checks source-request limits around the canonical native export pipeline.</summary>
internal sealed class MermaidArtifactPolicy : IVisualArtifactExportPolicy {
    private readonly MermaidResourceLimits _limits;
    private readonly MermaidSourceSpan _span;
    private readonly CancellationToken _token;
    private MermaidArtifactPolicy(MermaidResourceLimits limits, MermaidSourceSpan span, CancellationToken token) {
        _limits = limits; _span = span; _token = token;
    }
    internal static VisualArtifact Attach(VisualArtifact artifact, MermaidDocument document, MermaidResourceLimits? limits = null,
        CancellationToken cancellationToken = default) {
        artifact.ExportPolicy = new MermaidArtifactPolicy((limits ?? document.CapturedResourceLimits ?? new MermaidResourceLimits()).Clone(),
            document.HeaderSpan, cancellationToken);
        return artifact;
    }

    public void BeforeExport(VisualArtifact artifact, VisualArtifactExportFormat format, VisualArtifactRenderOptions? options) {
        _token.ThrowIfCancellationRequested();
        if (format != VisualArtifactExportFormat.Png) return;
        // The native owner measures unrounded layout and output scale. SVG string dimensions can
        // be rounded and are not a reliable raster-allocation oracle.
        var size = artifact.MeasureRasterSize(options);
        var pixels = size.Width * size.Height;
        if (pixels > _limits.MaximumRasterPixels) throw new MermaidResourceLimitException(
            nameof(MermaidResourceLimits.MaximumRasterPixels), _limits.MaximumRasterPixels,
            pixels > long.MaxValue ? long.MaxValue : (long)pixels, _span);
        _token.ThrowIfCancellationRequested();
    }

    public void ValidateOutput(VisualArtifactExportFormat format, long? byteCount) {
        _token.ThrowIfCancellationRequested();
        if (byteCount.HasValue && byteCount.Value > _limits.MaximumOutputBytes) throw new MermaidResourceLimitException(
            nameof(MermaidResourceLimits.MaximumOutputBytes), _limits.MaximumOutputBytes, byteCount.Value, _span);
    }
}
