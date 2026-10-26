using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
namespace ChartForgeX.Tests;
[Trait("BrowserSuite","Compatibility")]
public sealed class SequenceNavigationBrowserTests {
    [BrowserTheory] [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task NativeDisclosuresRemainAnchoredKeyboardOperableAndOrderedForBothSourceStates(bool dark,int width) {
        foreach(var expanded in new[]{false,true}) {
            var source=SequenceNavigationSourceFixture.Canonical().Replace("\"forceMenus\":true","\"forceMenus\":"+(expanded?"true":"false"),StringComparison.Ordinal);var options=new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light,FontFamily=BrowserProofFonts.Family},Sequence=new MermaidSequenceRenderOptions{Width=width-32,Height=820}};var direct=MermaidRenderer.Render(source,options);Assert.Empty(direct.Diagnostics);var document=Assert.IsType<MermaidSequenceDocument>(direct.Document);var fence=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");Assert.Empty(fence.Diagnostics);
            foreach(var pair in new[]{("direct",direct.Artifact!),("fence",Assert.Single(fence.Artifacts))}) {
                var sequence=Assert.IsType<SequenceArtifact>(pair.Item2.Model);var prepared=SequencePreparedCompiler.PrepareDefault(sequence);var header=prepared.Regions.Single(r=>r.Role=="sequence-participant"&&r.Id=="A");var html=sequence.ToNavigationHtmlPage(new HtmlSequenceNavigationOptions{InitiallyExpanded=document.Configuration.SequenceForceMenus==true});await using var session=await BrowserProofFonts.OpenAsync(html,width,900);var page=session.Page;var menu=page.Locator("details[data-source-id='A']");var summary=menu.Locator("summary");var before=await page.EvaluateAsync<JsonElement>("""
                    () => {
                        const d=document.querySelector('details'),s=document.querySelector('svg');
                        const b=s.getBoundingClientRect(),q=d.getBoundingClientRect(),resolved=getComputedStyle(d);
                        const declared=Object.fromEntries(d.getAttribute('style').split(';').filter(Boolean).map(item=>{
                            const at=item.indexOf(':');return [item.slice(0,at).trim(),item.slice(at+1).trim()];
                        }));
                        const reference=document.createElement('div');
                        reference.style.cssText='position:fixed;left:0;top:0;width:0;height:0;pointer-events:none';
                        reference.style.marginLeft=declared['margin-left'];reference.style.marginTop=declared['margin-top'];
                        document.body.append(reference);const quantized=reference.getBoundingClientRect();reference.remove();
                        return {layoutReference:{x:quantized.left,y:quantized.top},open:d.open,links:[...d.querySelectorAll('a')].map(a=>({label:a.textContent,href:a.getAttribute('href')})),
                            authoredOffset:{x:parseFloat(declared['margin-left']),y:parseFloat(declared['margin-top'])},
                            resolvedOffset:{x:parseFloat(resolved.marginLeft),y:parseFloat(resolved.marginTop)},
                            offset:{x:q.left-b.left,y:q.top-b.top},scripts:document.querySelectorAll('script').length,
                            overflow:document.documentElement.scrollWidth>innerWidth,svg:s.outerHTML};
                    }
                    """);
                var stem="sequence-navigation-"+pair.Item1+"-"+expanded+"-"+width+(dark?"-dark":"-light");await Capture("initial",before);
                Assert.Equal(expanded,before.GetProperty("open").GetBoolean());Assert.Equal(new[]{"Guide","Status"},before.GetProperty("links").EnumerateArray().Select(x=>x.GetProperty("label").GetString()));Assert.Equal(new[]{"#guide","#status"},before.GetProperty("links").EnumerateArray().Select(x=>x.GetProperty("href").GetString()));// Keep the native CSS anchor precise; compare physical placement to an independently
                // laid-out reference with the same offsets, including browser layout quantization.
                Assert.Equal(header.Bounds.Left,before.GetProperty("authoredOffset").GetProperty("x").GetDouble(),9);
                Assert.Equal(header.Bounds.Bottom,before.GetProperty("authoredOffset").GetProperty("y").GetDouble(),9);
                Assert.Equal(before.GetProperty("layoutReference").GetProperty("x").GetDouble(),before.GetProperty("offset").GetProperty("x").GetDouble(),2);
                Assert.Equal(before.GetProperty("layoutReference").GetProperty("y").GetDouble(),before.GetProperty("offset").GetProperty("y").GetDouble(),2);Assert.False(before.GetProperty("overflow").GetBoolean());Assert.Equal(0,before.GetProperty("scripts").GetInt32());
                await summary.FocusAsync();await page.Keyboard.PressAsync("Enter");Assert.Equal(!expanded,await menu.EvaluateAsync<bool>("d=>d.open"));if(expanded){await page.Keyboard.PressAsync("Enter");Assert.True(await menu.EvaluateAsync<bool>("d=>d.open"));}
                var nextLinkKey = InteractiveChartBrowser.Engine == HtmlBrowserEngine.WebKit
                    && await page.EvaluateAsync<bool>("()=>navigator.platform.startsWith('Mac')") ? "Alt+Tab" : "Tab";
                await page.Keyboard.PressAsync(nextLinkKey);Assert.Equal("Guide",await page.EvaluateAsync<string>("()=>document.activeElement.textContent"));await page.Keyboard.PressAsync("Enter");Assert.Equal("#guide",await page.EvaluateAsync<string>("()=>location.hash"));await summary.FocusAsync();await page.Keyboard.PressAsync("Space");Assert.False(await menu.EvaluateAsync<bool>("d=>d.open"));var after=await page.EvaluateAsync<JsonElement>("()=>({open:document.querySelector('details').open,focused:document.activeElement.tagName,hash:location.hash})");await Capture("keyboard",after);Assert.Equal(prepared.ToSvg(),pair.Item2.ToSvg());InteractiveChartBrowser.AssertNoConsoleErrors(session);
                async Task Capture(string state,JsonElement observation){var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".mmd"),source);File.WriteAllText(Path.Combine(output,stem+".svg"),prepared.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".native.png"),prepared.ToPng());File.WriteAllText(Path.Combine(output,stem+".native.json"),prepared.ToArtifact(sequence.Id,VisualArtifactKind.Sequence).ToInterchangeJson());File.WriteAllText(Path.Combine(output,stem+"."+state+".observed.json"),observation.GetRawText());await page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(output,stem+"."+state+".browser.png"),FullPage=true});}
            }
        }
    }
}
