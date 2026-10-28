using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    /// <summary>Native frame painters keep their original bare-scene indices; the same root operation chain is applied once afterward.</summary>
    internal bool TryRemoveRootEffects(out PreparedVisual bare, out IReadOnlyList<VisualCapturedEffect> effects) {
        bare = this; effects = Array.Empty<VisualCapturedEffect>();
        if (!_scene.HasRetainedEffects) return false;
        if (!TryGetRootEffects(out effects))
            throw new NotSupportedException("Native frame capture requires represented complete-root appearance scopes.");
        var count = effects.Count;
        if (count == 0) return false;
        var nodes = new VisualSceneNode[_scene.Nodes.Count - count * 2];
        for (var index = 0; index < nodes.Length; index++) nodes[index] = _scene.Nodes[index + count];
        var scene = new VisualScene(_scene.Size, nodes, _scene.Diagnostics, _scene.Regions,
            _scene.DataProjection, _scene.ValueCapture, _scene.VisibilityCapture);
        bare = new PreparedVisual(scene, this, _defaultSvgOptions);
        return true;
    }

    internal PreparedVisual WithRootEffects(IReadOnlyList<VisualCapturedEffect> effects, bool rebindVisibility = false) {
        var result = this;
        foreach (var effect in effects) result = result.WithRootEffect(effect, rebindVisibility);
        return result;
    }

    /// <summary>Uses the same raw ordered native identity owner, including original mask bytes and authored scalar effects.</summary>
    internal string CapturedNativeIdentity => VisualSceneSvgRenderer.Identity(_scene, _accessibility.Name,
        _accessibility.Description, _accessibility.Language, _accessibility.IsDecorative, _defaultSvgOptions);
    /// <summary>Raw ordered appearance policy without value-changing source ink; uses the same native identity writer.</summary>
    internal string? CapturedRootEffectIdentity {
        get {
            if (!_scene.HasRetainedEffects) return null;
            if (!TryGetRootEffects(out var effects))
                throw new NotSupportedException("Native frame appearance must be represented by the bounded root chain.");
            var nodes = new VisualSceneNode[effects.Count * 2];
            for (var index = 0; index < effects.Count; index++) nodes[index] = _scene.Nodes[index];
            for (var index = effects.Count; index < nodes.Length; index++) nodes[index] = new VisualSceneEndGroup();
            var policy = new VisualScene(_scene.Size, nodes, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
            return VisualSceneSvgRenderer.Identity(policy, null, null, null, false, null);
        }
    }

}
