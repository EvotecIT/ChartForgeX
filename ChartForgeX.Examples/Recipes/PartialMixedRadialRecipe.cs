using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Shared category columns and a curved reference polygon in one declared partial annular sector.</summary>
public static class PartialMixedRadialRecipe {
    /// <summary>Creates a three-quarter radial comparison with independent radius units and retained category identities.</summary>
    public static Chart Create(int width = 800, VisualThemeMode mode = VisualThemeMode.Light) {
        var counts = new[] { 1200d, 700, 1050, 950, 800, 1100 };
        var reference = new ChartSeries(new ChartSeriesId("reference"), "Readiness", ChartSeriesKind.Radar,
            counts.Select((_, index) => new ChartPoint(index + 1, 70 + index * 4)))
            .UseSecondaryValueAxis().ConfigureRadar(options => { options.FillOpacity = .15; options.CurveTension = .15; });
        return Chart.Create().WithSize(width, 440).WithTheme(VisualTheme.Graphite(), mode)
            .WithTitle("Regional workload and readiness").WithSubtitle("One three-quarter sector · independent numeric radius units")
            .WithXLabels("North", "East", "South", "West", "Central", "Remote").WithYAxisBounds(0, 1600).WithSecondaryYAxisBounds(0, 100)
            .WithDataLabels(false).WithRadialGeometry(new ChartRadialGeometryOptions(-90, 180, innerRadiusRatio: .25))
            .AddRadialColumn("Requests", counts.Select((value, index) => new ChartPoint(index + 1, value))).AddSeries(reference);
    }
}
