/// <summary>Configured legacy charts whose downloadable source shares their compiled factory.</summary>
public static partial class ExampleRecipes {
    /// <summary>Gets the original downloadable chart selections, including the narrower dashboard preview.</summary>
    public static IEnumerable<ExampleChartSelection> LegacyCharts {
        get {
            yield return new("license-cost-light", Named("license-cost"), "wide", ThemeMode.Light);
            yield return new("dashboard-premium-trend-style", Named("dashboard-premium-trend"), "snippet", ThemeMode.Light);
            yield return new("control-coverage-heatmap-dark", Named("control-coverage-heatmap"), "wide", ThemeMode.Dark);
            yield return new("travel-dotted-map-dark", Named("travel-dotted-map"), "wide", ThemeMode.Dark);
        }
    }
}

/// <summary>A configured chart selection whose own frame, theme and size are part of the example.</summary>
public sealed record ExampleChartSelection(string Name, ExampleRecipe Recipe, string Variant, ThemeMode Mode);
