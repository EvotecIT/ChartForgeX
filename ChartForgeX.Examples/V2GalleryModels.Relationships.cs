using ChartForgeX.Core;

public static partial class V2GalleryModels {
    private static Chart TeamRelationships(ChartSeriesKind kind, string variant) {
        var configured = variant == "options";
        var nodes = new[] {
            new ChartNode("all", "All teams"), new ChartNode("engineering", "Engineering"), new ChartNode("operations", "Operations"),
            new ChartNode("platform", "Platform"), new ChartNode("engineering-support", configured ? "Support" : "Services"),
            new ChartNode("operations-support", "Support")
        };
        var links = new[] {
            new ChartTreeLink("all", "engineering", 60), new ChartTreeLink("all", "operations", 40),
            new ChartTreeLink("engineering", "platform", 35), new ChartTreeLink("engineering", "engineering-support", 25),
            new ChartTreeLink("operations", "operations-support", 40)
        };
        var chart = Chart.Create().WithDataLabels();
        return kind == ChartSeriesKind.Tree ? chart.AddTree("Teams", nodes, links) : chart.AddSunburst("Teams", nodes, links);
    }

    private static Chart FlowRelationships(string variant) {
        if (variant == "options") {
            var configured = Chart.Create().AddSankey("Requests", new[] {
                new ChartNode("received", "Received"), new ChartNode("north", "North"), new ChartNode("south", "South"),
                new ChartNode("north-support", "Support"), new ChartNode("south-support", "Support"), new ChartNode("completed", "Completed")
            }, new[] {
                new ChartFlowLink("received-north", "received", "north", 72), new ChartFlowLink("received-south", "received", "south", 28),
                new ChartFlowLink("north-standard", "north", "north-support", 52), new ChartFlowLink("north-priority", "north", "north-support", 20),
                new ChartFlowLink("south-standard", "south", "south-support", 28),
                new ChartFlowLink("north-completed", "north-support", "completed", 72), new ChartFlowLink("south-completed", "south-support", "completed", 28)
            });
            configured.Series[0].WithNodeState("north-support", ChartSeriesState.Warning);
            return configured;
        }
        return Chart.Create().AddSankey("Requests", new[] {
            new ChartNode("received", "Received"), new ChartNode("automatic", "Automatic"), new ChartNode("manual", "Manual"),
            new ChartNode("completed", "Completed"), new ChartNode("review", "Review")
        }, new[] {
            new ChartFlowLink("received-automatic", "received", "automatic", 72), new ChartFlowLink("received-manual", "received", "manual", 28),
            new ChartFlowLink("automatic-completed", "automatic", "completed", 65), new ChartFlowLink("automatic-review", "automatic", "review", 7),
            new ChartFlowLink("manual-completed", "manual", "completed", 20), new ChartFlowLink("manual-review", "manual", "review", 8)
        });
    }
}
