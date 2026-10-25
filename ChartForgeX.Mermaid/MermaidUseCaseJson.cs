using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

internal static class MermaidUseCaseJson {
    internal const string SourceProperty = "usecase.json";
    private static readonly Regex Start = new(@"^json[\t ]+(?<id>\w+)[\t ]*@[\t ]*(?=\{)", RegexOptions.CultureInvariant);
    internal sealed class Declaration {
        internal string Id = "", Source = "";
        internal List<string> Classes { get; } = new();
        internal MermaidSourceSpan Span, IdSpan;
    }
    internal static bool Starts(string text) => MermaidParserUtilities.StartsStatement(text, "json");
    internal static Declaration Read(string[] lines, ref int line) {
        var raw = lines[line - 1]; var indent = MermaidParserUtilities.LeadingWhitespace(raw); var text = raw.Substring(indent);
        var match = Start.Match(text);
        var span = new MermaidSourceSpan(line, indent + 1, text.Length);
        if (!match.Success) throw new ArgumentException("UseCase JSON requires 'json ID @ { ... }' with an object body.");
        var position = indent + match.Length; var firstLine = line;
        var content = MermaidBracedDataReader.Read(lines, ref line, ref position, span, "UseCase JSON");
        var suffix = lines[line - 1].Substring(position).Trim();
        var comment = suffix.IndexOf("%%", StringComparison.Ordinal); if (comment >= 0) suffix = suffix.Substring(0, comment).TrimEnd();
        var classes = new List<string>();
        if (suffix.StartsWith(":::", StringComparison.Ordinal)) {
            classes = MermaidStyleParser.Names(suffix.Substring(3));
            if (classes.Count == 0 || classes.Any(name => !Regex.IsMatch(name, @"^\w+$", RegexOptions.CultureInvariant)))
                throw new ArgumentException("UseCase JSON inline classes require comma-separated plain class identifiers.");
            MermaidResourceGuard.ReserveElements(classes.Count, span);
        } else if (suffix.Length > 0 && !suffix.StartsWith("%%", StringComparison.Ordinal))
            throw new ArgumentException("Unexpected text after a UseCase JSON object.");
        var source = "{" + content + "}";
        MermaidEventModelingData.CheckPortableFactText(source, span);
        var length = position - indent;
        if (line != firstLine) { length = lines[firstLine - 1].Length - indent; for (var row = firstLine; row < line - 1; row++) length += 1 + lines[row].Length; length += 1 + position; }
        var declaration = new Declaration { Id = match.Groups["id"].Value, Source = source,
            Span = new MermaidSourceSpan(firstLine, indent + 1, length),
            IdSpan = new MermaidSourceSpan(firstLine, indent + match.Groups["id"].Index + 1, match.Groups["id"].Length) };
        declaration.Classes.AddRange(classes);
        Create(declaration.Id, source, declaration.Span); // Reserve and validate before attaching a graph declaration.
        return declaration;
    }
    internal static HashSet<int> OpaqueLines(string[] lines, int firstLine) {
        var result = new HashSet<int>();
        for (var index = firstLine; index < lines.Length; index++) {
            MermaidResourceGuard.Checkpoint(); if (MermaidParser.SkipSharedDeclaration(lines, ref index)) continue;
            if (!Starts(lines[index].Trim())) continue;
            var line = index + 1;
            try { ReadOpaque(lines, ref line); } catch (ArgumentException) { }
            for (var opaque = index; opaque < line; opaque++) result.Add(opaque);
            index = line - 1;
        }
        return result;
    }
    private static void ReadOpaque(string[] lines, ref int line) {
        var text = lines[line - 1]; var indent = MermaidParserUtilities.LeadingWhitespace(text); var match = Start.Match(text.Substring(indent));
        if (!match.Success) return;
        var position = indent + match.Length;
        MermaidBracedDataReader.Read(lines, ref line, ref position, new MermaidSourceSpan(line, indent + 1, text.Length - indent), "UseCase JSON");
    }
    internal static TableArtifact Create(string id, string source, MermaidSourceSpan span) {
        var limits = MermaidResourceGuard.CapturedLimits();
        var value = GeoJsonValue.Parse(source, StringComparer.Ordinal,
            new GeoJsonReadLimits(limits.MaximumElements, limits.MaximumElements, limits.MaximumElements)
                .LimitDepth(Math.Min(64, limits.MaximumNestingDepth)).RejectDuplicates().KeepNumberSpelling());
        value.AsObject("UseCase JSON vertex");
        var rows = new List<(string Key, string Accessible, string Pointer, string Kind, string Value)>();
        Visit(value, "", "", 1, rows, span);
        MermaidResourceGuard.ReserveElements(rows.Count * 2L, span);
        if (rows.Count > VisualArtifactInterchangeValidation.MaximumTableRows)
            throw new ArgumentException("UseCase JSON exceeds the finite embedded table row limit.");
        var table = TableArtifact.Create(id).AddColumn("property", "Property").AddColumn("value", "Value");
        MermaidEventModelingData.Facts(table.Metadata, "json", source, span);
        foreach (var row in rows) {
            table.AddRow(row.Pointer, row.Key, row.Value);
            var target = table.Rows[table.Rows.Count - 1];
            target.Metadata["mermaid.data.pointer"] = row.Pointer; target.Metadata["mermaid.data.kind"] = row.Kind;
            target.Metadata["mermaid.data.accessibleKey"] = row.Accessible;
            target.Cells[0].SearchText = row.Accessible; target.Cells[0].CopyText = row.Accessible;
            target.Cells[1].Metadata["mermaid.data.kind"] = row.Kind;
            target.Cells[1].Metadata["mermaid.data.scalarText"] = row.Value;
        }
        return table;
    }
    private static void Visit(GeoJsonValue value, string path, string pointer, int depth,
        List<(string Key, string Accessible, string Pointer, string Kind, string Value)> rows, MermaidSourceSpan span) {
        MermaidResourceGuard.Checkpoint(); MermaidResourceGuard.CheckDepth(depth, span);
        if (value.TryAsObject(out var properties)) {
            if (properties.Count == 0) Add(path, path, pointer, "Object", "{}");
            foreach (var property in properties.OrderBy(pair => pair.Value.PropertyOffset))
                Visit(property.Value, path.Length == 0 ? property.Key : path + "." + property.Key,
                    pointer + "/" + MermaidEventModelingData.Escape(property.Key), depth + 1, rows, span);
        } else if (value.TryAsArray(out var items)) {
            if (items.Count == 0) Add(path, path, pointer, "Array", "[]");
            else if (items.All(item => !item.TryAsArray(out _) && !item.TryAsObject(out _))) {
                for (var index = 0; index < items.Count; index++) Scalar(items[index], index == 0 ? path : "", path,
                    pointer + "/" + index.ToString(CultureInfo.InvariantCulture));
            } else for (var index = 0; index < items.Count; index++) Visit(items[index], path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                pointer + "/" + index.ToString(CultureInfo.InvariantCulture), depth + 1, rows, span);
        } else Scalar(value, path, path, pointer);
        void Scalar(GeoJsonValue scalar, string key, string accessible, string location) {
            var text = scalar.AsOptionalString();
            Add(key, accessible, location, scalar.IsNull ? "Null" : scalar.IsString ? "String" : scalar.RawValue is bool ? "Boolean" : "Number", scalar.IsNull ? "null" : text ?? "");
        }
        void Add(string key, string accessible, string location, string kind, string text) {
            foreach (var fact in new[] { key, accessible, location, kind, text }) MermaidEventModelingData.CheckPortableFactText(fact, span);
            if (rows.Count >= VisualArtifactInterchangeValidation.MaximumTableRows) throw new ArgumentException("UseCase JSON exceeds the finite embedded table row limit.");
            rows.Add((key, accessible, location.Length == 0 ? "/" : location, kind, text));
        }
    }
}
