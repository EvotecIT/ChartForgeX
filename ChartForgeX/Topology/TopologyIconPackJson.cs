using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ChartForgeX.Topology;

/// <summary>
/// Reads and writes dependency-free JSON manifests for topology icon packs.
/// </summary>
public static partial class TopologyIconPackJson {
    /// <summary>Gets the manifest schema identifier.</summary>
    public const string Schema = "chartforgex.topology.iconPack";

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Adds a JSON manifest icon pack to the catalog.
    /// </summary>
    /// <param name="catalog">The target catalog.</param>
    /// <param name="json">The JSON manifest.</param>
    /// <returns>The current catalog.</returns>
    public static TopologyIconCatalog AddJsonPack(this TopologyIconCatalog catalog, string json) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        return catalog.AddPack(FromJson(json));
    }

    /// <summary>
    /// Adds a JSON manifest icon pack read from a text reader to the catalog.
    /// </summary>
    /// <param name="catalog">The target catalog.</param>
    /// <param name="reader">The source text reader.</param>
    /// <returns>The current catalog.</returns>
    public static TopologyIconCatalog AddJsonPack(this TopologyIconCatalog catalog, TextReader reader) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        return catalog.AddPack(LoadJsonManifest(reader));
    }

    /// <summary>
    /// Adds a JSON manifest icon pack read from a file to the catalog.
    /// </summary>
    /// <param name="catalog">The target catalog.</param>
    /// <param name="path">The manifest file path.</param>
    /// <returns>The current catalog.</returns>
    public static TopologyIconCatalog AddJsonPackFile(this TopologyIconCatalog catalog, string path) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        return catalog.AddPack(LoadJsonManifest(path));
    }

    /// <summary>
    /// Adds all JSON icon-pack manifests from a directory to the catalog in deterministic file-name order.
    /// </summary>
    /// <param name="catalog">The target catalog.</param>
    /// <param name="directoryPath">The source directory path.</param>
    /// <param name="searchPattern">The file search pattern.</param>
    /// <param name="recursive">Whether child directories should be searched.</param>
    /// <returns>The current catalog.</returns>
    public static TopologyIconCatalog AddJsonPacksFromDirectory(this TopologyIconCatalog catalog, string directoryPath, string searchPattern = "*.json", bool recursive = false) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        foreach (var pack in LoadJsonManifestsFromDirectory(directoryPath, searchPattern, recursive)) catalog.AddPack(pack);
        return catalog;
    }

    /// <summary>
    /// Creates a topology icon pack from a JSON manifest.
    /// </summary>
    /// <param name="json">The JSON manifest.</param>
    /// <returns>The imported topology icon pack.</returns>
    public static TopologyIconPack FromJson(string json) {
        if (json == null) throw new ArgumentNullException(nameof(json));
        return FromJson(json, null);
    }

    /// <summary>
    /// Creates a topology icon pack from a JSON manifest and optionally resolves pack-local sidecar artwork.
    /// </summary>
    /// <param name="json">The JSON manifest.</param>
    /// <param name="manifestDirectory">The manifest directory used to resolve sidecar artwork paths.</param>
    /// <returns>The imported topology icon pack.</returns>
    public static TopologyIconPack FromJson(string json, string? manifestDirectory) {
        if (json == null) throw new ArgumentNullException(nameof(json));
        var root = JsonReader.Parse(json).AsObject("manifest");
        var schema = root.OptionalString("schema");
        if (!string.IsNullOrEmpty(schema) && !string.Equals(schema, Schema, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Unsupported topology icon pack schema '" + schema + "'.", nameof(json));
        }

        var id = root.RequiredString("id");
        var label = root.RequiredString("label");
        var pack = new TopologyIconPack(id, label, root.OptionalString("vendor"), root.OptionalString("packVersion") ?? root.OptionalString("version"), root.OptionalBool("builtIn") ?? root.OptionalBool("isBuiltIn") ?? false);
        foreach (var tag in root.OptionalStringArray("tags")) pack.WithTags(tag);
        foreach (var pair in root.OptionalStringMap("metadata")) pack.WithMetadata(pair.Key, pair.Value);

        foreach (var iconNode in root.RequiredArray("icons")) {
            var iconObject = iconNode.AsObject("icons[]");
            var icon = new TopologyIconDefinition(
                pack.Id,
                iconObject.RequiredString("id"),
                iconObject.RequiredString("label"),
                ParseNodeKind(iconObject.RequiredString("nodeKind")),
                ParseIconShape(iconObject.OptionalString("shape") ?? "Auto")) {
                Symbol = iconObject.OptionalString("symbol"),
                Color = iconObject.OptionalString("color"),
                Category = iconObject.OptionalString("category"),
                DisplayMode = ParseDisplayMode(iconObject.OptionalString("displayMode")),
                Artwork = ParseArtwork(iconObject.OptionalObject("artwork"))
            };

            foreach (var tag in iconObject.OptionalStringArray("tags")) icon.WithTags(tag);
            foreach (var pair in iconObject.OptionalStringMap("metadata")) icon.WithMetadata(pair.Key, pair.Value);
            pack.AddIcon(icon);
        }

        if (!string.IsNullOrWhiteSpace(manifestDirectory)) ResolvePackArtworkFiles(pack, manifestDirectory!);
        return pack;
    }

    /// <summary>
    /// Loads a topology icon pack from a JSON manifest file.
    /// </summary>
    /// <param name="path">The manifest file path.</param>
    /// <returns>The imported topology icon pack.</returns>
    public static TopologyIconPack LoadJsonManifest(string path) {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Value cannot be empty.", nameof(path));
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        var json = File.ReadAllText(path);
        var pack = FromJson(json, directory);
        pack.WithMetadata("manifest.path", fullPath);
        pack.WithMetadata("manifest.fileName", Path.GetFileName(path));
        if (!string.IsNullOrWhiteSpace(directory)) pack.WithMetadata("manifest.directory", directory!);
        return pack;
    }

    /// <summary>
    /// Loads a topology icon pack from a JSON manifest text reader.
    /// </summary>
    /// <param name="reader">The source text reader.</param>
    /// <returns>The imported topology icon pack.</returns>
    public static TopologyIconPack LoadJsonManifest(TextReader reader) {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        return FromJson(reader.ReadToEnd());
    }

    /// <summary>
    /// Loads a topology icon pack from a JSON manifest text reader and resolves pack-local sidecar artwork.
    /// </summary>
    /// <param name="reader">The source text reader.</param>
    /// <param name="manifestDirectory">The manifest directory used to resolve sidecar artwork paths.</param>
    /// <returns>The imported topology icon pack.</returns>
    public static TopologyIconPack LoadJsonManifest(TextReader reader, string manifestDirectory) {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (string.IsNullOrWhiteSpace(manifestDirectory)) throw new ArgumentException("Value cannot be empty.", nameof(manifestDirectory));
        return FromJson(reader.ReadToEnd(), manifestDirectory);
    }

    /// <summary>
    /// Loads all JSON icon-pack manifests from a directory in deterministic file-name order.
    /// </summary>
    /// <param name="directoryPath">The source directory path.</param>
    /// <param name="searchPattern">The file search pattern.</param>
    /// <param name="recursive">Whether child directories should be searched.</param>
    /// <returns>The imported topology icon packs.</returns>
    public static IReadOnlyList<TopologyIconPack> LoadJsonManifestsFromDirectory(string directoryPath, string searchPattern = "*.json", bool recursive = false) {
        return GetManifestFiles(directoryPath, searchPattern, recursive)
            .Select(LoadJsonManifest)
            .ToList();
    }

    /// <summary>
    /// Loads all JSON icon-pack manifests from a directory and reports per-file success or failure.
    /// </summary>
    /// <param name="directoryPath">The source directory path.</param>
    /// <param name="searchPattern">The file search pattern.</param>
    /// <param name="recursive">Whether child directories should be searched.</param>
    /// <returns>Per-file load results in deterministic file-name order.</returns>
    public static IReadOnlyList<TopologyIconPackLoadResult> LoadJsonManifestResultsFromDirectory(string directoryPath, string searchPattern = "*.json", bool recursive = false) {
        var results = new List<TopologyIconPackLoadResult>();
        foreach (var path in GetManifestFiles(directoryPath, searchPattern, recursive)) {
            try {
                results.Add(new TopologyIconPackLoadResult(path, LoadJsonManifest(path)));
            } catch (Exception exception) {
                results.Add(new TopologyIconPackLoadResult(path, exception));
            }
        }

        return results;
    }

    /// <summary>
    /// Loads a topology icon catalog from a directory and returns per-file diagnostics.
    /// </summary>
    /// <param name="directoryPath">The source directory path.</param>
    /// <param name="searchPattern">The file search pattern.</param>
    /// <param name="recursive">Whether child directories should be searched.</param>
    /// <param name="includeBuiltInPacks">Whether the returned catalog should start with ChartForgeX built-in packs.</param>
    /// <returns>The loaded catalog plus per-file diagnostics.</returns>
    public static TopologyIconCatalogLoadResult LoadJsonCatalogFromDirectory(string directoryPath, string searchPattern = "*.json", bool recursive = false, bool includeBuiltInPacks = true) {
        return LoadJsonCatalogFromDirectory(directoryPath, new TopologyIconCatalogLoadOptions {
            SearchPattern = searchPattern,
            Recursive = recursive,
            IncludeBuiltInPacks = includeBuiltInPacks
        });
    }

    /// <summary>
    /// Loads a topology icon catalog from a directory and returns per-file diagnostics.
    /// </summary>
    /// <param name="directoryPath">The source directory path.</param>
    /// <param name="options">The load options.</param>
    /// <returns>The loaded catalog plus per-file diagnostics.</returns>
    public static TopologyIconCatalogLoadResult LoadJsonCatalogFromDirectory(string directoryPath, TopologyIconCatalogLoadOptions? options) {
        options ??= new TopologyIconCatalogLoadOptions();
        var catalog = options.IncludeBuiltInPacks ? TopologyIconCatalog.Default() : new TopologyIconCatalog();
        var results = new List<TopologyIconPackLoadResult>();
        foreach (var path in GetManifestFiles(directoryPath, options.SearchPattern, options.Recursive)) {
            try {
                var pack = LoadJsonManifest(path);
                AddPackWithConflictBehavior(catalog, pack, path, options.ConflictBehavior, results);
            } catch (Exception exception) {
                results.Add(new TopologyIconPackLoadResult(path, exception));
            }
        }

        return new TopologyIconCatalogLoadResult(catalog, results);
    }

    /// <summary>
    /// Serializes a topology icon pack as a deterministic JSON manifest.
    /// </summary>
    /// <param name="pack">The icon pack.</param>
    /// <param name="indented">Whether the manifest should be indented.</param>
    /// <returns>The JSON manifest.</returns>
    public static string ToJsonManifest(this TopologyIconPack pack, bool indented = true) {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        var writer = new JsonWriter(indented);
        writer.BeginObject();
        writer.WriteProperty("schema", Schema);
        writer.WriteProperty("version", 1);
        writer.WriteProperty("id", pack.Id);
        writer.WriteProperty("label", pack.Label);
        writer.WriteOptionalProperty("vendor", pack.Vendor);
        writer.WriteOptionalProperty("packVersion", pack.Version);
        writer.WriteProperty("builtIn", pack.IsBuiltIn);
        writer.WriteStringArray("tags", pack.Tags);
        writer.WriteStringMap("metadata", PortableMetadata(pack.Metadata));
        writer.BeginArrayProperty("icons");
        foreach (var icon in pack.Icons) {
            writer.BeginArrayObject();
            writer.WriteProperty("id", icon.Id);
            writer.WriteProperty("label", icon.Label);
            writer.WriteProperty("nodeKind", icon.NodeKind.ToString());
            writer.WriteProperty("shape", icon.Shape.ToString());
            writer.WriteOptionalProperty("symbol", icon.Symbol);
            writer.WriteOptionalProperty("color", icon.Color);
            writer.WriteOptionalProperty("category", icon.Category);
            if (icon.DisplayMode.HasValue) writer.WriteProperty("displayMode", icon.DisplayMode.Value.ToString());
            WriteArtwork(writer, icon.Artwork);
            writer.WriteStringArray("tags", icon.Tags);
            writer.WriteStringMap("metadata", icon.Metadata);
            writer.EndObject();
        }

        writer.EndArray();
        writer.EndObject();
        return NormalizeNewLines(writer.ToString());
    }

    /// <summary>
    /// Saves a topology icon pack to a JSON manifest file.
    /// </summary>
    /// <param name="pack">The icon pack.</param>
    /// <param name="path">The target manifest file path.</param>
    /// <param name="indented">Whether the manifest should be indented.</param>
    public static void SaveJsonManifest(this TopologyIconPack pack, string path, bool indented = true) {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Value cannot be empty.", nameof(path));
        WriteUtf8NoBomFile(path, pack.ToJsonManifest(indented));
    }

    /// <summary>
    /// Writes a topology icon pack JSON manifest to a text writer.
    /// </summary>
    /// <param name="pack">The icon pack.</param>
    /// <param name="writer">The target text writer.</param>
    /// <param name="indented">Whether the manifest should be indented.</param>
    public static void WriteJsonManifest(this TopologyIconPack pack, TextWriter writer, bool indented = true) {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        writer.Write(pack.ToJsonManifest(indented));
    }

    private static TopologyNodeKind ParseNodeKind(string value) {
        if (Enum.TryParse<TopologyNodeKind>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(typeof(TopologyNodeKind), parsed)) return parsed;
        throw new ArgumentException("Unknown topology icon nodeKind '" + value + "'.");
    }

    private static TopologyIconShape ParseIconShape(string value) {
        if (Enum.TryParse<TopologyIconShape>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(typeof(TopologyIconShape), parsed)) return parsed;
        throw new ArgumentException("Unknown topology icon shape '" + value + "'.");
    }

    private static TopologyNodeDisplayMode? ParseDisplayMode(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<TopologyNodeDisplayMode>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(typeof(TopologyNodeDisplayMode), parsed)) return parsed;
        throw new ArgumentException("Unknown topology icon displayMode '" + value + "'.");
    }

    private static TopologyIconArtwork? ParseArtwork(Dictionary<string, JsonValue>? values) {
        if (values == null) return null;
        return new TopologyIconArtwork {
            SvgViewBox = values.OptionalString("svgViewBox") ?? values.OptionalString("viewBox") ?? "0 0 24 24",
            SvgBody = values.OptionalString("svg"),
            SvgPath = values.OptionalString("svgPath"),
            PreviewPath = values.OptionalString("previewPath") ?? values.OptionalString("previewPngPath"),
            ImageHref = values.OptionalString("imageHref") ?? values.OptionalString("href"),
            PreserveAspectRatio = values.OptionalString("preserveAspectRatio") ?? "xMidYMid meet"
        };
    }

    private static void WriteArtwork(JsonWriter writer, TopologyIconArtwork? artwork) {
        if (artwork == null || (!artwork.HasSvgBody && !artwork.HasSvgPath && !artwork.HasImageHref)) return;
        writer.BeginObjectProperty("artwork");
        writer.WriteOptionalProperty("svgViewBox", artwork.SvgViewBox);
        if (!artwork.HasSvgPath) writer.WriteOptionalProperty("svg", artwork.SvgBody);
        writer.WriteOptionalProperty("svgPath", artwork.SvgPath);
        writer.WriteOptionalProperty("previewPath", artwork.PreviewPath);
        writer.WriteOptionalProperty("imageHref", artwork.ImageHref);
        writer.WriteOptionalProperty("preserveAspectRatio", artwork.PreserveAspectRatio);
        writer.EndObject();
    }

    internal static void WriteUtf8NoBomFile(string path, string content) {
        File.WriteAllText(path, NormalizeNewLines(content), Utf8NoBom);
    }

    internal static string NormalizeNewLines(string content) {
        return content.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static void AddPackWithConflictBehavior(TopologyIconCatalog catalog, TopologyIconPack pack, string path, TopologyIconPackConflictBehavior conflictBehavior, List<TopologyIconPackLoadResult> results) {
        if (!catalog.ContainsPack(pack.Id)) {
            catalog.AddPack(pack);
            results.Add(new TopologyIconPackLoadResult(path, pack));
            return;
        }

        switch (conflictBehavior) {
            case TopologyIconPackConflictBehavior.ReportError:
                catalog.AddPack(pack);
                results.Add(new TopologyIconPackLoadResult(path, pack));
                break;
            case TopologyIconPackConflictBehavior.Skip:
                results.Add(new TopologyIconPackLoadResult(path, pack, "Topology icon pack '" + pack.Id + "' was skipped because it is already registered."));
                break;
            case TopologyIconPackConflictBehavior.Replace:
                catalog.AddOrReplacePack(pack);
                results.Add(new TopologyIconPackLoadResult(path, pack));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(conflictBehavior), "Unknown topology icon pack conflict behavior.");
        }
    }

    private static IReadOnlyList<string> GetManifestFiles(string directoryPath, string searchPattern, bool recursive) {
        if (string.IsNullOrWhiteSpace(directoryPath)) throw new ArgumentException("Value cannot be empty.", nameof(directoryPath));
        if (string.IsNullOrWhiteSpace(searchPattern)) throw new ArgumentException("Value cannot be empty.", nameof(searchPattern));
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(directoryPath, searchPattern, option)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IDictionary<string, string> PortableMetadata(IDictionary<string, string> metadata) {
        return metadata
            .Where(pair => !pair.Key.StartsWith("manifest.", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class JsonWriter {
        private readonly StringBuilder _builder = new();
        private readonly bool _indented;
        private readonly Stack<bool> _firstProperty = new();
        private int _depth;

        public JsonWriter(bool indented) {
            _indented = indented;
        }

        public void BeginObject() {
            _builder.Append('{');
            _firstProperty.Push(true);
            _depth++;
        }

        public void BeginArrayObject() {
            WriteArraySeparator();
            _builder.Append('{');
            _firstProperty.Push(true);
            _depth++;
        }

        public void EndObject() {
            _depth--;
            if (!_firstProperty.Pop()) WriteNewLine();
            _builder.Append('}');
        }

        public void BeginArrayProperty(string name) {
            WriteName(name);
            _builder.Append('[');
            _firstProperty.Push(true);
            _depth++;
        }

        public void BeginObjectProperty(string name) {
            WriteName(name);
            _builder.Append('{');
            _firstProperty.Push(true);
            _depth++;
        }

        public void EndArray() {
            _depth--;
            if (!_firstProperty.Pop()) WriteNewLine();
            _builder.Append(']');
        }

        public void WriteProperty(string name, string value) {
            WriteName(name);
            WriteString(value);
        }

        public void WriteProperty(string name, int value) {
            WriteName(name);
            _builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteProperty(string name, bool value) {
            WriteName(name);
            _builder.Append(value ? "true" : "false");
        }

        public void WriteOptionalProperty(string name, string? value) {
            if (string.IsNullOrWhiteSpace(value)) return;
            WriteProperty(name, value!);
        }

        public void WriteStringArray(string name, IReadOnlyList<string> values) {
            if (values.Count == 0) return;
            WriteName(name);
            _builder.Append('[');
            for (var i = 0; i < values.Count; i++) {
                if (i > 0) _builder.Append(',');
                if (_indented) _builder.Append(' ');
                WriteString(values[i]);
            }

            if (_indented) _builder.Append(' ');
            _builder.Append(']');
        }

        public void WriteStringMap(string name, IDictionary<string, string> values) {
            if (values.Count == 0) return;
            WriteName(name);
            _builder.Append('{');
            _firstProperty.Push(true);
            _depth++;
            foreach (var pair in values.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)) WriteProperty(pair.Key, pair.Value);
            EndObject();
        }

        private void WriteName(string name) {
            WriteArraySeparator();
            WriteString(name);
            _builder.Append(_indented ? ": " : ":");
        }

        private void WriteArraySeparator() {
            if (_firstProperty.Count == 0) return;
            var first = _firstProperty.Pop();
            if (!first) _builder.Append(',');
            _firstProperty.Push(false);
            if (_indented) WriteNewLine();
        }

        private void WriteNewLine() {
            if (!_indented) return;
            _builder.AppendLine();
            _builder.Append(' ', _depth * 2);
        }

        private void WriteString(string value) {
            _builder.Append('"');
            foreach (var c in value) {
                switch (c) {
                    case '\\':
                        _builder.Append("\\\\");
                        break;
                    case '"':
                        _builder.Append("\\\"");
                        break;
                    case '\b':
                        _builder.Append("\\b");
                        break;
                    case '\f':
                        _builder.Append("\\f");
                        break;
                    case '\n':
                        _builder.Append("\\n");
                        break;
                    case '\r':
                        _builder.Append("\\r");
                        break;
                    case '\t':
                        _builder.Append("\\t");
                        break;
                    default:
                        if (c < ' ') {
                            _builder.Append("\\u");
                            _builder.Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                        } else {
                            _builder.Append(c);
                        }
                        break;
                }
            }
            _builder.Append('"');
        }

        public override string ToString() {
            return _builder.ToString();
        }
    }

    private sealed class JsonReader {
        private readonly string _json;
        private int _position;

        private JsonReader(string json) {
            _json = json;
        }

        public static JsonValue Parse(string json) {
            var reader = new JsonReader(json);
            var value = reader.ReadValue();
            reader.SkipWhitespace();
            if (!reader.End) throw reader.Error("Unexpected content after JSON value.");
            return value;
        }

        private bool End => _position >= _json.Length;

        private JsonValue ReadValue() {
            SkipWhitespace();
            if (End) throw Error("Unexpected end of JSON.");
            var c = _json[_position];
            if (c == '"') return new JsonValue(ReadString());
            if (c == '{') return ReadObject();
            if (c == '[') return ReadArray();
            if (Match("true")) return new JsonValue(true);
            if (Match("false")) return new JsonValue(false);
            if (Match("null")) return JsonValue.Null;
            return ReadNumber();
        }

        private JsonValue ReadObject() {
            _position++;
            var values = new Dictionary<string, JsonValue>(StringComparer.OrdinalIgnoreCase);
            SkipWhitespace();
            if (TryRead('}')) return new JsonValue(values);
            while (true) {
                SkipWhitespace();
                if (End || _json[_position] != '"') throw Error("Expected object property name.");
                var name = ReadString();
                SkipWhitespace();
                Expect(':');
                values[name] = ReadValue();
                SkipWhitespace();
                if (TryRead('}')) break;
                Expect(',');
            }

            return new JsonValue(values);
        }

        private JsonValue ReadArray() {
            _position++;
            var values = new List<JsonValue>();
            SkipWhitespace();
            if (TryRead(']')) return new JsonValue(values);
            while (true) {
                values.Add(ReadValue());
                SkipWhitespace();
                if (TryRead(']')) break;
                Expect(',');
            }

            return new JsonValue(values);
        }

        private JsonValue ReadNumber() {
            var start = _position;
            if (!End && _json[_position] == '-') _position++;
            var integerStart = _position;
            if (!End && _json[_position] == '0') {
                _position++;
            } else {
                while (!End && char.IsDigit(_json[_position])) _position++;
            }

            if (integerStart == _position) throw Error("Expected JSON number.");
            if (!End && _json[_position] == '.') {
                _position++;
                var fractionStart = _position;
                while (!End && char.IsDigit(_json[_position])) _position++;
                if (fractionStart == _position) throw Error("Expected JSON number fraction.");
            }
            if (!End && (_json[_position] == 'e' || _json[_position] == 'E')) {
                _position++;
                if (!End && (_json[_position] == '+' || _json[_position] == '-')) _position++;
                var exponentStart = _position;
                while (!End && char.IsDigit(_json[_position])) _position++;
                if (exponentStart == _position) throw Error("Expected JSON number exponent.");
            }

            if (start == _position) throw Error("Expected JSON value.");
            return new JsonValue(_json.Substring(start, _position - start));
        }

        private string ReadString() {
            Expect('"');
            var builder = new StringBuilder();
            while (!End) {
                var c = _json[_position++];
                if (c == '"') return builder.ToString();
                if (c != '\\') {
                    builder.Append(c);
                    continue;
                }

                if (End) throw Error("Unexpected end of JSON string escape.");
                var escaped = _json[_position++];
                switch (escaped) {
                    case '"':
                    case '\\':
                    case '/':
                        builder.Append(escaped);
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'u':
                        if (_position + 4 > _json.Length) throw Error("Invalid unicode escape.");
                        var hex = _json.Substring(_position, 4);
                        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code)) throw Error("Invalid unicode escape.");
                        builder.Append((char)code);
                        _position += 4;
                        break;
                    default:
                        throw Error("Invalid JSON string escape.");
                }
            }

            throw Error("Unterminated JSON string.");
        }

        private bool Match(string text) {
            if (_position + text.Length > _json.Length) return false;
            if (!string.Equals(_json.Substring(_position, text.Length), text, StringComparison.Ordinal)) return false;
            _position += text.Length;
            return true;
        }

        private void Expect(char expected) {
            SkipWhitespace();
            if (End || _json[_position] != expected) throw Error("Expected '" + expected + "'.");
            _position++;
        }

        private bool TryRead(char expected) {
            SkipWhitespace();
            if (End || _json[_position] != expected) return false;
            _position++;
            return true;
        }

        private void SkipWhitespace() {
            while (!End && char.IsWhiteSpace(_json[_position])) _position++;
        }

        private ArgumentException Error(string message) {
            return new ArgumentException(message + " Position " + _position.ToString(CultureInfo.InvariantCulture) + ".");
        }
    }

    private sealed class JsonValue {
        public static readonly JsonValue Null = new(null);

        private readonly object? _value;

        public JsonValue(object? value) {
            _value = value;
        }

        public Dictionary<string, JsonValue> AsObject(string context) {
            return _value as Dictionary<string, JsonValue> ?? throw new ArgumentException("Expected JSON object for " + context + ".");
        }

        public List<JsonValue> AsArray(string context) {
            return _value as List<JsonValue> ?? throw new ArgumentException("Expected JSON array for " + context + ".");
        }

        public string? AsString(string context) {
            if (_value == null) return null;
            return _value as string ?? throw new ArgumentException("Expected JSON string for " + context + ".");
        }

        public bool? AsBool(string context) {
            if (_value == null) return null;
            return _value is bool value ? value : throw new ArgumentException("Expected JSON boolean for " + context + ".");
        }
    }

    private static string RequiredString(this Dictionary<string, JsonValue> values, string name) {
        if (!values.TryGetValue(name, out var value)) throw new ArgumentException("Missing required topology icon manifest property '" + name + "'.");
        var text = value.AsString(name);
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Topology icon manifest property '" + name + "' cannot be empty.");
        return text!.Trim();
    }

    private static string? OptionalString(this Dictionary<string, JsonValue> values, string name) {
        return values.TryGetValue(name, out var value) ? value.AsString(name) : null;
    }

    private static bool? OptionalBool(this Dictionary<string, JsonValue> values, string name) {
        return values.TryGetValue(name, out var value) ? value.AsBool(name) : null;
    }

    private static Dictionary<string, JsonValue>? OptionalObject(this Dictionary<string, JsonValue> values, string name) {
        return values.TryGetValue(name, out var value) ? value.AsObject(name) : null;
    }

    private static List<JsonValue> RequiredArray(this Dictionary<string, JsonValue> values, string name) {
        if (!values.TryGetValue(name, out var value)) throw new ArgumentException("Missing required topology icon manifest property '" + name + "'.");
        return value.AsArray(name);
    }

    private static IEnumerable<string> OptionalStringArray(this Dictionary<string, JsonValue> values, string name) {
        if (!values.TryGetValue(name, out var value)) yield break;
        foreach (var item in value.AsArray(name)) {
            var text = item.AsString(name);
            if (!string.IsNullOrWhiteSpace(text)) yield return text!.Trim();
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> OptionalStringMap(this Dictionary<string, JsonValue> values, string name) {
        if (!values.TryGetValue(name, out var value)) yield break;
        foreach (var pair in value.AsObject(name)) {
            var text = pair.Value.AsString(name);
            yield return new KeyValuePair<string, string>(pair.Key, text ?? string.Empty);
        }
    }
}
