using System;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Rebuilds a new editable native timeline from validated semantics and presentation.</summary>
    /// <remarks>Import detaches mutable host collections and options. Immutable native records and themes may be
    /// shared safely. Source language, span facts and caller metadata remain on the envelope. No SVG fallback,
    /// calendar interpretation, source parser or external resource is consulted.</remarks>
    public static TimelineDiagramBlock ToTimelineDiagramBlock(this VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        envelope.Validate();
        RequireEditableSourcePresentation(envelope);
        if (envelope.Family != VisualArtifactInterchangeFamily.Timeline || envelope.Timeline == null)
            throw new NotSupportedException("Only a typed timeline envelope can rebuild native periods.");
        return CreateTimelineDiagramBlock(envelope);
    }

    internal static TimelineDiagramBlock CreateTimelineDiagramBlock(VisualArtifactInterchangeEnvelope envelope) {
        var source = envelope.Timeline!;
        var block = TimelineDiagramBlock.Create().WithTitle(envelope.Title).WithSubtitle(envelope.Subtitle)
            .WithTheme(source.Theme, source.ThemeMode).WithTransparentBackground(source.TransparentBackground)
            .WithCard(source.ShowCard).WithPngOutputScale(source.PngOutputScale);
        block.Options.Padding = source.Padding; block.Options.HostOwnsFrame = source.HostOwnsFrame;
        block.Options.Accessibility.Name = envelope.AccessibleName;
        block.Options.Accessibility.Description = envelope.AccessibleDescription;
        block.Options.Accessibility.Language = envelope.Language;
        block.Options.Accessibility.IsDecorative = envelope.IsDecorative;
        block.Direction = source.Direction;
        foreach (var section in source.Sections) block.AddSection(section.Id, section.Label);
        foreach (var item in source.Periods) block.AddPeriod(item.Id, item.Label, item.Events, item.SectionId);
        if (envelope.Width.HasValue) block.Options.Size = new ChartSize((int)envelope.Width.Value, (int)envelope.Height!.Value);
        return block;
    }

    private static void MapTimeline(VisualArtifactInterchangeEnvelope envelope, TimelineDiagramBlock block) {
        var source = block.Options;
        envelope.Family = VisualArtifactInterchangeFamily.Timeline;
        envelope.Title = block.Title; envelope.Subtitle = block.Subtitle;
        envelope.Timeline = new VisualArtifactInterchangeTimelineArtifact {
            Theme = source.Theme, ThemeMode = source.ThemeMode, Padding = source.ConfiguredPadding,
            TransparentBackground = source.TransparentBackground, ShowCard = source.ShowCard,
            HostOwnsFrame = source.HostOwnsFrame, PngOutputScale = source.PngOutputScale, Direction = block.Direction
        };
        envelope.Timeline.Sections.AddRange(block.Sections); envelope.Timeline.Periods.AddRange(block.Periods);
        envelope.Width = source.Size.Width; envelope.Height = source.Size.Height;
        envelope.AccessibleName ??= source.Accessibility.Name;
        envelope.AccessibleDescription ??= source.Accessibility.Description;
        envelope.Language ??= source.Accessibility.Language;
        envelope.IsDecorative |= source.Accessibility.IsDecorative;
    }
}
