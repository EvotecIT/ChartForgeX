using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public static void ValidateJourneyDiagram(JourneyDiagramBlock block) {
        if (block.Tasks.Count == 0 || block.Tasks.Count > 512) throw new InvalidOperationException("Journeys require one through 512 tasks.");
        if (block.Sections.Count > 128) throw new InvalidOperationException("Journeys allow no more than 128 sections.");
        var sections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in block.Sections) DiagramIdentity(sections, section.Id, section.Label, "journey section");
        var tasks = new HashSet<string>(StringComparer.Ordinal);
        var actors = new HashSet<string>(StringComparer.Ordinal);
        foreach (var task in block.Tasks) {
            DiagramIdentity(tasks, task.Id, task.Label, "journey task");
            if (!IsFinite(task.Score) || task.Score < 1 || task.Score > 5) throw new InvalidOperationException("Journey scores must be finite values from one through five.");
            if (task.SectionId != null && !sections.Contains(task.SectionId)) throw new InvalidOperationException("Unknown journey section: " + task.SectionId + ".");
            var membership = new HashSet<string>(StringComparer.Ordinal);
            foreach (var actor in task.Actors) {
                if (string.IsNullOrWhiteSpace(actor) || !membership.Add(actor)) throw new InvalidOperationException("Journey task actors must be non-empty and unique.");
                actors.Add(actor);
            }
        }
        if (actors.Count > 64) throw new InvalidOperationException("Journeys allow no more than 64 distinct actors.");
    }

    internal static void DiagramIdentity(HashSet<string> identities, string id, string label, string kind) {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(label)) throw new InvalidOperationException("A " + kind + " requires a non-empty identity and label.");
        if (!identities.Add(id)) throw new InvalidOperationException("Duplicate " + kind + " identity: " + id + ".");
    }

    internal static IReadOnlyList<string> JourneyActors(JourneyDiagramBlock block) {
        var result = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var task in block.Tasks) foreach (var actor in task.Actors) if (seen.Add(actor)) result.Add(actor);
        return result;
    }
}
