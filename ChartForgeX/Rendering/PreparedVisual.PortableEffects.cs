using System;
using System.Collections.Generic;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    /// <summary>Recognizes finite complete-root wrappers; family source geometry is not inferred from paint commands.</summary>
    internal bool TryGetRootEffects(out IReadOnlyList<VisualCapturedEffect> effects) {
        effects = Array.Empty<VisualCapturedEffect>();
        if (!_scene.HasRetainedEffects) return true;
        var ends = VisualSceneScopes.Ends(_scene);
        var captured = new List<VisualCapturedEffect>();
        var first = 0; var last = _scene.Nodes.Count - 1;
        while (first <= last && _scene.Nodes[first] is VisualSceneGroup { RetainedEffect: not null } group) {
            var scope = group.RetainedEffect!;
            if (ends[first] != last || scope.SourceSize.Width != _scene.Size.Width || scope.SourceSize.Height != _scene.Size.Height ||
                group.Opacity != 1 || group.Clip != null || group.PathClip != null || group.Rotation != null ||
                group.Translation != null || group.ImageResource != null || group.ColorScope != null || group.SurfaceShadow != null ||
                group.Href != null || group.Tooltip != null || group.Role != null || group.Id != null || group.Metadata.Count != 0)
                return false;
            captured.Add(scope.Effect);
            if (captured.Count > VisualSceneRasterRenderer.MaximumIsolatedDepth) return false;
            first++; last--;
        }
        for (var index = first; index <= last; index++)
            if (_scene.Nodes[index] is VisualSceneGroup { RetainedEffect: not null }) return false;
        if (captured.Count == 0) return false;
        captured.Reverse(); effects = captured.AsReadOnly(); return true;
    }

    /// <summary>Explicit wire export checks the complete borrowed envelope before copying any mask input.</summary>
    internal void CapturePortableEffects(VisualArtifactInterchangeEnvelope envelope, bool cloneMaskPixels = true) {
        RequireSupportedInterchangePresentation();
        if (!TryGetRootEffects(out var effects)) throw new NotSupportedException("Unsupported captured-effect scope.");
        CapturePortableEffects(envelope, effects, cloneMaskPixels: cloneMaskPixels);
    }

    internal static void CapturePortableEffects(VisualArtifactInterchangeEnvelope envelope,
        IReadOnlyList<VisualCapturedEffect> effects,
        int terminalIndex = -1, VisualArtifactInterchangeCaptureFrame? terminalFrame = null, bool cloneMaskPixels = true) {
        if (envelope.Presentation?.CapturedEffects.Count > 0)
            throw new NotSupportedException("Supplied source semantics already carry presentation. Export current prepared effects once.");
        if ((terminalIndex >= 0) != (terminalFrame != null) || terminalIndex >= effects.Count)
            throw new ArgumentException("A terminal frame must identify exactly one captured operation.");
        if (effects.Count == 0) return;
        envelope.Presentation ??= new VisualArtifactInterchangePresentation();
        foreach (var effect in effects) envelope.Presentation.CapturedEffects.Add(effect.ToPortableBorrowed());
        if (terminalFrame != null) envelope.Presentation.CapturedEffects[terminalIndex].CaptureFrame = terminalFrame;
        // Existing validation checks aggregate borrowed/snapshot pixel bytes, depth and the
        // COMPLETE encoded envelope. The counting Base64 writer does not encode or copy pixels.
        envelope.Validate();
        if (!cloneMaskPixels) return; // Capability qualification borrows immutable native input only; the envelope never escapes.
        foreach (var record in envelope.Presentation.CapturedEffects) {
            if (record.Mask is { } mask) mask.Pixels = (byte[])mask.Pixels.Clone();
        }
    }
}
