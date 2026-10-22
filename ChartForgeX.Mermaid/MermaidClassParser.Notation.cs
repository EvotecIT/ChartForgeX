using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidClassParser {
    private static readonly Regex Lollipop = new(@"^(?<source>\S+)\s*(?<connector>\(\)--|--\(\))\s*(?<target>\S+?)(?:\s*:\s*(?<label>.*))?$", RegexOptions.CultureInvariant);
    private static readonly Regex Link = new(@"^(?:link\s+(?<id>\S+)|click\s+(?<id>\S+)\s+href)\s+""(?<url>[^""]*)""(?:\s+""(?<tooltip>[^""]*)"")?(?:\s+_(?:blank|self|parent|top))?\s*;?$", RegexOptions.CultureInvariant);

    private static bool TryNamespace(MermaidClassDocument document, string text, MermaidSourceSpan span,
        Stack<string> namespaces, Stack<MermaidSourceSpan> openings, MermaidParseResult<MermaidDocument> result) {
        if (!text.StartsWith("namespace ", StringComparison.Ordinal) || !text.EndsWith("{", StringComparison.Ordinal)) return false;
        var name = text.Substring(10, text.Length - 11).Trim();
        string? label = null;
        if (MermaidParserUtilities.TryBracketed(name, out var id, out var display, out _)) {
            name = id;
            label = MermaidParserUtilities.Unquote(display);
        }
        name = MermaidParserUtilities.Unquote(name);
        if (name.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Class namespace names must not be empty.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        var qualified = namespaces.Count == 0 ? name : namespaces.Peek() + "." + name;
        string? parent = null;
        foreach (var part in qualified.Split('.')) {
            if (part.Length == 0) {
                MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                    "Class namespace paths must not contain an empty segment.", MermaidDiagnosticCodes.InvalidStatement);
                return true;
            }
            var current = parent == null ? part : parent + "." + part;
            var existing = document.Namespaces.Find(item => item.Id == current);
            if (existing == null) document.Namespaces.Add(new MermaidClassNamespace(current, current == qualified ? label ?? part : part, span) { ParentId = parent });
            else if (current == qualified && label != null) existing.Label = label;
            parent = current;
        }
        MermaidResourceGuard.CheckDepth(qualified.Split('.').Length, span);
        namespaces.Push(qualified);
        openings.Push(span);
        return true;
    }

    private static bool TryLink(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var match = Link.Match(text);
        if (!match.Success) return false;
        document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
        if (!classes.TryGetValue(CleanEndpoint(match.Groups["id"].Value), out var target)) {
            MermaidParserUtilities.RetainUnsupported(document, text, span, result, "class link target not yet declared");
            return true;
        }
        target.Href = match.Groups["url"].Value;
        target.Tooltip = match.Groups["tooltip"].Success ? match.Groups["tooltip"].Value : null;
        return true;
    }

    private static bool TryLollipop(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        out MermaidClassRelationship? relationship) {
        var match = Lollipop.Match(text);
        relationship = null!;
        if (!match.Success) return false;
        var source = CleanEndpoint(match.Groups["source"].Value);
        var target = CleanEndpoint(match.Groups["target"].Value);
        if (MissingClassIdentity(source) || MissingClassIdentity(target)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "Class lollipop endpoints require nonempty identities.", MermaidDiagnosticCodes.InvalidStatement);
            return true;
        }
        relationship = new MermaidClassRelationship(source, target,
            match.Groups["connector"].Value, match.Groups["label"].Success ? match.Groups["label"].Value.Trim() : null, span);
        return true;
    }
}
