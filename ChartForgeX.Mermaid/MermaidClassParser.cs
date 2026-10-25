using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidClassParser {
    public static void ParseStatements(MermaidClassDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var classes = new Dictionary<string, MermaidClassNode>(StringComparer.Ordinal);
        MermaidClassNode? activeClass = null;
        MermaidSourceSpan activeClassOpening = document.HeaderSpan;
        var namespaces = new Stack<string>();
        var namespaceOpenings = new Stack<MermaidSourceSpan>();
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);

            if (activeClass != null) {
                if (trimmed == "}") {
                    activeClass = null;
                    continue;
                }

                AddMember(activeClass, trimmed, span);
                continue;
            }

            if (TryNamespace(document, trimmed, span, namespaces, namespaceOpenings, result)) continue;
            if (trimmed == "}") {
                if (namespaces.Count > 0) { namespaces.Pop(); namespaceOpenings.Pop(); }
                else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Class diagram closing brace has no open class or namespace.");
                continue;
            }

            if (MermaidParserUtilities.TryReadDirection(trimmed, span, result, out var direction)) {
                if (namespaces.Count == 0) document.Direction = direction;
                else MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "namespace direction");
                continue;
            }

            if (MermaidParserUtilities.StartsStatement(trimmed, "note")) {
                if (TryNote(document, classes, trimmed, span, result, out var note)) document.Notes.Add(note!);
                else MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "class note syntax");
                continue;
            }

            if (TryLollipop(trimmed, span, result, out var lollipop)) {
                if (lollipop == null) continue;
                document.Relationships.Add(lollipop);
                var concrete = EnsureClass(document, classes, lollipop.SourceIsInterface
                    ? lollipop.TargetIdentity ?? lollipop.TargetId : lollipop.SourceIdentity ?? lollipop.SourceId, span, result);
                RetainGenericReference(document, null, lollipop.SourceIsInterface ? lollipop.SourceId : lollipop.TargetId,
                    lollipop.SourceIsInterface ? lollipop.SourceIdentity ?? lollipop.SourceId : lollipop.TargetIdentity ?? lollipop.TargetId, span, result);
                if (concrete != null && concrete.Namespace == null && namespaces.Count > 0) concrete.Namespace = namespaces.Peek();
                continue;
            }

            if (TryStyle(document, classes, trimmed, span, result)) continue;
            if (TryLink(document, classes, trimmed, span, result)) continue;

            if (MermaidParserUtilities.StartsStatement(trimmed, "click") ||
                MermaidParserUtilities.StartsStatement(trimmed, "link") || MermaidParserUtilities.StartsStatement(trimmed, "callback")) {
                document.StyleStatements.Add(new MermaidClassStyleStatement(trimmed, span));
                MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "class note or interaction");
                continue;
            }

            if (trimmed.StartsWith("classDef ", StringComparison.Ordinal) || trimmed.StartsWith("class ", StringComparison.Ordinal)) {
                if (trimmed.Substring(6).TrimStart().StartsWith("{", StringComparison.Ordinal)) {
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Class declarations require an identifier.");
                    continue;
                }
                var previousStyleCount = document.StyleStatements.Count;
                var previousRetainedCount = document.RawStatements.Count;
                ParseClassOrStyle(document, classes, trimmed, span, ref activeClass, result, out var declared);
                if (activeClass != null) activeClassOpening = span;
                if (document.StyleStatements.Count > previousStyleCount && document.RawStatements.Count == previousRetainedCount)
                    MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "class style");
                if (declared != null && namespaces.Count > 0) declared.Namespace = namespaces.Peek();
                continue;
            }

            if (trimmed.StartsWith("style ", StringComparison.Ordinal) || trimmed.StartsWith("linkStyle ", StringComparison.Ordinal) || trimmed.StartsWith("cssClass ", StringComparison.Ordinal)) {
                document.StyleStatements.Add(new MermaidClassStyleStatement(trimmed, span));
                MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "class style");
                continue;
            }

            if (trimmed.StartsWith("<<", StringComparison.Ordinal)) {
                ParseAnnotation(document, classes, trimmed, span, result);
                continue;
            }

            if (TryParseRelationship(trimmed, span, result, out var relationship)) {
                if (relationship == null) continue;
                document.Relationships.Add(relationship);
                var source = EnsureClass(document, classes, relationship.SourceIdentity ?? relationship.SourceId, span, result);
                var target = EnsureClass(document, classes, relationship.TargetIdentity ?? relationship.TargetId, span, result);
                if (namespaces.Count > 0) {
                    if (source != null) source.Namespace ??= namespaces.Peek();
                    if (target != null) target.Namespace ??= namespaces.Peek();
                }
                continue;
            }

            var colonIndex = MemberSeparator(trimmed);
            if (colonIndex > 0) {
                var classId = trimmed.Substring(0, colonIndex).Trim();
                var member = trimmed.Substring(colonIndex + 1).Trim();
                if (classId.Length > 0 && member.Length > 0) {
                    var owner = EnsureClass(document, classes, classId, span, result);
                    if (owner == null) continue;
                    if (owner.Namespace == null && namespaces.Count > 0) owner.Namespace = namespaces.Peek();
                    AddMember(owner, member, span);
                }
                continue;
            }

            MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "unrecognized class statement");
            document.StyleStatements.Add(new MermaidClassStyleStatement(trimmed, span));
        }

        if (activeClass != null) Unclosed(activeClassOpening);
        foreach (var opening in namespaceOpenings) Unclosed(opening);
        foreach (var note in document.Notes) if (note.NodeId != null && !classes.ContainsKey(note.NodeId))
            MermaidParserUtilities.Add(result, note.Span, MermaidDiagnosticSeverity.Error, "Class notes must reference a declared class or relationship endpoint.", MermaidDiagnosticCodes.InvalidStatement);
        if (document.Classes.Count == 0 && document.Relationships.Count == 0 && document.Namespaces.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid class diagrams require at least one class, namespace or relationship.");
        ApplyStyles(document);
        DiagnoseClassCaptions(document, result);

        void Unclosed(MermaidSourceSpan opening) => MermaidParserUtilities.Add(result, opening, MermaidDiagnosticSeverity.Error,
            "Class definitions and namespaces must close with '}'.");
    }

    private static void ParseClassOrStyle(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes, string text, MermaidSourceSpan span,
        ref MermaidClassNode? activeClass, MermaidParseResult<MermaidDocument> result, out MermaidClassNode? declared) {
        declared = null;
        var body = ReadInlineClass(text.Substring(6).Trim(), out var style);
        if (style != null && (style.Length == 0 || style.IndexOf(":::", StringComparison.Ordinal) >= 0)) {
            InvalidClass(result, span, "Class inline styling requires one name after ':::'.");
            return;
        }
        var position = 0;
        if (!ReadIdentity(body, ref position, span, result, out var identity)) return;
        var tail = body.Substring(position).Trim();
        string? label = null;
        if (tail.StartsWith("[", StringComparison.Ordinal)) {
            if (!ReadAlias(tail, out var display, out var remainder)) {
                InvalidClass(result, span, "Class aliases require a complete bracketed label.");
                return;
            }
            label = display;
            tail = remainder.Trim();
        }
        if (tail.Length > 0 && !tail.StartsWith("{", StringComparison.Ordinal)) {
            if ("~,]}\"`".IndexOf(tail[0]) >= 0) InvalidClass(result, span, "Class declarations require one identity with balanced delimiters.");
            else document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
            return;
        }
        declared = EnsureClass(document, classes, identity, span, result);
        if (declared == null) return;
        if (label != null) { declared.Label = label; document.CaptionSpans[declared.Id] = span; }
        if (style != null) declared.Classes.Add(style);
        if (tail.Length == 0) return;
        activeClass = declared;
        var member = tail.Substring(1).Trim();
        if (member.EndsWith("}", StringComparison.Ordinal)) {
            member = member.Substring(0, member.Length - 1).Trim();
            if (member.Length > 0) AddMember(activeClass, member, span);
            activeClass = null;
        } else if (member.Length > 0) AddMember(activeClass, member, span);
    }

    private static void ParseAnnotation(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes, string text, MermaidSourceSpan span,
        MermaidParseResult<MermaidDocument> result) {
        var end = text.IndexOf(">>", StringComparison.Ordinal);
        if (end <= 2) return;
        var annotation = text.Substring(2, end - 2).Trim();
        var id = text.Substring(end + 2).Trim();
        if (id.Length == 0) return;
        EnsureClass(document, classes, id, span, result)?.Annotations.Add(annotation);
    }

    private static bool TryParseRelationship(string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        out MermaidClassRelationship? relationship) => TryReadRelationship(text, span, result, false, out relationship);

    private static string CleanEndpoint(string value) {
        value = value.Trim();
        if (value.Length >= 2 && (value[0] == '`' || value[0] == '"') && value[value.Length - 1] == value[0])
            return value.Substring(1, value.Length - 2);
        return MermaidNotationText.ClassId(value);
    }

    private static bool MissingClassIdentity(string id) => string.IsNullOrWhiteSpace(id);

    private static MermaidClassNode? EnsureClass(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes, string id,
        MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var authored = id.Trim();
        var position = 0;
        if (!ReadIdentity(authored, ref position, span, result, out _)) return null;
        if (position != authored.Length) {
            InvalidClass(result, span, "Class references require one complete identity.");
            return null;
        }
        var missingAliasIdentity = id.TrimStart().StartsWith("[", StringComparison.Ordinal);
        id = CleanEndpoint(id);
        if (missingAliasIdentity || MissingClassIdentity(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "Class declarations require an identity before aliases, generics or inline classes.", MermaidDiagnosticCodes.InvalidStatement);
            return null;
        }
        if (!classes.TryGetValue(id, out var node)) {
            node = new MermaidClassNode(id, span);
            classes.Add(id, node);
            document.Classes.Add(node);
        }
        RetainGenericReference(document, node, id, authored, span, result);
        return node;
    }

    private static void RetainGenericReference(MermaidClassDocument document, MermaidClassNode? node, string id, string authored,
        MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var genericReference = authored.Length > 0 && authored[0] != '`' && authored[0] != '"' && authored.IndexOf('~') > 0;
        if (genericReference) {
            document.GenericDeclarations[id] = authored;
            if (authored.IndexOf(',') >= 0) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning,
                "Balanced comma generics are a ChartForgeX extension; Mermaid 12.2 does not support comma generic arguments.", MermaidDiagnosticCodes.ApproximateMapping);
        }
        // Forward notes may retain a reference before the declared class exists. Apply its stored
        // type caption when that owner arrives, without repeating the occurrence's warning or replacing an alias.
        if (node != null && !document.CaptionSpans.ContainsKey(id) && (genericReference || node.Label == null)
            && document.GenericDeclarations.TryGetValue(id, out var generic))
            node.Label = MermaidNotationText.Generics(generic);
    }

    private static void AddMember(MermaidClassNode node, string text, MermaidSourceSpan span) {
        if (text.StartsWith("<<", StringComparison.Ordinal) && text.EndsWith(">>", StringComparison.Ordinal)) {
            node.Annotations.Add(text.Substring(2, text.Length - 4).Trim());
            return;
        }

        node.Members.Add(new MermaidClassMember(text, text.IndexOf("(", StringComparison.Ordinal) >= 0 && text.IndexOf(")", StringComparison.Ordinal) > text.IndexOf("(", StringComparison.Ordinal), span));
    }
}
