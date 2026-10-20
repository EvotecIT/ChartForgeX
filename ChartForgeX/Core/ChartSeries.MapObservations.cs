using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private IReadOnlyList<ChartMapPoint> _mapPoints = Array.Empty<ChartMapPoint>();
    private IReadOnlyList<ChartCalendarHeatmapItem> _calendarItems = Array.Empty<ChartCalendarHeatmapItem>();
    private IReadOnlyList<ChartRegionMapItem> _regionValues = Array.Empty<ChartRegionMapItem>();

    /// <summary>Gets complete dotted-map observations in authored order, including repeated coordinates.</summary>
    public IReadOnlyList<ChartMapPoint> MapPoints => _mapPoints;
    /// <summary>Gets complete calendar observations. Factories and replacement aggregate duplicate dates and sort by date.</summary>
    public IReadOnlyList<ChartCalendarHeatmapItem> CalendarItems => _calendarItems;
    /// <summary>Gets complete region or tile values. Factories and replacement resolve aliases, aggregate duplicate regions and sort by canonical code.</summary>
    public IReadOnlyList<ChartRegionMapItem> RegionValues => _regionValues;
    /// <summary>Gets the immutable geometry owned by a region-map series, or null for other series kinds.</summary>
    public ChartMapDefinition? RegionMapDefinition { get; private set; }
    /// <summary>Gets the immutable geometry owned by a tile-map series, or null for other series kinds.</summary>
    public ChartTileMapDefinition? TileMapDefinition { get; private set; }

    /// <summary>Replaces map points atomically in input order and clears observation identities and presentation overrides.</summary>
    public ChartSeries ReplaceMapPoints(IEnumerable<ChartMapPoint> values) {
        RequireObservationKind(ChartSeriesKind.DottedMap);
        ReplaceObservations(ref _mapPoints, values, ValidateMapPoint);
        return this;
    }

    /// <summary>Updates one complete map point while preserving its ordinal, identity and presentation overrides.</summary>
    public ChartSeries UpdateMapPoint(int index, ChartMapPoint value) {
        RequireObservationKind(ChartSeriesKind.DottedMap);
        UpdateObservation(ref _mapPoints, index, value, ValidateMapPoint);
        return this;
    }

    /// <summary>Replaces calendar observations atomically. Duplicate dates are summed; the last explicit color wins. Clears observation metadata.</summary>
    public ChartSeries ReplaceCalendarItems(IEnumerable<ChartCalendarHeatmapItem> values) {
        RequireObservationKind(ChartSeriesKind.CalendarHeatmap);
        var normalized = NormalizeCalendarItems(values);
        ReplaceObservations(ref _calendarItems, normalized, ValidateCalendarItem);
        return this;
    }

    /// <summary>Updates one complete dated value while preserving its ordinal and metadata. A date already owned by another observation is rejected.</summary>
    public ChartSeries UpdateCalendarItem(int index, ChartCalendarHeatmapItem value) {
        RequireObservationKind(ChartSeriesKind.CalendarHeatmap);
        if (index < 0 || index >= _calendarItems.Count) throw new ArgumentOutOfRangeException(nameof(index));
        value = ValidateCalendarItem(value);
        for (var other = 0; other < _calendarItems.Count; other++)
            if (other != index && _calendarItems[other].Date == value.Date)
                throw new ArgumentException("The date already belongs to another calendar observation.", nameof(value));
        UpdateObservation(ref _calendarItems, index, value, ValidateCalendarItem);
        return this;
    }

    /// <summary>Replaces region or tile values atomically, resolving aliases and summing duplicate keys. Clears observation metadata; geometry is retained.</summary>
    public ChartSeries ReplaceRegionValues(IEnumerable<ChartRegionMapItem> values) {
        RequireObservationKind(ChartSeriesKind.RegionMap, ChartSeriesKind.TileMap);
        var normalized = NormalizeRegionValues(values);
        ReplaceObservations(ref _regionValues, normalized, ValidateRegionValue);
        return this;
    }

    /// <summary>Updates one complete region or tile value while preserving its ordinal and metadata. Collisions with another canonical region are rejected.</summary>
    public ChartSeries UpdateRegionValue(int index, ChartRegionMapItem value) {
        RequireObservationKind(ChartSeriesKind.RegionMap, ChartSeriesKind.TileMap);
        if (index < 0 || index >= _regionValues.Count) throw new ArgumentOutOfRangeException(nameof(index));
        value = ResolveRegionValue(value);
        for (var other = 0; other < _regionValues.Count; other++)
            if (other != index && string.Equals(_regionValues[other].Region, value.Region, StringComparison.Ordinal))
                throw new ArgumentException("The region already belongs to another observation.", nameof(value));
        UpdateObservation(ref _regionValues, index, value, ValidateRegionValue);
        return this;
    }

    private static ChartMapPoint ValidateMapPoint(ChartMapPoint value) => value.ValidatedCopy();

    private static ChartCalendarHeatmapItem ValidateCalendarItem(ChartCalendarHeatmapItem value) {
        var item = new ChartCalendarHeatmapItem(value.Date, value.Value, value.Color);
        // The numeric scene projection must represent the same date without a lossy special DateTime mapping.
        if (DateTime.FromOADate(item.Date.ToOADate()).Date != item.Date)
            throw new ArgumentOutOfRangeException(nameof(value), "Calendar dates must be representable as an OLE Automation date.");
        return item;
    }

    private static ChartRegionMapItem ValidateRegionValue(ChartRegionMapItem value) => new(value.Region, value.Value, value.Color);

    private static IEnumerable<ChartCalendarHeatmapItem> NormalizeCalendarItems(IEnumerable<ChartCalendarHeatmapItem> values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var dates = new SortedDictionary<DateTime, ChartCalendarHeatmapItem>();
        foreach (var raw in values) {
            var item = ValidateCalendarItem(raw);
            if (dates.TryGetValue(item.Date, out var previous))
                item = new ChartCalendarHeatmapItem(item.Date, previous.Value + item.Value, item.Color ?? previous.Color);
            dates[item.Date] = item;
        }
        return dates.Values.ToArray();
    }

    private IEnumerable<ChartRegionMapItem> NormalizeRegionValues(IEnumerable<ChartRegionMapItem> values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var regions = new SortedDictionary<string, ChartRegionMapItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in values) {
            var item = ResolveRegionValue(raw);
            if (regions.TryGetValue(item.Region, out var previous))
                item = new ChartRegionMapItem(item.Region, previous.Value + item.Value, item.Color ?? previous.Color);
            regions[item.Region] = item;
        }
        return regions.Values.ToArray();
    }

    private ChartRegionMapItem ResolveRegionValue(ChartRegionMapItem raw) {
        var item = ValidateRegionValue(raw);
        string code;
        var resolved = Kind == ChartSeriesKind.RegionMap
            ? (RegionMapDefinition ?? throw new InvalidOperationException("The region-map series requires geometry.")).TryResolveRegion(item.Region, out code)
            : (TileMapDefinition ?? throw new InvalidOperationException("The tile-map series requires geometry.")).TryResolveRegion(item.Region, out code);
        if (!resolved) throw new ArgumentException("Unknown or ambiguous map region: " + item.Region + ".", nameof(raw));
        return new ChartRegionMapItem(code, item.Value, item.Color);
    }
}
