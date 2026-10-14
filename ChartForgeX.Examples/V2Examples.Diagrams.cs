using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

public static partial class V2Examples {
    private static void WriteDiagrams(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var family in new[] { "topology", "sequence" }) {
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                var id = family + "-feasibility-" + mode.ToString().ToLowerInvariant();
                var title = family == "topology" ? "Service request route" : "Client and server exchange";
                var recipe = ExampleRecipes.Named("simple-" + family);
                var model = recipe.Create("feasibility", mode);
                var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)),
                    VisualTheme.Graphite(), mode, new VisualFrame(title, "Product-neutral diagram semantics", showLegend: false), FontSpec.FromFamily(ProofFont));
                var prepared = model.Prepare(context);
                ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
                File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
                WriteThumbnail(output, model, id, title, family, mode, legend: false);
                WriteRecipe(output, id, recipe, "feasibility", title, "Product-neutral diagram semantics", mode, 640, 400, false, family == "topology" ? "Topology" : "Sequence");
                artifacts.Add(new ProofArtifact(id, family, title, "feasibility", mode.ToString().ToLowerInvariant(), 640, 400,
                    prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count,
                    DiagnosticMessages: prepared.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray()));
            }
        }
    }

}
