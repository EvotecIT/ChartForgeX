using System.Text.Json;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StaticExportNativeFontBrowserTests {
    // Nonbreaking spaces reserve real advance while keeping glyph ink out of the corridor.
    private static readonly string DecorationCorridorCaption = "I" + new string('\u00a0', 12) + "I";

    private static Task<JsonElement> CaptureDecorationCorridorAsync(ILocator svg) => svg.EvaluateAsync<JsonElement>("""
        (svg,caption)=>{
            const text=[...svg.querySelectorAll('text')].find(node=>node.textContent===caption);
            if(!text)throw new Error('Missing reserved decoration caption.');
            const box=text.getBBox(),font=parseFloat(getComputedStyle(text).fontSize),matrix=text.getCTM();
            const left=new DOMPoint(box.x+box.width*.25,box.y-font*.125).matrixTransform(matrix);
            const right=new DOMPoint(box.x+box.width*.75,box.y+box.height+font*.25).matrixTransform(matrix);
            return {left:Math.ceil(left.x),top:Math.floor(left.y),right:Math.floor(right.x),bottom:Math.ceil(right.y)};
        }
        """, DecorationCorridorCaption);

    private static void AssertDecorationCorridor(Exported decorated, Exported undecorated, JsonElement region, string name) {
        var actual = PngReader.Decode(Convert.FromBase64String(decorated.Png));
        var control = PngReader.Decode(Convert.FromBase64String(undecorated.Png));
        var scale = decorated.Report.GetProperty("scale").GetInt32();
        var left = region.GetProperty("left").GetInt32() * scale; var top = region.GetProperty("top").GetInt32() * scale;
        var right = region.GetProperty("right").GetInt32() * scale; var bottom = region.GetProperty("bottom").GetInt32() * scale;
        Assert.Equal(control.Width, actual.Width); Assert.Equal(control.Height, actual.Height);
        Assert.InRange(left, 0, actual.Width - 1); Assert.InRange(right, left + 1, actual.Width);
        Assert.InRange(top, 0, actual.Height - 1); Assert.InRange(bottom, top + 1, actual.Height);
        var background = control.Pixels.AsSpan((top * control.Width + left) * 4, 4).ToArray();
        Assert.Equal(255, background[3]);
        var controlInk = 0; var decoratedInk = 0;
        for (var y = top; y < bottom; y++) for (var x = left; x < right; x++) {
            var index = (y * control.Width + x) * 4;
            if (!control.Pixels.AsSpan(index, 4).SequenceEqual(background)) controlInk++;
            if (!actual.Pixels.AsSpan(index, 4).SequenceEqual(background)) decoratedInk++;
        }
        File.WriteAllText(CapturePath(name + ".corridor.json"), JsonSerializer.Serialize(new {
            left, top, right, bottom, background, controlInk, decoratedInk,
            boundary = "Actual paired PNG downloads; an otherwise empty internal caption corridor. No replay-difference or RGB tolerance."
        }));
        Assert.Equal(0, controlInk);
        Assert.True(decoratedInk > 0, "The actual PNG must paint the authored decoration through the empty caption corridor.");
    }
}
