namespace ChartForgeX.Core;

/// <summary>Describes one page of a paginated chart grid.</summary>
/// <remarks>Counts and indices describe the partition when created; later edits to Grid do not recompute them.</remarks>
public sealed class ChartGridPage {
    internal ChartGridPage(ChartGrid grid, int number, int totalPages, int firstChartIndex, int totalCharts) {
        Grid = grid;
        Number = number;
        TotalPages = totalPages;
        FirstChartIndex = firstChartIndex;
        ChartCount = grid.Charts.Count;
        TotalCharts = totalCharts;
    }

    /// <summary>Gets the independently configurable grid for rendering this page.</summary>
    public ChartGrid Grid { get; }

    /// <summary>Gets the one-based page number.</summary>
    public int Number { get; }

    /// <summary>Gets the number of pages in the original partition.</summary>
    public int TotalPages { get; }

    /// <summary>Gets the zero-based source index of this page's first chart.</summary>
    public int FirstChartIndex { get; }

    /// <summary>Gets the number of charts originally assigned to this page.</summary>
    public int ChartCount { get; }

    /// <summary>Gets the chart count of the source grid when partitioned.</summary>
    public int TotalCharts { get; }
}
