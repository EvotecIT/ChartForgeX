using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    [ThreadStatic]
    private static Dictionary<TrueTypeFont, TrueTypeFont>? _emphasisFaces;

    [ThreadStatic]
    private static Dictionary<string, ThemeFace>? _themeFaces;

    /// <summary>
    /// Opens a scope for one render in which emphasized text drawn or measured with a paired regular
    /// face uses its real bold face instead of drawing the regular face twice. Faces are paired with
    /// <see cref="PairEmphasisFace"/> as the renderer resolves its font stacks; an unpaired face,
    /// such as an explicit font file, keeps the synthesized emphasis.
    /// </summary>
    internal static EmphasisScope OpenEmphasisScope() {
        var previous = _emphasisFaces;
        var previousThemes = _themeFaces;
        _emphasisFaces = new Dictionary<TrueTypeFont, TrueTypeFont>();
        _themeFaces = new Dictionary<string, ThemeFace>(StringComparer.OrdinalIgnoreCase);
        return new EmphasisScope(previous, previousThemes);
    }

    /// <summary>Isolates aliases that share a font file but register different emphasis faces.</summary>
    internal static TrueTypeFont? ThemeOutlineFace(string? family, TrueTypeFont? source) {
        if (source == null || _themeFaces == null) return source;
        var key = (family ?? "sans-serif").Trim();
        if (!_themeFaces.TryGetValue(key, out var face) || !ReferenceEquals(face.Source, source)) {
            face = new ThemeFace(source, source.WithRenderingIdentity());
            _themeFaces[key] = face;
        }
        return face.Outline;
    }

    /// <summary>Pairs a regular face with its bold face in the open scope; outside a scope this does nothing.</summary>
    internal static void PairEmphasisFace(TrueTypeFont? regular, TrueTypeFont? bold) {
        var faces = _emphasisFaces;
        if (faces == null || regular == null) return;
        if (bold == null || ReferenceEquals(regular, bold)) {
            faces.Remove(regular);
            return;
        }
        faces[regular] = bold;
    }

    private static TrueTypeFont? EmphasisFace(TrueTypeFont? font) {
        var faces = _emphasisFaces;
        return font != null && faces != null && faces.TryGetValue(font, out var bold) ? bold : null;
    }

    internal readonly struct EmphasisScope : IDisposable {
        private readonly Dictionary<TrueTypeFont, TrueTypeFont>? _previous;
        private readonly Dictionary<string, ThemeFace>? _previousThemes;

        internal EmphasisScope(Dictionary<TrueTypeFont, TrueTypeFont>? previous, Dictionary<string, ThemeFace>? previousThemes) {
            _previous = previous;
            _previousThemes = previousThemes;
        }

        public void Dispose() {
            _emphasisFaces = _previous;
            _themeFaces = _previousThemes;
        }
    }

    internal readonly struct ThemeFace {
        public ThemeFace(TrueTypeFont source, TrueTypeFont outline) { Source = source; Outline = outline; }
        public TrueTypeFont Source { get; }
        public TrueTypeFont Outline { get; }
    }
}
