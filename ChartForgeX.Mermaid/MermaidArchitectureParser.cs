using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidArchitectureParser {
    private static readonly string[] EdgeOperators = { "<-->", "-->", "<--", "--" };

    public static void ParseStatements(MermaidArchitectureDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var groups = new Dictionary<string, MermaidArchitectureGroup>(StringComparer.Ordinal);
        var services = new Dictionary<string, MermaidArchitectureService>(StringComparer.Ordinal);
        var junctions = new Dictionary<string, MermaidArchitectureJunction>(StringComparer.Ordinal);
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (StartsWithKeyword(trimmed, "group")) {
                ParseGroup(document, groups, trimmed, span, result);
                continue;
            }

            if (StartsWithKeyword(trimmed, "service")) {
                ParseService(document, services, junctions, groups, trimmed, span, result);
                continue;
            }

            if (StartsWithKeyword(trimmed, "junction")) {
                ParseJunction(document, services, junctions, groups, trimmed, span, result);
                continue;
            }

            if (TryParseEdge(trimmed, span, out var edge)) {
                ValidateEndpoint(edge.Left, services, junctions, span, result);
                ValidateEndpoint(edge.Right, services, junctions, span, result);
                document.Edges.Add(edge);
                continue;
            }

            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, "Unrecognized architecture diagram statement was retained but not rendered exactly: " + trimmed);
        }

        if (document.Services.Count == 0 && document.Groups.Count == 0 && document.Junctions.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid architecture diagrams require at least one group, service, or junction.");
    }

    private static void ParseGroup(MermaidArchitectureDocument document, Dictionary<string, MermaidArchitectureGroup> groups, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!TryParseComponent(text.Substring(5).Trim(), out var id, out var icon, out var title, out var parentId)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture groups must use Mermaid syntax 'group id(icon)[Title]' or 'group id[Title]'.");
            return;
        }

        var group = new MermaidArchitectureGroup(id, title, span) { Icon = icon, ParentId = parentId };
        if (groups.ContainsKey(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture group id '" + id + "' is already defined.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(parentId) && !groups.ContainsKey(parentId!)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture group parent '" + parentId + "' must be declared before the child group.");
        }

        groups.Add(id, group);
        document.Groups.Add(group);
    }

    private static void ParseService(MermaidArchitectureDocument document, Dictionary<string, MermaidArchitectureService> services, Dictionary<string, MermaidArchitectureJunction> junctions, Dictionary<string, MermaidArchitectureGroup> groups, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (!TryParseComponent(text.Substring(7).Trim(), out var id, out var icon, out var title, out var parentId)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture services must use Mermaid syntax 'service id(icon)[Title]' or 'service id[Title]'.");
            return;
        }

        var service = new MermaidArchitectureService(id, title, span) { Icon = icon, GroupId = parentId };
        if (services.ContainsKey(id) || junctions.ContainsKey(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture service id '" + id + "' is already defined.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(parentId) && !groups.ContainsKey(parentId!)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture service group '" + parentId + "' must be declared before the service.");
        }

        services.Add(id, service);
        document.Services.Add(service);
    }

    private static void ParseJunction(MermaidArchitectureDocument document, Dictionary<string, MermaidArchitectureService> services, Dictionary<string, MermaidArchitectureJunction> junctions, Dictionary<string, MermaidArchitectureGroup> groups, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var body = text.Substring(8).Trim();
        var parentId = ReadParent(ref body);
        var id = Clean(body);
        if (id.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture junctions require an id.");
            return;
        }

        if (junctions.ContainsKey(id) || services.ContainsKey(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture junction id '" + id + "' is already defined.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(parentId) && !groups.ContainsKey(parentId!)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture junction group '" + parentId + "' must be declared before the junction.");
        }

        var junction = new MermaidArchitectureJunction(id, span) { GroupId = parentId };
        junctions.Add(id, junction);
        document.Junctions.Add(junction);
    }

    private static bool TryParseComponent(string body, out string id, out string? icon, out string title, out string? parentId) {
        id = string.Empty;
        icon = null;
        title = string.Empty;
        parentId = null;
        var bracketStart = body.IndexOf('[');
        var bracketEnd = body.LastIndexOf(']');
        if (bracketStart <= 0 || bracketEnd <= bracketStart) return false;
        parentId = ReadParentAfterTitle(ref body, bracketEnd);
        bracketStart = body.IndexOf('[');
        bracketEnd = body.LastIndexOf(']');
        title = MermaidParserUtilities.Unquote(body.Substring(bracketStart + 1, bracketEnd - bracketStart - 1));
        var prefix = body.Substring(0, bracketStart).Trim();
        if (prefix.EndsWith(")", StringComparison.Ordinal)) {
            var parenStart = prefix.LastIndexOf('(');
            if (parenStart <= 0) return false;
            id = Clean(prefix.Substring(0, parenStart));
            icon = Clean(prefix.Substring(parenStart + 1, prefix.Length - parenStart - 2));
        } else {
            id = Clean(prefix);
        }

        if (title.Length == 0) title = id;
        return id.Length > 0;
    }

    private static string? ReadParent(ref string body) {
        var marker = " in ";
        var index = body.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;
        var parent = Clean(body.Substring(index + marker.Length));
        body = body.Substring(0, index).Trim();
        return parent.Length == 0 ? null : parent;
    }

    private static string? ReadParentAfterTitle(ref string body, int titleEnd) {
        if (titleEnd >= body.Length - 1) return null;
        var suffix = body.Substring(titleEnd + 1).Trim();
        if (!suffix.StartsWith("in ", StringComparison.OrdinalIgnoreCase)) return null;
        var parent = Clean(suffix.Substring(3));
        if (parent.Length == 0) return null;
        body = body.Substring(0, titleEnd + 1).Trim();
        return parent;
    }

    private static bool TryParseEdge(string text, MermaidSourceSpan span, out MermaidArchitectureEdge edge) {
        edge = null!;
        foreach (var op in EdgeOperators) {
            var index = text.IndexOf(op, StringComparison.Ordinal);
            if (index <= 0) continue;
            var leftText = text.Substring(0, index).Trim();
            var rightText = text.Substring(index + op.Length).Trim();
            if (!TryParseEndpoint(leftText, rightSide: false, out var left) || !TryParseEndpoint(rightText, rightSide: true, out var right)) return false;
            edge = new MermaidArchitectureEdge(left, right, op, span);
            return true;
        }

        return false;
    }

    private static bool TryParseEndpoint(string text, bool rightSide, out MermaidArchitectureEndpoint endpoint) {
        endpoint = null!;
        var side = default(string);
        var body = text.Trim();
        if (rightSide) {
            var colon = body.IndexOf(':');
            if (colon > 0) {
                side = Clean(body.Substring(0, colon));
                body = body.Substring(colon + 1).Trim();
            }
        } else {
            var colon = body.LastIndexOf(':');
            if (colon > 0) {
                side = Clean(body.Substring(colon + 1));
                body = body.Substring(0, colon).Trim();
            }
        }

        var groupBoundary = body.EndsWith("{group}", StringComparison.OrdinalIgnoreCase);
        if (groupBoundary) body = body.Substring(0, body.Length - 7).Trim();
        var id = Clean(body);
        if (id.Length == 0) return false;
        endpoint = new MermaidArchitectureEndpoint(id) { Side = side, GroupBoundary = groupBoundary };
        return true;
    }

    private static void ValidateEndpoint(MermaidArchitectureEndpoint endpoint, Dictionary<string, MermaidArchitectureService> services, Dictionary<string, MermaidArchitectureJunction> junctions, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (services.ContainsKey(endpoint.Id) || junctions.ContainsKey(endpoint.Id)) return;
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Architecture edge endpoint '" + endpoint.Id + "' must refer to a previously declared service or junction.");
    }

    private static bool StartsWithKeyword(string text, string keyword) {
        if (!text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return text.Length == keyword.Length || char.IsWhiteSpace(text[keyword.Length]);
    }

    private static string Clean(string value) => MermaidParserUtilities.Unquote(value.Trim());
}
