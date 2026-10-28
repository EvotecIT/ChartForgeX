using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    internal const int MaximumTreeViewItems = 10000;
    private const int MaximumTreeViewDepth = 128;
    private const int MaximumTreeViewExtent = 8192;

    internal static void ValidateTreeView(TreeViewBlock block) {
        if (block.Items.Count == 0 || block.Items.Count > MaximumTreeViewItems)
            throw new InvalidOperationException("Tree views require 1 through 10000 rows.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ancestors = new List<TreeViewItem>();
        foreach (var item in block.Items) {
            if (!seen.Add(item.Id)) throw new InvalidOperationException("Tree view row IDs must be unique: " + item.Id + ".");
            if (item.ParentId == null) ancestors.Clear();
            else {
                while (ancestors.Count > 0 && ancestors[ancestors.Count - 1].Id != item.ParentId) ancestors.RemoveAt(ancestors.Count - 1);
                if (ancestors.Count == 0) throw new InvalidOperationException("Tree view parents must precede contiguous descendants: " + item.Id + ".");
                if (ancestors[ancestors.Count - 1].Kind == TreeViewItemKind.File) throw new InvalidOperationException("A file row cannot have children: " + item.ParentId + ".");
            }
            if (ancestors.Count >= MaximumTreeViewDepth) throw new InvalidOperationException("Tree views support at most 128 levels.");
            ancestors.Add(item);
        }
    }

    internal static ChartSize TreeViewNaturalSize(TreeViewBlock block, int width, int minimumHeight) {
        ValidateTreeView(block);
        if (width < 1 || width > MaximumTreeViewExtent) throw new ArgumentOutOfRangeException(nameof(width));
        if (minimumHeight < 0 || minimumHeight > MaximumTreeViewExtent) throw new ArgumentOutOfRangeException(nameof(minimumHeight));
        var padding = block.Options.Padding;
        var rows = TreeViewRows(block, width - padding.Left - padding.Right);
        var theme = block.Options.ResolvedTheme;
        double Heading(string text, double size, int weight, double gap) => text.Length == 0 ? 0 :
            TextLayoutEngine.Measure(SingleLineText(text), new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = weight }, FontSize = size, LineHeight = 1 }).Height + gap;
        var headings = Heading(block.Title, theme.TitleFontSize, 700, block.Subtitle.Length == 0 ? 15 : 7)
            + Heading(block.Subtitle, theme.SubtitleFontSize, 400, 13);
        var height = Math.Max(minimumHeight, Math.Ceiling(padding.Top + headings + rows[rows.Count - 1].Bottom + padding.Bottom));
        if (height > MaximumTreeViewExtent) throw new InvalidOperationException("The measured tree exceeds the 8192-pixel native frame. Split or filter its rows.");
        return new ChartSize(width, Math.Max(1, (int)height));
    }

    // Layout is shared by SVG marks and the native label scene. Text is never reduced to make more rows fit.
    internal static IReadOnlyList<TreeViewRowPlan> TreeViewRows(TreeViewBlock block, double width) {
        var theme = block.Options.ResolvedTheme;
        var depths = new Dictionary<string, int>(StringComparer.Ordinal);
        var parents = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in block.Items) if (item.ParentId != null) parents.Add(item.ParentId);
        var plans = new List<TreeViewRowPlan>(block.Items.Count);
        var y = 0.0;
        foreach (var item in block.Items) {
            var depth = item.ParentId == null ? 0 : depths[item.ParentId] + 1;
            depths.Add(item.Id, depth);
            var directory = item.Kind == TreeViewItemKind.Directory || item.Kind == TreeViewItemKind.Auto &&
                (item.Label.EndsWith("/", StringComparison.Ordinal) || parents.Contains(item.Id));
            var icon = item.Icon == TreeViewIcon.Auto ? block.ShowIcons ? directory ? TreeViewIcon.Folder : TreeViewIcon.File : TreeViewIcon.None : item.Icon;
            var label = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = directory ? 700 : 400 }, FontSize = BodyFontSize(theme, 13), LineHeight = 1 };
            var description = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Italic = true }, FontSize = BodyFontSize(theme, 12), LineHeight = 1 };
            var iconSize = Math.Max(12, label.FontSize);
            var x = depth * block.RowIndent + (icon == TreeViewIcon.None ? 0 : iconSize + 7);
            var available = width - x - 4;
            if (available < Math.Max(12, TextLayoutEngine.Measure("...", label).Width))
                throw new InvalidOperationException("Tree indentation and icons leave no readable caption width. Increase the width or split the tree.");
            var labelMetrics = TextLayoutEngine.Measure(SingleLineText(item.Label), label);
            var descriptionMetrics = TextLayoutEngine.Measure(SingleLineText(item.Description), description);
            var inline = item.Description.Length > 0 && labelMetrics.Width + 16 + descriptionMetrics.Width <= available;
            var labelHeight = Math.Max(labelMetrics.Height, icon == TreeViewIcon.None ? 0 : iconSize);
            var rowHeight = block.RowPadding * 2 + (item.Description.Length == 0 ? labelHeight : inline ? Math.Max(labelHeight, descriptionMetrics.Height) : labelHeight + 3 + descriptionMetrics.Height);
            plans.Add(new TreeViewRowPlan(item, depth, directory, icon, x, available, y, rowHeight, labelHeight, inline, iconSize, label, description));
            y += rowHeight;
        }
        return plans;
    }

    internal sealed class TreeViewRowPlan {
        internal TreeViewRowPlan(TreeViewItem item, int depth, bool directory, TreeViewIcon icon, double x, double width,
            double top, double height, double labelHeight, bool inlineDescription, double iconSize, TextStyle label, TextStyle description) {
            Item = item; Depth = depth; Directory = directory; Icon = icon; X = x; Width = width; Top = top; Height = height;
            LabelHeight = labelHeight; InlineDescription = inlineDescription; IconSize = iconSize; Label = label; Description = description;
        }
        internal TreeViewItem Item { get; }
        internal int Depth { get; }
        internal bool Directory { get; }
        internal TreeViewIcon Icon { get; }
        internal double X { get; }
        internal double Width { get; }
        internal double Top { get; }
        internal double Height { get; }
        internal double Bottom => Top + Height;
        internal double LabelHeight { get; }
        internal bool InlineDescription { get; }
        internal double IconSize { get; }
        internal TextStyle Label { get; }
        internal TextStyle Description { get; }
    }
}
