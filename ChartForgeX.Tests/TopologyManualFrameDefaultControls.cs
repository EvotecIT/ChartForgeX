using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyManualFrameDefaultControls {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrdinaryAuthoredManualCoordinatesKeepTheirExistingFrameExportBytes(bool subtitle) {
        var chart = TopologyPreparedSceneRestorationCausalTests.Source(subtitle); var prepared = chart.Prepare(); var saved = prepared.ToInterchangeEnvelope(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "authored-manual-frame-default-" + (subtitle ? "subtitle" : "title"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png); File.WriteAllText(path + ".json", saved.ToJson());
        }
        Assert.True(saved.Groups[0].Y!.Value > chart.Groups[0].Y); Assert.Equal(100, chart.Groups[0].Y); Assert.Contains("Saved title", svg);
        if (subtitle) Assert.Contains("Saved subtitle", svg); Assert.True(png.Length > 64);
    }
}
