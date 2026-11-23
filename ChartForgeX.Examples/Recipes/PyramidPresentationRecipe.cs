/// <summary>Builds triangular allocation partitions with independent category/value captions and source-owned borders.</summary>
public static class PyramidPresentationRecipe {
    /// <summary>Creates the full allocation example. Exact documentation typography needs Carlito regular/bold installed or registered; other hosts use normal font fallback.</summary>
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "horizontal" or "compact-horizontal" or "reversed" or "compact-reversed")) throw new ArgumentOutOfRangeException(nameof(variant));
        var compact = variant.StartsWith("compact", StringComparison.Ordinal);
        var horizontal = variant.EndsWith("horizontal", StringComparison.Ordinal);
        var reversed = variant.EndsWith("reversed", StringComparison.Ordinal);
        var chart = Chart.Create().Size(compact ? 390 : 800, compact ? 500 : 560)
            .Theme(VisualTheme.Graphite()).ThemeMode(mode).Title("Allocation by service")
            .Subtitle("Partition area encodes values; caption placement and borders preserve source order")
            .Legend(false).DataLabels().XLabels("Services", "Platform", "Support", "Unassigned");
        var series = new ChartSeries(new ChartSeriesId("allocation"), "Allocation", ChartSeriesKind.Pyramid,
            new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20), new ChartPoint(4, 0) })
            .ObservationIds(new[] { new ChartObservationId("services"), new ChartObservationId("platform"), new ChartObservationId("support"), new ChartObservationId("unassigned") })
            .PointColor(0, ChartColor.FromHex("#357BA4")).PointColor(1, ChartColor.FromHex("#C18B3D")).PointColor(2, ChartColor.FromHex("#4D947D"))
            .PointFillPattern(1, ChartFillPattern.DiagonalForward).PointStrokeColor(1, ChartColor.FromHex("#8D3E69"));
        chart.AddSeries(series).Pyramid(options => {
            options.ValueEncoding = ChartPyramidValueEncoding.Area; options.Reversed = reversed;
            options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
            options.LabelContent = ChartStageLabelContent.Separate;
            options.CategoryLabel.Style.FontSize = 12; options.ValueLabel.Style.FontSize = 13;
            options.PartitionOutline = new ChartStrokeStyle(width: 2, dash: new[] { 6d, 2d });
            options.PartitionOutlineDashOffset = -1;
        });
        return chart.ValueFormat(ChartValueFormat.Number("0", CultureInfo.InvariantCulture));
    }
}
