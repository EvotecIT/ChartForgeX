using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidStateParser {
    /// <summary>Records assignments before resolving identities so forward styles do not manufacture states.</summary>
    private static bool TryReadStyle(MermaidStateDocument document, string text, MermaidSourceSpan span,
        List<StateStyleAssignment> assignments, MermaidParseResult<MermaidDocument> result) {
        var keyword = text.TrimEnd(';').TrimEnd();
        if (keyword is "classDef" or "class" or "style") {
            document.StyleStatements.Add(new MermaidRawStatement(text, span));
            MermaidResourceGuard.ReserveElements(1, span);
            Invalid("State style statements require state identities and a value.");
            return true;
        }
        if (MermaidStyleParser.TryDefinition(text, span, result, document.ClassDefinitions, "State")) {
            document.StyleStatements.Add(new MermaidRawStatement(text, span));
            MermaidResourceGuard.ReserveElements(1, span);
            return true;
        }
        var direct = MermaidParserUtilities.StartsStatement(text, "style");
        if (!direct && !MermaidParserUtilities.StartsStatement(text, "class")) return false;
        document.StyleStatements.Add(new MermaidRawStatement(text, span));
        MermaidResourceGuard.ReserveElements(1, span);
        if (!MermaidStyleParser.SplitHead(text.Substring(5).Trim(), out var selector, out var value)) {
            Invalid("State style statements require state identities and a value.");
            return true;
        }
        var targets = MermaidStyleParser.Names(selector);
        var classes = direct ? new List<string>() : MermaidStyleParser.Names(value);
        if (targets.Count == 0 || !direct && classes.Count == 0) {
            Invalid("State style statements require nonempty state identities and class names.");
            return true;
        }
        var styles = new Dictionary<string, string>();
        if (direct) MermaidStyleParser.Read(styles, value, span, result, "State");
        MermaidResourceGuard.ReserveElements((long)targets.Count * (direct ? styles.Count : classes.Count), span);
        assignments.Add(new StateStyleAssignment(new MermaidRawStatement(text, span), targets, classes, styles));
        return true;

        void Invalid(string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
            message, MermaidDiagnosticCodes.InvalidStatement);
    }

    private static bool ReadInlineClasses(ref string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result,
        out List<string> classes) {
        classes = new List<string>();
        var suffix = MermaidStyleParser.Suffix(text);
        if (suffix < 0) return true;
        var end = suffix + 3;
        while (end < text.Length && !char.IsWhiteSpace(text[end]) && text[end] != '{'
            && (text[end] != ':' || end + 1 < text.Length && text[end + 1] == ':')) end++;
        var value = text.Substring(suffix + 3, end - suffix - 3).TrimEnd(';');
        classes = MermaidStyleParser.Names(value);
        if (classes.Count == 0 || value.IndexOf(':') >= 0 || MermaidStyleParser.Suffix(text.Substring(end)) >= 0) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "Inline state classes require one nonempty ':::' suffix with comma-separated class names.", MermaidDiagnosticCodes.InvalidStatement);
            return false;
        }
        text = (text.Substring(0, suffix) + text.Substring(end)).Trim();
        return true;
    }

    private static void AddInlineAssignment(List<StateStyleAssignment> assignments, string id, List<string> classes, MermaidSourceSpan span) {
        if (classes.Count == 0) return;
        MermaidResourceGuard.ReserveElements(classes.Count, span);
        assignments.Add(new StateStyleAssignment(null, new List<string> { id }, classes, new Dictionary<string, string>()));
    }

    private static void ApplyStyles(MermaidStateDocument document, Dictionary<string, MermaidStateNode> states,
        List<StateStyleAssignment> assignments, MermaidParseResult<MermaidDocument> result) {
        foreach (var assignment in assignments) {
            MermaidResourceGuard.Checkpoint();
            var missing = false;
            foreach (var id in assignment.Targets) {
                if (!states.TryGetValue(id, out var state)) { missing = true; continue; }
                foreach (var name in assignment.Classes) state.Classes.Add(name);
                MermaidStyleParser.Copy(state.Styles, assignment.Styles);
            }
            if (missing && assignment.Statement is { } statement)
                MermaidParserUtilities.RetainUnsupported(document, statement.Text, statement.Span, result, "state style target not declared");
        }
    }

    private sealed class StateStyleAssignment {
        internal StateStyleAssignment(MermaidRawStatement? statement, List<string> targets, List<string> classes, Dictionary<string, string> styles) {
            Statement = statement; Targets = targets; Classes = classes; Styles = styles;
        }
        internal MermaidRawStatement? Statement { get; }
        internal List<string> Targets { get; }
        internal List<string> Classes { get; }
        internal Dictionary<string, string> Styles { get; }
    }
}
