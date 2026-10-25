using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidClassParser {
    private static readonly Regex LinkTail = new(@"^""(?<url>[^""]*)""(?:\s+""(?<tooltip>[^""]*)"")?(?:\s+_(?:blank|self|parent|top))?\s*;?$", RegexOptions.CultureInvariant);

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
            else if (current == qualified && label != null) {
                existing.Label = label;
                document.NamespaceCaptionSpans[current] = span;
            }
            parent = current;
        }
        MermaidResourceGuard.CheckDepth(qualified.Split('.').Length, span);
        namespaces.Push(qualified);
        openings.Push(span);
        return true;
    }

    private static bool TryLink(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var click = MermaidParserUtilities.StartsStatement(text, "click");
        if (!click && !MermaidParserUtilities.StartsStatement(text, "link")) return false;
        var position = click ? 5 : 4;
        if (!ReadIdentity(text, ref position, span, result, out var identity)) return true;
        var tail = text.Substring(position).Trim();
        if (click) {
            if (!MermaidParserUtilities.StartsStatement(tail, "href")) return false;
            tail = tail.Substring(4).TrimStart();
        }
        var match = LinkTail.Match(tail);
        if (!match.Success) return false;
        document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
        if (!classes.TryGetValue(CleanEndpoint(identity), out var target)) {
            MermaidParserUtilities.RetainUnsupported(document, text, span, result, "class link target not yet declared");
            return true;
        }
        RetainGenericReference(document, target, target.Id, identity, span, result);
        target.Href = match.Groups["url"].Value;
        target.Tooltip = match.Groups["tooltip"].Success ? match.Groups["tooltip"].Value : null;
        return true;
    }

    private static bool TryLollipop(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        out MermaidClassRelationship? relationship) => TryReadRelationship(text, span, result, true, out relationship);
}
