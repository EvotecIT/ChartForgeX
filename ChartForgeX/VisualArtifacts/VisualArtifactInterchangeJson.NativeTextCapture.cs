using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    /// <summary>Actual resolver inputs, including language. Only the private lookup key contains a file path; canonical facts bind owned font bytes instead.</summary>
    internal static string NativeTextRequestKey(TextStyle style, bool includeFilePath) {
        var writer = new VisualArtifactInterchangeJsonWriter(); writer.StartObject(); var font = style.Font;
        String(writer, "family", font.Family);
        if (includeFilePath) {
            if (font.CapturedFileKey is {} owned) String(writer, "ownedFile", owned);
            else OptionalString(writer, "file", font.FilePath);
        } else Boolean(writer, "explicitFile", font.FilePath != null || font.CapturedExplicitFile);
        OptionalNumber(writer, "collection", font.CollectionIndex); OptionalString(writer, "face", font.FaceName); Number(writer, "weight", font.Weight);
        Boolean(writer, "italic", font.Italic); Number(writer, "palette", font.ColorPaletteIndex); String(writer, "variations", font.Variations.Css);
        OptionalString(writer, "language", style.OpenTypeLanguageTag); writer.EndObject(); return writer.ToString();
    }
    internal static string SerializeNativeTextCapture(VisualSceneTextCapture capture, bool includeRuns) {
        var writer = new VisualArtifactInterchangeJsonWriter(); writer.StartObject(); writer.Property("requests"); writer.StartArray();
        foreach (var request in capture.Requests) {
            writer.StartObject(); String(writer, "request", request.PublicKey); NativeCapturedFace(writer, request.Face);
            if (request.Clusters.Decisions.Count > 0) {
                writer.Property("fallbackClusters"); writer.StartArray();
                foreach (var decision in request.Clusters.Decisions) {
                    writer.StartObject(); writer.Property("raw"); writer.StartArray(); foreach (var point in decision.Raw) writer.Number(point); writer.EndArray();
                    NativeCapturedFont(writer, decision.Face); writer.Property("output"); writer.StartArray(); foreach (var point in decision.Output) writer.Number(point); writer.EndArray(); writer.EndObject();
                }
                writer.EndArray();
            }
            if (request.Probes.Count > 0) {
                writer.Property("valueTextProbes"); writer.StartArray();
                foreach (var probe in request.Probes) {
                    writer.StartObject(); String(writer, "owner", probe.Owner); String(writer, "raw", probe.Raw);
                    String(writer, "case", probe.Case.ToString()); String(writer, "displayed", probe.Displayed); OptionalString(writer, "caseCulture", probe.CaseCulture); writer.EndObject();
                }
                writer.EndArray();
            }
            writer.EndObject();
        }
        writer.EndArray();
        if (includeRuns) {
            writer.Property("runs"); writer.StartArray();
            foreach (var run in capture.Runs) {
                writer.StartObject(); Number(writer, "size", run.Size); Number(writer, "ascent", run.Ascent); NativeCapturedFace(writer, run.Face);
                writer.Property("lines"); writer.StartArray();
                foreach (var line in run.Lines) { writer.StartObject(); String(writer, "text", line.Text); Number(writer, "width", line.Width); writer.Property("glyphs"); writer.StartArray();
                    foreach (var glyph in line.Glyphs) { writer.StartObject(); NativeCapturedFont(writer, glyph.Face); Number(writer, "glyph", glyph.Glyph); Number(writer, "cluster", glyph.SourceIndex);
                        OptionalNumber(writer, "advance", glyph.Advance); Number(writer, "x", glyph.OffsetX); Number(writer, "y", glyph.OffsetY); writer.EndObject(); }
                    writer.EndArray(); writer.EndObject(); }
                writer.EndArray(); writer.EndObject();
            }
            writer.EndArray();
        }
        writer.EndObject(); return writer.ToString();
    }
    private static void NativeCapturedFace(VisualArtifactInterchangeJsonWriter writer, ResolvedTypeface face) {
        Boolean(writer, "bold", face.SynthesizeBold); Boolean(writer, "italic", face.SynthesizeItalic);
        if (face.Font == null) String(writer, "font", "native-tiny-font"); else NativeCapturedFont(writer, face.Font);
    }
    private static void NativeCapturedFont(VisualArtifactInterchangeJsonWriter writer, TrueTypeFont font) {
        String(writer, "fontBytes", font.CapturedByteIdentity); OptionalNumber(writer, "fontCollection", font.CollectionIndex);
        OptionalString(writer, "fontFamily", font.SelectedFamily); OptionalString(writer, "fontLanguage", font.LanguageTag);
        Number(writer, "fontWeight", font.FallbackWeight); Boolean(writer, "fontItalic", font.FallbackItalic);
        String(writer, "fontVariations", font.Variations.Css); Number(writer, "fontPalette", font.ColorPaletteIndex);
        writer.Property("fallbackFamilies"); writer.StartArray(); foreach (var family in font.FallbackFamilies) writer.String(family); writer.EndArray();
    }
}
