using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;
namespace ChartForgeX.Tests;
public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory] [Trait("Category","MermaidReference")]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public async Task SourceMenuOrderAndInitialStateAreObservedAgainstPinnedParticipantPopups(bool dark,int width) {
        var corpus=Path.Combine(TestRepository.Root,"tests","mermaid-conformance");using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus,"compatibility.json")));var reference=manifest.RootElement.GetProperty("references").GetProperty("12");var bundle=Path.Combine(corpus,"node_modules",reference.GetProperty("package").GetString()!);using var installed=JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle,"package.json")));Assert.Equal(reference.GetProperty("version").GetString(),installed.RootElement.GetProperty("version").GetString());
        foreach(var expanded in new[]{false,true}) {
            var source=SequenceNavigationSourceFixture.Canonical().Replace("forceMenus:true","forceMenus:"+(expanded?"true":"false"),StringComparison.Ordinal);var result=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light,FontFamily=BrowserProofFonts.Family},Sequence=new MermaidSequenceRenderOptions{Width=width-32,Height=820}});Assert.Empty(result.Diagnostics);var sequence=Assert.IsType<SequenceArtifact>(result.Artifact!.Model);var host=sequence.ToNavigationHtmlPage(new HtmlSequenceNavigationOptions{InitiallyExpanded=Assert.IsType<MermaidSequenceDocument>(result.Document).Configuration.SequenceForceMenus==true});var body=host.Substring(host.IndexOf("<body>",StringComparison.Ordinal)+6);body=body.Substring(0,body.LastIndexOf("</body>",StringComparison.Ordinal));var svg=result.Artifact.ToSvg();var png=result.Artifact.ToPng();
            var html="<style>body{margin:0;padding:16px;box-sizing:border-box}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style><section id='reference'></section><section id='native'>"+svg+"</section><section id='host'>"+body+"</section><section><img src='data:image/png;base64,"+Convert.ToBase64String(png)+"'></section>";
            await using var session=await BrowserProofFonts.OpenAsync(html,width,1000);await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions{Path=Path.Combine(bundle,"dist","mermaid.min.js")});var observed=await ObserveAsync(session.Page,source,"",dark,new Dictionary<string,int>{{"sequence-participant",2},{"sequence-message",1}});var menu=await session.Page.EvaluateAsync<JsonElement>("""
                () => ({reference:{display:document.querySelector('#reference .actorPopupMenu').getAttribute('display'),links:[...document.querySelectorAll('#reference .actorPopupMenu a')].map(a=>({label:a.textContent.trim(),href:a.getAttribute('href')||a.getAttribute('xlink:href')}))},native:{open:document.querySelector('#host details').open,links:[...document.querySelectorAll('#host details a')].map(a=>({label:a.textContent,href:a.getAttribute('href')}))}})
                """);var stem="sequence-navigation-reference12-"+expanded+"-"+width+(dark?"-dark":"-light");await CaptureAsync(session.Page,stem,svg,png,result.Artifact.ToInterchangeJson(),observed);if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output)File.WriteAllText(Path.Combine(output,stem+".menus.json"),menu.GetRawText());
            foreach(var surface in new[]{"reference","native"}){Assert.Equal(new[]{"Guide","Status"},menu.GetProperty(surface).GetProperty("links").EnumerateArray().Select(x=>x.GetProperty("label").GetString()));Assert.Equal(new[]{"#guide","#status"},menu.GetProperty(surface).GetProperty("links").EnumerateArray().Select(x=>x.GetProperty("href").GetString()));}Assert.Equal(expanded,menu.GetProperty("native").GetProperty("open").GetBoolean());Assert.Equal(expanded?"block !important":"none",menu.GetProperty("reference").GetProperty("display").GetString());Assert.Equal(0,observed.GetProperty("nativeScripts").GetInt32());InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
    }
}
