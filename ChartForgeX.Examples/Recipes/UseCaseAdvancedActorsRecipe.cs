/// <summary>Authors every finite native Actor form and a business use case through the Core topology model.</summary>
/// <remarks>The separately shared .mmd fixture proves parsed-source consumption. This recipe has no Mermaid dependency.</remarks>
public static class UseCaseAdvancedActorsRecipe {
    /// <summary>Creates wide/compact and light/dark examples with source-stable actors, glyphs, stereotypes, routes and notes.</summary>
    public static TopologyChart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var compact = variant == "compact";
        var chart = TopologyChart.Create().WithId("native-usecase-advanced-actors").WithTitle("Participants in a request")
            .WithSubtitle("Plain, hollow, business, silhouette and finite native icon actors")
            .WithTheme(VisualTheme.Graphite(), mode).WithViewport(compact ? 390 : 1100, compact ? 1160 : 680, 28)
            .WithLayout(TopologyLayoutMode.Layered, compact ? TopologyLayoutDirection.LeftToRight : TopologyLayoutDirection.TopToBottom);
        chart.DefaultRenderOptions = new TopologyRenderOptions { FitContentToViewport = false, IncludeLegend = false,
            WrapNodeLabels = true, AllowMultilineNodeLabels = true, MaxNodeLabelLines = 6, MaxNodeSubtitleLines = 6 };
        var ids = new[] { "Plain", "Hollow", "Business", "BusinessHollow", "Silhouette", "Icon" };
        var stereotypes = new[] { "participant", "external", "business", "partner", "operator", "system" };
        var shapes = new[] { TopologyNodeShape.Actor, TopologyNodeShape.ActorHollow, TopologyNodeShape.ActorBusiness,
            TopologyNodeShape.ActorBusinessHollow, TopologyNodeShape.ActorSilhouette, TopologyNodeShape.ActorIcon };
        var associations = new[] { "plain", "hollow", "business", "partner", "operator", "system" };
        for (var index = 0; index < ids.Length; index++) {
            chart.AddAutoNode(ids[index], ids[index], width: 140, height: 116);
            var actor = chart.Nodes[index]; actor.Shape = shapes[index]; actor.Kind = TopologyNodeKind.Generic;
            actor.Subtitle = "«" + stereotypes[index] + "»";
        }
        // Retain finite native geometry independently of the optional catalog id, including after save/load.
        chart.Nodes[5].IconId = "network:router"; chart.Nodes[5].IconShape = TopologyIconShape.Router;
        chart.Nodes[5].Kind = TopologyNodeKind.Gateway; chart.Nodes[5].Symbol = "RTR"; chart.Nodes[5].Color = "#2563EB";
        chart.AddAutoNode("Submit", "Submit request", width: 190, height: 100);
        chart.Nodes[6].Shape = TopologyNodeShape.BusinessEllipse; chart.Nodes[6].Kind = TopologyNodeKind.Generic; chart.Nodes[6].Subtitle = "«business action»";
        foreach (var node in chart.Nodes) { node.ShowStatusBadge = false; node.DisplayMode = TopologyNodeDisplayMode.Card; node.PreserveDisplayModeSize = true; }
        for (var index = 0; index < ids.Length; index++)
            chart.AddEdge(associations[index], ids[index], "Submit", kind: TopologyEdgeKind.Dependency,
                direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.Straight);
        chart.Annotations.Add(new TopologyAnnotation("participants", "All participants retain their own identity", "Submit"));
        chart.Accessibility.Name = "Finite native use case actor forms";
        return chart;
    }
}
