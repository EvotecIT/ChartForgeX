using ChartForgeX.Themes;

/// <summary>Writes the Active Directory site map that combines sibling grouping with annotated links.</summary>
public static partial class V2Examples {
    private static void WriteSiteLinks(string output, ICollection<ProofArtifact> artifacts) {
        var recipe = ExampleRecipes.Named("topology-site-links");
        foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark })
            WriteModel(output, artifacts, recipe.Create("sites", mode), "topology-site-links-" + mode.ToString().ToLowerInvariant(),
                "topology", "Contoso replication sites", "sites", "Domain controllers grouped per site; links show transports, cost and evidence",
                mode, 1360, 720, true, recipe);
    }
}
