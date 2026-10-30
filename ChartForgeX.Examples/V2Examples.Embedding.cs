using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

public static partial class V2Examples {
    private static readonly string[] EmbeddingAssets = new[] {
        "index.html", "light.html", "dark.html", "embedding.css", "ServerRenderedRecipe.cs.txt", "shared-strip.csharp.txt", "compact-shared-strip.csharp.txt",
        "embedding-story.svg", "embedding-story.png", "embedding-story.html", "embedding-story.txt"
    }.Select(file => "embedding/" + file).ToArray();

    private static void WriteEmbedding(string output, ICollection<ProofArtifact> artifacts) {
        var recipe = new ExampleRecipe(nameof(ServerRenderedRecipe), (variant, mode) => ServerRenderedRecipe.Create(variant, mode));
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var compact in new[] { false, true }) {
                var width = compact ? 390 : 800;
                foreach (var example in new[] {
                    (Variant: "linked-columns", Family: "bar", Title: "Linked update columns", Subtitle: "A destination, accessible name and host hooks on every segment"),
                    (Variant: "support-schedule", Family: "gantt", Title: "Open-ended release support", Subtitle: "An open fading task and an independently current row")
                }) {
                    var variant = (compact ? "compact-" : "") + example.Variant;
                    WriteModel(output, artifacts, recipe.Create(variant, mode), "embedding-" + variant + "-" + mode.ToString().ToLowerInvariant(),
                        example.Family, example.Title, variant, example.Subtitle, mode, width, 360, false, recipe,
                        new[] { example.Family == "bar" ? nameof(ChartSeriesKind.Bar) : nameof(ChartSeriesKind.Gantt) }, compact);
                }
                WriteSharedStrip(output, artifacts, width, compact, mode);
            }
        }
    }

    private static void WriteSharedStrip(string output, ICollection<ProofArtifact> artifacts, int width, bool compact, VisualThemeMode mode) {
        var variant = compact ? "compact-shared-strip" : "shared-strip";
        var id = "embedding-" + variant + "-" + mode.ToString().ToLowerInvariant();
        var chart = ServerRenderedRecipe.Create(variant, mode);
        var prepared = chart.PrepareStrip(ServerRenderedChartExamples.Context(width, 96, mode, padding: 0, family: ProofFont), ServerRenderedRecipe.SharedScale());
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
        File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
        var thumbnail = chart.PrepareStrip(ServerRenderedChartExamples.Context(ThumbnailWidth, ThumbnailHeight, mode, padding: 0, family: ProofFont), ServerRenderedRecipe.SharedScale());
        var preview = thumbnail.ToArtifact(id + "-thumbnail", VisualArtifactKind.Chart);
        preview.Accessibility.Name = "A row against a shared domain";
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".thumbnail.svg"), preview.ToSvg());
        File.WriteAllBytes(Path.Combine(output, id + ".thumbnail.png"), thumbnail.ToPng());
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitServerRenderedStrip(variant, mode, width, 96));
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".gallery.csharp.txt"), ExampleRecipeSource.EmitServerRenderedStrip(variant, mode, width, 96, galleryTypography: true));
        artifacts.Add(new ProofArtifact(id, "gantt", "A row against a shared domain", variant, mode.ToString().ToLowerInvariant(), width, 96,
            prepared.Diagnostics.Select(item => item.Code).ToArray(), prepared.Regions.Count, new[] { nameof(ChartSeriesKind.Gantt) },
            prepared.Diagnostics.Select(item => item.Message).ToArray(), compact));
    }
}
