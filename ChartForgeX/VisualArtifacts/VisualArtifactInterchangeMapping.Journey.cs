using System;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Rebuilds a new editable native journey from validated semantics and presentation.</summary>
    /// <remarks>Import detaches mutable host collections and options. Immutable native records and themes may be
    /// shared safely. Source language, span facts and caller metadata remain on the envelope. No SVG fallback,
    /// calendar interpretation, source parser or external resource is consulted.</remarks>
    public static JourneyDiagramBlock ToJourneyDiagramBlock(this VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        envelope.Validate();
        RequireEditableSourcePresentation(envelope);
        if (envelope.Family != VisualArtifactInterchangeFamily.Journey || envelope.Journey == null)
            throw new NotSupportedException("Only a typed journey envelope can rebuild native tasks.");
        return CreateJourneyDiagramBlock(envelope);
    }

    internal static JourneyDiagramBlock CreateJourneyDiagramBlock(VisualArtifactInterchangeEnvelope envelope) {
        var source = envelope.Journey!;
        var block = JourneyDiagramBlock.Create().WithTitle(envelope.Title).WithSubtitle(envelope.Subtitle)
            .WithTheme(source.Theme, source.ThemeMode).WithTransparentBackground(source.TransparentBackground)
            .WithCard(source.ShowCard).WithPngOutputScale(source.PngOutputScale);
        block.Options.Padding = source.Padding; block.Options.HostOwnsFrame = source.HostOwnsFrame;
        block.Options.Accessibility.Name = envelope.AccessibleName;
        block.Options.Accessibility.Description = envelope.AccessibleDescription;
        block.Options.Accessibility.Language = envelope.Language;
        block.Options.Accessibility.IsDecorative = envelope.IsDecorative;

        foreach (var section in source.Sections) block.AddSection(section.Id, section.Label);
        foreach (var item in source.Tasks) block.AddTask(item.Id, item.Label, item.Score, item.Actors, item.SectionId);
        if (envelope.Width.HasValue) block.Options.Size = new ChartSize((int)envelope.Width.Value, (int)envelope.Height!.Value);
        return block;
    }

    private static void MapJourney(VisualArtifactInterchangeEnvelope envelope, JourneyDiagramBlock block) {
        var source = block.Options;
        envelope.Family = VisualArtifactInterchangeFamily.Journey;
        envelope.Title = block.Title; envelope.Subtitle = block.Subtitle;
        envelope.Journey = new VisualArtifactInterchangeJourneyArtifact {
            Theme = source.Theme, ThemeMode = source.ThemeMode, Padding = source.ConfiguredPadding,
            TransparentBackground = source.TransparentBackground, ShowCard = source.ShowCard,
            HostOwnsFrame = source.HostOwnsFrame, PngOutputScale = source.PngOutputScale
        };
        envelope.Journey.Sections.AddRange(block.Sections); envelope.Journey.Tasks.AddRange(block.Tasks);
        envelope.Width = source.Size.Width; envelope.Height = source.Size.Height;
        envelope.AccessibleName ??= source.Accessibility.Name;
        envelope.AccessibleDescription ??= source.Accessibility.Description;
        envelope.Language ??= source.Accessibility.Language;
        envelope.IsDecorative |= source.Accessibility.IsDecorative;
    }
}
