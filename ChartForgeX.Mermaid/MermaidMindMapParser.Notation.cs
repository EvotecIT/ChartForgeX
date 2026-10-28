using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidMindMapParser {
    private static readonly string[] Openers = { "(-", "((", "{{", "[", "(", ")" };
    private static readonly string[] Closers = { "-)", "))", "}}", "]", ")", "(" };
    private static readonly string[] Shapes = { "cloud", "circle", "hexagon", "square", "rounded", "bang" };

    private static MermaidMindMapNode? ParseNode(string text, int index, int level, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result, HashSet<MermaidMindMapNode> nativeIconNodes) {
        var shape = "default"; string? sourceId = null; var labelEnd = text.Length; var labelStart = 0; var tail = text.Length;
        var quote = '\0';
        for (var offset = 0; offset < text.Length; offset++) {
            if ((offset & 255) == 0) MermaidResourceGuard.Checkpoint();
            if (Quoted(text, offset, ref quote)) continue;
            if (IsModifier(text, offset)) { labelEnd = tail = offset; break; }
            var delimiter = -1;
            for (var token = 0; token < Openers.Length; token++) if (At(text, offset, Openers[token])) { delimiter = token; break; }
            if (delimiter < 0) continue;
            sourceId = text.Substring(0, offset).Trim(); if (sourceId.Length == 0) sourceId = null;
            shape = Shapes[delimiter]; labelStart = offset + Openers[delimiter].Length;
            var closing = Closing(text, labelStart, Closers[delimiter]);
            if (closing < 0) { Invalid(span, result, "Mindmap shape delimiters must be balanced; quote literal delimiters inside captions."); return null; }
            labelEnd = closing; tail = closing + Closers[delimiter].Length;
            break;
        }
        var label = text.Substring(labelStart, labelEnd - labelStart).Trim();
        // Double quotes quote a source caption; a backtick wrapper belongs to Markdown lowering.
        if (label.Length >= 2 && label[0] == '"' && label[label.Length - 1] == '"') label = label.Substring(1, label.Length - 2);
        if (string.IsNullOrWhiteSpace(MermaidLabelText.DisplayCaption(label))) { Invalid(span, result, "Mindmap captions must not be empty."); return null; }
        var node = new MermaidMindMapNode(MermaidParserUtilities.StableId("mindmap-node", index), label, level, span, sourceId) { Shape = shape };
        MermaidInlineCaption.Diagnose(label, MermaidFlowchartText.AtOffset(text, span, labelStart, labelEnd - labelStart), result, "Mindmap", markdownByDefault: true);
        if (!ReadModifiers(text, tail, node, span, result, nativeIconNodes)) return null;
        return node;
    }

    private static int Closing(string text, int start, string close) {
        var quote = '\0';
        for (var offset = start; offset < text.Length; offset++) {
            if ((offset & 255) == 0) MermaidResourceGuard.Checkpoint();
            if (!Quoted(text, offset, ref quote) && At(text, offset, close)) return offset;
        }
        return -1;
    }

    private static bool Quoted(string text, int offset, ref char quote) {
        var ch = text[offset];
        if (quote != '\0') {
            if (ch == quote && !MermaidFlowchartText.IsEscaped(text, offset)) quote = '\0';
            return true;
        }
        if ((ch == '"' || ch == '`') && !MermaidFlowchartText.IsEscaped(text, offset)) { quote = ch; return true; }
        return false;
    }

    private static bool At(string text, int offset, string token) => offset + token.Length <= text.Length
        && string.CompareOrdinal(text, offset, token, 0, token.Length) == 0;
    private static bool IsModifier(string text, int offset) => At(text, offset, ":::") || At(text, offset, "::icon(");
}
