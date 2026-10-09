using System.Text;
using ChartForgeX.Themes;

public static partial class V2Examples {
    private static readonly string[] ModelSourceFiles = {
        "V2GalleryModels.cs", "V2GalleryModels.Ranges.cs", "V2GalleryModels.Radial.cs", "V2GalleryModels.NumericRadial.cs", "V2GalleryModels.MatrixMap.cs", "V2GalleryModels.Specialty.cs", "V2GalleryModels.Diagrams.cs", "V2GalleryModels.Options.cs"
    };
    private static string ModelSnippet(string expression, string title, string subtitle, VisualThemeMode mode, int width, int height, bool? legend, string artifactKind) {
        var source = new StringBuilder("using System;\nusing System.Linq;\nusing System.Globalization;\nusing ChartForgeX.Core;\nusing ChartForgeX.Primitives;\nusing ChartForgeX.Rendering;\nusing ChartForgeX.Themes;\nusing ChartForgeX.Topology;\nusing ChartForgeX.Typography;\nusing ChartForgeX.VisualArtifacts;\nusing ChartForgeX.VisualBlocks;\n\n");
        source.Append("// The licensed font fixtures are included beside the gallery outputs.\nFontRegistry.Register(\"").Append(ProofFont)
            .Append("\", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(\"").Append(ProofFont).Append("\", \"fonts/Carlito-Bold.ttf\", 700);\n");
        source.Append("var model = ").Append(expression).Append(";\nvar context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(").Append(width).Append(", ").Append(height)
            .Append(")), VisualTheme.Graphite(), VisualThemeMode.").Append(mode).Append(",\n    new VisualFrame(").Append(Literal(title)).Append(", ").Append(Literal(subtitle))
            .Append(", showLegend: ").Append(legend.HasValue ? (legend.Value ? "true" : "false") : "null").Append("), FontSpec.FromFamily(\"").Append(ProofFont).Append("\"));\n")
            .Append("var prepared = model.Prepare(context);\nSystem.IO.File.WriteAllText(\"visual.svg\", prepared.ToSvg());\nSystem.IO.File.WriteAllBytes(\"visual.png\", prepared.ToPng());\n")
            .Append("System.IO.File.WriteAllText(\"visual.html\", prepared.ToArtifact(\"example\", VisualArtifactKind.").Append(artifactKind).Append(").ToHtmlPage());\n\n");
        // Embed the compiled factory sources, so the downloadable program cannot drift from the model used to render its row.
        foreach (var file in ModelSourceFiles) {
            using var stream = typeof(V2Examples).Assembly.GetManifestResourceStream("ChartForgeX.Examples." + file)
                ?? throw new InvalidOperationException("Gallery model source is missing: " + file);
            using var reader = new StreamReader(stream);
            foreach (var line in reader.ReadToEnd().Split('\n')) if (!line.StartsWith("using ", StringComparison.Ordinal)) source.AppendLine(line.TrimEnd('\r'));
        }
        return source.ToString().TrimEnd() + "\n";
    }
}
