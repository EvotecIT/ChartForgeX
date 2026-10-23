using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Shares declaration parsing and native paint validation, without choosing a family's grammar or cascade.</summary>
internal static class MermaidStyleParser {
    internal static bool TryDefinition(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        List<MermaidStyleDefinition> definitions, string family) {
        if (!MermaidParserUtilities.StartsStatement(text, "classDef")) return false;
        var body = text.Substring(8).Trim();
        if (!SplitHead(body, out var head, out var declarations)) {
            Invalid("Named style definitions require a name and declarations.");
            return true;
        }
        var names = Names(head);
        if (names.Count == 0) { Invalid("Named style definitions require a name and declarations."); return true; }
        MermaidResourceGuard.ReserveElements(names.Count, span);
        var styles = new Dictionary<string, string>();
        Read(styles, declarations, span, result, family);
        MermaidResourceGuard.ReserveElements(checked((long)names.Count * styles.Count), span);
        foreach (var name in names) {
            MermaidResourceGuard.Checkpoint();
            var definition = new MermaidStyleDefinition(name, span);
            Copy(definition.Styles, styles);
            definitions.Add(definition);
        }
        return true;

        void Invalid(string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message,
            MermaidDiagnosticCodes.InvalidStatement);
    }

    /// <summary>Splits a selector/name from its value without splitting quoted identifiers.</summary>
    internal static bool SplitHead(string body, out string head, out string tail) {
        var quote = '\0';
        for (var index = 0; index < body.Length; index++) {
            var character = body[index];
            if (quote != '\0') {
                if (character == quote) quote = '\0';
            } else if (character == '"' || character == '`') quote = character;
            else if (char.IsWhiteSpace(character)) {
                head = body.Substring(0, index).Trim();
                tail = body.Substring(index + 1).Trim().TrimEnd(';').TrimEnd();
                return head.Length > 0 && tail.Length > 0;
            }
        }
        head = body;
        tail = string.Empty;
        return false;
    }

    /// <summary>Finds an inline class suffix outside quoted identifiers and labels.</summary>
    internal static int Suffix(string text) {
        var quote = '\0';
        var brackets = 0;
        for (var index = 0; index + 2 < text.Length; index++) {
            var character = text[index];
            if (quote != '\0') {
                if (character == quote) quote = '\0';
                continue;
            }
            if (character == '"' || character == '`') quote = character;
            else if (character == '[') brackets++;
            else if (character == ']') brackets--;
            else if (brackets == 0 && character == ':' && text[index + 1] == ':' && text[index + 2] == ':') return index;
        }
        return -1;
    }

    internal static Dictionary<string, string> Resolve(Dictionary<string, Dictionary<string, string>> definitions,
        IEnumerable<string> classes, Dictionary<string, string> direct, MermaidSourceSpan span, bool includeDefault = true) {
        // Count the union before cloning. Direct model properties are already retained;
        // named/default properties newly expanded onto this target consume the same policy.
        var keys = new HashSet<string>(direct.Keys, StringComparer.Ordinal);
        var selected = new List<Dictionary<string, string>>();
        if (includeDefault && definitions.TryGetValue("default", out var defaults)) Add(defaults);
        foreach (var name in classes) {
            MermaidResourceGuard.Checkpoint();
            if (definitions.TryGetValue(name, out var style)) Add(style);
        }
        MermaidResourceGuard.ReserveElements(keys.Count - direct.Count, span);
        var resolved = new Dictionary<string, string>();
        foreach (var style in selected) Copy(resolved, style);
        Copy(resolved, direct);
        return resolved;

        void Add(Dictionary<string, string> style) {
            selected.Add(style);
            foreach (var key in style.Keys) { MermaidResourceGuard.Checkpoint(); keys.Add(key); }
        }
    }

    internal static void Read(Dictionary<string, string> styles, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result, string family, bool edge = false) {
        foreach (var declaration in SplitDeclarations(text)) {
            var split = declaration.IndexOf(':');
            if (split <= 0) {
                Unsupported(family + " style declaration is retained without native application: " + declaration + ".");
                continue;
            }
            var property = declaration.Substring(0, split).Trim().ToLowerInvariant();
            var value = declaration.Substring(split + 1).Trim();
            styles[property] = value;
            if ((property == "stroke" || !edge && (property == "fill" || property == "color"))
                && MermaidStyleValues.IsColor(value)) continue;
            if (edge && property == "stroke-dasharray" && MermaidStyleValues.TryDash(value, out var dashed)) {
                if (dashed) Unsupported(family + " stroke-dasharray is retained; native rendering uses its shared dashed pattern.");
                continue;
            }
            Unsupported(family + " style '" + property + "' is retained without native application.");
        }

        void Unsupported(string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
            message, MermaidDiagnosticCodes.UnsupportedStatement);
    }

    internal static Dictionary<string, Dictionary<string, string>> Definitions(IEnumerable<MermaidStyleDefinition> definitions) {
        var resolved = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var definition in definitions) {
            if (!resolved.TryGetValue(definition.Name, out var styles))
                resolved[definition.Name] = styles = new Dictionary<string, string>();
            Copy(styles, definition.Styles);
        }
        return resolved;
    }

    internal static void Copy(Dictionary<string, string> target, Dictionary<string, string> source, MermaidSourceSpan? span = null) {
        if (span.HasValue) {
            long added = 0;
            foreach (var key in source.Keys) { MermaidResourceGuard.Checkpoint(); if (!target.ContainsKey(key)) added++; }
            MermaidResourceGuard.ReserveElements(added, span.Value);
        }
        foreach (var item in source) { MermaidResourceGuard.Checkpoint(); target[item.Key] = item.Value; }
    }

    internal static List<string> Names(string text) {
        var names = new List<string>();
        foreach (var name in text.Trim().TrimEnd(';').Split(',')) {
            var value = MermaidParserUtilities.Unquote(name.Trim());
            if (value.Length > 0) names.Add(value);
        }
        return names;
    }

    internal static List<string> SplitDeclarations(string text) {
        var declarations = new List<string>();
        var current = new System.Text.StringBuilder();
        var depth = 0;
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var ch = text[index];
            if (ch == '\\' && index + 1 < text.Length && (text[index + 1] == ',' || text[index + 1] == ';')) {
                current.Append(text[++index]);
                continue;
            }
            if (quote != '\0') {
                current.Append(ch);
                if (ch == quote) quote = '\0';
            } else if (ch == '\'' || ch == '"') { quote = ch; current.Append(ch); }
            else if (ch == '(') { depth++; current.Append(ch); }
            else if (ch == ')') { depth--; current.Append(ch); }
            else if (depth == 0 && (ch == ',' || ch == ';')) Add();
            else current.Append(ch);
        }
        Add();
        return declarations;

        void Add() {
            var declaration = current.ToString().Trim();
            if (declaration.Length > 0) declarations.Add(declaration);
            current.Clear();
        }
    }
}
