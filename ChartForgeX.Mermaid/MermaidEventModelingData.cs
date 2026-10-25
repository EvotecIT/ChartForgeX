using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

// Bounded lowering into existing table facts. Neither source maps nor literal formats execute.
internal static class MermaidEventModelingData {
    internal static bool IsLiteral(string? type) => type != null && !type.Equals("json", StringComparison.OrdinalIgnoreCase)
        && !type.Equals("jsobj", StringComparison.OrdinalIgnoreCase) && !type.Equals("text", StringComparison.OrdinalIgnoreCase);

    internal static bool Validate(string? type, string content, MermaidSourceSpan span) {
        Visit(type, content, span, null); return IsLiteral(type) || ReservedMapKey(type, content);
    }

    internal static TableArtifact Create(string id, string? type, string content, MermaidSourceSpan span) {
        var table = NewTable(id);
        // The first pass reserves every parsed value and visible row before constructing rows.
        Visit(type, content, span, (path, kind, value) => table.AddRow(path, path, kind, value));
        Facts(table.Metadata, type, content, span);
        foreach (var row in table.Rows) {
            row.Metadata["mermaid.data.path"] = row.Key; row.Metadata["mermaid.data.kind"] = row.Cells[1].DisplayText;
            row.Cells[2].Metadata["mermaid.data.kind"] = row.Cells[1].DisplayText;
        }
        return table;
    }
    internal static TableArtifact NewTable(string id) => TableArtifact.Create(id).AddColumn("field", "Field")
        .AddColumn("type", "Type").AddColumn("value", "Value");

    private static void Visit(string? type, string content, MermaidSourceSpan span, Action<string, string, string>? row) {
        MermaidResourceGuard.ValidateSource(content);
        CheckPortableFactText(content, span);
        if (type != null) { MermaidResourceGuard.ValidateSource(type); CheckPortableFactText(type, span); }
        if (IsLiteral(type) || string.Equals(type, "text", StringComparison.OrdinalIgnoreCase) || ReservedMapKey(type, content)) {
            MermaidResourceGuard.ReserveElements(1, span); row?.Invoke("/", "Literal", content); return;
        }
        if (string.Equals(type, "json", StringComparison.OrdinalIgnoreCase)) {
            var limits = MermaidResourceGuard.CapturedLimits();
            var trimmed = content.TrimStart();
            var jsonText = trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal) ? content : "{" + content + "}";
            var json = GeoJsonValue.Parse(jsonText, StringComparer.Ordinal,
                new GeoJsonReadLimits(limits.MaximumElements, limits.MaximumElements, limits.MaximumElements)
                    .LimitDepth(Math.Min(64, limits.MaximumNestingDepth)).RejectDuplicates().KeepNumberSpelling());
            long count = 0; Json(json, "", 1, (_, _, _) => count++, span);
            MermaidResourceGuard.ReserveElements(count * 2, span);
            if (row != null) Json(json, "", 1, row, span);
        } else {
            var metadata = MermaidParser.ReadMetadata(content, span);
            long count = 0; Metadata(metadata, "", 1, (_, _, _) => count++);
            MermaidResourceGuard.ReserveElements(count, span);
            if (row != null) Metadata(metadata, "", 1, row);
        }
    }

    internal static void CheckPortableFactText(string text, MermaidSourceSpan span) => CheckPortableFactLength(text.Length, span);

    internal static void CheckPortableFactLength(long length, MermaidSourceSpan span) =>
        MermaidResourceGuard.CheckInterchangeFactLength(length, span);

    private static void Fact(Action<string, string, string> row, string path, string kind, string value, MermaidSourceSpan span) {
        CheckPortableFactText(path, span); CheckPortableFactText(kind, span); CheckPortableFactText(value, span);
        row(path, kind, value);
    }

    private static void Json(GeoJsonValue value, string path, int depth, Action<string, string, string> row, MermaidSourceSpan span) {
        MermaidResourceGuard.CheckDepth(depth, span); MermaidResourceGuard.Checkpoint();
        if (value.TryAsObject(out var map)) {
            if (path.Length > 0 || map.Count == 0) Fact(row, Path(path), "Object", "{" + map.Count.ToString(CultureInfo.InvariantCulture) + " fields}", span);
            foreach (var pair in map.OrderBy(pair => pair.Value.PropertyOffset)) Json(pair.Value, path + "/" + Escape(pair.Key), depth + 1, row, span);
        } else if (value.TryAsArray(out var items)) {
            Fact(row, Path(path), "Array", "[" + items.Count.ToString(CultureInfo.InvariantCulture) + " items]", span);
            for (var index = 0; index < items.Count; index++) Json(items[index], path + "/" + index.ToString(CultureInfo.InvariantCulture), depth + 1, row, span);
        } else {
            var text = value.AsOptionalString();
            Fact(row, Path(path), value.IsNull ? "Null" : value.IsString ? "String" : text is "true" or "false" ? "Boolean" : "Number", value.IsNull ? "null" : text ?? "", span);
        }
    }
    private static void Metadata(MermaidMetadataValue value, string path, int depth, Action<string, string, string> row) {
        MermaidResourceGuard.CheckDepth(depth, value.Span); MermaidResourceGuard.Checkpoint();
        if (value.Kind == MermaidMetadataKind.Object) {
            if (path.Length > 0 || value.Properties.Count == 0) Fact(row, Path(path), "Object", "{" + value.Properties.Count.ToString(CultureInfo.InvariantCulture) + " fields}", value.Span);
            foreach (var pair in value.Properties) Metadata(pair.Value, path + "/" + Escape(pair.Key), depth + 1, row);
        } else if (value.Kind == MermaidMetadataKind.Array) {
            Fact(row, Path(path), "Array", "[" + value.Items.Count.ToString(CultureInfo.InvariantCulture) + " items]", value.Span);
            for (var index = 0; index < value.Items.Count; index++) Metadata(value.Items[index], path + "/" + index.ToString(CultureInfo.InvariantCulture), depth + 1, row);
        } else Fact(row, Path(path), value.Kind.ToString(), value.Kind switch {
            MermaidMetadataKind.Null => "null", MermaidMetadataKind.Boolean => value.Boolean!.Value ? "true" : "false",
            MermaidMetadataKind.Number => value.Number!.Value.ToString("R", CultureInfo.InvariantCulture), _ => value.Text ?? ""
        }, value.Span);
    }
    // Prototype-shaped map fields use literal fallback rather than disappearing under
    // the existing metadata owner's intentional prototype filtering. JSON keeps all keys.
    private static bool ReservedMapKey(string? type, string content) {
        if (type != null && !type.Equals("jsobj", StringComparison.OrdinalIgnoreCase)) return false;
        for (var index = 0; index < content.Length; index++) {
            MermaidResourceGuard.Checkpoint(); var start = index; var quote = content[index]; string token;
            if (quote is '\'' or '"') {
                index++;
                while (index < content.Length) {
                    if (content[index] == '\\' && quote == '"') { index += 2; continue; }
                    if (content[index] == quote) { if (quote == '\'' && index + 1 < content.Length && content[index + 1] == '\'') { index += 2; continue; } break; }
                    index++;
                }
                if (index >= content.Length) return false;
                var raw = content.Substring(start, index - start + 1);
                token = quote == '"' ? GeoJsonValue.Parse(raw).AsString("Event Modeling field") : MermaidYamlScalar.ReadSingleQuoted(raw);
            } else if (char.IsLetter(content[index]) || content[index] == '_') {
                while (index + 1 < content.Length && (char.IsLetterOrDigit(content[index + 1]) || content[index + 1] == '_')) index++;
                token = content.Substring(start, index - start + 1);
            } else continue;
            var after = index + 1; while (after < content.Length && char.IsWhiteSpace(content[after])) after++;
            if (after < content.Length && content[after] == ':' && MermaidMetadataValue.IsPrototypeKey(token)) return true;
        }
        return false;
    }
    internal static void Facts(IDictionary<string, string> metadata, string? type, string content, MermaidSourceSpan span) {
        metadata["mermaid.data.type"] = type ?? "schema"; metadata["mermaid.data.source"] = content;
        metadata["mermaid.source.line"] = span.Line.ToString(CultureInfo.InvariantCulture);
        metadata["mermaid.source.column"] = span.Column.ToString(CultureInfo.InvariantCulture);
        metadata["mermaid.source.length"] = span.Length.ToString(CultureInfo.InvariantCulture);
    }
    private static string Path(string path) => path.Length == 0 ? "/" : path;
    internal static string Escape(string value) => value.Replace("~", "~0").Replace("/", "~1");
}
