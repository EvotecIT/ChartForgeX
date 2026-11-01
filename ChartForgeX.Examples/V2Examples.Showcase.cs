using ChartForgeX.Core;
using ChartForgeX.Themes;

public static partial class V2Examples {
    /// <summary>Writes the gallery families that are a named form of another series kind or a composed visual.</summary>
    private static void WriteShowcaseFamilies(string output, ICollection<ProofArtifact> artifacts) {
        var showcases = new (string Family, string Recipe, string Variant, string Title, string Subtitle, int Width, int Height, string[] Kinds)[] {
            ("quadrant", "quadrant-chart", "wide", "Projects by effort and impact", "Thirteen proposals sorted into four action regions", 800, 520, new[] { "Scatter" }),
            ("org-chart", "org-chart", "wide", "Northwind Services team", "Reporting lines across three levels", 960, 520, Array.Empty<string>()),
            ("cone-funnel", "cone-funnel", "wide", "From visitor to renewed subscription", "Each stage narrows the cone by the customers it keeps", 800, 480, new[] { "Funnel" }),
            ("dashboard", "operations-dashboard", "wide", "Service desk operations", "Four chart families in one ChartGrid", 1040, 620, new[] { "Area", "Line", "Donut", "Heatmap", "Gauge" })
        };
        var forms = new (string Family, ChartSeriesKind Kind, string Variant, string Title, string Subtitle)[] {
            ("linear-gauge", ChartSeriesKind.Gauge, "vertical", "Available capacity against a target", "A linear scale with bands, a goal and a baseline")
        };
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var suffix = mode.ToString().ToLowerInvariant();
            foreach (var showcase in showcases) {
                var recipe = ExampleRecipes.Named(showcase.Recipe);
                WriteModel(output, artifacts, recipe.Create(showcase.Variant, mode), "family-" + showcase.Family + "-wide-" + suffix, showcase.Family,
                    showcase.Title, showcase.Variant, showcase.Subtitle, mode, showcase.Width, showcase.Height, null, recipe, showcase.Kinds);
            }
            foreach (var form in forms) {
                WriteModel(output, artifacts, V2GalleryModels.Create(form.Kind, form.Variant, mode), "family-" + form.Family + "-wide-" + suffix, form.Family,
                    form.Title, form.Variant, form.Subtitle, mode, 800, 440, null, ExampleRecipes.ForKind(form.Kind, form.Variant), new[] { form.Kind.ToString() });
            }
        }
    }
}
