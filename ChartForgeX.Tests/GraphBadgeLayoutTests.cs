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
    [InlineData(HtmlGraphRenderBackend.Canvas, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true)]
    public async Task LiveUpdatesAndReconstructedExportsKeepCompleteBadges(HtmlGraphRenderBackend backend, bool dark) {
        if (!Enabled) return;
        var graph = GraphScene.Create("badge-live", "Service counts")
            .AddNode("root", "Service", node => { node.Fixed = true; node.X = 480; node.Y = 280; node.Size = 20; node.BadgeText = "+2000"; node.IconText = "A"; })
            .AddNode("target", "Preference", node => { node.Fixed = true; node.X = 700; node.Y = 280; })
            .AddNode("member", "Member", node => { node.Fixed = true; node.X = 200; node.Y = 150; })
            .AddEdge("route", "root", "target", "replication")
            .AddCluster("region", "Region", new[] { "member" }, cluster => cluster.Collapsed = true);
        graph.Options.Features = GraphSceneFeatures.Viewport | GraphSceneFeatures.Export | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Clustering;
        if (backend == HtmlGraphRenderBackend.Canvas) {
            graph.Options.Features |= GraphSceneFeatures.LevelOfDetail;
            graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        }
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false;
        }), 1000, 720, configurePageAsync: page => page.EvaluateAsync("""
            () => {
              window.graphFontPaints=[]; window.graphFontMeasures=[];
              const paint=CanvasRenderingContext2D.prototype.fillText, measure=CanvasRenderingContext2D.prototype.measureText;
              CanvasRenderingContext2D.prototype.fillText=function(text,...args){
                graphFontPaints.push({text:String(text),font:this.font,loaded:document.fonts.check(this.font,String(text))});
                return paint.call(this,text,...args);
              };
              CanvasRenderingContext2D.prototype.measureText=function(text){
                graphFontMeasures.push({text:String(text),font:this.font,loaded:document.fonts.check(this.font,String(text))});
                return measure.call(this,text);
              };
            }
            """));
        var page = session.Page;
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-graph-explorer').dataset.cfxGraphRendererActive !== undefined");
        if (backend == HtmlGraphRenderBackend.Canvas) {
            // No update, input or physics tick may be needed to replace a font-loading first paint.
            await page.EvaluateAsync("async()=>{await document.fonts.ready;await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));}");
            Assert.NotEqual("running", await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-physics-state"));
            await AssertCanvasFontsAsync(page, "Archivo", "+2000");
        }
        await page.EvaluateAsync("""
            () => {
              window.badgePaint=[]; const paint=CanvasRenderingContext2D.prototype.fillText;
              CanvasRenderingContext2D.prototype.fillText=function(text,...args){badgePaint.push(String(text));return paint.call(this,text,...args);};
              document.querySelector('.cfx-graph-explorer').addEventListener('cfxgraphexport',event=>{window.badgeExport=event.detail.content;event.preventDefault();});
              ChartForgeXGraphExplorer.update('badge-live',{upsertNodes:[{id:'root',label:'Service',x:480,y:280,size:20,fixed:true,icon:'A',badge:'+1234567890'}],fit:true});
            }
            """);
        if (backend == HtmlGraphRenderBackend.Canvas)
            await page.WaitForFunctionAsync("() => badgePaint.includes('+1234567890')");
        else {
            var measured = await page.Locator("[data-node-details-for=root] .cfx-graph-node-badge").EvaluateAsync<JsonElement>("""
                badge=>{const text=badge.querySelector('text'),surface=badge.querySelector('rect'),t=text.getBBox(),s=surface.getBBox(),font=getComputedStyle(text).font;
                  const context=document.createElement('canvas').getContext('2d');context.font=font;const metrics=context.measureText(text.textContent);
                  return {text:text.textContent,font,left:t.x-s.x,right:s.x+s.width-t.x-t.width,surface:s.width,glyph:t.width,advance:metrics.width,ink:metrics.actualBoundingBoxLeft+metrics.actualBoundingBoxRight};}
                """);
            Assert.Equal("+1234567890", measured.GetProperty("text").GetString());
            Assert.True(measured.GetProperty("left").GetDouble() >= 3 && measured.GetProperty("right").GetDouble() >= 3, measured.GetRawText());
        }
        await page.EvaluateAsync("() => ChartForgeXGraphExplorer.export('badge-live','svg')");
        var exported = XDocument.Parse(await page.EvaluateAsync<string>("() => badgeExport"));
        AssertPaddedBadge(exported.Root!, "+1234567890");
        var retained = await page.EvaluateAsync<JsonElement>("() => ChartForgeXGraphExplorer.get('badge-live').nodes[0]");
        Assert.Equal("root", retained.GetProperty("id").GetString());
        Assert.Equal("+1234567890", retained.GetProperty("badge").GetString());
        if (backend == HtmlGraphRenderBackend.Canvas) {
            await page.EvaluateAsync("""
                ()=>{const root=document.querySelector('.cfx-graph-explorer');root.style.setProperty('--hfx-font-sans','Georgia');
                  graphFontPaints=[];graphFontMeasures=[];ChartForgeXGraphExplorer.update(root,{upsertNodes:[{id:'root',label:'Service',x:480,y:280,size:20,fixed:true,icon:'A',badge:'+1234567890'}]});}
                """);
            await AssertCanvasFontsAsync(page, "Georgia", "+1234567890");
        }
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "graph-badge-" + backend + ".png"), FullPage = true });
        }
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertCanvasFontsAsync(IPage page, string family, string badge) {
        var facts = await page.EvaluateAsync<JsonElement>("""
            badge=>({theme:document.querySelector('.cfx-graph-explorer').dataset.cfxGraphThemeActive,
              firstPaint:graphFontPaints[0],unloadedPaints:graphFontPaints.filter(paint=>!paint.loaded).length,
              paints:['Service','Preference','A','replication','Region','1 object',badge].map(text=>
              graphFontPaints.findLast(paint=>paint.text===text)||{text,font:'missing',loaded:false}),
              measure:graphFontMeasures.findLast(measure=>measure.text===badge)||{font:'missing',loaded:false}})
            """, badge);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = "graph-font-" + facts.GetProperty("theme").GetString() + "-" + family;
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".json"), facts.GetRawText());
            await page.ScreenshotAsync(new() { Path = Path.Combine(capture, name + ".png"), FullPage = true });
        }
        foreach (var paint in facts.GetProperty("paints").EnumerateArray()) {
            Assert.Contains(family, paint.GetProperty("font").GetString());
            Assert.True(paint.GetProperty("loaded").GetBoolean(), facts.GetRawText());
        }
        var measured = facts.GetProperty("measure");
        Assert.Contains(family, measured.GetProperty("font").GetString());
        Assert.True(measured.GetProperty("loaded").GetBoolean(), facts.GetRawText());
        Assert.Equal(measured.GetProperty("font").GetString(), facts.GetProperty("paints").EnumerateArray().Last().GetProperty("font").GetString());
    }

    private static void AssertPaddedBadge(XElement root, string expected) {
        var text = root.Descendants().Single(element => element.Name.LocalName == "text" && element.Value == expected);
        var badge = text.Parent!;
        var surface = badge.Elements().Single(element => element.Name.LocalName == "rect");
        Assert.True(double.Parse(surface.Attribute("width")!.Value, CultureInfo.InvariantCulture) > 16);
        Assert.Equal("16", surface.Attribute("height")!.Value);
    }
}
