/// <summary>Uses authored optional uncertainty intervals and the native whisker/cap owner.</summary>
public static class NativeErrorComponentRecipe {
    /// <summary>Creates one fixed numeric source with real nullable interval shape and caller-owned typography.</summary>
    public static Chart Create(string form, bool finish, int width, VisualThemeMode mode, string family) {
        if (form is not ("x" or "y" or "xy")) throw new ArgumentOutOfRangeException(nameof(form));
        var chart = Chart.Create().WithTitle("Actual uncertainty values").WithSize(width, 420).WithTheme(VisualTheme.Graphite(), mode).WithFontFamily(family).WithLegend(true).WithDataLabels(true);
        foreach (var key in new[] { "delivery", "comparison" }) {
            var changed = finish && key == "delivery";
            var first = new ChartErrorBar(1, changed ? 50 : 30, form.IndexOf('x') >= 0 ? new ChartErrorBounds(changed ? .25 : .5, changed ? 1.75 : 1.5) : null,
                form.IndexOf('y') >= 0 ? new ChartErrorBounds(changed ? 40 : 20, changed ? 60 : 40) : null);
            var later = new ChartErrorBar(3, 45, form.IndexOf('x') >= 0 ? new ChartErrorBounds(2.5, 3.5) : null, form.IndexOf('y') >= 0 ? new ChartErrorBounds(35, 55) : null);
            chart.AddSeries(ChartSeries.CreateErrorBar(new ChartSeriesId(key), key, new[] { first, later }).WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later") }));
        }
        chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 100; }); return chart;
    }
}
