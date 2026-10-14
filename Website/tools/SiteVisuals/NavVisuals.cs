// Thumbnails for the header navigation panels: one small ChartForgeX render per destination, in both themes.
// Menu items in site.json name their thumbnail with their Id (static/site/nav/<id>-light.svg and -dark.svg).
using System.Text.Json;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Terminal;
using ChartForgeX.Themes;

namespace SiteVisuals;

internal static class NavVisuals {
    private const int Width = 240;
    private const int Height = 150;

    public static Dictionary<string, object> Write(VisualOutput output, Dictionary<string, object> docs) {
        var figures = (SortedDictionary<string, object>)docs["figures"];
        var thumbs = new SortedDictionary<string, object>(StringComparer.Ordinal) {
            ["gallery"] = output.Pair("nav/gallery", (tokens, _) => GalleryGrid(tokens).ToSvg("cfx-nav-gallery")),
            ["mermaid"] = output.Pair("nav/mermaid", (_, dark) => MermaidAtlas.Render("nav-mermaid", """
                flowchart LR
                  src([Mermaid]) --> parse{Parse}
                  parse --> svg[SVG]
                  parse --> png[PNG]
                """, dark, new List<string>())),
            ["playground"] = output.Pair("nav/playground", (_, dark) => VisualOutput.Playground("chartforgex", """
                ```chartforgex chart v1 {#nav title="Live preview" type="donut"}
                series "Checks"
                | Label    | Value |
                | -------- | ----- |
                | Passed   | 1260  |
                | Warnings | 340   |
                | Failed   | 120   |
                ```
                """, dark)),
            // The terminal SVG types itself out; the PNG is the finished console, which suits a still thumbnail.
            ["powershell"] = output.PngPair("nav/powershell", (_, dark) => Terminal(output.Website, dark).ToPng()),
            ["docs"] = VisualOutput.Reuse((Dictionary<string, object>)figures["topology"]),
            ["api"] = output.Pair("nav/api", (_, dark) => MermaidAtlas.Render("nav-api", """
                classDiagram
                  class Chart {
                    +WithTitle(title)
                    +ToSvg() string
                  }
                  class ChartSeries
                  class ChartGrid
                  ChartGrid o-- Chart
                  Chart *-- ChartSeries
                """, dark, new List<string>())),
            ["examples"] = output.Pair("nav/examples", (tokens, _) => SmallMultiples(tokens).ToSvg("cfx-nav-examples"))
        };

        WriteNavIndex(output, thumbs);
        return new Dictionary<string, object> { ["thumbs"] = thumbs };
    }

    // The API reference header comes from the API docs generator, which renders menus without descriptions.
    // site.js enriches it from this index, built from the same site.json menu the theme header uses.
    private static void WriteNavIndex(VisualOutput output, SortedDictionary<string, object> thumbs) {
        using var site = JsonDocument.Parse(File.ReadAllText(Path.Combine(output.Website, "site.json")));
        var menus = site.RootElement.GetProperty("Navigation").GetProperty("Menus");
        var main = menus.EnumerateArray().Single(menu => menu.GetProperty("Name").GetString() == "main");
        var groups = new List<object>();
        foreach (var group in main.GetProperty("Items").EnumerateArray()) {
            if (!group.TryGetProperty("Items", out var items)) continue;
            groups.Add(new {
                title = group.GetProperty("Title").GetString(),
                id = group.GetProperty("Id").GetString(),
                description = group.GetProperty("Description").GetString(),
                items = items.EnumerateArray().Select(item => new {
                    id = item.GetProperty("Id").GetString(),
                    title = item.GetProperty("Title").GetString(),
                    url = item.GetProperty("Url").GetString(),
                    description = item.GetProperty("Description").GetString(),
                    key = item.GetProperty("Meta").GetProperty("key").GetInt32(),
                    hollow = item.GetProperty("Meta").TryGetProperty("hollow", out var hollow) && hollow.GetBoolean(),
                    thumb = thumbs[item.GetProperty("Id").GetString()!]
                }).ToArray()
            });
        }

        output.Text("nav/nav.json", JsonSerializer.Serialize(new { groups }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();

    private static Chart Mini(VisualDesignTokens tokens) => Chart.Create().WithDesignTokens(tokens).WithSparkline().WithTransparentBackground();

    private static ChartGrid GalleryGrid(VisualDesignTokens tokens) {
        var bars = Mini(tokens).WithXLabels("A", "B", "C", "D", "E").AddBar("Orders", Points(42, 61, 54, 78, 70));
        var line = Mini(tokens).AddSmoothLine("Latency", Points(182, 176, 190, 168, 150, 142, 139))
            .AddSmoothLine("Target", Points(160, 160, 160, 160, 160, 160, 160));
        var donut = Mini(tokens).WithXLabels("Passed", "Warnings", "Failed").WithDonutCenterLabel(false).AddDonut("Checks", Points(70, 20, 10));
        var area = Mini(tokens).AddStackedArea("Passkey", Points(8, 17, 30, 47, 64, 81))
            .AddStackedArea("App", Points(72, 75, 75, 73, 69, 64))
            .AddStackedArea("SMS", Points(42, 38, 32, 26, 20, 15));
        foreach (var chart in new[] { bars, line, donut, area }) chart.WithSize(Width / 2, Height / 2);
        return ChartGrid.Create().WithColumns(2).WithGap(6).WithPadding(0).WithPanelSize(Width / 2 - 3, Height / 2 - 3)
            .Add(bars).Add(line).Add(donut).Add(area);
    }

    private static ChartGrid SmallMultiples(VisualDesignTokens tokens) {
        var sites = new[] {
            ("Warsaw", new double[] { 35, 42, 39, 51, 47, 58 }),
            ("London", new double[] { 48, 61, 57, 63, 70, 66 }),
            ("Berlin", new double[] { 22, 28, 31, 30, 36, 41 }),
            ("Madrid", new double[] { 51, 47, 44, 49, 42, 39 })
        };
        var grid = ChartGrid.Create().WithColumns(2).WithGap(6).WithPadding(0).WithPanelSize(Width / 2 - 3, Height / 2 - 3);
        foreach (var (site, values) in sites) {
            grid.Add(Mini(tokens).WithSize(Width / 2, Height / 2).AddArea(site, Points(values)));
        }

        return grid;
    }

    // The ImagePlayground lines of a published PowerShell example, typed into a console. No output is invented.
    private static TerminalStory Terminal(string website, bool dark) {
        var lines = File.ReadAllLines(Path.Combine(website, "powershell", "latency-trend.ps1"))
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("New-ImageChart", StringComparison.Ordinal))
            .Take(2)
            .ToArray();
        var story = TerminalStory.Create()
            .WithDialect(TerminalDialect.PowerShell)
            .WithTheme(dark ? TerminalTheme.GraphiteDark() : TerminalTheme.GraphiteLight())
            .WithWorkingDirectory(@"C:\Reports")
            .WithWidth(Width * 3)
            .WithPngOutputScale(1)
            .WithFinalPrompt(false);
        foreach (var line in lines) story.Command(line);
        return story;
    }
}
