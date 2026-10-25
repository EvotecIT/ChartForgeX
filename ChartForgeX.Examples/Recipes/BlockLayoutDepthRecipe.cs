/// <summary>A complete core-only nested block diagram with genuine cross-boundary source IDs.</summary>
public static class BlockLayoutDepthRecipe {
    /// <summary>Creates a deterministic organization of source blocks; no browser layout or Mermaid runtime is required.</summary>
    public static BlockLayoutBlock Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant != "wide" && variant != "compact") throw new ArgumentOutOfRangeException(nameof(variant));
        var block = BlockLayoutBlock.Create().WithTitle("Request processing").WithSubtitle("Nested source grids and cross-boundary connectors")
            .WithTheme(VisualTheme.Graphite(), mode).WithSize(variant == "compact" ? 390 : 1000, 620).WithColumns(3)
            .AddComposite("services", "Services", columnSpan: 2, columns: 2)
            .AddItem("api", "API", "services", shape: BlockLayoutShape.Hexagon)
            .AddItem("worker", "Worker", "services", shape: BlockLayoutShape.Rounded)
            .AddComposite("storage", "Storage", columnSpan: 2, columns: 2, parentId: "services")
            .AddItem("queue", "Queue", "storage", shape: BlockLayoutShape.Subroutine)
            .AddItem("data", "Data", "storage", shape: BlockLayoutShape.Database)
            .AddItem("client", "Client", shape: BlockLayoutShape.Diamond)
            .AddEdge("client", "api", "Submit").AddEdge("worker", "data", "Store");
        block.Items[1].Fill = ChartForgeX.Primitives.ChartColor.FromHex("#dbeafe");
        block.Items[1].Stroke = new ChartForgeX.Core.ChartStrokeStyle(ChartForgeX.Primitives.ChartColor.FromHex("#1e3a8a"), 2);
        block.Items[1].TextStyle.WithColor("#172554").WithWeight("bold");
        return block;
    }
}
