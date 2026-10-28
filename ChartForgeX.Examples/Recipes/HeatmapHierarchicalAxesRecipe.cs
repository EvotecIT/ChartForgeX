/// <summary>Groups rectangular matrix columns and source row slots through the ordinary axis-label owner.</summary>
public static class HeatmapHierarchicalAxesRecipe {
    /// <summary>Creates numeric or categorical rows with complete paths, missing cells and independent source IDs.</summary>
    /// <param name="variant">Use wide, compact, states-wide or states-compact. Numeric and categorical cells remain separate models.</param>
    /// <param name="mode">The paired theme mode.</param>
    /// <returns>A complete native chart ready for SVG, PNG or the optional HTML adapter.</returns>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "states-wide" or "states-compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var compact = variant.EndsWith("compact", StringComparison.Ordinal); var states = variant.StartsWith("states", StringComparison.Ordinal);
        var theme = VisualTheme.Graphite(); var colors = theme.Resolve(mode);
        var chart = Chart.Create().WithSize(compact ? 390 : 1000, 560).WithTheme(theme, mode)
            .WithTitle(states ? "Service states by region and work window" : "Service measurements by region and work window")
            .WithXAxis("Work window").WithYAxis("Source row slots").WithDataLabels(false)
            .WithHeatmapCellGap(3).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
        if (states) {
            chart.WithStateCategories(new ChartStateCategory("pass", "Available", colors.Status.Pass.Fill),
                new ChartStateCategory("review", "Review", colors.Status.Medium.Fill));
            chart.AddSeries(States("west", "West service", new[] { "pass", null, "review", "pass" }));
            chart.AddSeries(States("unknown", "Unknown service", new string?[] { null, null, null, null }));
            chart.AddSeries(States("east", "East service", new[] { "review", "pass", "pass", "review" }));
            chart.AddSeries(States("return", "Return service", new[] { null, "pass", "review", "pass" }));
        } else {
            chart.AddHeatmapRow("West service", new double?[] { 0, null, 12, 24 }, id: new ChartSeriesId("west"))
                .AddHeatmapRow("Unknown service", new double?[] { null, null, null, null }, id: new ChartSeriesId("unknown"))
                .AddHeatmapRow("East service", new double?[] { 20, 10, 0, 5 }, id: new ChartSeriesId("east"))
                .AddHeatmapRow("Return service", new double?[] { null, 2, 3, 4 }, id: new ChartSeriesId("return"));
            foreach (var row in chart.Series) row.WithObservationIds(row.Points.Select(point => new ChartObservationId("column-" + point.X.ToString("0", CultureInfo.InvariantCulture))));
            chart.WithHeatmapColorScale(ChartColorScale.Sequential(colors.Surface, colors.Accent).WithValueRange(0, 24));
        }
        chart.WithXLabels(new[] {
            new ChartAxisLabel(1, "A", "Morning", "Work"), new ChartAxisLabel(2, "B", "Morning", "Work"),
            new ChartAxisLabel(3, "A", "Evening", "Work"), new ChartAxisLabel(4, "B", "Evening", "Work")
        });
        // Y labels bind one-based SOURCE ROW SLOTS, not intensities or series IDs.
        // Reorder these mappings deliberately when changing row membership; mirroring axes leaves them intact.
        chart.ConfigureYAxis(axis => axis.Labels.AddRange(new[] {
            new ChartAxisLabel(1, "West", "North", "Core"), new ChartAxisLabel(2, "Unknown", "North", "Core"),
            new ChartAxisLabel(3, "East", "South", "Core"), new ChartAxisLabel(4, "Return", "North", "Core")
        }));
        return chart;
    }

    private static ChartSeries States(string id, string name, string?[] values) => ChartSeries.CreateHeatmapCategoryRow(new ChartSeriesId(id), name,
        values.Select(value => value == null ? (ChartHeatmapCell?)null : new ChartHeatmapCell(value)))
        .WithObservationIds(Enumerable.Range(1, values.Length).Select(column => new ChartObservationId("column-" + column.ToString(CultureInfo.InvariantCulture))));
}
