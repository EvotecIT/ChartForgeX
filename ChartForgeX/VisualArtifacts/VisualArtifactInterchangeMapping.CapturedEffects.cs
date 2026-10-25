using System;
using System.Threading;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Replays a bounded ordered root presentation on caller-prepared source in the captured source viewport.</summary>
    /// <param name="envelope">Validated typed effect inputs, independently retained from editable source facts.</param>
    /// <param name="source">Fresh unwrapped source prepared through its existing typed family owner.</param>
    /// <param name="cancellationToken">Cancels before snapshots and publication; no token is retained.</param>
    /// <returns>The same source for empty policy, otherwise its detached retained presentation.</returns>
    /// <remarks>Terminal captures require ApplyCapturedLayer. This operation does not reconstruct scene commands or layout.</remarks>
    public static PreparedVisual ApplyCapturedEffects(this VisualArtifactInterchangeEnvelope envelope,
        PreparedVisual source, CancellationToken cancellationToken = default) {
        ValidateReplaySource(envelope, source, cancellationToken);
        var records = envelope.Presentation?.CapturedEffects;
        if (records == null || records.Count == 0) return source;
        foreach (var record in records)
            if (record.CaptureFrame != null) throw new NotSupportedException("Terminal captures require ApplyCapturedLayer.");
        var effects = ReplayEffects(envelope, cancellationToken);
        var result = source;
        foreach (var effect in effects) {
            cancellationToken.ThrowIfCancellationRequested();
            result = result.WithRootEffect(effect);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    /// <summary>Replays root effects, one explicit capture and any following captured-layer masks.</summary>
    /// <param name="envelope">Validated ordered presentation with exactly one terminal frame.</param>
    /// <param name="source">Fresh caller-prepared source; its own facts become the returned layer's source.</param>
    /// <param name="cancellationToken">Cancels native capture, effect rows and publication without retaining a token.</param>
    /// <returns>An actual captured layer whose source and output frames remain distinct.</returns>
    /// <remarks>Changed output Bounds reject clearly. Editing that changes a no-bright footprint requires a fresh capture;
    /// stale envelope source facts or source-coordinate animation plans are never attached to the baked output.</remarks>
    public static CapturedVisualLayer ApplyCapturedLayer(this VisualArtifactInterchangeEnvelope envelope,
        PreparedVisual source, CancellationToken cancellationToken = default) {
        ValidateReplaySource(envelope, source, cancellationToken);
        var records = envelope.Presentation?.CapturedEffects;
        if (records == null) throw new ArgumentException("A terminal capture record is required.", nameof(envelope));
        var terminal = -1;
        for (var index = 0; index < records.Count; index++) if (records[index].CaptureFrame != null) terminal = index;
        if (terminal < 0) throw new ArgumentException("A terminal capture record is required.", nameof(envelope));
        var effects = ReplayEffects(envelope, cancellationToken);
        var prepared = source;
        for (var index = 0; index < terminal; index++) {
            cancellationToken.ThrowIfCancellationRequested();
            prepared = prepared.WithRootEffect(effects[index]);
        }
        var frame = records[terminal].CaptureFrame!;
        var layer = effects[terminal].Capture(prepared, frame.RasterOptions, cancellationToken);
        if (!layer.Bounds.Equals(frame.Bounds))
            throw new InvalidOperationException("Prepared or edited source changed the actual captured output Bounds. Create a fresh capture instead of replaying the old footprint.");
        for (var index = terminal + 1; index < effects.Length; index++)
            layer = layer.WithMask(effects[index].Mask!, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return layer;
    }

    private static VisualCapturedEffect[] ReplayEffects(VisualArtifactInterchangeEnvelope envelope, CancellationToken cancellation) {
        var records = envelope.Presentation!.CapturedEffects;
        var result = new VisualCapturedEffect[records.Count];
        // Validate() already preflights every borrowed mask and future immutable copy together.
        for (var index = 0; index < result.Length; index++) {
            cancellation.ThrowIfCancellationRequested();
            result[index] = VisualCapturedEffect.From(records[index]);
        }
        return result;
    }

    private static void ValidateReplaySource(VisualArtifactInterchangeEnvelope envelope,
        PreparedVisual source, CancellationToken cancellation) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        if (source == null) throw new ArgumentNullException(nameof(source));
        cancellation.ThrowIfCancellationRequested();
        envelope.Validate();
        if (envelope.Topology?.Hierarchy?.Source.Presentation?.CapturedEffects.Count > 0)
            throw new NotSupportedException("Captured replay admits only the root chain; nested hierarchy source presentation requires a fresh explicit source preparation.");
        if (envelope.Presentation?.CapturedEffects.Count is not > 0) return;
        if (source.Scene.HasRetainedEffects)
            throw new ArgumentException("Replay requires fresh unwrapped source; existing captured effects cannot be applied twice implicitly.", nameof(source));
        if (envelope.Width != source.Size.Width || envelope.Height != source.Size.Height)
            throw new ArgumentException("Prepared source must match the captured source viewport exactly; replay does not resample or reflow.", nameof(source));
    }

    private static void RequireEditableSourcePresentation(VisualArtifactInterchangeEnvelope envelope) {
        if (envelope.Presentation?.CapturedEffects.Count > 0 || envelope.Topology?.Hierarchy?.Source.Presentation?.CapturedEffects.Count > 0)
            throw new NotSupportedException("This envelope carries captured effects. Clear CapturedEffects on a detached source-facts copy before editing; " +
                "prepare through its existing family owner, then use ApplyCapturedEffects or ApplyCapturedLayer on the original presentation.");
    }
}
