using System;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidSequenceParser {
    private static void DiagnoseCaptions(MermaidSequenceDocument document, MermaidParseResult<MermaidDocument> result) {
        foreach (var participant in document.Participants) MermaidLabelText.Diagnose(participant.Alias, participant.Span, result, "Sequence");
        foreach (var message in document.Messages) MermaidLabelText.Diagnose(message.Text, message.Span, result, "Sequence");
        foreach (var note in document.Notes) MermaidLabelText.Diagnose(note.Text, note.Span, result, "Sequence");
        foreach (var block in document.Blocks) MermaidLabelText.Diagnose(block.Text, block.Span, result, "Sequence");
        foreach (var box in document.ParticipantBoxes) MermaidLabelText.Diagnose(box.Label, box.Span, result, "Sequence");
    }

    // A Mermaid named/decimal character escape owns its semicolon inside a compact statement.
    private static bool IsEntityTerminator(string line, int index) {
        var start = index - 1;
        while (start >= 0 && (line[start] >= 'a' && line[start] <= 'z' || line[start] >= 'A' && line[start] <= 'Z' || line[start] >= '0' && line[start] <= '9')) start--;
        if (start < 0 || line[start] != '#' || start + 1 == index) return false;
        var numeric = char.IsDigit(line[start + 1]);
        for (var cursor = start + 1; cursor < index; cursor++) if (char.IsDigit(line[cursor]) != numeric) return false;
        return true;
    }
}
