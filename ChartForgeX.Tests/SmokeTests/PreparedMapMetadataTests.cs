using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Verify = Xunit.Assert;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void AssertMapCatalogSources(PreparedVisual prepared, string role, int filled, int missing, double california) {
        var map = Verify.Single(FamilyGroups(prepared, role));
        Verify.Equal("us-states", map.Metadata["data-cfx-map-id"]);
        Verify.Equal("51", map.Metadata["data-cfx-region-count"]);
        Verify.Equal(filled.ToString(), map.Metadata["data-cfx-filled-region-count"]);
        Verify.Equal(missing.ToString(), map.Metadata["data-cfx-missing-region-count"]);
        var regions = FamilyGroups(prepared, role + "-region-source");
        Verify.Equal(51, regions.Length);
        var ca = Verify.Single(regions, region => region.Metadata["data-cfx-region"] == "CA");
        Verify.Equal("California", ca.Metadata["data-cfx-region-name"]); Verify.Equal(california, FamilyNumber(ca, "data-cfx-value"));
        Verify.Contains(prepared.Regions, region => region.Role == role + "-region" && region.Label!.Contains("California"));
        Verify.Equal(51, prepared.Scene.Nodes.Count(node => node.Role == role + "-region"));
        Verify.Contains(prepared.Scene.Nodes, node => node.Role == "map-scale-step");
        Verify.Contains(prepared.Scene.Nodes, node => node.Role == "map-scale-no-data");
        Verify.NotEmpty(prepared.ToPng());
    }
}
