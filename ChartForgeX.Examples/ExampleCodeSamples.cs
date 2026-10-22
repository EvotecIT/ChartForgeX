/// <summary>Writes standalone source from the same compiled factories used by selected generated charts.</summary>
internal static class ExampleCodeSamples {
    public static void Write(string output) {
        foreach (var selection in ExampleRecipes.LegacyCharts)
            File.WriteAllText(Path.Combine(output, selection.Name + ".csharp.txt"),
                ExampleRecipeSource.EmitChart(selection.Recipe, selection.Name, selection.Variant, selection.Mode));
    }
}
