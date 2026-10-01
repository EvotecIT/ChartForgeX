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

    /// <summary>
    /// Chart description of a calendar heatmap from <see cref="ChartDescriptionFacts.FirstDate"/> (the earliest value)
    /// to <see cref="ChartDescriptionFacts.LastDate"/> (the latest): <see cref="ChartDescriptionFacts.Count"/> days with
    /// a value, <see cref="ChartDescriptionFacts.ZeroCount"/> days at zero, and
    /// <see cref="ChartDescriptionFacts.MissingCount"/> days without data between them.
    /// </summary>
    CalendarHeatmap,

    /// <summary>Chart description of a dotted world map: <see cref="ChartDescriptionFacts.Count"/> highlighted points.</summary>
    DottedMap,

    /// <summary>Chart description of a region map: <see cref="ChartDescriptionFacts.Count"/> filled and <see cref="ChartDescriptionFacts.MissingCount"/> missing regions.</summary>
    RegionMap,

    /// <summary>Chart description of a tile map: <see cref="ChartDescriptionFacts.Count"/> filled and <see cref="ChartDescriptionFacts.MissingCount"/> missing tiles.</summary>
    TileMap,

    /// <summary>
    /// Accessible name of the calendar group, in the drawn weeks from <see cref="ChartDescriptionFacts.FirstDate"/> to
    /// <see cref="ChartDescriptionFacts.LastDate"/>: <see cref="ChartDescriptionFacts.Count"/> days with a value,
    /// <see cref="ChartDescriptionFacts.ZeroCount"/> days at zero, and <see cref="ChartDescriptionFacts.MissingCount"/>
    /// days without data.
    /// </summary>
    CalendarHeatmapGroup,

    /// <summary>Accessible name of the dotted map group: <see cref="ChartDescriptionFacts.Count"/> highlighted points.</summary>
    DottedMapGroup,

    /// <summary>Accessible name of the region map group: <see cref="ChartDescriptionFacts.Count"/> filled and <see cref="ChartDescriptionFacts.MissingCount"/> missing regions.</summary>
    RegionMapGroup,

    /// <summary>Accessible name of the tile map group: <see cref="ChartDescriptionFacts.Count"/> filled and <see cref="ChartDescriptionFacts.MissingCount"/> missing tiles.</summary>
    TileMapGroup,

    /// <summary>
    /// Description of a topology diagram (<see cref="ChartForgeX.Topology.TopologyLabels.AccessibleTextFormatter"/>):
    /// <see cref="ChartDescriptionFacts.Count"/> nodes, <see cref="ChartDescriptionFacts.GroupCount"/> groups, and
    /// <see cref="ChartDescriptionFacts.EdgeCount"/> edges.
    /// </summary>
    Topology
}

/// <summary>
/// The facts behind one automatic accessible sentence: the chart description (the SVG <c>desc</c> element), the
/// accessible name of a calendar or map group, or the description of a topology diagram. <see cref="ChartLabels.AccessibleTextFormatter"/> and <see cref="ChartForgeX.Topology.TopologyLabels.AccessibleTextFormatter"/> receive them so hosts
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
    /// <param name="groupCount">The number of groups, for topology descriptions; zero or greater.</param>
    /// <param name="edgeCount">The number of edges, for topology descriptions; zero or greater.</param>
    /// <param name="zeroCount">The number of calendar days at zero, for calendar sentences; zero or greater.</param>
    public ChartDescriptionFacts(ChartDescriptionKind kind, string? title, IEnumerable<string> seriesNames, int count, int missingCount = 0, DateTime? firstDate = null, DateTime? lastDate = null, string? mapName = null, int groupCount = 0, int edgeCount = 0, int zeroCount = 0) {
        if (seriesNames == null) throw new ArgumentNullException(nameof(seriesNames));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, "Counts must be zero or greater.");
        if (missingCount < 0) throw new ArgumentOutOfRangeException(nameof(missingCount), missingCount, "Counts must be zero or greater.");
        if (groupCount < 0) throw new ArgumentOutOfRangeException(nameof(groupCount), groupCount, "Counts must be zero or greater.");
        if (edgeCount < 0) throw new ArgumentOutOfRangeException(nameof(edgeCount), edgeCount, "Counts must be zero or greater.");
        if (zeroCount < 0) throw new ArgumentOutOfRangeException(nameof(zeroCount), zeroCount, "Counts must be zero or greater.");
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
        GroupCount = groupCount;
        EdgeCount = edgeCount;
        ZeroCount = zeroCount;
    }

    /// <summary>Gets which sentence is described.</summary>
    public ChartDescriptionKind Kind { get; }

    /// <summary>Gets the chart title, or null when the chart has none.</summary>
    public string? Title { get; }

    /// <summary>Gets the described series names: every series with points, or the one calendar or map series.</summary>
    public IReadOnlyList<string> SeriesNames => _seriesNames;

    /// <summary>
    /// Gets the main count: described series, calendar days with a value, highlighted map points, filled map regions,
    /// or topology nodes, depending on <see cref="Kind"/>. A calendar day at zero is drawn neutral and counts in
    /// <see cref="ZeroCount"/>, not here, so a sentence can tell days with changes (<see cref="Count"/>) from days in the
    /// data (<see cref="Count"/> plus <see cref="ZeroCount"/>).
    /// </summary>
    public int Count { get; }

    /// <summary>Gets the number of calendar days without data, or of map regions without a value; zero for other kinds.</summary>
    public int MissingCount { get; }

    /// <summary>Gets the number of calendar days whose value is zero, which are drawn neutral; zero for other kinds.</summary>
    public int ZeroCount { get; }

    /// <summary>Gets the first day of a calendar sentence.</summary>
    public DateTime? FirstDate { get; }

    /// <summary>Gets the last day of a calendar sentence.</summary>
    public DateTime? LastDate { get; }

    /// <summary>Gets the name of the region or tile map definition, when there is one.</summary>
    public string? MapName { get; }

    /// <summary>Gets the number of groups of a topology diagram; zero for other kinds.</summary>
    public int GroupCount { get; }

    /// <summary>Gets the number of edges of a topology diagram; zero for other kinds.</summary>
    public int EdgeCount { get; }

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
                    return title + " calendar heatmap for " + series + " from " + Date(FirstDate) + " to " + Date(LastDate) + " with " + CalendarDays() + ".";
                case ChartDescriptionKind.DottedMap:
                    return title + " dotted world map for " + series + " with " + Number(Count) + " highlighted " + (Count == 1 ? "point" : "points") + ".";
                case ChartDescriptionKind.RegionMap:
                    return title + " region map for " + series + " on " + (MapName ?? "region") + " with " + Number(Count) + " filled regions and " + Number(MissingCount) + " missing regions.";
                case ChartDescriptionKind.TileMap:
                    return title + " tile map for " + series + " on " + (MapName ?? "tile") + " with " + Number(Count) + " filled regions and " + Number(MissingCount) + " missing regions.";
                case ChartDescriptionKind.CalendarHeatmapGroup:
                    return series + " calendar heatmap from " + Date(FirstDate) + " to " + Date(LastDate) + " with " + CalendarDays();
                case ChartDescriptionKind.DottedMapGroup:
                    return series + " dotted map with " + Number(Count) + " highlighted " + (Count == 1 ? "point" : "points");
                case ChartDescriptionKind.RegionMapGroup:
                    return series + " region map with " + Number(Count) + " filled regions and " + Number(MissingCount) + " missing regions";
                case ChartDescriptionKind.Topology:
                    return (Title ?? "Topology chart") + " with " + Number(GroupCount) + " groups, " + Number(Count) + " nodes, and " + Number(EdgeCount) + " edges.";
                case ChartDescriptionKind.TileMapGroup:
                    return series + " tile map with " + Number(Count) + " filled regions and " + Number(MissingCount) + " missing regions";
                default:
                    return title + " with " + Number(Count) + " data series: " + string.Join(", ", _seriesNames) + ".";
            }
        }
    }

    /// <summary>Lists the calendar day counts: days with a value, then days at zero and days without data when there are any.</summary>
    private string CalendarDays() {
        var parts = new List<string> { Days(Count) + " with a value" };
        if (ZeroCount > 0) parts.Add(Days(ZeroCount) + " at zero");
        if (MissingCount > 0) parts.Add(Days(MissingCount) + " without data");
        if (parts.Count == 1) return parts[0];
        return string.Join(", ", parts.GetRange(0, parts.Count - 1).ToArray()) + (parts.Count > 2 ? "," : string.Empty) + " and " + parts[parts.Count - 1];
    }

    private static string Days(int count) => Number(count) + (count == 1 ? " day" : " days");

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Date(DateTime? value) => value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
}
