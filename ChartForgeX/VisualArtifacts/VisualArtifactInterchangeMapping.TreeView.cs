using System;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Rebuilds editable native directory rows from validated portable semantics and presentation.</summary>
    /// <remarks>Import creates a new block and row collection; immutable row values and themes may be shared safely.
    /// Source language and extension metadata remain on the envelope. After editing rows or typography, call
    /// TreeViewBlock.WithNaturalSize before rendering. No SVG fallback, CSS, icon pack or filesystem is consulted.</remarks>
    public static TreeViewBlock ToTreeViewBlock(this VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        envelope.Validate();
        RequireEditableSourcePresentation(envelope);
        if (envelope.Family != VisualArtifactInterchangeFamily.TreeView || envelope.TreeView == null)
            throw new NotSupportedException("Only a typed tree-view envelope can rebuild directory rows.");
        var block = CreateTreeViewBlock(envelope);
        if (!envelope.Width.HasValue) block.WithNaturalSize();
        return block;
    }

    // Validation and reconstruction share the same native object builder; native measurement owns graph and frame limits.
    internal static TreeViewBlock CreateTreeViewBlock(VisualArtifactInterchangeEnvelope envelope) {
        var source = envelope.TreeView!;
        var block = TreeViewBlock.Create().WithTitle(envelope.Title).WithSubtitle(envelope.Subtitle)
            .WithTheme(source.Theme, source.ThemeMode).WithTransparentBackground(source.TransparentBackground)
            .WithCard(source.ShowCard).WithPngOutputScale(source.PngOutputScale);
        block.Options.Padding = source.Padding; block.Options.HostOwnsFrame = source.HostOwnsFrame;
        block.ShowIcons = source.ShowIcons; block.RowIndent = source.RowIndent; block.RowPadding = source.RowPadding;
        block.Options.Accessibility.Name = envelope.AccessibleName;
        block.Options.Accessibility.Description = envelope.AccessibleDescription;
        block.Options.Accessibility.Language = envelope.Language;
        block.Options.Accessibility.IsDecorative = envelope.IsDecorative;
        foreach (var item in source.Items) block.AddItem(item);
        if (envelope.Width.HasValue) block.Options.Size = new ChartSize((int)envelope.Width.Value, (int)envelope.Height!.Value);
        return block;
    }

    private static void MapTreeView(VisualArtifactInterchangeEnvelope envelope, TreeViewBlock block) {
        var source = block.Options;
        envelope.Family = VisualArtifactInterchangeFamily.TreeView;
        envelope.Title = block.Title; envelope.Subtitle = block.Subtitle;
        envelope.TreeView = new VisualArtifactInterchangeTreeViewArtifact {
            Theme = source.Theme, ThemeMode = source.ThemeMode, Padding = source.ConfiguredPadding,
            TransparentBackground = source.TransparentBackground, ShowCard = source.ShowCard,
            HostOwnsFrame = source.HostOwnsFrame, PngOutputScale = source.PngOutputScale,
            ShowIcons = block.ShowIcons, RowIndent = block.RowIndent, RowPadding = block.RowPadding
        };
        envelope.TreeView.Items.AddRange(block.Items);
        envelope.Width = source.Size.Width; envelope.Height = source.Size.Height;
        // Artifact alternatives remain authoritative; model alternatives only fill absent common fields.
        envelope.AccessibleName ??= source.Accessibility.Name;
        envelope.AccessibleDescription ??= source.Accessibility.Description;
        envelope.Language ??= source.Accessibility.Language;
        envelope.IsDecorative |= source.Accessibility.IsDecorative;
    }
}
