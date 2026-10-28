using System.Text;
using System.Text.Json;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyNestedArtworkBudgetCausalTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void NestedRasterDeclarationsUseTheSameExistingEarlyNativeResourceBudget(bool dataSvg) {
        var ppm = "data:image/x-portable-pixmap;base64," + Convert.ToBase64String(Encoding.ASCII.GetBytes("P6\n6000 6000\n255\n"));
        var chart = Source(Artwork(ppm, dataSvg));
        var error = Observe("declared-limit-" + dataSvg, chart);
        var limit = Assert.IsType<VisualResourceLimitException>(error);
        Assert.Equal("NativeRasterResourceBytes", limit.Limit); Assert.Equal("nested", limit.ItemId);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OrdinaryMalformedNestedImageKeepsVisibleArtworkFallbackWithoutBecomingABudgetError(bool dataSvg) {
        var chart = Source(Artwork("data:image/png;base64,AA==", dataSvg));
        Assert.Null(Observe("malformed-control-" + dataSvg, chart));
        Assert.NotEmpty(chart.ToPng());
    }
    private static TopologyIconArtwork Artwork(string raster, bool dataSvg) {
        var fragment = "<rect width='64' height='64' fill='#24A060'/><image width='64' height='64' href='" + raster + "'/>";
        return dataSvg ? TopologyIconArtwork.Image("data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(
            "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64' viewBox='0 0 64 64'>" + fragment + "</svg>")))
            : TopologyIconArtwork.InlineSvg(fragment, "0 0 64 64");
    }
    private static TopologyChart Source(TopologyIconArtwork artwork) {
        var chart = TopologyChart.Create().WithId("nested-budget").WithTitle("").WithViewport(320, 240, 24).WithLegend(null)
            .AddArtworkNode("nested", "Nested", artwork, 40, 40, width: 64, height: 64);
        chart.Nodes[0].ShowLabel = false; chart.Nodes[0].ShowStatusBadge = false;
        return chart.WithRenderOptions(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, FitContentToViewport = false });
    }
    private static Exception? Observe(string name, TopologyChart chart) {
        Exception? error = null;
        try {
            var prepared = chart.Prepare();
            if (Output() is {} output) {
                File.WriteAllText(Path.Combine(output, "nested-artwork-" + name + ".svg"), prepared.ToSvg());
                File.WriteAllBytes(Path.Combine(output, "nested-artwork-" + name + ".png"), prepared.ToPng());
                File.WriteAllText(Path.Combine(output, "nested-artwork-" + name + ".json"), prepared.ToInterchangeEnvelope().ToJson());
            }
        } catch (Exception observed) { error = observed; }
        if (Output() is {} folder) File.WriteAllText(Path.Combine(folder, "nested-artwork-" + name + ".observed.json"),
            JsonSerializer.Serialize(new { Type = error?.GetType().FullName, error?.Message, Limit = (error as VisualResourceLimitException)?.Limit }));
        return error;
    }
    private static string? Output() { if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return null; Directory.CreateDirectory(output); return output; }
}
