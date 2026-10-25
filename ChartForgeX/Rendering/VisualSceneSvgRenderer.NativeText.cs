using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    /// <summary>One SVG owns one captured glyph resource set with explicit visible-scene or capture-only lifetime.</summary>
    private sealed class PngTextResources {
        private readonly string _prefix;
        private readonly Dictionary<(TrueTypeFont Face, ushort Glyph, double Scale, bool Italic, double BoldOffset), int> _ids = new();
        private readonly List<(string Id, string Path, bool SceneInk)> _paths = new();
        private readonly Dictionary<(char Character, double Size, bool Italic, double BoldOffset), int> _tinyIds = new();
        internal PngTextResources(string prefix) { _prefix = prefix; }

        internal bool TryCapture(VisualSceneText node, VisualSceneTextLine line, int lineIndex, out string references) {
            references = string.Empty;
            if (!TryCapture(node, line, lineIndex, false, out var captured)) return false;
            var result = new StringBuilder();
            foreach (var glyph in captured) {
                if (result.Length > 0) result.Append('|');
                result.Append(glyph.Id).Append(";matrix(1 0 0 1 ").Append(R(glyph.X)).Append(' ').Append(R(glyph.Baseline)).Append(')');
            }
            references = result.ToString(); return true;
        }
        internal bool TryCapture(VisualSceneText node, VisualSceneTextLine line, int lineIndex, bool sceneInk,
            out IReadOnlyList<NativeGlyphReference> references) {
            references = System.Array.Empty<NativeGlyphReference>();
            var prepared = node.Text; var face = prepared.Face;
            if (face.Font == null) return sceneInk && TryCaptureTiny(node, line, lineIndex, out references);
            var boldOffset = face.SynthesizeBold ? RgbaCanvas.EmphasisOffset(prepared.Size) : 0;
            if (!face.Font.TryGetOutlineRun(line.Glyphs, node.LineLeft(line),
                node.Baseline + lineIndex * prepared.Metrics.LineHeight, prepared.Size,
                face.SynthesizeItalic, boldOffset, out var placements, allowSyntheticBold: sceneInk)) return false;
            var result = new List<NativeGlyphReference>();
            foreach (var glyph in placements) {
                var key = (glyph.Face, glyph.Glyph, glyph.Scale, glyph.Italic, glyph.BoldOffset);
                if (!_ids.TryGetValue(key, out var index)) {
                    index = _paths.Count; var id = _prefix + "-png-glyph-" + index.ToString(CultureInfo.InvariantCulture);
                    var path = new SvgPathDataBuilder();
                    foreach (var contour in glyph.Face.OutlineContours(glyph.Glyph, glyph.Scale, glyph.Italic)) {
                        if (contour.Count == 0) continue;
                        path.MoveTo(contour[0]);
                        for (var i = 1; i < contour.Count; i++) path.LineTo(contour[i]);
                        path.Close();
                        // Native synthetic weight fills both placements as one non-zero union.
                        if (glyph.BoldOffset != 0) {
                            path.MoveTo(new ChartForgeX.Primitives.ChartPoint(contour[0].X + glyph.BoldOffset, contour[0].Y));
                            for (var i = 1; i < contour.Count; i++) path.LineTo(new ChartForgeX.Primitives.ChartPoint(contour[i].X + glyph.BoldOffset, contour[i].Y));
                            path.Close();
                        }
                    }
                    _ids.Add(key, index); _paths.Add((id, path.Build(), sceneInk));
                } else if (sceneInk && !_paths[index].SceneInk) {
                    var old = _paths[index]; _paths[index] = (old.Id, old.Path, true);
                }
                result.Add(new NativeGlyphReference(_paths[index].Id, glyph.X, glyph.Baseline));
            }
            references = result; return true;
        }
        private bool TryCaptureTiny(VisualSceneText node, VisualSceneTextLine line, int lineIndex, out IReadOnlyList<NativeGlyphReference> references) {
            var prepared = node.Text; var face = prepared.Face; var result = new List<NativeGlyphReference>();
            var cursor = node.LineLeft(line); var boldOffset = face.SynthesizeBold ? RgbaCanvas.EmphasisOffset(prepared.Size) : 0;
            foreach (var character in line.Text) {
                var key = (character, prepared.Size, face.SynthesizeItalic, boldOffset);
                if (!_tinyIds.TryGetValue(key, out var index)) {
                    index = _paths.Count; var path = new SvgPathDataBuilder();
                    foreach (var contour in TinyFont.Ink(character, prepared.Size, face.SynthesizeItalic, boldOffset)) {
                        if (contour.Count == 0) continue;
                        path.MoveTo(contour[0]); for (var point = 1; point < contour.Count; point++) path.LineTo(contour[point]); path.Close();
                    }
                    _paths.Add((_prefix + "-png-glyph-" + index.ToString(CultureInfo.InvariantCulture), path.Build(), true));
                    _tinyIds.Add(key, index);
                }
                result.Add(new NativeGlyphReference(_paths[index].Id, cursor,
                    node.Baseline + lineIndex * prepared.Metrics.LineHeight - prepared.Ascent));
                cursor += TinyFont.AdvanceFor(character) * TinyFont.ScaleForFontSize(prepared.Size);
            }
            references = result; return true;
        }
        internal void Write(SvgMarkupWriter writer) {
            var capture = false; var visible = false;
            foreach (var path in _paths) { capture |= !path.SceneInk; visible |= path.SceneInk; }
            if (capture) WriteGroup(writer, false);
            if (visible) WriteGroup(writer, true);
        }
        private void WriteGroup(SvgMarkupWriter writer, bool sceneInk) {
            writer.StartElement("defs").Attribute(sceneInk ? "data-cfx-native-text-resources" : "data-cfx-png-text-resources", "true").EndStartElement();
            foreach (var path in _paths) if (path.SceneInk == sceneInk)
                writer.StartElement("path").Attribute("id", path.Id).Attribute("d", path.Path).EndEmptyElement();
            writer.EndElement();
        }
        private static string R(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
    private readonly struct NativeGlyphReference {
        internal NativeGlyphReference(string id, double x, double baseline) { Id = id; X = x; Baseline = baseline; }
        internal string Id { get; }
        internal double X { get; }
        internal double Baseline { get; }
    }
}
