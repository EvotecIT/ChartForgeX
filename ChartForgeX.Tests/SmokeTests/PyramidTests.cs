using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Verify = Xunit.Assert;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PyramidSeriesRenderProportionalPartitions() {
        var chart = Chart.Create().WithSize(720, 440).WithDataLabels().WithXLabels("Services", "Platform", "Support")
            .AddPyramid("Allocation", new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20) });
        var prepared = PreparedFamily(chart); var stages = FamilyGroups(prepared, "pyramid-stage");
        Verify.Equal(3, stages.Length); Verify.Equal(.5, FamilyNumber(stages[0], "data-cfx-length-fraction"), 12);
        Verify.Equal(.3, FamilyNumber(stages[1], "data-cfx-length-fraction"), 12);
        Verify.Equal(.2, FamilyNumber(stages[2], "data-cfx-length-fraction"), 12);
        Verify.Equal(3, prepared.Scene.Nodes.Count(node => node.Role == "pyramid-segment"));
        Verify.Contains(prepared.Regions, region => region.Role == "pyramid-stage" && region.Label == "Platform: 30, height encoding, 30% of total");
        Verify.NotEmpty(prepared.ToPng());
    }
}
