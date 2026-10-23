using System;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Rebuilds an editable native railroad block from validated portable rules and presentation.</summary>
    /// <param name="envelope">A railroad-family interchange envelope. Metadata and source-language identity remain on this envelope.</param>
    /// <returns>A new native block and rule collection; immutable expressions and theme values may be shared safely.</returns>
    /// <remarks>This imports semantics, not executable grammar or source text. Editing rules may require
    /// <see cref="RailroadDiagramBlock.WithNaturalSize"/> before rendering. No rendered fallback is consulted.</remarks>
    public static RailroadDiagramBlock ToRailroadDiagramBlock(this VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        envelope.Validate();
        if (envelope.Family != VisualArtifactInterchangeFamily.Railroad || envelope.Railroad == null)
            throw new NotSupportedException("Only a typed railroad envelope can rebuild a railroad diagram.");
        var source = envelope.Railroad;
        var block = RailroadDiagramBlock.Create().WithTitle(envelope.Title).WithSubtitle(envelope.Subtitle)
            .WithTheme(source.Theme, source.ThemeMode).WithTransparentBackground(source.TransparentBackground)
            .WithCard(source.ShowCard).WithPngOutputScale(source.PngOutputScale);
        block.RuleComparison = source.RuleComparison; block.Options.Padding = source.Padding;
        block.Options.HostOwnsFrame = source.HostOwnsFrame;
        block.Options.Accessibility.Name = envelope.AccessibleName;
        block.Options.Accessibility.Description = envelope.AccessibleDescription;
        block.Options.Accessibility.Language = envelope.Language;
        block.Options.Accessibility.IsDecorative = envelope.IsDecorative;
        foreach (var rule in source.Rules) block.AddRule(rule.Id, rule.Expression, rule.Label);
        if (envelope.Width.HasValue) block.Options.Size = new ChartSize((int)envelope.Width.Value, (int)envelope.Height!.Value);
        else block.WithNaturalSize();
        return block;
    }

    private static void MapRailroad(VisualArtifactInterchangeEnvelope envelope, RailroadDiagramBlock block) {
        if (block.Rules.Count == 0 || block.Rules.Count > RailroadDiagramLimits.MaximumRules)
            throw new ArgumentException("Railroad interchange requires one through 512 native rules.", nameof(block));
        var source = block.Options;
        envelope.Family = VisualArtifactInterchangeFamily.Railroad;
        envelope.Title = block.Title; envelope.Subtitle = block.Subtitle;
        envelope.Railroad = new VisualArtifactInterchangeRailroadArtifact {
            RuleComparison = block.RuleComparison, Theme = source.Theme, ThemeMode = source.ThemeMode,
            Padding = source.Padding, TransparentBackground = source.TransparentBackground, ShowCard = source.ShowCard,
            HostOwnsFrame = source.HostOwnsFrame, PngOutputScale = source.PngOutputScale
        };
        envelope.Railroad.Rules.AddRange(block.Rules);
        envelope.Width = source.Size.Width; envelope.Height = source.Size.Height;
        // Artifact envelope edits remain authoritative. Native model values fill only absent alternatives.
        envelope.AccessibleName ??= source.Accessibility.Name;
        envelope.AccessibleDescription ??= source.Accessibility.Description;
        envelope.Language ??= source.Accessibility.Language;
        envelope.IsDecorative |= source.Accessibility.IsDecorative;
    }
}
