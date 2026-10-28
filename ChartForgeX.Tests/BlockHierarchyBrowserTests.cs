using System.Text.Json;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class BlockHierarchyBrowserTests {
    [BrowserTheory]
    [InlineData(1100, false)]
    [InlineData(390, false)]
    [InlineData(1100, true)]
    [InlineData(390, true)]
    public async Task NativeNestedBlockContoursCaptionsAndCrossBoundaryConnectorsPaintWithoutBrowserLayout(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var block = BlockHierarchyDepthTests.Parsed(width, dark); var svg = block.ToSvg();
        BlockHierarchyDepthTests.Save(block, "browser-source-" + width + "-" + dark);
        await using var session = await BrowserProofFonts.OpenAsync(svg, width, 620);
        await session.Page.EvaluateAsync("()=>document.fonts.ready");
        var observations = await session.Page.EvaluateAsync<JsonElement>("""
            ()=>{const rect=node=>{const b=node.getBBox();return {x:b.x,y:b.y,width:b.width,height:b.height}};const cells=[...document.querySelectorAll('[data-cfx-role=block-layout-item],[data-cfx-role=block-layout-composite]')];return {
                cells:cells.map(node=>({id:node.dataset.blockId,parent:node.dataset.parentId||null,rect:rect(node)})),
                connectors:[...document.querySelectorAll('[data-cfx-role=block-layout-edge]')].map(node=>({source:node.dataset.sourceId,target:node.dataset.targetId,length:node.getTotalLength()})),
                captions:[...document.querySelectorAll('[data-cfx-role=block-layout-node-label]')].map(node=>({id:node.dataset.blockId,text:node.textContent,rect:rect(node),font:getComputedStyle(node).fontSize})),
                active:document.querySelectorAll('script,foreignObject').length};}
            """);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var prefix = Path.Combine(directory, "block-depth-browser-" + width + "-" + dark);
            await File.WriteAllTextAsync(prefix + ".json", observations.GetRawText());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = prefix + ".png", FullPage = true });
        }
        Assert.Equal(7, observations.GetProperty("cells").GetArrayLength()); Assert.Equal(2, observations.GetProperty("connectors").GetArrayLength());
        Assert.Equal(0, observations.GetProperty("active").GetInt32());
        var cells = observations.GetProperty("cells").EnumerateArray().ToDictionary(cell => cell.GetProperty("id").GetString()!, cell => cell);
        foreach (var cell in cells.Values) {
            var parentId = cell.GetProperty("parent").GetString(); if (parentId == null) continue;
            var child = cell.GetProperty("rect"); var parent = cells[parentId].GetProperty("rect");
            Assert.True(child.GetProperty("x").GetDouble() >= parent.GetProperty("x").GetDouble() - .02);
            Assert.True(child.GetProperty("y").GetDouble() >= parent.GetProperty("y").GetDouble() - .02);
            Assert.True(child.GetProperty("x").GetDouble() + child.GetProperty("width").GetDouble() <= parent.GetProperty("x").GetDouble() + parent.GetProperty("width").GetDouble() + .02);
            Assert.True(child.GetProperty("y").GetDouble() + child.GetProperty("height").GetDouble() <= parent.GetProperty("y").GetDouble() + parent.GetProperty("height").GetDouble() + .02);
        }
        Assert.Equal(new[] { "API", "Worker", "Queue", "Data", "Client" }, observations.GetProperty("captions").EnumerateArray().Select(caption => caption.GetProperty("text").GetString()));
        Assert.All(observations.GetProperty("connectors").EnumerateArray(), edge => Assert.True(edge.GetProperty("length").GetDouble() > 0));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
