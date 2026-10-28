using System;
using ChartForgeX.Effects;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    /// <summary>Retains one complete-root outer alpha-band policy in this fixed viewport.</summary>
    /// <param name="effect">Immutable outline; zero radius or transparent tint returns this source unchanged.</param>
    /// <returns>A detached retained presentation with the same source facts, regions and projections.</returns>
    /// <remarks>Support outside the native viewport clips. CaptureLayer provides an expanded baked output instead.
    /// NativeFilters retains child vectors once; Baked uses the canonical source-plus-band result once.</remarks>
    public PreparedVisual WithOutline(VisualOutline effect) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        return WithRootEffect(VisualCapturedEffect.From(effect));
    }

    /// <summary>Retains an explicit luminance-ramp bloom in the fixed source viewport.</summary>
    /// <param name="effect">Immutable threshold, strength, sigma and quantization policy.</param>
    /// <returns>A detached retained presentation. Threshold one or strength zero returns this source unchanged.</returns>
    /// <remarks>Source pixels, text and facts are not rewritten. NativeFilters retains vector children; Baked
    /// resolves the same finite native operation. The profiles do not promise browser-identical quantization.</remarks>
    public PreparedVisual WithBloom(VisualBloom effect) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        return WithRootEffect(VisualCapturedEffect.From(effect));
    }

    /// <summary>Retains the complete source with its alpha shadow inside the fixed native viewport.</summary>
    /// <param name="effect">Immutable shadow policy; a zero-alpha tint or opacity returns this source unchanged.</param>
    /// <returns>A detached retained source-plus-shadow presentation.</returns>
    /// <remarks>Zero sigma and offset are not generally identity: nonzero tint paints under semitransparent source.
    /// Native surface-only Sequence shadows retain their existing independent surface/label owner.</remarks>
    public PreparedVisual WithDropShadow(VisualDropShadow effect) {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        return WithRootEffect(VisualCapturedEffect.From(effect));
    }

    internal PreparedVisual WithRootEffect(VisualCapturedEffect effect, bool rebindVisibility = true) {
        if (effect.IsUnconditionalIdentity) return this;
        if (effect.Mask is { } mask) {
            _ = new VisualRenderOptions(scale: mask.PixelsPerUnit);
            foreach (var node in _scene.Nodes) {
                if (node is VisualSceneGroup { RetainedEffect: not null } group &&
                    group.RetainedEffect!.Effect.Mask is { } prior && prior.PixelsPerUnit != mask.PixelsPerUnit)
                    throw new ArgumentException("Ordered retained masks require one native density.", nameof(effect));
            }
        }
        var nodes = new VisualSceneNode[_scene.Nodes.Count + 2];
        nodes[0] = new VisualSceneGroup(null, null, null, null, null, null, null, null,
            null, null, null, 1, null, new VisualSceneRetainedEffect(effect, _scene.Size));
        for (var index = 0; index < _scene.Nodes.Count; index++) nodes[index + 1] = _scene.Nodes[index];
        nodes[nodes.Length - 1] = new VisualSceneEndGroup();
        var scene = new VisualScene(_scene.Size, nodes, _scene.Diagnostics, _scene.Regions, _scene.DataProjection,
            _scene.ValueCapture, rebindVisibility ? null : _scene.VisibilityCapture);
        var result = new PreparedVisual(scene, this, _defaultSvgOptions);
        return rebindVisibility && _scene.VisibilityCapture != null
            ? _scene.VisibilityCapture.WithAppearance(this, result, effect) : result;
    }
}
