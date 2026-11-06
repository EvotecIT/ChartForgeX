using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteRecipe(string output, string id, ExampleRecipe recipe, string variant, string title, string subtitle,
        VisualThemeMode mode, int width, int height, bool? legend, string artifactKind = "Chart", bool surface = false, bool viewport = false) {
        var program = ExampleRecipeSource.Emit(recipe, variant, title, subtitle, mode, width, height, legend, artifactKind, surface, viewport);
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".csharp.txt"), program);
        // The website shows this variant's code alone; the program above stays the download.
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".snippet.csharp.txt"), ExampleSnippetSource.Emit(recipe, variant, program));
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".gallery.csharp.txt"),
            ExampleRecipeSource.Emit(recipe, variant, title, subtitle, mode, width, height, legend, artifactKind, surface, viewport, galleryTypography: true));
    }
}
