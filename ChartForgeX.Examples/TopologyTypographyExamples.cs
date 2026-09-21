using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.Interactivity.Html;

internal static class TopologyTypographyExamples {
    public static void Write(string output) {
        Directory.CreateDirectory(output);
        foreach (var (name, family) in new[] { ("sans", "Arial, sans-serif"), ("mono", "monospace") }) {
            var theme = TopologyTheme.Light();
            theme.FontFamily = family;
            var chart = TopologyChart.Create().WithTheme(theme).WithViewport(1000, 420)
                .WithTitle("Measured labels across cards, compact cards, and tiles")
                .AddNode("card", "Wide WWW characters\nSecond title line", 55, 150, width: 260, height: 150, subtitle: "Service ownership")
                .AddNode("compact", "Compact WWW label", 430, 170, width: 108, height: 52, subtitle: "WWWWWWWWWWWW")
                .AddNode("tile", "Tile WWW label", 750, 170, width: 64, height: 46, subtitle: "WWWWWWWWWWWW")
                .AddEdge("first", "card", "compact", "WWWWWWWWWWWW")
                .AddEdge("second", "compact", "tile", "serves");
            chart.Nodes[0].DisplayMode = TopologyNodeDisplayMode.Card;
            chart.Nodes[0].Details.Add(new TopologyNodeDetail { Label = "Owner", Value = "Operations" });
            chart.Nodes[1].DisplayMode = TopologyNodeDisplayMode.CompactCard;
            chart.Nodes[2].DisplayMode = TopologyNodeDisplayMode.Tile;
            var options = new TopologyRenderOptions {
                TextMeasurementMode = TextMeasurementMode.InstalledFonts,
                HeaderStyle = TopologyHeaderStyle.CenterBanner,
                IncludeLegend = false,
                IncludeTileSubtitles = true,
                CardSubtitleMode = TopologyCardSubtitleMode.Chip
            };
            var path = Path.Combine(output, "topology-measured-typography-" + name);
            File.WriteAllText(path + ".html", chart.ToInteractiveHtmlPage(options));
            File.WriteAllText(path + ".svg", chart.ToSvg(options));
            File.WriteAllBytes(path + ".png", chart.ToPng(options));
            Console.WriteLine($"{name}: {chart.GetPngFontInfo().ResolvedFaceName}");
        }
    }
}
