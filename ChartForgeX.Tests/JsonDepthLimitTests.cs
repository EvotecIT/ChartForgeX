using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class JsonDepthLimitTests {
    [Fact]
    public void GeoJson_DeepNesting_FailsWithCleanErrorInsteadOfStackOverflow() {
        var hostile = new string('[', 100_000) + new string(']', 100_000);
        var error = Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapDefinition("deep", "Deep", hostile));
        Assert.Contains("too deep", error.Message, StringComparison.Ordinal);

        var justOver = "{\"type\":\"FeatureCollection\",\"features\":" + new string('[', 64) + new string(']', 64) + "}";
        Assert.Contains("too deep", Assert.Throws<ArgumentException>(() => ChartMapGeoJson.ToMapDefinition("over", "Over", justOver)).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parsers_HostileNestingOnSmallStack_FailCleanly() {
        var hostile = new string('[', 100_000) + new string(']', 100_000);
        var errors = new List<Exception?>();
        var thread = new Thread(() => {
            foreach (var parse in new Action[] {
                () => ChartMapGeoJson.ToMapDefinition("deep", "Deep", hostile),
                () => VisualDesignTokens.FromJson(hostile),
                () => TopologyIconPackJson.FromJson(hostile)
            }) {
                try {
                    parse();
                    errors.Add(null);
                } catch (Exception exception) {
                    errors.Add(exception);
                }
            }
        }, 512 * 1024);
        thread.Start();
        thread.Join();
        Assert.Equal(3, errors.Count);
        Assert.All(errors, error => Assert.IsType<ArgumentException>(error));
    }

    [Fact]
    public void GeoJson_DepthLimit_AllowsExactly64Levels() {
        var atLimit = "{\"type\":\"FeatureCollection\",\"features\":" + new string('[', 63) + new string(']', 63) + "}";
        var error = Record.Exception(() => ChartMapGeoJson.ToMapDefinition("edge", "Edge", atLimit));
        Assert.True(error == null || !error.Message.Contains("too deep", StringComparison.Ordinal), error?.Message);
    }

    [Fact]
    public void GeoJson_RealisticMultiPolygonDepth_StillParses() {
        const string json = "{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"id\":\"a\",\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"MultiPolygon\",\"coordinates\":[[[[0,0],[1,0],[1,1],[0,1],[0,0]]]]}}]}";
        var map = ChartMapGeoJson.ToMapDefinition("ok", "Ok", json);
        Assert.Single(map.Regions);
    }

    [Fact]
    public void DesignTokens_NestingBeyond32_IsRejected() {
        var deep = "{\"light\":" + new string('[', 40) + new string(']', 40) + "}";
        Assert.Contains("too deep", Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(deep)).Message, StringComparison.Ordinal);
        var hostile = new string('{', 1) + "\"light\":" + new string('[', 100_000) + new string(']', 100_000) + "}";
        Assert.Contains("too deep", Assert.Throws<ArgumentException>(() => VisualDesignTokens.FromJson(hostile)).Message, StringComparison.Ordinal);
    }
}
