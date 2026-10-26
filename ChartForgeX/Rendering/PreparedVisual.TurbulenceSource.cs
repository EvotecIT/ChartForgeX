using ChartForgeX.VisualArtifacts;
using ChartForgeX.Effects;

namespace ChartForgeX.Rendering;

public sealed partial class PreparedVisual {
    private VisualScene? _turbulenceSourceScene;

    // Called only by the native source producer before the completed preparation escapes.
    internal PreparedVisual CaptureTurbulenceSource() {
        _turbulenceSourceScene = _scene;
        return this;
    }

    /// <summary>Only a producer-captured pure source can advertise its typed source policy; caller-supplied semantics cannot invent it.</summary>
    internal bool HasCapturedTurbulenceSource => CapturedTurbulenceSource != null;

    internal VisualTurbulence? CapturedTurbulenceSource {
        get {
            // Uses root's existing detached snapshot seam, not a serialize/readback or a second retained policy bag.
            var original = _turbulenceSourceScene;
            if (original == null || !TryGetRootEffects(out var roots) ||
                _scene.Size.Width != original.Size.Width || _scene.Size.Height != original.Size.Height ||
                _scene.Nodes.Count != original.Nodes.Count + roots.Count * 2) return null;
            for (var index = 0; index < original.Nodes.Count; index++)
                if (!object.ReferenceEquals(original.Nodes[index], _scene.Nodes[index + roots.Count])) return null;
            var envelope = CapturedSemanticSnapshot;
            var policy = envelope?.Presentation?.TurbulenceSource;
            if (envelope == null || policy == null || envelope.Kind != VisualArtifactKind.Unknown || envelope.Family != VisualArtifactInterchangeFamily.None ||
                _scene.ValueCapture != null || _scene.VisibilityCapture != null || _scene.DataProjection != null) return null;
            var count = 0;
            foreach (var node in _scene.Nodes) if (node is VisualSceneProceduralSource source) {
                if (++count > 1 || !policy.SameSourcePolicy(source.Effect)) return null;
            }
            // A legitimate empty frame keeps the same source policy even when headings leave no source surface.
            return policy;
        }
    }
}
