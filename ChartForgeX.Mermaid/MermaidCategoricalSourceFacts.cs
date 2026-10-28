using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Retains inert source locations beside categorical native semantics without coupling Core to a source parser.</summary>
internal static class MermaidCategoricalSourceFacts {
    internal static void Apply(VisualArtifact artifact, MermaidJourneyDocument document) {
        var facts = new List<(string Id, MermaidSourceSpan Span)>();
        for (var index = 0; index < document.Sections.Count; index++) facts.Add(("section-" + N(index), document.Sections[index].Span));
        for (var index = 0; index < document.Tasks.Count; index++) facts.Add(("task-" + N(index), document.Tasks[index].Span));
        Write(artifact, "journey", facts);
    }
    internal static void Apply(VisualArtifact artifact, MermaidTimelineDocument document) {
        var facts = new List<(string Id, MermaidSourceSpan Span)>();
        for (var index = 0; index < document.Sections.Count; index++) facts.Add(("section-" + N(index), document.Sections[index].Span));
        for (var index = 0; index < document.Periods.Count; index++) {
            var period = document.Periods[index]; facts.Add(("period-" + N(index), period.Span));
            for (var item = 0; item < period.Events.Count; item++) facts.Add(("period-" + N(index) + "/event-" + N(item), period.Events[item].Span));
        }
        Write(artifact, "timeline", facts);
    }
    private static void Write(VisualArtifact artifact, string family, IEnumerable<(string Id, MermaidSourceSpan Span)> facts) {
        // Each line is stable native identity|UTF-16 source line|column|length. Chunking preserves
        // complete facts within the existing extension-value limit; no source is evaluated on load.
        var text = new StringBuilder(); var chunk = 0;
        foreach (var fact in facts) {
            var line = fact.Id + "|" + N(fact.Span.Line) + "|" + N(fact.Span.Column) + "|" + N(fact.Span.Length) + "\n";
            if (text.Length + line.Length > 60000) { Save(); text.Clear(); }
            text.Append(line);
        }
        if (text.Length > 0) Save();
        void Save() { artifact.Metadata["mermaid." + family + ".sourceSpans." + N(chunk++)] = text.ToString(); }
    }
    private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
}
