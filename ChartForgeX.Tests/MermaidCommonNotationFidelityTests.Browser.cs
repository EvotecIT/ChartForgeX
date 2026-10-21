using System.Globalization;
using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidCommonNotationFidelityTests {
    [BrowserTheory]
    [InlineData("class", false, 360)]
    [InlineData("class", false, 1100)]
    [InlineData("class", true, 360)]
    [InlineData("class", true, 1100)]
    [InlineData("state", false, 360)]
    [InlineData("state", false, 1100)]
    [InlineData("state", true, 360)]
    [InlineData("state", true, 1100)]
    [InlineData("er", false, 360)]
    [InlineData("er", false, 1100)]
    [InlineData("er", true, 360)]
    [InlineData("er", true, 1100)]
    public async Task NativeNotationRemainsVisibleInCompactAndWideScrollableHosts(string family, bool dark, int width) {
        var body = family switch {
            "class" => "classDiagram\nclass Repository~Item~ {\n+List~Item~ items\n+find() Item\n}\nRepository \"1\" <|--|> Store : contracts\nnote for Repository \"One stable identity\\nTyped generic members\"",
            "state" => "stateDiagram-v2\nstate Fork <<fork>>\nstate Join <<join>>\nstate Choice <<choice>>\n[*] --> Fork\nFork --> Ready\nFork --> Waiting\nReady --> Join\nWaiting --> Join\nJoin --> Choice\nChoice --> Complete : approved\nnote right of Ready : Two independent paths",
            _ => "erDiagram\nC[\"Customer account\"] {\nstring id PK, FK \"External key\"\nstring display_name\n}\nO[Orders] {\nint id PK\n}\nC ||--o{ O : places" 
        };
        var source = "%%{init: {'fontFamily':'" + BrowserProofFonts.Family + "','theme':'" + (dark ? "dark" : "default") + "'}}%%\n" + body;
        var options = new MermaidTopologyRenderOptions { Width = width, Height = 620 };
        var rendered = MermaidRenderer.Render(source, new MermaidRenderOptions { Class = options, State = options, EntityRelationship = options });
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        var svg = artifact.ToSvg();
        var png = artifact.ToPng();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 720);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(family == "er" ? 0 : 1, await session.Page.Locator("[data-cfx-role='topology-annotation']").CountAsync());
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        await host.FocusAsync();
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "notation-" + family + "-" + width.ToString(CultureInfo.InvariantCulture) + (dark ? "-dark" : "-light");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        }
        var scroll = await host.EvaluateAsync<double>("element => { element.scrollLeft = element.scrollWidth; return element.scrollLeft; }");
        Assert.True(scroll >= 0);
        if (string.IsNullOrWhiteSpace(output)) return;
        File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png);
        File.WriteAllText(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(new { family, width, dark, scroll, consoleErrors = 0 }));
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true });
    }
}
