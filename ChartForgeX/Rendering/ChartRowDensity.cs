using System;

namespace ChartForgeX.Rendering;

/// <summary>
/// Design rule 10, shared by every row-based family (bullet, progress, Gantt, state timeline, strips): each row gets a
/// fixed 28–40px slot and the rows stack from the top of their area, so three rows read as a compact list instead of
/// spreading across the canvas. Group headers sit <see cref="GroupHeaderGap"/> above their first row.
/// </summary>
internal static class ChartRowDensity {
    /// <summary>The smallest comfortable row slot.</summary>
    internal const double MinimumRow = 28;

    /// <summary>The largest row slot: extra height stays below the rows instead of stretching them.</summary>
    internal const double MaximumRow = 40;

    /// <summary>The fewest row slots a schedule's plot keeps, so a one-row schedule still has room for markers and captions.</summary>
    internal const double MinimumScheduleRows = 3;

    /// <summary>The gap between a group header and its first row.</summary>
    internal const double GroupHeaderGap = 8;

    /// <summary>
    /// The row pitch for <paramref name="count"/> rows in <paramref name="available"/> height: the even share, capped at
    /// <see cref="MaximumRow"/>. A crowded area keeps its even share below <see cref="MinimumRow"/> rather than clipping rows.
    /// </summary>
    internal static double Pitch(double available, int count) =>
        count <= 0 || !(available > 0) ? 0 : Math.Min(MaximumRow, available / count);

    /// <summary>The height a block of rows actually uses.</summary>
    internal static double Height(double available, int count) => Pitch(available, count) * Math.Max(0, count);
}
