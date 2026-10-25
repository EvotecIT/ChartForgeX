using System;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateTreeView(VisualArtifactInterchangeEnvelope envelope) {
        var tree = envelope.TreeView!;
        Defined(tree.ThemeMode, "tree-view theme mode", envelope);
        if (tree.Theme == null) throw new ArgumentException("Tree-view presentation requires a native theme.", nameof(envelope));
        if (tree.Theme.ToJson().Length > 1024 * 1024) throw new ArgumentException("Tree-view theme JSON exceeds the native theme input limit.", nameof(envelope));
        if (envelope.Width.HasValue != envelope.Height.HasValue) throw new ArgumentException("Tree-view frames require both dimensions or neither.", nameof(envelope));
        foreach (var dimension in new[] { envelope.Width, envelope.Height })
            if (dimension.HasValue && (dimension.Value > int.MaxValue || Math.Truncate(dimension.Value) != dimension.Value))
                throw new ArgumentOutOfRangeException(nameof(envelope), "Tree-view frames require positive whole native pixels.");
        if (envelope.Groups.Count != 0 || envelope.Nodes.Count != 0 || envelope.Edges.Count != 0 || envelope.Annotations.Count != 0 || envelope.Scenarios.Count != 0)
            throw new ArgumentException("Tree-view semantics belong to native directory rows, not generic graph entities.", nameof(envelope));
        if (envelope.Presentation != null)
            throw new ArgumentException("Tree-view presentation belongs to its typed native payload.", nameof(envelope));
        Count(tree.Items.Count, VisualBlockRendering.MaximumTreeViewItems, "tree-view rows");
        if (tree.Items.Count == 0) throw new ArgumentException("Tree views require at least one row.", nameof(envelope));
        foreach (var item in tree.Items) {
            if (item == null) throw new ArgumentException("Tree-view rows must not be null.", nameof(envelope));
            RequiredId(item.Id, "tree-view row id");
            if (item.ParentId != null) RequiredId(item.ParentId, "tree-view parent id");
            Text(item.Label, "tree-view label"); Text(item.Description, "tree-view description");
            OptionalText(item.IconReference, "tree-view icon reference");
            foreach (var name in item.Classes) Text(name, "tree-view class");
        }
        try {
            // Reuse native enum, spacing, padding, PNG-scale, preorder, ID, depth and measured-frame invariants.
            var block = VisualArtifactInterchangeMapping.CreateTreeViewBlock(envelope);
            var measured = block.MeasureNaturalSize(envelope.Width.HasValue ? (int)envelope.Width.Value : 520,
                envelope.Height.HasValue ? (int)envelope.Height.Value : 0);
            if (envelope.Height.HasValue && measured.Height > envelope.Height.Value)
                throw new ArgumentException("The portable tree frame is smaller than its measured native rows.", nameof(envelope));
        } catch (InvalidOperationException exception) {
            throw new ArgumentException("Invalid native tree-view semantics: " + exception.Message, nameof(envelope), exception);
        }
    }
}
