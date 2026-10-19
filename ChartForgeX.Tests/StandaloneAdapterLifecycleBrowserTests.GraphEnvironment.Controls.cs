using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    private static async Task CaptureGraphEnvironmentWideHoverAsync(IPage page, string name) {
        var zoom = page.GetByRole(AriaRole.Button, new() { Name = "Zoom out", Exact = true });
        await zoom.HoverAsync();
        await page.WaitForFunctionAsync("()=>getComputedStyle(retainedGraph.querySelector('[data-cfx-graph-action=zoom-out]'),'::before').opacity==='1'");
        await WriteReceiptAsync(name + "-wide-hover", await zoom.EvaluateAsync<JsonElement>("""
            node=>{const hint=getComputedStyle(node,'::before');return{label:hint.content,opacity:hint.opacity,
                colour:hint.color,background:hint.backgroundColor,control:node.getBoundingClientRect().toJSON()};}
            """));
        await CaptureAsync(page, name + "-wide-hover");
        await page.Mouse.MoveAsync(2, 2);
    }

    private static Task<JsonElement> GraphEnvironmentControlWitnessAsync(ILocator target) => target.EvaluateAsync<JsonElement>("""
        target=>{
            const box=target.getBoundingClientRect(),x=box.x+box.width/2,y=box.y+box.height/2,hit=document.elementFromPoint(x,y);
            return {name:target.getAttribute('aria-label'),box:box.toJSON(),center:{x,y},nativeHit:target===hit||target.contains(hit),
                hit:hit?.outerHTML.slice(0,400),hitName:hit?.closest('button,summary')?.getAttribute('aria-label'),
                insideViewport:x>0&&x<innerWidth&&y>0&&y<innerHeight};
        }
        """);

    private static async Task ClickGraphEnvironmentControlAsync(IPage page, ILocator target, string name) {
        await target.ScrollIntoViewIfNeededAsync();
        var witness = await GraphEnvironmentControlWitnessAsync(target);
        await WriteReceiptAsync(name, witness);
        Assert.True(witness.GetProperty("insideViewport").GetBoolean(), witness.GetRawText());
        Assert.True(witness.GetProperty("nativeHit").GetBoolean(), witness.GetRawText());
        var center = witness.GetProperty("center");
        await page.Mouse.ClickAsync((float)center.GetProperty("x").GetDouble(), (float)center.GetProperty("y").GetDouble());
    }

    private static async Task AssertGraphEnvironmentMenuPaintAsync(IPage page, string name) {
        // Check before any option scroll/focus can reveal a clipped menu accidentally.
        var observations = new List<JsonElement>();
        foreach (var label in new[] { "Export SVG document", "Export PNG image", "Export JSON data" }) {
            var witness = await GraphEnvironmentControlWitnessAsync(page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true }));
            observations.Add(witness);
        }
        await WriteReceiptAsync(name + "-export-menu", JsonSerializer.SerializeToElement(observations));
        await CaptureAsync(page, name + "-export-menu");
        foreach (var witness in observations) {
            Assert.True(witness.GetProperty("insideViewport").GetBoolean(), witness.GetRawText());
            Assert.True(witness.GetProperty("nativeHit").GetBoolean(), witness.GetRawText());
        }
    }
}
