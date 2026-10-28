using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void CapturedFontRoots(VisualArtifactInterchangeJsonWriter writer, OwnedFontResources roots) {
        if (roots.Roots.Count == 0) return;
        writer.Property("fontRoots"); writer.StartArray();
        foreach (var font in roots.Roots) {
            writer.StartObject(); String(writer, "id", roots.Reference(font)); writer.Property("data");
            writer.Base64(font.CopyCapturedBytes(OwnedFontResources.MaximumRootBytes)); writer.EndObject();
        }
        writer.EndArray();
    }
    private static void CapturedFontView(VisualArtifactInterchangeJsonWriter writer, TrueTypeFont font, OwnedFontResources roots) {
        if (font.PaletteContext != null) throw new NotSupportedException("Captured portable font views require the existing finite palette-index contract.");
        writer.StartObject(); String(writer, "root", roots.Reference(font)); OptionalNumber(writer, "collection", font.CollectionIndex);
        OptionalString(writer, "faceName", font.DisplayName); OptionalString(writer, "selectedFamily", font.SelectedFamily);
        OptionalString(writer, "language", font.LanguageTag);
        FontValue(writer, new FontSpec { Family = font.SelectedFamily ?? "sans-serif", Weight = font.FallbackWeight,
            Italic = font.FallbackItalic, ColorPaletteIndex = font.ColorPaletteIndex, Variations = font.Variations }, "view", roots);
        writer.Property("fallbackFamilies"); writer.StartArray(); foreach (var family in font.FallbackFamilies) writer.String(family); writer.EndArray();
        writer.EndObject();
    }
    private static TrueTypeFont ReadCapturedFontView(Dictionary<string, GeoJsonValue> item, OwnedFontRootReader roots) {
        var view = OptionalObject(item, "view") ?? throw new ArgumentException("An owned face requires its finite view policy.");
        if (view.ContainsKey("ownedFile") || view.ContainsKey("explicitFile")) throw new ArgumentException("An owned view cannot introduce a nested file binding.");
        var policy = ReadFontValue(item, "view", roots);
        if (policy.CollectionIndex.HasValue || policy.FaceName != null) throw new ArgumentException("Collection selection belongs to the owned face descriptor.");
        var font = roots.Face(RequiredString(item, "root"), OptionalInt(item, "collection"), OptionalString(item, "faceName"));
        var families = new List<string>(); foreach (var entry in OptionalArray(item, "fallbackFamilies")) families.Add(entry.AsString("captured fallback family"));
        var language = new TextStyle { OpenTypeLanguageTag = OptionalString(item, "language") }.OpenTypeLanguageTag;
        font = font.WithFallbackFamilies(families, policy.Weight, policy.Italic).WithLanguage(language == "normal" ? null : language)
            .WithVariations(policy.Variations).WithColorPalette(policy.ColorPaletteIndex);
        if (OptionalString(item, "selectedFamily") is string family) font = font.WithSelectedFamily(family);
        return font;
    }

    // The existing parser owns font interpretation. This private table only preflights/copies admitted binary strings and reuses parsed collection faces.
    private sealed class OwnedFontRootReader {
        private readonly Dictionary<string, Root> _roots = new(StringComparer.Ordinal);
        internal OwnedFontRootReader(Dictionary<string, GeoJsonValue> owner) {
            var values = OptionalArray(owner, "fontRoots");
            if (values.Count > OwnedFontResources.MaximumRoots) throw new ArgumentException("Owned fonts exceed the 32-root portable limit.");
            long total = 0;
            foreach (var value in values) {
                var item = value.AsObject("owned font root"); var id = RequiredString(item, "id");
                if (!HashToken(id) || _roots.ContainsKey(id)) throw new ArgumentException("Owned font root identities must be unique canonical byte hashes.");
                var data = RequiredString(item, "data"); var length = DecodedLength(data);
                if (length > OwnedFontResources.MaximumRootBytes) throw new ArgumentException("Owned font data exceeds the 2 MiB per-root limit.");
                total += length;
                if (total > OwnedFontResources.MaximumDistinctBytes) throw new ArgumentException("Owned font data exceeds the 4 MiB aggregate limit.");
                // The eight-character probe is bounded independently of the admitted payload and rejects unsupported codecs before its decoded allocation.
                var prefix = Convert.FromBase64String(data.Substring(0, 8));
                var signature = ((uint)prefix[0] << 24) | ((uint)prefix[1] << 16) | ((uint)prefix[2] << 8) | prefix[3];
                if (signature != 0x00010000 && signature != 0x74727565 && signature != 0x4F54544F && signature != 0x74746366)
                    throw new ArgumentException("Owned font data requires an existing sfnt/TrueType/CFF/collection signature.");
                _roots.Add(id, new Root(id, Convert.FromBase64String(data)));
            }
        }
        internal TrueTypeFont Face(string id, int? collection, string? faceName) {
            if (!_roots.TryGetValue(id, out var root)) throw new ArgumentException("A captured font view references an absent owned root.");
            return root.Face(collection, faceName);
        }
        internal void RequireAllReferenced() {
            foreach (var root in _roots.Values) if (!root.Referenced) throw new ArgumentException("Owned font roots must be referenced by captured requests or the context file binding.");
        }
        private static bool HashToken(string value) {
            if (value.Length != 64) return false;
            foreach (var c in value) if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')) return false;
            return true;
        }
        private static int DecodedLength(string value) {
            var maximumEncoded = ((long)OwnedFontResources.MaximumRootBytes + 2) / 3 * 4;
            if (value.Length < 16 || value.Length > maximumEncoded || value.Length % 4 != 0)
                throw new ArgumentException("Owned font data requires bounded canonical Base64.");
            var padding = value[value.Length - 1] == '=' ? value[value.Length - 2] == '=' ? 2 : 1 : 0;
            for (var index = 0; index < value.Length - padding; index++) {
                var c = value[index];
                if (!(c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '+' || c == '/'))
                    throw new ArgumentException("Owned font data requires canonical Base64 without whitespace.");
            }
            if (padding == 2 && (SixBits(value[value.Length - 3]) & 15) != 0 || padding == 1 && (SixBits(value[value.Length - 2]) & 3) != 0)
                throw new ArgumentException("Owned font Base64 has noncanonical padding bits.");
            return checked(value.Length / 4 * 3 - padding);
            static int SixBits(char c) => c >= 'A' && c <= 'Z' ? c - 'A' : c >= 'a' && c <= 'z' ? c - 'a' + 26
                : c >= '0' && c <= '9' ? c - '0' + 52 : c == '+' ? 62 : 63;
        }
        private sealed class Root {
            private readonly string _id;
            private readonly byte[] _bytes;
            private readonly Dictionary<int, TrueTypeFont> _faces = new();
            internal Root(string id, byte[] bytes) { _id = id; _bytes = bytes; }
            internal bool Referenced { get; private set; }
            internal TrueTypeFont Face(int? collection, string? faceName) {
                if (!_faces.TryGetValue(collection ?? -1, out var face)) {
                    face = TrueTypeFont.TryLoad(_bytes, collection, faceName) ?? throw new ArgumentException("Owned font bytes or collection descriptor are unsupported.");
                    if (face.CapturedByteIdentity != _id || face.CollectionIndex != collection)
                        throw new ArgumentException("Owned font bytes or collection identity contradict their captured descriptor.");
                    _faces.Add(collection ?? -1, face);
                }
                // Names are validation facts, not family-alias resolution. Reuse the same owned bytes/parser when a descriptor supplies a name.
                if (faceName != null && !string.Equals(face.DisplayName, faceName, StringComparison.Ordinal))
                    throw new ArgumentException("Owned font face name contradicts its captured collection member.");
                Referenced = true; return face;
            }
        }
    }
}
