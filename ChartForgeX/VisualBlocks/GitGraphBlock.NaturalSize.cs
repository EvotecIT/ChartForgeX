using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

public sealed partial class GitGraphBlock {
    /// <summary>Fits commit columns and branch lanes at the current typography within the configured width.</summary>
    /// <remarks>Call again after changing content, direction or typography. A later WithSize call selects an explicit frame.</remarks>
    public GitGraphBlock WithNaturalSize() {
        VisualBlockRendering.ValidateGitGraph(this);
        var heading = VisualBlockRendering.Headings(this);
        var padding = Options.Padding;
        var scale = Options.Theme.Typography.DataLabelSize / 12;
        var title = heading.TitleAdvance + heading.SubtitleAdvance;
        var horizontal = Direction == GitGraphDirection.LeftToRight;
        var width = horizontal ? Math.Min(Options.Size.Width, padding.Left + padding.Right + 150 * scale + Math.Max(1, Commits.Count - 1) * 88 * scale)
            : padding.Left + padding.Right + Branches.Count * 140 * scale;
        var height = padding.Top + padding.Bottom + title + (horizontal ? 72 * scale + Math.Max(0, Branches.Count - 1) * 68 * scale
            : 64 * scale + Math.Max(1, Commits.Count - 1) * 72 * scale);
        Options.Size = new ChartSize(Math.Max(1, (int)Math.Ceiling(width)), Math.Max(1, (int)Math.Ceiling(height)));
        return this;
    }
}
