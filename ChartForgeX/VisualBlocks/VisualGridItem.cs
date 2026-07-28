using System;
using ChartForgeX.Core;
using ChartForgeX.Motion;

namespace ChartForgeX.VisualBlocks;

/// <summary>Describes one visual grid panel.</summary>
public sealed class VisualGridItem {
    private VisualGridItem(string? motionTargetId, Chart? chart, IVisualBlock? block, int columnSpan, int rowSpan) {
        if (columnSpan <= 0) throw new ArgumentOutOfRangeException(nameof(columnSpan), columnSpan, "Column span must be positive.");
        if (rowSpan <= 0) throw new ArgumentOutOfRangeException(nameof(rowSpan), rowSpan, "Row span must be positive.");
        MotionTargetId = motionTargetId == null ? null : VisualMotionGuards.RequiredTargetId(motionTargetId, nameof(motionTargetId));
        Chart = chart;
        Block = block;
        ColumnSpan = columnSpan;
        RowSpan = rowSpan;
    }

    /// <summary>Gets the optional stable id used to target this panel from a visual motion timeline.</summary>
    public string? MotionTargetId { get; }

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

    /// <summary>Creates a chart grid item with a stable motion target id.</summary>
    public static VisualGridItem FromChart(string targetId, Chart chart, int columnSpan = 1, int rowSpan = 1) => new(targetId, chart ?? throw new ArgumentNullException(nameof(chart)), null, columnSpan, rowSpan);

    /// <summary>Creates a visual block grid item.</summary>
    public static VisualGridItem FromBlock(IVisualBlock block, int columnSpan = 1, int rowSpan = 1) => new(null, null, block ?? throw new ArgumentNullException(nameof(block)), columnSpan, rowSpan);

    /// <summary>Creates a visual block grid item with a stable motion target id.</summary>
    public static VisualGridItem FromBlock(string targetId, IVisualBlock block, int columnSpan = 1, int rowSpan = 1) => new(targetId, null, block ?? throw new ArgumentNullException(nameof(block)), columnSpan, rowSpan);
}
