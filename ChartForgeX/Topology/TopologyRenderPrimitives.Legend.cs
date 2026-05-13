using System;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public const int LegendColumns = 4;
    public const double LegendMaxWidth = 860;
    public const double LegendItemColumnWidth = 190;
    public const double LegendItemRowHeight = 31;
    public const double LegendFirstItemOffsetY = 54;
    public const double LegendBottomPadding = 20;
    public const double LegendHorizontalPadding = 48;

    public static double LegendWidth(TopologyLegend? legend, TopologyViewport viewport) => legend == null ? 0 : Math.Min(LegendMaxWidth, Math.Max(1, viewport.Width - viewport.Padding * 2));

    public static int LegendColumnCount(TopologyLegend legend, double width) {
        var columns = Math.Min(LegendColumns, Math.Max(1, legend.Items.Count));
        while (columns > 1 && RawLegendColumnWidth(width, columns) < LegendItemColumnWidth) columns--;
        return columns;
    }

    public static double LegendColumnWidth(double width, int columns) => Math.Max(LegendItemColumnWidth, RawLegendColumnWidth(width, columns));

    private static double RawLegendColumnWidth(double width, int columns) => (Math.Max(1, width) - LegendHorizontalPadding) / Math.Max(1, columns);

    public static double LegendHeight(TopologyLegend legend) => LegendHeight(legend, LegendMaxWidth);

    public static double LegendHeight(TopologyLegend legend, double width) {
        var columns = LegendColumnCount(legend, width);
        var rows = Math.Max(1, (int)Math.Ceiling(legend.Items.Count / (double)columns));
        return LegendFirstItemOffsetY + (rows - 1) * LegendItemRowHeight + LegendBottomPadding;
    }

    public static double LegendReservedHeight(TopologyLegend? legend) => legend == null ? 0 : LegendHeight(legend) + 24;

    public static double LegendReservedHeight(TopologyLegend? legend, TopologyViewport viewport) => legend == null ? 0 : LegendHeight(legend, LegendWidth(legend, viewport)) + 24;
}
