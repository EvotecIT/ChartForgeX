using ChartForgeX.Themes;

internal static partial class MermaidExamples {
    private static void WriteKanbanSourceGeometryPrograms(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            foreach (var variant in new[] { "wide", "compact" }) {
                var name = "mermaid-kanban-source-" + variant + "-" + mode.ToString().ToLowerInvariant();
                File.WriteAllText(Path.Combine(output, name + ".csharp.txt"), ExampleRecipeSource.EmitKanban(variant, mode));
                File.WriteAllText(Path.Combine(output, name + ".mmd"), MermaidFixtureSources.Read("kanban-source-geometry"));
            }
    }
}
