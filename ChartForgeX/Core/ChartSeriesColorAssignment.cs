namespace ChartForgeX.Core;

/// <summary>How series without an explicit colour pick their theme palette colour.</summary>
public enum ChartSeriesColorAssignment {
    /// <summary>Series take palette colours in series order, so the first series is always the theme accent and neighbours never repeat a colour until the palette wraps.</summary>
    Order,

    /// <summary>
    /// Series with an explicit <see cref="ChartSeriesId"/> take a palette colour derived from that identifier, so the same series keeps its colour
    /// across linked charts and when other series are added, removed or filtered. Two series can share a colour.
    /// </summary>
    Identity
}
