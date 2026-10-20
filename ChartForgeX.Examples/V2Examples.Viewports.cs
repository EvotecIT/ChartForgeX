using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

public static partial class V2Examples {
    private static void WriteViewports(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var kind in new[] { ChartSeriesKind.Line, ChartSeriesKind.StepLine })
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
                foreach (var compact in new[] { false, true }) {
                    var steps = kind == ChartSeriesKind.StepLine;
                    var family = FamilyName(kind);
                    var variant = compact ? "compact-x-viewport" : "x-viewport";
                    var id = "family-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                    var width = compact ? 360 : 800; var height = compact ? 360 : 440;
                    var title = steps ? "Reversed steps, X 2–8" : "Requests, X 2–8";
                    var subtitle = compact ? "Full-data Y; gap at 6; labels in X 2–8" : "Utilization labels within X 2–8; full-data Y scales; gap at sample 6";
                    var recipe = ExampleRecipes.Named(steps ? "step-line-viewport" : "line-viewport");
                    var chart = (Chart)recipe.Create(variant, mode);
                    var viewport = new ChartCartesianViewport(x: new ChartXViewport(2, 8));
                    var model = new ViewportExample(chart, viewport);
                    var prepared = model.Prepare(GalleryContext(width, height, mode, title, subtitle, !compact));
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
                    File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
                    WriteThumbnail(output, model, id, title, family, mode, !compact);
                    WriteRecipe(output, id, recipe, variant, title, subtitle, mode, width, height, !compact, viewport: true);
                    artifacts.Add(new ProofArtifact(id, family, title, variant, mode.ToString().ToLowerInvariant(), width, height,
                        prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count, new[] { kind.ToString() },
                        prepared.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray(), Compact: compact));
                }
    }

    // Example glue keeps the same immutable viewport when the ordinary thumbnail writer prepares a new display size.
    // Downloaded programs call Chart.Prepare(context, viewport) directly and do not depend on this private wrapper.
    private sealed class ViewportExample : IVisualRenderable {
        private readonly Chart _chart;
        private readonly ChartCartesianViewport _viewport;

        public ViewportExample(Chart chart, ChartCartesianViewport viewport) { _chart = chart; _viewport = viewport; }
        public PreparedVisual Prepare(VisualRenderContext context) => _chart.Prepare(context, _viewport);
    }

}
