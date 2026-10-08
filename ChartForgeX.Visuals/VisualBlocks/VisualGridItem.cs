using System;
using ChartForgeX.Core;

namespace ChartForgeX.VisualBlocks;

/// <summary>Describes one visual grid panel.</summary>
public sealed class VisualGridItem {
    private VisualGridItem(string? targetId, Chart? chart, IVisualBlock? block, int columnSpan, int rowSpan) {
        if (columnSpan <= 0) throw new ArgumentOutOfRangeException(nameof(columnSpan), columnSpan, "Column span must be positive.");
        if (rowSpan <= 0) throw new ArgumentOutOfRangeException(nameof(rowSpan), rowSpan, "Row span must be positive.");
        TargetId = targetId == null ? null : RequiredTargetId(targetId);
        Chart = chart;
        Block = block;
        ColumnSpan = columnSpan;
        RowSpan = rowSpan;
    }

    /// <summary>Gets the optional stable panel identity used by external presentation orchestration.</summary>
    public string? TargetId { get; }

    /// <summary>Gets the chart when this item hosts a chart.</summary>
    public Chart? Chart { get; }

    /// <summary>Gets the visual block when this item hosts a block.</summary>
    public IVisualBlock? Block { get; }

    /// <summary>Gets the column span.</summary>
    public int ColumnSpan { get; }

    /// <summary>Gets the row span.</summary>
    public int RowSpan { get; }

    /// <summary>Creates a chart grid item.</summary>
    public static VisualGridItem FromChart(Chart chart, int columnSpan = 1, int rowSpan = 1) => new(null, chart ?? throw new ArgumentNullException(nameof(chart)), null, columnSpan, rowSpan);

    /// <summary>Creates a chart grid item with a stable target id.</summary>
    public static VisualGridItem FromChart(string targetId, Chart chart, int columnSpan = 1, int rowSpan = 1) => new(targetId, chart ?? throw new ArgumentNullException(nameof(chart)), null, columnSpan, rowSpan);

    /// <summary>Creates a visual block grid item.</summary>
    public static VisualGridItem FromBlock(IVisualBlock block, int columnSpan = 1, int rowSpan = 1) => new(null, null, block ?? throw new ArgumentNullException(nameof(block)), columnSpan, rowSpan);

    /// <summary>Creates a visual block grid item with a stable target id.</summary>
    public static VisualGridItem FromBlock(string targetId, IVisualBlock block, int columnSpan = 1, int rowSpan = 1) => new(targetId, null, block ?? throw new ArgumentNullException(nameof(block)), columnSpan, rowSpan);

    private static string RequiredTargetId(string targetId) {
        var trimmed = targetId.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("A grid target id must not be empty.", nameof(targetId));
        foreach (var character in trimmed) {
            if (!(char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.')) {
                throw new ArgumentException("Grid target ids must contain only letters, digits, dots, underscores or hyphens.", nameof(targetId));
            }
        }
        return trimmed;
    }
}
