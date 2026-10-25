/// <summary>Ordinary dependency-free Wardley composition with native numbered annotations and sourcing paint.</summary>
public static class WardleyDepthRecipe {
    /// <summary>Creates the same source-ordered map for wide or compact native output.</summary>
    public static WardleyMapBlock Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var block = WardleyMapBlock.Create().WithTitle("Delivery strategy").WithSize(variant == "compact" ? 390 : 1100, 600)
            .WithPadding(30).WithTheme(VisualTheme.Graphite(), mode).WithAnnotationsBox(.18, 0);
        block.ShowGrid = true;
        block.SetStages(new[] { "New / Idea", "Custom", "Product", "Commodity" }).WithStageBoundaries(new[] { .30, .55, .78, 1d });
        block.AddNode("User", "User", .94, .20, WardleyMapNodeKind.Anchor);
        var build = block.AddNode("Build", "Build", .78, .12); build.Strategy = "build";
        var buy = block.AddNode("Buy", "Buy", .64, .29); buy.Strategy = "buy"; buy.Inertia = true;
        var service = block.AddNode("Service", "Service", .50, .30); service.Strategy = "outsource";
        var market = block.AddNode("Market", "Market", .76, .65); market.Strategy = "market";
        foreach (var node in new[] { build, buy, service, market }) { node.LabelOffsetX = 14; node.LabelOffsetY = -14; }
        service.LabelOffsetY = -30;
        block.AddLink("User", "Build"); block.AddLink("Build", "Buy"); block.AddLink("Buy", "Service");
        var pipeline = block.AddPipeline("Service"); pipeline.Outline = new ChartForgeX.Core.ChartStrokeStyle(dash: Array.Empty<double>()); pipeline.ShowEvolutionLinks = true;
        var small = pipeline.AddComponent("Service_Small", "Small", .18); small.LabelOffsetX = -12; small.LabelOffsetY = 16;
        var scale = pipeline.AddComponent("Service_Scale", "Scale", .48); scale.LabelOffsetX = -8; scale.LabelOffsetY = 30;
        block.AddLink("Service_Scale", "Market"); block.AddEvolution("Service_Scale", .62);
        block.AddMarker("Standards", .36, .05, WardleyMapMarkerKind.Accelerator);
        block.AddMarker("Regulation", .36, .65, WardleyMapMarkerKind.Deaccelerator);
        block.AddAnnotation(2, "Prefer a portable service contract", .70, .50);
        block.AddAnnotation(1, "Keep the user need visible", .88, .40);
        return block;
    }
}
