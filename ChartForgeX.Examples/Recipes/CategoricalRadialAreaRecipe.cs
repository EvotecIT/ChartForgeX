/// <summary>Uses the existing categorical radial column owner for grouped or stacked sector-area measurements.</summary>
public static class CategoricalRadialAreaRecipe {
    /// <summary>Creates native categorical columns with an explicit area or radius encoding and stable source IDs.</summary>
    /// <param name="variant">Use wide, compact, stacked, compact-stacked, radius or compact-radius.</param>
    /// <param name="mode">The paired visual theme mode.</param>
    /// <returns>A complete chart ready for the ordinary SVG, PNG and HTML output workflow.</returns>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "stacked" or "compact-stacked" or "radius" or "compact-radius"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var compact = variant.StartsWith("compact", StringComparison.Ordinal);
        var stacked = variant.EndsWith("stacked", StringComparison.Ordinal);
        var radius = variant.EndsWith("radius", StringComparison.Ordinal);
        var completed = Values("completed", "Completed", new[] { 25d, 80, 0, 45 });
        var followup = Values("follow-up", "Follow-up", new[] { 15d, 25, 20, 30 });
        if (stacked) { completed.StackGroup("requests"); followup.StackGroup("requests"); }
        // Zero baseline makes nominal sector area proportional to the source value. A nonzero
        // visible minimum encodes only the displayed interval; gaps/corners are separate paint.
        return Chart.Create().Size(compact ? 390 : 900, compact ? 520 : 600)
            .Theme(VisualTheme.Graphite()).ThemeMode(mode).Title("Requests by region and intake channel")
            .XAxis(x => x.Title("Region")).YAxis(y => y.Title("Requests")).XLabels("North", "South", "East", "West")
            .YAxis(y => y.Bounds(0, 120)).Legend().DataLabels(false)
            .ValueFormat(ChartValueFormat.Number("0' requests'"))
            .RadialGeometry(new ChartRadialGeometryOptions(-90, 270, .12, .12, .08,
                valueEncoding: radius ? ChartRadialValueEncoding.Radius : ChartRadialValueEncoding.Area))
            .AddSeries(completed).AddSeries(followup);
    }

    private static ChartSeries Values(string key, string name, double[] values) {
        var points = values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
        var series = new ChartSeries(new ChartSeriesId(key), name, ChartSeriesKind.RadialColumn, points);
        var ids = new[] { "north", "south", "east", "west" };
        for (var index = 0; index < ids.Length; index++) series.ObservationId(index, new ChartObservationId(ids[index]));
        return series;
    }
}
