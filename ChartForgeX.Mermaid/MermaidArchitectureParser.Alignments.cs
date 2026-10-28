using System;
using System.Collections.Generic;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidArchitectureParser {
    private static void ParseAlignment(MermaidArchitectureDocument document, string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var position = 5;
        var direction = Word();
        var kind = direction == "row" ? TopologyAlignmentKind.Row : TopologyAlignmentKind.Column;
        if (direction != "row" && direction != "column") { Invalid("Architecture align requires 'row' or 'column'."); return; }
        MermaidResourceGuard.ReserveElements(1, span);
        var members = new List<string>();
        string id;
        while ((id = Word()).Length > 0) {
            if (!ValidId(id)) { Invalid("Architecture alignment members require service/junction identifiers."); return; }
            if (id.Length > VisualArtifactInterchangeValidation.MaximumIdCharacters)
                throw new MermaidResourceLimitException("Interchange.MaximumIdCharacters", VisualArtifactInterchangeValidation.MaximumIdCharacters, id.Length, span);
            MermaidResourceGuard.ReserveElements(1, span); MermaidArchitectureAlignment.CheckMemberCount(members.Count + 1, span); members.Add(id);
        }
        if (members.Count < 2) { Invalid("Architecture align requires at least two members."); return; }
        document.Alignments.Add(new MermaidArchitectureAlignment(kind, members, span));

        string Word() {
            while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
            var start = position;
            while (position < text.Length && !char.IsWhiteSpace(text[position])) position++;
            return text.Substring(start, position - start);
        }
        void Invalid(string message) => MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidStatement);
    }

    private static bool ValidId(string id) {
        if (id.Length == 0 || !Word(id[0]) || !Word(id[id.Length - 1])) return false;
        foreach (var value in id) if (value != '-' && !Word(value)) return false;
        return true;
        bool Word(char value) => value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_';
    }

    private static void ValidateAlignments(MermaidArchitectureDocument document, IReadOnlyDictionary<string, MermaidArchitectureService> services,
        IReadOnlyDictionary<string, MermaidArchitectureJunction> junctions, MermaidParseResult<MermaidDocument> result) {
        foreach (var alignment in document.Alignments) {
            var members = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in alignment.Members) {
                MermaidResourceGuard.Checkpoint();
                if (!members.Add(id)) MermaidParserUtilities.Add(result, alignment.Span, MermaidDiagnosticSeverity.Error,
                    "Architecture alignment must not repeat a member.", MermaidDiagnosticCodes.InvalidStatement);
                if (!services.ContainsKey(id) && !junctions.ContainsKey(id)) MermaidParserUtilities.Add(result, alignment.Span, MermaidDiagnosticSeverity.Error,
                    "Architecture alignment member must be an existing service or junction.", MermaidDiagnosticCodes.InvalidStatement);
            }
        }
    }
}
