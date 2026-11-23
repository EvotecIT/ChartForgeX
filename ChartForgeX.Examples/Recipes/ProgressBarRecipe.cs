/// <summary>Labeled progress rows with a shared maximum and optional track styling.</summary>
public static class ProgressBarRecipe {
    public static Chart Create(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        var chart = Chart.Create().AddProgressBars("Delivery", new[] {
            new ChartProgressItem("Reviewed", 76), new ChartProgressItem("Verified", 58), new ChartProgressItem("Complete", 42)
        });
        if (variant == "options") chart.Progress(p => p.Max(120).BarThicknessRatio(.42).TrackOpacity(.3));
        return chart;
    }
}
