/// <summary>Places independent notes, finite lines and a source-Y-offset channel on an ordinary chart.</summary>
public static class SourceDrawingsRecipe {
    /// <summary>Creates the same authored geometry in a compact or wide native scene.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        return Chart.Create().WithSize(variant == "compact" ? 390 : 800, 520)
            .WithTheme(VisualTheme.Graphite(), mode).WithTitle("Review a measured trend")
            .WithSubtitle("Independent source drawings retain their coordinates in every export")
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 100).WithLegend(false)
            .AddLine("Observed", new[] { new ChartPoint(0, 10), new ChartPoint(3, 40), new ChartPoint(10, 70) })
            .AddNote(new ChartPoint(3, 40), "Review", id: new ChartAnnotationId("review"))
            .AddLineSegment(new ChartPoint(2, 20), new ChartPoint(8, 30), "Plan", id: new ChartAnnotationId("plan"))
            .AddParallelChannel(new ChartPoint(2, 65), new ChartPoint(8, 75), 10, "Target", id: new ChartAnnotationId("target"));
    }
}
