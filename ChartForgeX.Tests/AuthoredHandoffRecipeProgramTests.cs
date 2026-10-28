using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class AuthoredHandoffRecipeProgramTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)] [InlineData(VisualThemeMode.Dark)]
    public void CompleteNativeConsumerProgramCapturesTheAuthoredRouteAndSeparateLabelSurface(VisualThemeMode mode) {
        var chart = TopologyAuthoredPathBadgeRecipe.Create().WithTheme(VisualTheme.Graphite(), mode);
        var artifact = chart.ToVisualArtifact(); var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var json = artifact.ToInterchangeJson(); var html = artifact.ToHtmlPage();
        var program = ExampleRecipeSource.EmitAuthoredHandoff(mode);
        var text = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text");
        Assert.Equal("#193857", (string?)Assert.Single(text, element => element.Value == "Queued").Attribute("fill"));
        Assert.Equal("#20472B", (string?)Assert.Single(text, element => element.Value == "Ready").Attribute("fill"));
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "authored-handoff-program-" + mode);
            File.WriteAllText(path + ".Program.cs", program); File.WriteAllText(path + ".expected.svg", svg);
            File.WriteAllBytes(path + ".expected.png", png); File.WriteAllText(path + ".expected.json", json);
            File.WriteAllText(path + ".expected.html", html);
        }
        var saved = VisualArtifactInterchangeEnvelope.FromJson(json); var restored = saved.ToTopologyChart();
        var after = restored.ToVisualArtifact();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } restoredOutput) {
            var path = Path.Combine(restoredOutput, "authored-handoff-program-" + mode + "-restored");
            File.WriteAllText(path + ".svg", after.ToSvg()); File.WriteAllBytes(path + ".png", after.ToPng()); File.WriteAllText(path + ".json", after.ToInterchangeJson());
        }
        Assert.Equal("#193857", restored.Edges.Single().LabelBackgroundColor);
        Assert.Equal("M 284 190 C 350 42 444 42 510 190", chart.Edges.Single().PathGeometry!.PathData);
        Assert.Equal(saved.Edges.Single().Topology!.PathGeometry!.PathData, restored.Edges.Single().PathGeometry!.PathData);
        Assert.Equal(TopologyStaticPaintObservation.Paint(svg), TopologyStaticPaintObservation.Paint(after.ToSvg())); Assert.Equal(png, after.ToPng());
    }
}
