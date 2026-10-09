using ChartForgeX.Core;

public static partial class V2GalleryModels {
    private static Chart ChordRelationships(string variant) {
        var configured = variant == "options";
        var chart = Chart.Create().AddChord("Transfers", new[] {
            new ChartNode("north", "North"), new ChartNode("south", "South"),
            new ChartNode("north-support", configured ? "Support" : "Services"), new ChartNode("south-support", "Support")
        }, new[] {
            new ChartFlowLink("north-south", "north", "south", 18), new ChartFlowLink("south-north", "south", "north", 7),
            new ChartFlowLink("north-standard", "north", "north-support", 12), new ChartFlowLink("north-priority", "north", "north-support", 5),
            new ChartFlowLink("support-south", "north-support", "south", 9), new ChartFlowLink("south-support", "south", "south-support", 14),
            new ChartFlowLink("support-return", "south-support", "north", 4), new ChartFlowLink("support-internal", "south-support", "south-support", 3),
            new ChartFlowLink("zero-transfer", "north-support", "south-support", 0)
        });
        if (configured) {
            chart.WithChord(options => {
                options.StartAngleDegrees = -120;
                options.SweepAngleDegrees = 300;
                options.NodeGapDegrees = 5;
                options.NodeThicknessRatio = .09;
                options.RibbonOpacity = .45;
                options.LabelContent = ChartChordLabelContent.LabelAndTotals;
            });
            chart.Series[0].WithNodeState("north-support", ChartSeriesState.Warning);
        }
        return chart;
    }
}
