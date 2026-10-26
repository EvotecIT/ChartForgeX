using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Actual source/native companions and executable download, using the existing browser session owner.</summary>
[Trait("BrowserSuite","Compatibility")]
public sealed class SvgRasterColorPrimitiveBrowserTests {
    [BrowserTheory]
    [InlineData(390,false)] [InlineData(390,true)] [InlineData(760,false)] [InlineData(760,true)]
    public async Task FiniteColorSourcesKeepTheirChannelsCoverageAndCompleteCoreDownloadVisible(int width,bool dark) {
        var primitives=new[] {
            "<feColorMatrix in='SourceGraphic' values='0 0 1 0 0 0 1 0 0 0 1 0 0 0 0 0 0 0 .5 0'/>",
            "<feColorMatrix in='SourceGraphic' type='luminanceToAlpha'/>",
            "<feComponentTransfer in='SourceGraphic'><feFuncR type='gamma' exponent='2'/></feComponentTransfer>",
            "<feComponentTransfer in='SourceGraphic'><feFuncR type='table' tableValues='1 0'/><feFuncG type='discrete' tableValues='0 .25 .75 1'/></feComponentTransfer>",
            "<feComponentTransfer in='SourceGraphic'><feFuncR type='gamma' amplitude='.25' exponent='-1'/></feComponentTransfer>",
            "<feColorMatrix in='SourceGraphic' type='saturate' values='0' result='gray'/><feComponentTransfer in='gray' result='power'><feFuncR type='gamma' exponent='2'/></feComponentTransfer><feComponentTransfer in='power'><feFuncR type='linear' slope='.5'/></feComponentTransfer>"
        };
        var names=new[]{"Matrix and alpha","Luminance to alpha","Gamma","Table and discrete","Negative gamma","Three color stages"};
        var html="<style>body{margin:0;padding:16px;box-sizing:border-box;background:"+(dark?"#151a22;color:#f4f6fa":"#f8fafc;color:#162030")
            +";font:14px sans-serif}main{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}figure{margin:0}svg,img{width:64px;height:64px;image-rendering:pixelated}a{color:inherit}</style><h1>Finite color sources</h1><main>";
        for(var index=0;index<primitives.Length;index++) {
            var source=Source(primitives[index],index);var image=ColorPrimitiveFixture.Import(source).Image;
            html+="<figure data-case='"+index+"'><figcaption>"+names[index]+"</figcaption>"+source
                +"<img alt='Native color output' src='data:image/png;base64,"+Convert.ToBase64String(PngWriter.WriteRgba(image))+"'></figure>";
        }
        using var stream=typeof(SvgRasterColorPrimitiveBrowserTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.RecipeColorSourceProgram.cs")
            ?? throw new InvalidOperationException("Missing complete Core color source program.");
        using var reader=new StreamReader(stream);var program=reader.ReadToEnd();
        html+="</main><a download='color-sources.cs' href='data:text/plain;charset=utf-8,"+Uri.EscapeDataString(program)+"'>Download complete C# program</a>";
        await using var session=await BrowserProofFonts.OpenAsync(html,width,460);
        var observed=await session.Page.EvaluateAsync<JsonElement>("""
            async () => {
              const rows=[];
              for(const figure of document.querySelectorAll('figure')) {
                const source=new Image();source.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(figure.querySelector('svg').outerHTML)));await source.decode();
                const native=figure.querySelector('img');await native.decode();
                function sample(image) { const canvas=document.createElement('canvas');canvas.width=8;canvas.height=8;const ctx=canvas.getContext('2d');ctx.drawImage(image,0,0);
                  return {inside:[...ctx.getImageData(3,3,1,1).data],outside:[...ctx.getImageData(0,0,1,1).data]}; }
                const svg=figure.querySelector('svg'),filter=svg.querySelector('filter'),mark=svg.querySelector('rect');
                const referencedId=mark.getAttribute('filter').slice(5,-1);
                rows.push({caption:figure.querySelector('figcaption').textContent,source:sample(source),native:sample(native),
                  filterId:filter.id,ownFilterResolves:document.getElementById(referencedId)===filter,
                  matchingIds:document.querySelectorAll('[id="'+filter.id+'"]').length});
              }
              const link=document.querySelector('a[download]');const program=decodeURIComponent(link.href.split(',').slice(1).join(','));
              return {userAgent:navigator.userAgent,rows,downloadName:link.download,complete:program.includes('SvgRasterizer.Rasterize')&&program.includes('File.WriteAllBytes')&&program.includes('using ChartForgeX.SvgRaster;'),
                overflow:document.documentElement.scrollWidth>innerWidth,scripts:document.querySelectorAll('svg script,svg foreignObject').length};
            }
            """);
        var directory=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if(!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);var stem=Path.Combine(directory,"color-source-"+width+"-"+dark);
            File.WriteAllText(stem+".observed.json",observed.GetRawText());File.WriteAllText(stem+".csharp.txt",program);
            for(var index=0;index<primitives.Length;index++) {
                var source=Source(primitives[index],index);File.WriteAllText(stem+"-"+index+".source.svg",source);
                File.WriteAllBytes(stem+"-"+index+".native.png",PngWriter.WriteRgba(ColorPrimitiveFixture.Import(source).Image));
            }
            await session.Page.ScreenshotAsync(new PageScreenshotOptions{Path=stem+".browser.png",FullPage=true});
        }
        Assert.Equal(6,observed.GetProperty("rows").GetArrayLength());
        foreach(var row in observed.GetProperty("rows").EnumerateArray()) {
            Assert.False(string.IsNullOrWhiteSpace(row.GetProperty("caption").GetString()));
            Assert.True(row.GetProperty("ownFilterResolves").GetBoolean());
            Assert.Equal(1,row.GetProperty("matchingIds").GetInt32());
            foreach(var kind in new[]{"source","native"}) {
                var samples=row.GetProperty(kind);
                Assert.True(samples.GetProperty("inside")[3].GetInt32()>0);
                Assert.Equal(new[]{0,0,0,0},samples.GetProperty("outside").EnumerateArray().Select(value=>value.GetInt32()));
            }
            // Representative constant interiors permit a one-byte consumer readback difference. This is not a whole-image identity claim.
            var actual=row.GetProperty("source").GetProperty("inside");var native=row.GetProperty("native").GetProperty("inside");
            for(var channel=0;channel<4;channel++)Assert.InRange(Math.Abs(actual[channel].GetInt32()-native[channel].GetInt32()),0,1);
        }
        Assert.True(observed.GetProperty("complete").GetBoolean());Assert.Equal("color-sources.cs",observed.GetProperty("downloadName").GetString());
        Assert.False(observed.GetProperty("overflow").GetBoolean());Assert.Equal(0,observed.GetProperty("scripts").GetInt32());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static string Source(string primitive,int index) => ColorPrimitiveFixture.Source(primitive)
        .Replace("id='colors'","id='colors-"+index+"'")
        .Replace("url(#colors)","url(#colors-"+index+")");
}
