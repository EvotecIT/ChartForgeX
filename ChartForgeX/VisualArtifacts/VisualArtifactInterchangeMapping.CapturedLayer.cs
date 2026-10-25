using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Materializes wire facts only for an explicit captured-layer interchange request.</summary>
    internal static VisualArtifactInterchangeEnvelope CaptureLayerEnvelope(VisualArtifact sourceArtifact, CapturedVisualLayer layer, bool cloneMaskPixels = true) {
        layer.Source.RequireSupportedInterchangePresentation();
        if (!layer.Source.TryGetRootEffects(out var roots))
            throw new NotSupportedException("Unsupported captured root presentation.");
        var operations = new List<VisualCapturedEffect>(roots) { layer.Plan.Operation };
        var terminal = operations.Count - 1;
        foreach (var mask in layer.PostCaptureMasks) operations.Add(VisualCapturedEffect.From(mask));
        var envelope = FromPreparedSourceFacts(sourceArtifact);
        PreparedVisual.CapturePortableEffects(envelope, operations, terminal,
            new VisualArtifactInterchangeCaptureFrame { Bounds = layer.Bounds, RasterOptions = layer.Plan.Options }, cloneMaskPixels);
        return envelope;
    }

    /// <summary>Only the explicit factory owns the source/output distinction; ordinary prepared artifacts keep strict dimensions.</summary>
    private static VisualArtifactInterchangeEnvelope FromCapturedLayerArtifact(VisualArtifact artifact, PreparedVisual prepared, bool cloneMaskPixels = true) {
        var layer = artifact.CapturedLayer!;
        if (!ReferenceEquals(prepared, layer.Visual) || !ReferenceEquals(artifact.Model, layer.Visual))
            throw new InvalidOperationException("A captured artifact's immutable paint model cannot be replaced while retaining terminal source facts.");
        if (artifact.PreparedInterchangeSnapshot == null)
            throw new NotSupportedException("This captured root presentation has no faithful portable source chain; native SVG/PNG remain available.");
        // A capability query must not force the terminal JSON lazy value, which also snapshots raw mask pixels.
        // Rebuild through the same source/frame/post-mask owner in borrowed mode, then apply current host facts below.
        var envelope = cloneMaskPixels
            ? VisualArtifactInterchangeEnvelope.FromJson(artifact.PreparedInterchangeSnapshot.Value)
            : CaptureLayerEnvelope(layer.Source.ToArtifact(artifact.Id), layer, cloneMaskPixels: false);
        if (envelope.Width != layer.Source.Size.Width || envelope.Height != layer.Source.Size.Height)
            throw new InvalidOperationException("Captured semantic dimensions must describe the source viewport.");
        VisualArtifactInterchangeCaptureFrame? frame = null;
        if (envelope.Presentation != null) foreach (var step in envelope.Presentation.CapturedEffects)
            if (step.CaptureFrame != null) frame = step.CaptureFrame;
        if (frame == null || !frame.Bounds.Equals(layer.Bounds) || frame.RasterOptions.Scale != layer.PixelsPerUnit)
            throw new InvalidOperationException("Captured output frame facts do not match the immutable native layer.");
        var common = Common(artifact, out _);
        if (common.Kind != envelope.Kind)
            throw new InvalidOperationException("A captured artifact's typed source family cannot be changed while retaining its source facts.");
        envelope.Id = common.Id; envelope.SourceLanguage = common.SourceLanguage;
        envelope.Title = common.Title; envelope.Subtitle = common.Subtitle;
        envelope.AccessibleName = common.AccessibleName; envelope.AccessibleDescription = common.AccessibleDescription;
        envelope.Language = common.Language; envelope.IsDecorative = common.IsDecorative;
        envelope.Extensions.Clear(); foreach (var pair in common.Extensions) envelope.Extensions.Add(pair.Key, pair.Value);
        // Width/Height deliberately remain SOURCE values. Output Bounds belong only to the selected terminal step.
        envelope.Validate();
        return envelope;
    }
}
