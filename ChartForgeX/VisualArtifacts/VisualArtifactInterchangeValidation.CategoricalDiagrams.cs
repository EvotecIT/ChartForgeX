using System;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateJourney(VisualArtifactInterchangeEnvelope envelope) {
        var source = envelope.Journey!;
        ValidateNativeCategoricalFrame(envelope, source.Theme, source.ThemeMode);
        Count(source.Sections.Count, 128, "journey sections"); Count(source.Tasks.Count, 512, "journey tasks");
        foreach (var section in source.Sections) {
            if (section == null) throw new ArgumentException("Journey sections must not be null.", nameof(envelope));
            RequiredId(section.Id, "journey section id"); Text(section.Label, "journey section label");
        }
        foreach (var task in source.Tasks) {
            if (task == null) throw new ArgumentException("Journey tasks must not be null.", nameof(envelope));
            RequiredId(task.Id, "journey task id"); Text(task.Label, "journey task label");
            if (task.SectionId != null) RequiredId(task.SectionId, "journey section reference");
            Count(task.Actors.Count, 64, "journey task actors"); foreach (var actor in task.Actors) Text(actor, "journey actor");
        }
        try { VisualBlockRendering.ValidateJourneyDiagram(VisualArtifactInterchangeMapping.CreateJourneyDiagramBlock(envelope)); }
        catch (InvalidOperationException error) { throw new ArgumentException("Invalid native journey semantics: " + error.Message, nameof(envelope), error); }
    }

    private static void ValidateTimeline(VisualArtifactInterchangeEnvelope envelope) {
        var source = envelope.Timeline!;
        ValidateNativeCategoricalFrame(envelope, source.Theme, source.ThemeMode);
        Defined(source.Direction, "timeline direction", envelope);
        Count(source.Sections.Count, 128, "timeline sections"); Count(source.Periods.Count, 512, "timeline periods");
        foreach (var section in source.Sections) {
            if (section == null) throw new ArgumentException("Timeline sections must not be null.", nameof(envelope));
            RequiredId(section.Id, "timeline section id"); Text(section.Label, "timeline section label");
        }
        var events = 0;
        foreach (var period in source.Periods) {
            if (period == null) throw new ArgumentException("Timeline periods must not be null.", nameof(envelope));
            RequiredId(period.Id, "timeline period id"); Text(period.Label, "timeline period label");
            if (period.SectionId != null) RequiredId(period.SectionId, "timeline section reference");
            if (period.Events.Count > 2048 - events) throw new ArgumentException("Timeline events exceed the native limit.", nameof(envelope));
            events += period.Events.Count; foreach (var item in period.Events) Text(item, "timeline event");
        }
        try { VisualBlockRendering.ValidateTimelineDiagram(VisualArtifactInterchangeMapping.CreateTimelineDiagramBlock(envelope)); }
        catch (InvalidOperationException error) { throw new ArgumentException("Invalid native timeline semantics: " + error.Message, nameof(envelope), error); }
    }

    private static void ValidateNativeCategoricalFrame(VisualArtifactInterchangeEnvelope envelope, ChartForgeX.Themes.VisualTheme theme,
        ChartForgeX.Themes.VisualThemeMode mode) {
        if (theme == null) throw new ArgumentException("Categorical diagrams require a native theme.", nameof(envelope));
        Defined(mode, "categorical diagram theme mode", envelope);
        if (theme.ToJson().Length > 1024 * 1024) throw new ArgumentException("Categorical theme JSON exceeds the native input limit.", nameof(envelope));
        if (envelope.Width.HasValue != envelope.Height.HasValue) throw new ArgumentException("Categorical frames require both dimensions or neither.", nameof(envelope));
        foreach (var dimension in new[] { envelope.Width, envelope.Height })
            if (dimension.HasValue && (dimension.Value > int.MaxValue || Math.Truncate(dimension.Value) != dimension.Value))
                throw new ArgumentOutOfRangeException(nameof(envelope), "Categorical frames require positive whole native pixels.");
        if (envelope.Presentation != null || envelope.Groups.Count != 0 || envelope.Nodes.Count != 0 || envelope.Edges.Count != 0 || envelope.Annotations.Count != 0 || envelope.Scenarios.Count != 0)
            throw new ArgumentException("Categorical semantics and presentation belong to their typed native payload, not graph entities.", nameof(envelope));
    }
}
