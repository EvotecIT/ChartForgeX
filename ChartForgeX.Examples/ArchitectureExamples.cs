using System.Reflection;
using System.Text;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Markup;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

/// <summary>
/// Writes the architecture markup examples: the static diagram, the interactive use case player with its sequence
/// diagrams, a terminal rendering, and one sequence diagram per use case.
/// </summary>
internal static class ArchitectureExamples {
    private const string ResourcePrefix = "ChartForgeX.Examples.Architecture.";

    public static void Write(string output) {
        ExampleTypography.Register();
        var assembly = typeof(ArchitectureExamples).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames().Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal)).OrderBy(name => name, StringComparer.Ordinal)) {
            var name = "architecture-" + Path.GetFileNameWithoutExtension(resource.Substring(ResourcePrefix.Length));
            Save(output, name, Read(assembly, resource));
        }
    }

    private static void Save(string output, string name, string source) {
        var result = new MarkupArchitectureParser().Parse(source);
        if (result.HasErrors || result.Document == null) {
            throw new InvalidOperationException("Architecture example '" + name + "' has errors: " + string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Code + "(" + diagnostic.Line + "): " + diagnostic.Message)));
        }

        var chart = result.Document.WithTheme(result.Document.Theme.WithFontFamily(ExampleTypography.FontFamily));
        var prepared = chart.Prepare();
        File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg(), Encoding.UTF8);
        File.WriteAllBytes(Path.Combine(output, name + ".png"), prepared.ToPng());
        var options = new TopologyRenderOptions { EnableHtmlScenarioUrlState = true, ActiveScenarioId = chart.Scenarios.FirstOrDefault()?.Id }
            .WithHtmlScenarioPanel()
            .WithHtmlScenarioSequence();
        File.WriteAllText(Path.Combine(output, name + ".html"), ExampleTypography.EmbedInHtml(chart.ToInteractiveHtmlPage(options)), Encoding.UTF8);
        File.WriteAllText(Path.Combine(output, name + ".cfx.md"), source.Replace("\r\n", "\n"), Encoding.UTF8);
        var layout = TopologyCellLayout.Create(prepared, new TopologyCellLayoutOptions { MaximumColumns = 110 });
        File.WriteAllText(Path.Combine(output, name + ".txt"), layout.Paint().ToText() + "\n", new UTF8Encoding(false));
        foreach (var scenario in chart.Scenarios) {
            var sequence = chart.ToSequenceArtifact(scenario.Id);
            File.WriteAllText(Path.Combine(output, name + "-" + scenario.Id + "-sequence.svg"), sequence.ToSvg(), Encoding.UTF8);
            File.WriteAllBytes(Path.Combine(output, name + "-" + scenario.Id + "-sequence.png"), sequence.ToPng());
            var htmlPath = Path.Combine(output, name + "-" + scenario.Id + "-sequence.html");
            sequence.ToVisualArtifact().SaveHtml(htmlPath);
            File.WriteAllText(htmlPath, ExampleTypography.EmbedInHtml(File.ReadAllText(htmlPath)), Encoding.UTF8);
        }
    }

    private static string Read(Assembly assembly, string resource) {
        using var stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException("Missing embedded example " + resource + ".");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
