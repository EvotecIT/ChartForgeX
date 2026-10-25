using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidMindMapParser {
    private static bool ReadModifiers(string text, int offset, MermaidMindMapNode node, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, HashSet<MermaidMindMapNode> nativeIconNodes) {
        while (offset < text.Length) {
            while (offset < text.Length && char.IsWhiteSpace(text[offset])) offset++;
            if (offset == text.Length) return true;
            var start = offset;
            if (At(text, offset, "::icon(")) {
                var end = text.IndexOf(')', offset + 7);
                if (end < 0) return Fail("Mindmap icon modifiers require a closing parenthesis.");
                var icon = text.Substring(offset + 7, end - offset - 7).Trim();
                if (icon.Length == 0 || icon.IndexOf('(') >= 0) return Fail("Mindmap icon modifiers require one nonempty icon token.");
                var located = MermaidFlowchartText.AtOffset(text, span, start, end + 1 - start);
                MermaidResourceGuard.ReserveElements(1, located);
                node.Icons.Add(icon);
                if (MermaidMindMapNotation.BuiltInIcon(icon) == null)
                    Warn(located, "Mindmap icon '" + icon + "' is retained as metadata; only explicit qualified built-in catalog ids paint native icons.", MermaidDiagnosticCodes.UnsupportedStatement);
                else if (!nativeIconNodes.Add(node))
                    Warn(located, "Mindmap nodes paint at most the first resolved built-in icon; additional authored markers remain metadata.", MermaidDiagnosticCodes.ApproximateMapping);
                offset = end + 1;
            } else if (At(text, offset, ":::")) {
                offset += 3; var count = 0;
                while (offset < text.Length) {
                    while (offset < text.Length && char.IsWhiteSpace(text[offset])) offset++;
                    if (offset == text.Length || IsModifier(text, offset)) break;
                    var word = offset;
                    while (offset < text.Length && !char.IsWhiteSpace(text[offset]) && !IsModifier(text, offset)) offset++;
                    var item = text.Substring(word, offset - word);
                    var located = MermaidFlowchartText.AtOffset(text, span, word, item.Length);
                    MermaidResourceGuard.ReserveElements(1, located);
                    node.Classes.Add(item); count++;
                    Warn(located, item == "highlight" ? "Mindmap :::highlight maps to native selected-node stroke emphasis."
                        : "Mindmap class '" + item + "' is retained as metadata; external CSS is not applied.",
                        item == "highlight" ? MermaidDiagnosticCodes.ApproximateMapping : MermaidDiagnosticCodes.UnsupportedStatement);
                }
                if (count == 0) return Fail("Mindmap class modifiers require at least one class name.");
            } else return Fail("Only class or icon modifiers may follow a mindmap shape caption.");
        }
        return true;

        bool Fail(string message) {
            Invalid(MermaidFlowchartText.AtOffset(text, span, offset: 0, length: text.Length), result, message);
            return false;
        }
        void Warn(MermaidSourceSpan located, string message, string code) => MermaidParserUtilities.Add(result, located, MermaidDiagnosticSeverity.Warning, message, code);
    }
}
