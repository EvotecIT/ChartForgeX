using System.Globalization;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WardleyEvolutionDirectionTests {
    [Theory]
    [InlineData(.7, .2)] [InlineData(.2, .7)]
    public void EvolutionHeadPointsAtTheAuthoredTargetFromEitherDirection(double source, double target) {
        var map = WardleyMapBlock.Create().WithSize(600, 400); map.AddNode("api", "API", .6, source); map.AddEvolution("api", target);
        var svg = map.ToSvg(); var png = map.ToPng();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, target < source ? "wardley-left-evolution" : "wardley-right-evolution");
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png);
        }
        var xml = XDocument.Parse(svg); XElement Role(string role) => xml.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == role);
        var line = Role("wardley-evolve"); var path = Role("wardley-evolve-arrow");
        var points = Assert.Single(ChartMapPathParser.ParseSubpaths((string)path.Attribute("d")!)).Points;
        var tip = double.Parse(line.Attribute("x2")!.Value, CultureInfo.InvariantCulture); Assert.Equal(tip, points[0].X);
        var direction = Math.Sign(target - source);
        Assert.All(points.Skip(1), tail => Assert.True((tip - tail.X) * direction > 0, "The complete triangular head must point in the authored evolution direction."));
        Assert.Equal(source, map.Nodes[0].Evolution); Assert.Equal(target, map.Evolutions[0].TargetEvolution); Assert.NotEmpty(png);
    }
}
