using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static class VisualArtifactInterchangeJson {
    private const int MaximumJsonDepth = 32;
    private static readonly GeoJsonReadLimits JsonReadLimits = new GeoJsonReadLimits(
            VisualArtifactInterchangeValidation.MaximumJsonValues,
            VisualArtifactInterchangeValidation.MaximumEdges,
            VisualArtifactInterchangeValidation.MaximumMetadataEntries)
        .LimitArray("groups", VisualArtifactInterchangeValidation.MaximumGroups)
        .LimitArray("nodes", VisualArtifactInterchangeValidation.MaximumNodes)
        .LimitArray("edges", VisualArtifactInterchangeValidation.MaximumEdges)
        .LimitArray("annotations", VisualArtifactInterchangeValidation.MaximumAnnotations)
        .LimitArray("ports", VisualArtifactInterchangeValidation.MaximumPortsPerNode)
        .LimitArray("details", VisualArtifactInterchangeValidation.MaximumDetailsPerNode)
        .LimitArray("targetIds", VisualArtifactInterchangeValidation.MaximumTargetIdsPerAnnotation)
        .LimitObject("metadata", VisualArtifactInterchangeValidation.MaximumMetadataEntries)
        .RejectDuplicates();

    public static string Serialize(VisualArtifactInterchangeEnvelope envelope) {
        VisualArtifactInterchangeValidation.Validate(envelope);
        var writer = new VisualArtifactInterchangeJsonWriter();
        writer.StartObject();
        String(writer, "schema", envelope.Schema);
        Number(writer, "version", envelope.Version);
        String(writer, "kind", envelope.Kind.ToString());
        String(writer, "sourceLanguage", envelope.SourceLanguage.ToString());
        String(writer, "id", envelope.Id);
        String(writer, "title", envelope.Title);
        String(writer, "subtitle", envelope.Subtitle);
        String(writer, "layout", envelope.Layout);
        String(writer, "direction", envelope.Direction);
        OptionalNumber(writer, "width", envelope.Width);
        OptionalNumber(writer, "height", envelope.Height);
        OptionalString(writer, "accessibleName", envelope.AccessibleName);
        OptionalString(writer, "accessibleDescription", envelope.AccessibleDescription);
        OptionalString(writer, "language", envelope.Language);
        Boolean(writer, "decorative", envelope.IsDecorative);
        Metadata(writer, "metadata", envelope.Metadata);
        Groups(writer, envelope.Groups);
        Nodes(writer, envelope.Nodes);
        Edges(writer, envelope.Edges);
        Annotations(writer, envelope.Annotations);
        writer.EndObject();
        string json = writer.ToString();
        if (json.Length > VisualArtifactInterchangeValidation.MaximumJsonCharacters) throw new InvalidOperationException("The interchange JSON exceeds the maximum supported size.");
        return json;
    }

    public static VisualArtifactInterchangeEnvelope Deserialize(string json) {
        ValidateJsonInput(json);
        Dictionary<string, GeoJsonValue> root = GeoJsonValue.Parse(json, StringComparer.Ordinal, JsonReadLimits).AsObject("visual artifact interchange envelope");
        string schema = RequiredString(root, "schema");
        if (!string.Equals(schema, VisualArtifactInterchangeEnvelope.SchemaId, StringComparison.Ordinal)) throw new NotSupportedException("Unsupported visual artifact interchange schema: " + schema + ".");
        int version = RequiredInt(root, "version");
        if (version != VisualArtifactInterchangeEnvelope.CurrentVersion) throw new NotSupportedException("Unsupported visual artifact interchange version: " + version + ".");

        var envelope = new VisualArtifactInterchangeEnvelope {
            Kind = RequiredEnum<VisualArtifactKind>(root, "kind"),
            SourceLanguage = RequiredEnum<VisualArtifactSourceLanguage>(root, "sourceLanguage"),
            Id = RequiredString(root, "id"),
            Title = OptionalString(root, "title") ?? string.Empty,
            Subtitle = OptionalString(root, "subtitle") ?? string.Empty,
            Layout = OptionalString(root, "layout") ?? string.Empty,
            Direction = OptionalString(root, "direction") ?? string.Empty,
            Width = OptionalNumber(root, "width"),
            Height = OptionalNumber(root, "height"),
            AccessibleName = OptionalString(root, "accessibleName"),
            AccessibleDescription = OptionalString(root, "accessibleDescription"),
            Language = OptionalString(root, "language"),
            IsDecorative = OptionalBool(root, "decorative") ?? false
        };
        ReadMetadata(root, "metadata", envelope.Metadata);
        ReadGroups(root, envelope.Groups);
        ReadNodes(root, envelope.Nodes);
        ReadEdges(root, envelope.Edges);
        ReadAnnotations(root, envelope.Annotations);
        VisualArtifactInterchangeValidation.Validate(envelope);
        return envelope;
    }

    private static void Groups(VisualArtifactInterchangeJsonWriter writer, IEnumerable<VisualArtifactInterchangeGroup> groups) {
        writer.Property("groups");
        writer.StartArray();
        foreach (var group in groups) {
            writer.StartObject();
            String(writer, "id", group.Id);
            String(writer, "kind", group.Kind);
            String(writer, "label", group.Label);
            OptionalString(writer, "subtitle", group.Subtitle);
            OptionalString(writer, "status", group.Status);
            OptionalString(writer, "color", group.Color);
            OptionalString(writer, "href", group.Href);
            OptionalString(writer, "tooltip", group.Tooltip);
            OptionalNumber(writer, "x", group.X);
            OptionalNumber(writer, "y", group.Y);
            OptionalNumber(writer, "width", group.Width);
            OptionalNumber(writer, "height", group.Height);
            Metadata(writer, "metadata", group.Metadata);
            writer.EndObject();
        }
        writer.EndArray();
    }

    private static void Nodes(VisualArtifactInterchangeJsonWriter writer, IEnumerable<VisualArtifactInterchangeNode> nodes) {
        writer.Property("nodes");
        writer.StartArray();
        foreach (var node in nodes) {
            writer.StartObject();
            String(writer, "id", node.Id);
            String(writer, "kind", node.Kind);
            String(writer, "label", node.Label);
            OptionalString(writer, "subtitle", node.Subtitle);
            OptionalString(writer, "groupId", node.GroupId);
            OptionalString(writer, "status", node.Status);
            OptionalString(writer, "iconId", node.IconId);
            OptionalString(writer, "symbol", node.Symbol);
            OptionalString(writer, "badge", node.Badge);
            OptionalString(writer, "color", node.Color);
            OptionalString(writer, "backgroundColor", node.BackgroundColor);
            OptionalString(writer, "href", node.Href);
            OptionalString(writer, "tooltip", node.Tooltip);
            OptionalNumber(writer, "x", node.X);
            OptionalNumber(writer, "y", node.Y);
            OptionalNumber(writer, "width", node.Width);
            OptionalNumber(writer, "height", node.Height);
            Metadata(writer, "metadata", node.Metadata);
            Ports(writer, node.Ports);
            Details(writer, node.Details);
            writer.EndObject();
        }
        writer.EndArray();
    }

    private static void Ports(VisualArtifactInterchangeJsonWriter writer, IEnumerable<VisualArtifactInterchangePort> ports) {
        writer.Property("ports");
        writer.StartArray();
        foreach (var port in ports) {
            writer.StartObject();
            String(writer, "id", port.Id);
            String(writer, "side", port.Side);
            Number(writer, "offset", port.Offset);
            OptionalString(writer, "label", port.Label);
            Metadata(writer, "metadata", port.Metadata);
            writer.EndObject();
        }
        writer.EndArray();
    }

    private static void Details(VisualArtifactInterchangeJsonWriter writer, IEnumerable<VisualArtifactInterchangeDetail> details) {
        writer.Property("details");
        writer.StartArray();
        foreach (var detail in details) {
            writer.StartObject();
            String(writer, "label", detail.Label);
            String(writer, "value", detail.Value);
            OptionalString(writer, "iconId", detail.IconId);
            OptionalString(writer, "status", detail.Status);
            OptionalString(writer, "color", detail.Color);
            Metadata(writer, "metadata", detail.Metadata);
            writer.EndObject();
        }
        writer.EndArray();
    }

    private static void Edges(VisualArtifactInterchangeJsonWriter writer, IEnumerable<VisualArtifactInterchangeEdge> edges) {
        writer.Property("edges");
        writer.StartArray();
        foreach (var edge in edges) {
            writer.StartObject();
            String(writer, "id", edge.Id);
            String(writer, "kind", edge.Kind);
            String(writer, "sourceId", edge.SourceId);
            String(writer, "targetId", edge.TargetId);
            OptionalString(writer, "label", edge.Label);
            OptionalString(writer, "secondaryLabel", edge.SecondaryLabel);
            OptionalString(writer, "tertiaryLabel", edge.TertiaryLabel);
            OptionalString(writer, "sourceLabel", edge.SourceLabel);
            OptionalString(writer, "targetLabel", edge.TargetLabel);
            OptionalString(writer, "status", edge.Status);
            OptionalString(writer, "direction", edge.Direction);
            OptionalString(writer, "lineStyle", edge.LineStyle);
            OptionalString(writer, "sourcePort", edge.SourcePort);
            OptionalString(writer, "targetPort", edge.TargetPort);
            OptionalString(writer, "sourcePortId", edge.SourcePortId);
            OptionalString(writer, "targetPortId", edge.TargetPortId);
            OptionalString(writer, "color", edge.Color);
            OptionalString(writer, "href", edge.Href);
            OptionalString(writer, "tooltip", edge.Tooltip);
            Number(writer, "order", edge.Order);
            Metadata(writer, "metadata", edge.Metadata);
            writer.EndObject();
        }
        writer.EndArray();
    }

    private static void Annotations(VisualArtifactInterchangeJsonWriter writer, IEnumerable<VisualArtifactInterchangeAnnotation> annotations) {
        writer.Property("annotations");
        writer.StartArray();
        foreach (var annotation in annotations) {
            writer.StartObject();
            String(writer, "id", annotation.Id);
            String(writer, "kind", annotation.Kind);
            String(writer, "text", annotation.Text);
            OptionalString(writer, "placement", annotation.Placement);
            OptionalNumber(writer, "startIndex", annotation.StartIndex);
            OptionalNumber(writer, "endIndex", annotation.EndIndex);
            writer.Property("targetIds");
            writer.StartArray();
            foreach (var targetId in annotation.TargetIds) writer.String(targetId);
            writer.EndArray();
            Metadata(writer, "metadata", annotation.Metadata);
            writer.EndObject();
        }
        writer.EndArray();
    }

    private static void Metadata(VisualArtifactInterchangeJsonWriter writer, string name, IDictionary<string, string> values) {
        writer.Property(name);
        writer.StartObject();
        var keys = new List<string>(values.Keys);
        keys.Sort(StringComparer.Ordinal);
        foreach (var key in keys) String(writer, key, values[key]);
        writer.EndObject();
    }

    private static void ReadGroups(Dictionary<string, GeoJsonValue> root, ICollection<VisualArtifactInterchangeGroup> target) {
        foreach (var value in OptionalArray(root, "groups")) {
            var item = value.AsObject("group");
            var group = new VisualArtifactInterchangeGroup {
                Id = RequiredString(item, "id"), Kind = OptionalString(item, "kind") ?? string.Empty, Label = OptionalString(item, "label") ?? string.Empty,
                Subtitle = OptionalString(item, "subtitle"), Status = OptionalString(item, "status"), Color = OptionalString(item, "color"), Href = OptionalString(item, "href"), Tooltip = OptionalString(item, "tooltip"),
                X = OptionalNumber(item, "x"), Y = OptionalNumber(item, "y"), Width = OptionalNumber(item, "width"), Height = OptionalNumber(item, "height")
            };
            ReadMetadata(item, "metadata", group.Metadata);
            target.Add(group);
        }
    }

    private static void ReadNodes(Dictionary<string, GeoJsonValue> root, ICollection<VisualArtifactInterchangeNode> target) {
        foreach (var value in OptionalArray(root, "nodes")) {
            var item = value.AsObject("node");
            var node = new VisualArtifactInterchangeNode {
                Id = RequiredString(item, "id"), Kind = OptionalString(item, "kind") ?? string.Empty, Label = OptionalString(item, "label") ?? string.Empty,
                Subtitle = OptionalString(item, "subtitle"), GroupId = OptionalString(item, "groupId"), Status = OptionalString(item, "status"), IconId = OptionalString(item, "iconId"), Symbol = OptionalString(item, "symbol"), Badge = OptionalString(item, "badge"),
                Color = OptionalString(item, "color"), BackgroundColor = OptionalString(item, "backgroundColor"), Href = OptionalString(item, "href"), Tooltip = OptionalString(item, "tooltip"),
                X = OptionalNumber(item, "x"), Y = OptionalNumber(item, "y"), Width = OptionalNumber(item, "width"), Height = OptionalNumber(item, "height")
            };
            ReadMetadata(item, "metadata", node.Metadata);
            foreach (var portValue in OptionalArray(item, "ports")) {
                var portItem = portValue.AsObject("port");
                var port = new VisualArtifactInterchangePort { Id = RequiredString(portItem, "id"), Side = OptionalString(portItem, "side") ?? string.Empty, Offset = OptionalNumber(portItem, "offset") ?? 0.5, Label = OptionalString(portItem, "label") };
                ReadMetadata(portItem, "metadata", port.Metadata);
                node.Ports.Add(port);
            }
            foreach (var detailValue in OptionalArray(item, "details")) {
                var detailItem = detailValue.AsObject("detail");
                var detail = new VisualArtifactInterchangeDetail { Label = OptionalString(detailItem, "label") ?? string.Empty, Value = OptionalString(detailItem, "value") ?? string.Empty, IconId = OptionalString(detailItem, "iconId"), Status = OptionalString(detailItem, "status"), Color = OptionalString(detailItem, "color") };
                ReadMetadata(detailItem, "metadata", detail.Metadata);
                node.Details.Add(detail);
            }
            target.Add(node);
        }
    }

    private static void ReadEdges(Dictionary<string, GeoJsonValue> root, ICollection<VisualArtifactInterchangeEdge> target) {
        foreach (var value in OptionalArray(root, "edges")) {
            var item = value.AsObject("edge");
            var edge = new VisualArtifactInterchangeEdge {
                Id = RequiredString(item, "id"), Kind = OptionalString(item, "kind") ?? string.Empty, SourceId = RequiredString(item, "sourceId"), TargetId = RequiredString(item, "targetId"),
                Label = OptionalString(item, "label"), SecondaryLabel = OptionalString(item, "secondaryLabel"), TertiaryLabel = OptionalString(item, "tertiaryLabel"), SourceLabel = OptionalString(item, "sourceLabel"), TargetLabel = OptionalString(item, "targetLabel"),
                Status = OptionalString(item, "status"), Direction = OptionalString(item, "direction"), LineStyle = OptionalString(item, "lineStyle"), SourcePort = OptionalString(item, "sourcePort"), TargetPort = OptionalString(item, "targetPort"),
                SourcePortId = OptionalString(item, "sourcePortId"), TargetPortId = OptionalString(item, "targetPortId"), Color = OptionalString(item, "color"), Href = OptionalString(item, "href"), Tooltip = OptionalString(item, "tooltip"), Order = OptionalInt(item, "order") ?? 0
            };
            ReadMetadata(item, "metadata", edge.Metadata);
            target.Add(edge);
        }
    }

    private static void ReadAnnotations(Dictionary<string, GeoJsonValue> root, ICollection<VisualArtifactInterchangeAnnotation> target) {
        foreach (var value in OptionalArray(root, "annotations")) {
            var item = value.AsObject("annotation");
            var annotation = new VisualArtifactInterchangeAnnotation {
                Id = RequiredString(item, "id"), Kind = OptionalString(item, "kind") ?? string.Empty, Text = OptionalString(item, "text") ?? string.Empty,
                Placement = OptionalString(item, "placement"), StartIndex = OptionalInt(item, "startIndex"), EndIndex = OptionalInt(item, "endIndex")
            };
            foreach (var targetValue in OptionalArray(item, "targetIds")) annotation.TargetIds.Add(targetValue.AsString("annotation target id"));
            ReadMetadata(item, "metadata", annotation.Metadata);
            target.Add(annotation);
        }
    }

    private static void ReadMetadata(Dictionary<string, GeoJsonValue> values, string name, IDictionary<string, string> target) {
        if (!values.TryGetValue(name, out var raw) || raw.IsNull) return;
        foreach (var pair in raw.AsObject(name)) target[pair.Key] = pair.Value.AsString(name + " value");
    }

    private static List<GeoJsonValue> OptionalArray(Dictionary<string, GeoJsonValue> values, string name) =>
        values.TryGetValue(name, out var value) && !value.IsNull ? value.AsArray(name) : new List<GeoJsonValue>();

    private static string RequiredString(Dictionary<string, GeoJsonValue> values, string name) {
        string? value = OptionalString(values, name);
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Missing required interchange string: " + name + ".");
        return value!;
    }

    private static string? OptionalString(Dictionary<string, GeoJsonValue> values, string name) =>
        values.TryGetValue(name, out var value) && !value.IsNull ? value.AsString(name) : null;

    private static double? OptionalNumber(Dictionary<string, GeoJsonValue> values, string name) =>
        values.TryGetValue(name, out var value) && !value.IsNull ? value.AsNumber(name) : null;

    private static int RequiredInt(Dictionary<string, GeoJsonValue> values, string name) => OptionalInt(values, name) ?? throw new ArgumentException("Missing required interchange integer: " + name + ".");

    private static int? OptionalInt(Dictionary<string, GeoJsonValue> values, string name) {
        double? number = OptionalNumber(values, name);
        if (!number.HasValue) return null;
        if (number.Value < int.MinValue || number.Value > int.MaxValue || Math.Truncate(number.Value) != number.Value) throw new ArgumentException("Interchange property '" + name + "' must be an integer.");
        return (int)number.Value;
    }

    private static bool? OptionalBool(Dictionary<string, GeoJsonValue> values, string name) {
        if (!values.TryGetValue(name, out var value) || value.IsNull) return null;
        return value.AsBoolean(name);
    }

    private static TEnum RequiredEnum<TEnum>(Dictionary<string, GeoJsonValue> values, string name) where TEnum : struct {
        string value = RequiredString(values, name);
        foreach (string declaredName in Enum.GetNames(typeof(TEnum))) {
            if (string.Equals(declaredName, value, StringComparison.OrdinalIgnoreCase)) return (TEnum)Enum.Parse(typeof(TEnum), declaredName, ignoreCase: false);
        }
        throw new ArgumentException("Unknown " + typeof(TEnum).Name + " value: " + value + ".");
    }

    private static void String(VisualArtifactInterchangeJsonWriter writer, string name, string value) { writer.Property(name); writer.String(value); }
    private static void OptionalString(VisualArtifactInterchangeJsonWriter writer, string name, string? value) { if (value != null) String(writer, name, value); }
    private static void Number(VisualArtifactInterchangeJsonWriter writer, string name, double value) { writer.Property(name); writer.Number(value); }
    private static void Number(VisualArtifactInterchangeJsonWriter writer, string name, int value) { writer.Property(name); writer.Number(value); }
    private static void OptionalNumber(VisualArtifactInterchangeJsonWriter writer, string name, double? value) { if (value.HasValue) Number(writer, name, value.Value); }
    private static void OptionalNumber(VisualArtifactInterchangeJsonWriter writer, string name, int? value) { if (value.HasValue) Number(writer, name, value.Value); }
    private static void Boolean(VisualArtifactInterchangeJsonWriter writer, string name, bool value) { writer.Property(name); writer.Boolean(value); }

    private static void ValidateJsonInput(string json) {
        if (json == null) throw new ArgumentNullException(nameof(json));
        if (json.Length == 0) throw new ArgumentException("Interchange JSON cannot be empty.", nameof(json));
        if (json.Length > VisualArtifactInterchangeValidation.MaximumJsonCharacters) throw new ArgumentException("Interchange JSON exceeds the maximum supported size.", nameof(json));
        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = 0; index < json.Length; index++) {
            char c = json[index];
            if (inString) {
                if (c < ' ') throw new ArgumentException("Interchange JSON strings cannot contain unescaped control characters.", nameof(json));
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }
            if (char.IsWhiteSpace(c) && c != ' ' && c != '\t' && c != '\r' && c != '\n') {
                throw new ArgumentException("Interchange JSON contains whitespace that is not permitted by the JSON grammar.", nameof(json));
            }
            if (c == '"') {
                ValidateJsonStringSurrogates(json, index + 1);
                inString = true;
            }
            else if (IsJsonNumberStart(json, index)) ValidateJsonNumber(json, index);
            else if (c == '{' || c == '[') {
                depth++;
                if (depth > MaximumJsonDepth) throw new ArgumentException("Interchange JSON exceeds the maximum supported nesting depth.", nameof(json));
            } else if (c == '}' || c == ']') depth--;
            if (depth < 0) throw new ArgumentException("Interchange JSON contains unbalanced containers.", nameof(json));
        }
        if (inString || depth != 0) throw new ArgumentException("Interchange JSON is incomplete.", nameof(json));
    }

    private static bool IsJsonNumberStart(string json, int index) {
        char c = json[index];
        if (c != '-' && (c < '0' || c > '9')) return false;
        for (var previous = index - 1; previous >= 0; previous--) {
            char prior = json[previous];
            if (char.IsWhiteSpace(prior)) continue;
            return prior == ':' || prior == ',' || prior == '[';
        }
        return true;
    }

    private static void ValidateJsonNumber(string json, int startIndex) {
        int firstDigit = json[startIndex] == '-' ? startIndex + 1 : startIndex;
        if (firstDigit + 1 >= json.Length || json[firstDigit] != '0') return;
        char next = json[firstDigit + 1];
        if (next >= '0' && next <= '9') {
            throw new ArgumentException("Interchange JSON numbers cannot contain leading zeros.", nameof(json));
        }
    }

    private static void ValidateJsonStringSurrogates(string json, int startIndex) {
        for (var index = startIndex; index < json.Length; index++) {
            char c = json[index];
            if (c == '"') return;
            if (c == '\\') {
                if (index + 1 >= json.Length) return;
                if (json[index + 1] != 'u') {
                    index++;
                    continue;
                }
                if (!TryReadUnicodeEscape(json, index, out int codeUnit)) return;
                if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF) {
                    int lowEscape = index + 6;
                    if (!TryReadUnicodeEscape(json, lowEscape, out int lowCodeUnit) || lowCodeUnit < 0xDC00 || lowCodeUnit > 0xDFFF) {
                        throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(json));
                    }
                    index = lowEscape + 5;
                    continue;
                }
                if (codeUnit >= 0xDC00 && codeUnit <= 0xDFFF) {
                    throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(json));
                }
                index += 5;
                continue;
            }
            if (char.IsHighSurrogate(c)) {
                if (index + 1 >= json.Length || !char.IsLowSurrogate(json[index + 1])) {
                    throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(json));
                }
                index++;
            } else if (char.IsLowSurrogate(c)) {
                throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(json));
            }
        }
    }

    private static bool TryReadUnicodeEscape(string json, int slashIndex, out int codeUnit) {
        codeUnit = 0;
        if (slashIndex < 0 || slashIndex + 5 >= json.Length || json[slashIndex] != '\\' || json[slashIndex + 1] != 'u') return false;
        for (var index = slashIndex + 2; index <= slashIndex + 5; index++) {
            int digit = HexDigit(json[index]);
            if (digit < 0) return false;
            codeUnit = codeUnit * 16 + digit;
        }
        return true;
    }

    private static int HexDigit(char value) {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        return -1;
    }
}
