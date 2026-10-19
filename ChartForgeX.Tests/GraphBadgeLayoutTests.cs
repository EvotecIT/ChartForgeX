using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class GraphBadgeLayoutTests {
    [Fact]
    public void OverviewKeepsItsCompleteHiddenCountInAPaddedBadge() {
        var graph = GraphScene.Create("badge-stage", "Estate").AddNode("root", "Global estate", node => node.Size = 20);
        for (var index = 0; index < 2000; index++)
            graph.AddNode("child-" + index, "Service " + index, node => node.ParentId = "root");
        var stage = graph.CreateStages(options => options.Depths.Add(0)).First();
        var svg = graph.ToGraphSvg(stage);
        var document = XDocument.Parse(svg);
        var root = document.Descendants().Single(element => (string?)element.Attribute("data-node-id") == "root");
        Assert.Equal("+2000", (string?)root.Attribute("data-node-badge"));
        AssertPaddedBadge(document.Root!, "+2000");
        var image = RasterImageDecoder.Decode(graph.ToGraphPng(stage, options => { options.Width = 960; options.Height = 560; }));
        Assert.Equal(960, image.Width); Assert.Equal(560, image.Height);
    }

    [Fact]
    public void ExplicitLongBadgeRetainsEveryDigitInStaticOutput() {
        var graph = GraphScene.Create("long-badge", "Counts")
            .AddNode("root", "R", node => node.BadgeText = "+1234567890");
        AssertPaddedBadge(XDocument.Parse(graph.ToGraphSvg()).Root!, "+1234567890");
    }

    [Fact]
    public void PreparedCardBadgeKeepsTheFullCountAboveItsTitleWhileCircleAnchorStaysOutside() {
        var graph = GraphScene.Create("card-badge", "Counts")
            .AddNode("card", "KERBEROS.MICROSOFTONLINE.COM😀", node => {
                node.Shape = GraphNodeShape.Box; node.Size = 62; node.Metadata["topology.card"] = "true";
                node.BadgeText = "+1234567890"; node.SecondaryLabel = "Domain";
            })
            .AddNode("circle", "Circle", node => { node.Size = 30; node.BadgeText = "+2"; });
        var svg = XDocument.Parse(graph.ToGraphSvg());
        var card = svg.Descendants().Single(element => (string?)element.Attribute("data-node-details-for") == "card");
        var badge = card.Elements().Single(element => (string?)element.Attribute("class") == "cfx-graph-node-badge");
        var coordinates = badge.Attribute("transform")!.Value.Replace("translate(", "", StringComparison.Ordinal).TrimEnd(')').Split(' ');
        var badgeY = double.Parse(coordinates[1], CultureInfo.InvariantCulture);
        var titleY = double.Parse(card.Elements().Single(element => ((string?)element.Attribute("class"))?.Split(' ').Contains("cfx-graph-node-label") == true).Attribute("y")!.Value, CultureInfo.InvariantCulture);
        Assert.True(badgeY - 8 >= -36, "The badge remains inside the prepared card's upper edge.");
        Assert.True(badgeY + 8 < titleY - 12.5, "The badge's surface ends above the primary title's ink.");
        Assert.Equal("+1234567890", badge.Descendants().Single(element => element.Name.LocalName == "text").Value);
        var circle = svg.Descendants().Single(element => (string?)element.Attribute("data-node-details-for") == "circle");
        Assert.Equal("translate(24.6 -24.6)", circle.Elements().Single(element => (string?)element.Attribute("class") == "cfx-graph-node-badge").Attribute("transform")!.Value);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true)]
    public async Task LiveUpdatesAndReconstructedExportsKeepCompleteBadges(HtmlGraphRenderBackend backend, bool dark) {
        if (!Enabled) return;
        var graph = GraphScene.Create("badge-live", "Service counts")
            .AddNode("root", "Service", node => { node.Fixed = true; node.X = 480; node.Y = 280; node.Size = 20; node.BadgeText = "+2000"; });
        graph.Options.Features = GraphSceneFeatures.Viewport | GraphSceneFeatures.Export | GraphSceneFeatures.IncrementalUpdates;
        if (backend == HtmlGraphRenderBackend.Canvas) {
            graph.Options.Features |= GraphSceneFeatures.LevelOfDetail;
            graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        }
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false;
        }), 1000, 720);
        var page = session.Page;
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-graph-explorer').dataset.cfxGraphRendererActive !== undefined");
        await page.EvaluateAsync("""
            () => {
              window.badgePaint=[]; const paint=CanvasRenderingContext2D.prototype.fillText;
              CanvasRenderingContext2D.prototype.fillText=function(text,...args){badgePaint.push(String(text));return paint.call(this,text,...args);};
              document.querySelector('.cfx-graph-explorer').addEventListener('cfxgraphexport',event=>{window.badgeExport=event.detail.content;event.preventDefault();});
              ChartForgeXGraphExplorer.update('badge-live',{upsertNodes:[{id:'root',label:'Service',x:480,y:280,size:20,fixed:true,badge:'+1234567890'}],fit:true});
            }
            """);
        if (backend == HtmlGraphRenderBackend.Canvas)
            await page.WaitForFunctionAsync("() => badgePaint.includes('+1234567890')");
        else {
            var fits = await page.Locator("[data-node-details-for=root] .cfx-graph-node-badge").EvaluateAsync<bool>("""
                badge=>{const text=badge.querySelector('text'),surface=badge.querySelector('rect');if(!surface)return false;
                  const t=text.getBBox(),s=surface.getBBox();return text.textContent==='+1234567890'&&t.x>=s.x+3&&t.x+t.width<=s.x+s.width-3;}
                """);
            Assert.True(fits, "The complete live badge needs measured horizontal padding.");
        }
        await page.EvaluateAsync("() => ChartForgeXGraphExplorer.export('badge-live','svg')");
        var exported = XDocument.Parse(await page.EvaluateAsync<string>("() => badgeExport"));
        AssertPaddedBadge(exported.Root!, "+1234567890");
        var retained = await page.EvaluateAsync<JsonElement>("() => ChartForgeXGraphExplorer.get('badge-live').nodes[0]");
        Assert.Equal("root", retained.GetProperty("id").GetString());
        Assert.Equal("+1234567890", retained.GetProperty("badge").GetString());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "graph-badge-" + backend + ".png"), FullPage = true });
        }
        AssertNoConsoleErrors(session);
    }

    private static void AssertPaddedBadge(XElement root, string expected) {
        var text = root.Descendants().Single(element => element.Name.LocalName == "text" && element.Value == expected);
        var badge = text.Parent!;
        var surface = badge.Elements().Single(element => element.Name.LocalName == "rect");
        Assert.True(double.Parse(surface.Attribute("width")!.Value, CultureInfo.InvariantCulture) > 16);
        Assert.Equal("16", surface.Attribute("height")!.Value);
    }
}
