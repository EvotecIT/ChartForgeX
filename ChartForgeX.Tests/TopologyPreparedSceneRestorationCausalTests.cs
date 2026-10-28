using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyPreparedSceneRestorationCausalTests {
    internal static TopologyChart Source(bool subtitle) {
        var chart = TopologyChart.Create().WithTitle("Saved title").WithViewport(900, 650, 24).WithLegend(null).WithLayout(TopologyLayoutMode.Manual);
        chart.Subtitle = subtitle ? "Saved subtitle" : null;
        chart.Groups.Add(new TopologyGroup { Id = "scope", Label = "Scope", X = 24, Y = 100, Width = 700, Height = 240 });
        chart.Nodes.Add(new TopologyNode { Id = "a", Label = "Accept", GroupId = "scope", X = 60, Y = 170, Width = 144, Height = 64, DisplayMode = TopologyNodeDisplayMode.Card, PreserveDisplayModeSize = true });
        chart.Nodes.Add(new TopologyNode { Id = "b", Label = "Build", GroupId = "scope", X = 400, Y = 170, Width = 144, Height = 64, DisplayMode = TopologyNodeDisplayMode.Card, PreserveDisplayModeSize = true });
        chart.AddEdge("link", "a", "b"); chart.Edges[0].Waypoints.Add(new ChartPoint(260, 202)); chart.Edges[0].Waypoints.Add(new ChartPoint(340, 202));
        return chart;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ImportedPreparedSceneBoundsAndAuthoredWaypointsDoNotReceiveTheFrameOffsetAgain(bool subtitle) {
        var original = Source(subtitle).Prepare(); var saved = VisualArtifactInterchangeEnvelope.FromJson(original.ToInterchangeEnvelope().ToJson());
        var imported = saved.ToTopologyChart(); var restored = imported.Prepare(); var after = restored.ToInterchangeEnvelope();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var name = "prepared-scene-restore-" + (subtitle ? "subtitle" : "title");
            File.WriteAllText(Path.Combine(output, name + ".before.svg"), original.ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".before.png"), original.ToPng()); File.WriteAllText(Path.Combine(output, name + ".before.json"), saved.ToJson());
            File.WriteAllText(Path.Combine(output, name + ".after.svg"), restored.ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".after.png"), restored.ToPng()); File.WriteAllText(Path.Combine(output, name + ".after.json"), after.ToJson());
        }
        Assert.Equal(saved.Groups[0].Y, (double?)imported.Groups[0].Y);
        Assert.Equal((saved.Groups[0].X, saved.Groups[0].Y, saved.Groups[0].Width, saved.Groups[0].Height), (after.Groups[0].X, after.Groups[0].Y, after.Groups[0].Width, after.Groups[0].Height));
        for (var index = 0; index < saved.Nodes.Count; index++) Assert.Equal((saved.Nodes[index].X, saved.Nodes[index].Y, saved.Nodes[index].Width, saved.Nodes[index].Height), (after.Nodes[index].X, after.Nodes[index].Y, after.Nodes[index].Width, after.Nodes[index].Height));
        Assert.Equal(saved.Edges[0].Topology!.Waypoints.Select(point => (point.X, point.Y)), after.Edges[0].Topology!.Waypoints.Select(point => (point.X, point.Y)));
        Assert.Equal(saved.Width, after.Width); Assert.Equal(saved.Height, after.Height);
    }
}
