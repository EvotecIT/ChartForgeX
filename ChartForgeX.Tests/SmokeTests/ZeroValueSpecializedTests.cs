using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Verify = Xunit.Assert;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ZeroValueSpecializedChartsPreservePointIndexes() {
        var pie = Chart.Create().WithSize(540, 320).WithXLabels("Zero", "Live", "Tail")
            .AddPie("Slices", Points(0, 60, 40));
        pie.Series[0].WithPointColor(1, "#E11D48").WithPointSliceOffset(1, .12);
        var piePrepared = PreparedFamily(pie);
        var sources = FamilyGroups(piePrepared, "radial-point");
        Verify.Equal(new[] { "1", "2" }, sources.Select(source => source.Metadata["data-cfx-point"]));
        Verify.Equal(new[] { "Live", "Tail" }, sources.Select(source => source.Metadata["data-cfx-label"]));
        Verify.Equal(new[] { 60d, 40 }, sources.Select(source => FamilyNumber(source, "data-cfx-value")));
        var slices = piePrepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(slice => slice.Role == "pie-slice").ToArray();
        Verify.Equal(2, slices.Length);
        Verify.Equal(ChartColor.FromHex("#E11D48"), slices[0].Fill);
        var displacement = Math.Sqrt(Math.Pow(slices[0].Cx - slices[1].Cx, 2) + Math.Pow(slices[0].Cy - slices[1].Cy, 2));
        Verify.Equal(slices[0].Outer * .12, displacement, 8);
        Verify.Equal(new[] { "Live", "Tail", "Zero" }, FamilyLabels(piePrepared, "legend-label").Select(FamilyContent));
        Verify.Equal("0", FamilyContent(FamilyLabels(piePrepared, "legend-value")[^1]));
        Verify.Equal("0%", FamilyContent(FamilyLabels(piePrepared, "legend-percentage")[^1]));
        Verify.Contains(FamilyGroups(piePrepared, "legend-entry"), entry => entry.Id == "legend-series-0-point-0"
            && entry.Metadata["aria-label"] == "Zero: 0 (0%)");
        Verify.NotEmpty(piePrepared.ToPng());

        var polarArea = Chart.Create().WithSize(540, 320).WithXLabels("Zero", "Live", "Tail")
            .AddPolarArea("Segments", Points(0, 60, 40));
        polarArea.Series[0].WithPointColor(1, "#0F766E");
        var polarPrepared = PreparedFamily(polarArea);
        var polarSources = FamilyGroups(polarPrepared, "polar-area-point-source");
        Verify.Equal(new[] { "0", "1", "2" }, polarSources.Select(source => source.Metadata["data-cfx-point"]));
        var zero = Verify.Single(polarPrepared.Scene.Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "polar-area-zero-slot");
        var positive = polarPrepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(slice => slice.Role == "polar-area-segment").ToArray();
        Verify.Equal(2, positive.Length);
        Verify.InRange(zero.Inner / zero.Outer, .9, .99);
        Verify.Equal(ChartColor.FromHex("#0F766E"), positive[0].Fill);
        Verify.Equal(zero.Start + zero.Sweep, positive[0].Start, 10);
        Verify.Equal(positive[0].Start + positive[0].Sweep, positive[1].Start, 10);
        Verify.Equal(new[] { "Zero", "Live", "Tail" }, FamilyLabels(polarPrepared, "legend-label").Select(FamilyContent));
        Verify.NotEmpty(polarPrepared.ToPng());

        var funnel = Chart.Create().WithSize(920, 560).WithDataLabels().WithXLabels("Opened", "Deferred", "Closed")
            .AddFunnel("Pipeline", Points(120, 0, 32));
        funnel.Series[0].WithPointColor(2, "#7C3AED");
        var funnelPrepared = PreparedFamily(funnel);
        var stages = FamilyGroups(funnelPrepared, "funnel-stage");
        Verify.Equal(new[] { 120d, 0, 32 }, stages.Select(stage => FamilyNumber(stage, "data-cfx-value")));
        Verify.Equal("1", stages[1].Metadata["data-cfx-point"]);
        Verify.Equal("Deferred", stages[1].Metadata["data-cfx-label"]);
        Verify.Equal("true", stages[1].Metadata["data-cfx-zero"]);
        var marker = Verify.Single(funnelPrepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "funnel-zero");
        Verify.InRange(Math.Abs(marker.End.X - marker.Start.X), 1, 16);
        var marks = funnelPrepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "funnel-segment").ToArray();
        Verify.Equal(2, marks.Length);
        Verify.Equal(ChartColor.FromHex("#7C3AED"), marks[1].Fill);
        Verify.Equal(32d / 120, FamilyNumber(stages[2], "data-cfx-retention"), 12);
        Verify.Equal("false", stages[2].Metadata["data-cfx-dropoff-defined"]);
        Verify.False(stages[2].Metadata.ContainsKey("data-cfx-dropoff"));
        Verify.Contains(FamilyLabels(funnelPrepared, "funnel-label"), label => FamilyContent(label) == "Deferred: 0");
        Verify.Contains(FamilyLabels(funnelPrepared, "funnel-ratio"), label => FamilyContent(label).Contains("No previous baseline"));
        Verify.NotEmpty(funnelPrepared.ToPng());
    }
}
