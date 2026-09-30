using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    [ThreadStatic]
    private static Dictionary<TrueTypeFont, TrueTypeFont>? _emphasisFaces;

    /// <summary>
    /// Opens a scope for one render in which emphasized text drawn or measured with a paired regular
    /// face uses its real bold face instead of drawing the regular face twice. Faces are paired with
    /// <see cref="PairEmphasisFace"/> as the renderer resolves its font stacks; an unpaired face,
    /// such as an explicit font file, keeps the synthesized emphasis.
    /// </summary>
    internal static EmphasisScope OpenEmphasisScope() {
        var previous = _emphasisFaces;
        _emphasisFaces = new Dictionary<TrueTypeFont, TrueTypeFont>();
        return new EmphasisScope(previous);
    }

    /// <summary>Pairs a regular face with its bold face in the open scope; outside a scope this does nothing.</summary>
    internal static void PairEmphasisFace(TrueTypeFont? regular, TrueTypeFont? bold) {
        var faces = _emphasisFaces;
        if (faces == null || regular == null || bold == null || ReferenceEquals(regular, bold)) return;
        faces[regular] = bold;
    }

    private static TrueTypeFont? EmphasisFace(TrueTypeFont? font) {
        var faces = _emphasisFaces;
        return font != null && faces != null && faces.TryGetValue(font, out var bold) ? bold : null;
    }

    internal readonly struct EmphasisScope : IDisposable {
        private readonly Dictionary<TrueTypeFont, TrueTypeFont>? _previous;

        public EmphasisScope(Dictionary<TrueTypeFont, TrueTypeFont>? previous) {
            _previous = previous;
        }

        public void Dispose() => _emphasisFaces = _previous;
    }
}
