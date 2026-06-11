using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static class MermaidC4Parser {
    public static void ParseStatements(MermaidC4Document document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var elements = new Dictionary<string, MermaidC4Element>(StringComparer.Ordinal);
        var boundaries = new Dictionary<string, MermaidC4Boundary>(StringComparer.Ordinal);
        var boundaryStack = new Stack<string>();

        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);
            document.Statements.Add(new MermaidRawStatement(trimmed, span));

            if (trimmed == "}") {
                if (boundaryStack.Count == 0) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 boundary closing brace has no matching opening boundary.");
                else boundaryStack.Pop();
                continue;
            }

            if (TryParseTitle(document, trimmed)) continue;
            if (TryParseDirection(document, trimmed)) continue;
            if (!TryParseCall(trimmed, out var call)) {
                Retain(document, result, trimmed, span, "Unrecognized C4 statement was retained but not rendered exactly: ");
                continue;
            }

            if (IsBoundary(call.Name)) {
                ParseBoundary(document, boundaries, call, span, boundaryStack, result);
                continue;
            }

            if (IsElement(call.Name)) {
                ParseElement(document, elements, call, span, boundaryStack, result);
                continue;
            }

            if (IsRelationship(call.Name)) {
                ParseRelationship(document, elements, call, span, result);
                continue;
            }

            if (IsRetainedCall(call.Name)) {
                Retain(document, result, trimmed, span, "C4 statement was retained but is not rendered exactly yet: ");
                continue;
            }

            Retain(document, result, trimmed, span, "Unrecognized C4 statement was retained but not rendered exactly: ");
        }

        if (boundaryStack.Count > 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "C4 boundary block was not closed.");
        if (document.Elements.Count == 0 && document.Boundaries.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid C4 diagrams require at least one element or boundary.");
    }

    private static bool TryParseTitle(MermaidC4Document document, string text) {
        if (!text.StartsWith("title ", StringComparison.OrdinalIgnoreCase)) return false;
        document.Title = MermaidParserUtilities.Unquote(text.Substring(6));
        return true;
    }

    private static bool TryParseDirection(MermaidC4Document document, string text) {
        if (!text.StartsWith("direction ", StringComparison.OrdinalIgnoreCase)) return false;
        document.Direction = text.Substring(10).Trim();
        return true;
    }

    private static void ParseBoundary(MermaidC4Document document, Dictionary<string, MermaidC4Boundary> boundaries, CallStatement call, MermaidSourceSpan span, Stack<string> boundaryStack, MermaidParseResult<MermaidDocument> result) {
        if (call.Arguments.Count < 2) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 boundaries require at least alias and label arguments.");
            return;
        }

        var id = Clean(call.Arguments[0]);
        var label = Clean(call.Arguments[1]);
        if (id.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 boundary aliases cannot be empty.");
            return;
        }

        if (boundaries.ContainsKey(id)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 boundary alias '" + id + "' is already defined.");
            return;
        }

        var boundary = new MermaidC4Boundary(id, label.Length == 0 ? id : label, NormalizeKind(call.Name), span) {
            Type = call.Arguments.Count > 2 ? Clean(call.Arguments[2]) : null,
            ParentId = boundaryStack.Count == 0 ? null : boundaryStack.Peek()
        };
        boundaries.Add(id, boundary);
        document.Boundaries.Add(boundary);
        if (call.OpensBlock) boundaryStack.Push(id);
    }

    private static void ParseElement(MermaidC4Document document, Dictionary<string, MermaidC4Element> elements, CallStatement call, MermaidSourceSpan span, Stack<string> boundaryStack, MermaidParseResult<MermaidDocument> result) {
        if (call.Arguments.Count < 2) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 elements require at least alias and label arguments.");
            return;
        }

        var alias = Clean(call.Arguments[0]);
        var label = Clean(call.Arguments[1]);
        if (alias.Length == 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 element aliases cannot be empty.");
            return;
        }

        if (elements.ContainsKey(alias)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 element alias '" + alias + "' is already defined.");
            return;
        }

        var kind = NormalizeKind(call.Name);
        var containerLike = kind.StartsWith("container", StringComparison.Ordinal) || kind.StartsWith("component", StringComparison.Ordinal);
        var element = new MermaidC4Element(alias, label.Length == 0 ? alias : label, kind, span) {
            Technology = containerLike ? OptionalArg(call.Arguments, 2, 0, "techn", "technology") : null,
            Description = containerLike ? OptionalArg(call.Arguments, 2, 1, "descr", "description") : OptionalArg(call.Arguments, 2, 0, "descr", "description"),
            Sprite = OptionalArg(call.Arguments, 2, containerLike ? 2 : 1, "sprite"),
            Tags = OptionalArg(call.Arguments, 2, containerLike ? 3 : 2, "tags"),
            Link = OptionalArg(call.Arguments, 2, containerLike ? 4 : 3, "link"),
            BoundaryId = boundaryStack.Count == 0 ? null : boundaryStack.Peek()
        };

        elements.Add(alias, element);
        document.Elements.Add(element);
    }

    private static void ParseRelationship(MermaidC4Document document, Dictionary<string, MermaidC4Element> elements, CallStatement call, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (call.Arguments.Count < 3) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 relationships require source, target, and label arguments.");
            return;
        }

        var source = Clean(call.Arguments[0]);
        var target = Clean(call.Arguments[1]);
        if (!elements.ContainsKey(source)) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 relationship source '" + source + "' must refer to a previously declared element.");
        if (!elements.ContainsKey(target)) MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "C4 relationship target '" + target + "' must refer to a previously declared element.");

        document.Relationships.Add(new MermaidC4Relationship(source, target, NormalizeKind(call.Name), span) {
            Label = Arg(call.Arguments, 2),
            Technology = OptionalArg(call.Arguments, 3, 0, "techn", "technology"),
            Tags = OptionalArg(call.Arguments, 3, 1, "tags"),
            Link = OptionalArg(call.Arguments, 3, 2, "link")
        });
    }

    private static bool TryParseCall(string text, out CallStatement call) {
        call = default;
        var opensBlock = text.EndsWith("{", StringComparison.Ordinal);
        if (opensBlock) text = text.Substring(0, text.Length - 1).TrimEnd();
        var open = text.IndexOf('(');
        var close = text.LastIndexOf(')');
        if (open <= 0 || close <= open) return false;
        var name = text.Substring(0, open).Trim();
        if (name.Length == 0) return false;
        var args = MermaidParserUtilities.SplitCsvLike(text.Substring(open + 1, close - open - 1));
        call = new CallStatement(name, args, opensBlock);
        return true;
    }

    private static void Retain(MermaidC4Document document, MermaidParseResult<MermaidDocument> result, string text, MermaidSourceSpan span, string prefix) {
        document.RetainedStatements.Add(new MermaidRawStatement(text, span));
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, prefix + text);
    }

    private static bool IsBoundary(string name) {
        var normalized = NormalizeName(name);
        return normalized == "enterpriseboundary" || normalized == "systemboundary" || normalized == "containerboundary" || normalized == "boundary" || normalized == "deploymentnode" || normalized == "node" || normalized == "nodel" || normalized == "noder";
    }

    private static bool IsElement(string name) {
        var normalized = NormalizeName(name);
        return normalized == "person" || normalized == "personext" || normalized == "system" || normalized == "systemdb" || normalized == "systemqueue" || normalized == "systemext" || normalized == "systemdbext" || normalized == "systemqueueext" || normalized == "container" || normalized == "containerdb" || normalized == "containerqueue" || normalized == "containerext" || normalized == "containerdbext" || normalized == "containerqueueext" || normalized == "component" || normalized == "componentdb" || normalized == "componentqueue" || normalized == "componentext" || normalized == "componentdbext" || normalized == "componentqueueext";
    }

    private static bool IsRelationship(string name) {
        var normalized = NormalizeName(name);
        return normalized == "rel" || normalized == "birel" || normalized == "relu" || normalized == "relup" || normalized == "reld" || normalized == "reldown" || normalized == "rell" || normalized == "relleft" || normalized == "relr" || normalized == "relright" || normalized == "relback";
    }

    private static bool IsRetainedCall(string name) {
        var normalized = NormalizeName(name);
        return normalized == "relindex" || normalized == "updateelementstyle" || normalized == "updaterelstyle" || normalized == "updatelayoutconfig" || normalized.StartsWith("lay", StringComparison.Ordinal) || normalized.StartsWith("show", StringComparison.Ordinal) || normalized.StartsWith("hide", StringComparison.Ordinal);
    }

    private static string NormalizeKind(string name) {
        var normalized = NormalizeName(name);
        return normalized.EndsWith("ext", StringComparison.Ordinal) ? normalized.Substring(0, normalized.Length - 3) + "_external" : normalized;
    }

    private static string NormalizeName(string name) {
        var chars = new List<char>(name.Length);
        foreach (var ch in name) if (char.IsLetterOrDigit(ch)) chars.Add(char.ToLowerInvariant(ch));
        return new string(chars.ToArray());
    }

    private static string? Arg(IReadOnlyList<string> args, int index) {
        if (index >= args.Count) return null;
        var value = Clean(args[index]);
        return value.Length == 0 ? null : value;
    }

    private static string? OptionalArg(IReadOnlyList<string> args, int firstOptionalIndex, int optionalIndex, params string[] names) {
        for (var index = 0; index < names.Length; index++) {
            var named = NamedArg(args, firstOptionalIndex, names[index]);
            if (named != null) return named;
        }

        var position = 0;
        for (var index = firstOptionalIndex; index < args.Count; index++) {
            if (TrySplitNamedArgument(args[index], out _, out _)) continue;
            if (position == optionalIndex) return Arg(args, index);
            position++;
        }

        return null;
    }

    private static string? NamedArg(IReadOnlyList<string> args, int firstOptionalIndex, string name) {
        var normalized = NormalizeName(name);
        for (var index = firstOptionalIndex; index < args.Count; index++) {
            if (!TrySplitNamedArgument(args[index], out var candidate, out var value)) continue;
            if (!string.Equals(NormalizeName(candidate), normalized, StringComparison.Ordinal)) continue;
            var clean = Clean(value);
            return clean.Length == 0 ? null : clean;
        }

        return null;
    }

    private static string Clean(string value) {
        var clean = value.Trim();
        if (TrySplitNamedArgument(clean, out _, out var namedValue)) clean = namedValue.Trim();
        else if (clean.StartsWith("$", StringComparison.Ordinal)) clean = clean.Substring(1);
        return MermaidParserUtilities.Unquote(clean);
    }

    private static bool TrySplitNamedArgument(string value, out string name, out string rawValue) {
        name = string.Empty;
        rawValue = string.Empty;
        var clean = value.Trim();
        if (!clean.StartsWith("$", StringComparison.Ordinal)) return false;
        var equal = clean.IndexOf('=');
        if (equal <= 1) return false;
        name = clean.Substring(1, equal - 1).Trim();
        rawValue = clean.Substring(equal + 1).Trim();
        return name.Length > 0;
    }

    private readonly struct CallStatement {
        public CallStatement(string name, IReadOnlyList<string> arguments, bool opensBlock) {
            Name = name;
            Arguments = arguments;
            OpensBlock = opensBlock;
        }

        public string Name { get; }

        public IReadOnlyList<string> Arguments { get; }

        public bool OpensBlock { get; }
    }
}
