using System;
using System.Threading;
using System.Collections.Generic;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Checks source-request limits around the canonical native export pipeline.</summary>
internal sealed class MermaidArtifactPolicy : IVisualArtifactExportPolicy, IVisualArtifactResourcePolicy {
    private readonly MermaidResourceLimits _limits;
    private readonly MermaidSourceSpan _span;
    private readonly CancellationToken _token;
    private readonly IReadOnlyDictionary<string, MermaidSourceSpan> _resourceSpans;
    private MermaidArtifactPolicy(MermaidResourceLimits limits, MermaidSourceSpan span, CancellationToken token, MermaidDocument document) {
        _limits = limits; _span = span; _token = token; _resourceSpans = CaptureResourceSpans(document);
    }
    internal static VisualArtifact Attach(VisualArtifact artifact, MermaidDocument document, MermaidResourceLimits? limits = null,
        CancellationToken cancellationToken = default) {
        artifact.ExportPolicy = new MermaidArtifactPolicy((limits ?? document.CapturedResourceLimits ?? new MermaidResourceLimits()).Clone(),
            document.HeaderSpan, cancellationToken, document);
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

    int? IVisualArtifactResourcePolicy.MaximumSvgCharacters => (int)Math.Min(int.MaxValue, _limits.MaximumOutputBytes);
    Exception IVisualArtifactResourcePolicy.TranslateResourceFailure(VisualResourceLimitException exception) => Translate(exception, _span, _resourceSpans);

    internal static T ConvertNative<T>(MermaidDocument document, Func<T> convert) {
        try { return convert(); }
        catch (VisualResourceLimitException exception) { throw Translate(exception, document.HeaderSpan, CaptureResourceSpans(document)); }
    }

    internal static MermaidResourceLimitException Translate(VisualResourceLimitException exception, MermaidDocument document) =>
        Translate(exception, document.HeaderSpan, CaptureResourceSpans(document));

    private static MermaidResourceLimitException Translate(VisualResourceLimitException exception, MermaidSourceSpan fallback,
        IReadOnlyDictionary<string, MermaidSourceSpan> spans) => new(exception.Limit, exception.Maximum, exception.Observed,
            exception.ItemId != null && spans.TryGetValue(exception.ItemId, out var span) ? span : fallback, exception);

    private static IReadOnlyDictionary<string, MermaidSourceSpan> CaptureResourceSpans(MermaidDocument document) {
        var result = new Dictionary<string, MermaidSourceSpan>(StringComparer.Ordinal);
        if (document is MermaidFlowchartDocument flowchart) foreach (var node in flowchart.Nodes) {
            var key = node.Properties.ContainsKey("img") ? "img" : node.Properties.ContainsKey("icon") ? "icon" : null;
            if (key == null) continue;
            result[node.Id] = node.PropertyValueSpans.TryGetValue(key, out var valueSpan) ? valueSpan
                : node.PropertySpans.TryGetValue(key, out var propertySpan) ? propertySpan : node.Span;
        }
        return result;
    }
}
