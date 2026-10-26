using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

/// <summary>Detached actual face bindings for one SVG emission; no path, registry or text resolution occurs here.</summary>
internal sealed class VisualSvgFontResources {
    private readonly Dictionary<VisualSceneText, Dictionary<int, Face>> _lines = new();
    private readonly List<Face> _faces = new();
    internal VisualSvgFontResources(VisualScene scene, string prefix, ISet<(VisualSceneText Node, int Line)>? rasterLines = null) {
        foreach (var command in scene.Nodes) {
            if (command is not VisualSceneText node || node.Text.NativeGlyphInk || _lines.ContainsKey(node)) continue;
            var prepared = node.Text; var style = prepared.Style; var font = prepared.Face.Font;
            if (font == null || !font.IsSingleCapturedFace || prepared.Face.SynthesizeBold || prepared.Face.SynthesizeItalic
                || font.CapturedByteLength > OwnedFontResources.MaximumRootBytes || style.Font.Variations.Count != 0
                || style.Font.ColorPaletteIndex != 0 || style.OpenTypeLanguageTag != null
                || font.Weight != style.Font.Weight || font.IsItalic != style.Font.Italic) continue;
            for (var lineIndex = 0; lineIndex < prepared.Lines.Count; lineIndex++) {
                var line = prepared.Lines[lineIndex]; if (line.Text.Length == 0 || rasterLines?.Contains((node, lineIndex)) == true) continue;
                var admitted = true;
                foreach (var glyph in line.Glyphs) if (glyph.Glyph == 0 || !ReferenceEquals(glyph.Face.Root, font.Root)
                    || glyph.Face.CollectionIndex != font.CollectionIndex || glyph.Face.Variations.Count != 0
                    || glyph.Face.ColorPaletteIndex != 0) { admitted = false; break; }
                if (!admitted) continue;
                Face? face = null;
                foreach (var existing in _faces) if (existing.Weight == font.Weight && existing.Italic == font.IsItalic
                    && existing.Font.CapturedByteIdentity == font.CapturedByteIdentity && existing.Font.SameCapturedBytes(font)) { face = existing; break; }
                if (face == null) {
                    face = new Face(Alias(prefix, font, font.Weight, font.IsItalic), font, font.Weight, font.IsItalic); _faces.Add(face);
                }
                if (!_lines.TryGetValue(node, out var lines)) _lines.Add(node, lines = new Dictionary<int, Face>());
                lines.Add(lineIndex, face);
            }
        }
    }
    private static string Alias(string prefix, TrueTypeFont font, int weight, bool italic) {
        using var hash = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(prefix + "|" + font.CapturedByteIdentity + "|" + weight.ToString(CultureInfo.InvariantCulture) + "|" + (italic ? "italic" : "normal"));
        var result = new StringBuilder("cfx-owned-");
        foreach (var value in hash.ComputeHash(bytes)) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return result.ToString();
    }
    internal IReadOnlyList<Face> Faces => _faces.AsReadOnly();
    internal Face? ForLine(VisualSceneText node, int line) => _lines.TryGetValue(node, out var lines) && lines.TryGetValue(line, out var face) ? face : null;
    internal sealed class Face {
        internal Face(string alias, TrueTypeFont font, int weight, bool italic) { Alias = alias; Font = font; Weight = weight; Italic = italic; }
        internal string Alias { get; }
        internal TrueTypeFont Font { get; }
        internal int Weight { get; }
        internal bool Italic { get; }
    }
}
