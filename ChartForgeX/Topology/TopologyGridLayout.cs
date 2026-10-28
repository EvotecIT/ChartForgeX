using System;

namespace ChartForgeX.Topology;

/// <summary>Bounds source-ordered automatic node and nested-region rows in the existing GroupGrid layout.</summary>
public sealed class TopologyGridLayout {
    /// <summary>Creates immutable row limits. Geometry may expand beyond the requested viewport rather than shrink text.</summary>
    public TopologyGridLayout(int maximumNodesPerRow = 4, int maximumGroupsPerRow = 2,
        double horizontalSpacing = 28, double verticalSpacing = 28, double? paddingX = null, double? paddingY = null, double rowIndent = 0) {
        if (maximumNodesPerRow < 1) throw new ArgumentOutOfRangeException(nameof(maximumNodesPerRow));
        if (maximumGroupsPerRow < 1) throw new ArgumentOutOfRangeException(nameof(maximumGroupsPerRow));
        ValidateSpacing(horizontalSpacing, nameof(horizontalSpacing)); ValidateSpacing(verticalSpacing, nameof(verticalSpacing));
        if (paddingX.HasValue) ValidateSpacing(paddingX.Value, nameof(paddingX));
        if (paddingY.HasValue) ValidateSpacing(paddingY.Value, nameof(paddingY));
        ValidateSpacing(rowIndent, nameof(rowIndent));
        HorizontalSpacing = horizontalSpacing; VerticalSpacing = verticalSpacing; PaddingX = paddingX; PaddingY = paddingY; RowIndent = rowIndent;
        MaximumNodesPerRow = maximumNodesPerRow; MaximumGroupsPerRow = maximumGroupsPerRow;
    }
    /// <summary>Gets the maximum nodes in each source-ordered row.</summary>
    public int MaximumNodesPerRow { get; }
    /// <summary>Gets the maximum child regions in each source-ordered row.</summary>
    public int MaximumGroupsPerRow { get; }
    /// <summary>Gets the horizontal gap between siblings.</summary>
    public double HorizontalSpacing { get; }
    /// <summary>Gets the vertical gap between rows and node/region sections.</summary>
    public double VerticalSpacing { get; }
    /// <summary>Gets optional horizontal outer/region padding; null preserves the incumbent padding.</summary>
    public double? PaddingX { get; }
    /// <summary>Gets optional vertical outer/region padding; null preserves the incumbent padding.</summary>
    public double? PaddingY { get; }
    /// <summary>Gets the additional horizontal start of every row after the first.</summary>
    public double RowIndent { get; }
    private static void ValidateSpacing(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }
}

public sealed partial class TopologyChart {
    /// <summary>Gets or sets explicit automatic row limits for GroupGrid; null retains its incumbent layout and bytes.</summary>
    /// <remarks>This policy requires automatic node/group placement. Prepared portable artifacts retain the resolved geometry.</remarks>
    public TopologyGridLayout? GridLayout { get; set; }
}
