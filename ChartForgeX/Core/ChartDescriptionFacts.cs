using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ChartForgeX.Core;

/// <summary>Identifies which automatic accessible sentence a <see cref="ChartDescriptionFacts"/> describes.</summary>
public enum ChartDescriptionKind {
    /// <summary>Chart description: the chart has no series.</summary>
    NoSeries,

    /// <summary>Chart description: the chart has series, but none with data points.</summary>
    NoPoints,

    /// <summary>Chart description: ordinary series, listed in <see cref="ChartDescriptionFacts.SeriesNames"/>; <see cref="ChartDescriptionFacts.Count"/> is their number.</summary>
    Series,

    /// <summary>Chart description of a calendar heatmap: <see cref="ChartDescriptionFacts.Count"/> dated values from <see cref="ChartDescriptionFacts.FirstDate"/> to <see cref="ChartDescriptionFacts.LastDate"/>.</summary>
    CalendarHeatmap,

    /// <summary>Chart description of a dotted world map: <see cref="ChartDescriptionFacts.Count"/> highlighted points.</summary>
    DottedMap,

    /// <summary>Chart description of a region map: <see cref="ChartDescriptionFacts.Count"/> filled and <see cref="ChartDescriptionFacts.MissingCount"/> missing regions.</summary>
    RegionMap,

    /// <summary>Chart description of a tile map: <see cref="ChartDescriptionFacts.Count"/> filled and <see cref="ChartDescriptionFacts.MissingCount"/> missing tiles.</summary>
    TileMap,

    /// <summary>
    /// Accessible name of the calendar group: <see cref="ChartDescriptionFacts.Count"/> days with a value and
    /// <see cref="ChartDescriptionFacts.MissingCount"/> without one, in the drawn weeks from
    /// <see cref="ChartDescriptionFacts.FirstDate"/> to <see cref="ChartDescriptionFacts.LastDate"/>.
    /// </summary>
    CalendarHeatmapGroup,

    /// <summary>Accessible name of the dotted map group: <see cref="ChartDescriptionFacts.Count"/> highlighted points.</summary>
    DottedMapGroup,

    /// <summary>Accessible name of the region map group: <see cref="ChartDescriptionFacts.Count"/> filled and <see cref="ChartDescriptionFacts.MissingCount"/> missing regions.</summary>
    RegionMapGroup,

    /// <summary>Accessible name of the tile map group: <see cref="ChartDescriptionFacts.Count"/> filled and <see cref="ChartDescriptionFacts.MissingCount"/> missing tiles.</summary>
    TileMapGroup
}

/// <summary>
/// The facts behind one automatic accessible sentence: the chart description (the SVG <c>desc</c> element) or the
/// accessible name of a calendar or map group. <see cref="ChartLabels.AccessibleTextFormatter"/> receives them so hosts
/// can write the sentence in their own language, including its plural forms.
/// </summary>
public sealed class ChartDescriptionFacts {
    private readonly ReadOnlyCollection<string> _seriesNames;

    /// <summary>Initializes the facts of one sentence, for example to test a formatter.</summary>
    /// <param name="kind">Which sentence is described.</param>
    /// <param name="title">The chart title; null or white space when the chart has none.</param>
    /// <param name="seriesNames">The described series names.</param>
    /// <param name="count">The main count (see <see cref="Count"/>); zero or greater.</param>
    /// <param name="missingCount">The number of days, regions, or tiles without a value; zero or greater.</param>
    /// <param name="firstDate">The first day, for calendar sentences.</param>
    /// <param name="lastDate">The last day, for calendar sentences.</param>
    /// <param name="mapName">The name of the region or tile map definition.</param>
    public ChartDescriptionFacts(ChartDescriptionKind kind, string? title, IEnumerable<string> seriesNames, int count, int missingCount = 0, DateTime? firstDate = null, DateTime? lastDate = null, string? mapName = null) {
        if (seriesNames == null) throw new ArgumentNullException(nameof(seriesNames));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Counts must be zero or greater.");
        if (missingCount < 0) throw new ArgumentOutOfRangeException(nameof(missingCount), missingCount, "Counts must be zero or greater.");
        Kind = kind;
        Title = string.IsNullOrWhiteSpace(title) ? null : title;
        var names = new List<string>();
        foreach (var name in seriesNames) names.Add(name ?? string.Empty);
        _seriesNames = names.AsReadOnly();
        Count = count;
        MissingCount = missingCount;
        FirstDate = firstDate;
        LastDate = lastDate;
        MapName = string.IsNullOrWhiteSpace(mapName) ? null : mapName;
    }

    /// <summary>Gets which sentence is described.</summary>
    public ChartDescriptionKind Kind { get; }

    /// <summary>Gets the chart title, or null when the chart has none.</summary>
    public string? Title { get; }

    /// <summary>Gets the described series names: every series with points, or the one calendar or map series.</summary>
    public IReadOnlyList<string> SeriesNames => _seriesNames;

    /// <summary>
    /// Gets the main count: described series, dated calendar values, calendar days with a value, highlighted map
    /// points, or filled map regions, depending on <see cref="Kind"/>.
    /// </summary>
    public int Count { get; }

    /// <summary>Gets the number of calendar days or map regions without a value; zero for other kinds.</summary>
    public int MissingCount { get; }

    /// <summary>Gets the first day of a calendar sentence.</summary>
    public DateTime? FirstDate { get; }

    /// <summary>Gets the last day of a calendar sentence.</summary>
    public DateTime? LastDate { get; }

    /// <summary>Gets the name of the region or tile map definition, when there is one.</summary>
    public string? MapName { get; }

    /// <summary>Gets the English sentence ChartForgeX writes when no formatter is set.</summary>
    public string EnglishText {
        get {
            var title = Title ?? "Chart";
            var series = _seriesNames.Count > 0 ? _seriesNames[0] : string.Empty;
            switch (Kind) {
                case ChartDescriptionKind.NoSeries:
                    return title + " with no data series.";
                case ChartDescriptionKind.NoPoints:
                    return title + " with no data points.";
                case ChartDescriptionKind.CalendarHeatmap:
                    return title + " calendar heatmap for " + series + " from " + Date(FirstDate) + " to " + Date(LastDate) + " with " + Number(Count) + " dated " + (Count == 1 ? "value" : "values") + ".";
                case ChartDescriptionKind.DottedMap:
                    return title + " dotted world map for " + series + " with " + Number(Count) + " highlighted " + (Count == 1 ? "point" : "points") + ".";
                case ChartDescriptionKind.RegionMap:
                    return title + " region map for " + series + " on " + (MapName ?? "region") + " with " + Number(Count) + " filled regions and " + Number(MissingCount) + " missing regions.";
                case ChartDescriptionKind.TileMap:
                    return title + " tile map for " + series + " on " + (MapName ?? "tile") + " with " + Number(Count) + " filled regions and " + Number(MissingCount) + " missing regions.";
                case ChartDescriptionKind.CalendarHeatmapGroup:
                    return series + " calendar heatmap from " + Date(FirstDate) + " to " + Date(LastDate) + " with " + Number(Count) + " filled days and " + Number(MissingCount) + " empty days";
                case ChartDescriptionKind.DottedMapGroup:
                    return series + " dotted map with " + Number(Count) + " highlighted " + (Count == 1 ? "point" : "points");
                case ChartDescriptionKind.RegionMapGroup:
                    return series + " region map with " + Number(Count) + " filled regions and " + Number(MissingCount) + " missing regions";
                case ChartDescriptionKind.TileMapGroup:
                    return series + " tile map with " + Number(Count) + " filled regions and " + Number(MissingCount) + " missing regions";
                default:
                    return title + " with " + Number(Count) + " data series: " + string.Join(", ", _seriesNames) + ".";
            }
        }
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Date(DateTime? value) => value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
}
