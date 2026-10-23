using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class ExplorerLocalizationBrowserTests {
    private static void Arabic(HtmlChartLocalization ui) {
        ui.Language = "ar";
        ui.Direction = HtmlChartTextDirection.RightToLeft;
        ui[HtmlChartText.GraphSearch] = "بحث في الرسم";
        ui[HtmlChartText.GraphSearchPlaceholder] = "البحث عن عنصر";
        ui[HtmlChartText.GraphOneMatch] = "نتيجة {count}";
        ui[HtmlChartText.GraphEndpoints] = "صلة {source} / {target}";
        ui[HtmlChartText.GraphItemsSelected] = "عناصر محددة {count}";
        ui[HtmlChartText.GraphItemsHighlighted] = "عناصر مميزة {count}";
        ui[HtmlChartText.GraphThemeInstruction] = "السمة {current}؛ التالية {next}";
        ui[HtmlChartText.GraphThemeEnabled] = "السمة {active}";
        ui[HtmlChartText.GraphThemeDark] = "داكن";
        ui[HtmlChartText.GraphThemeLight] = "فاتح";
        ui[HtmlChartText.GraphThemeDarkLowercase] = "داكن";
        ui[HtmlChartText.GraphThemeLightLowercase] = "فاتح";
        ui[HtmlChartText.GraphPatchEdgeEndpoint] = "العلاقة {edgeId}: نهاية مفقودة";
        ui[HtmlChartText.GraphAddEdge] = "إضافة علاقة";
        ui[HtmlChartText.GraphOpenEditor] = "فتح المحرر";
        ui[HtmlChartText.GraphApplyEdge] = "تطبيق العلاقة";
        ui[HtmlChartText.ExplorerId] = "المعرّف";
        ui[HtmlChartText.ExplorerNode] = "عقدة";
        ui[HtmlChartText.ExplorerStatus] = "الحالة";
        ui[HtmlChartText.ExplorerNoItemSelected] = "لا يوجد اختيار";
        ui[HtmlChartText.TopologyFindPlaceholder] = "البحث في المسار";
        ui[HtmlChartText.TopologyVisibleSummary] = "العقد {nodes}؛ العلاقات {edges}";
        ui[HtmlChartText.TopologyHealthy] = "سليم";
        ui[HtmlChartText.TopologyControls] = "أدوات المسار";
        ui[HtmlChartText.TopologyAllRoutesVisible] = "كل المسارات ظاهرة";
        ui[HtmlChartText.Steps] = "خطوات {count}";
        ui[HtmlChartText.TopologyExportSvg] = "تصدير الرسم";
        ui[HtmlChartText.ReportOtherRelationships] = "علاقات الصفحات";
        ui[HtmlChartText.ReportDetailPages] = "الصفحات التفصيلية";
        ui[HtmlChartText.ReportFindPlaceholder] = "البحث عن اسم";
        ui[HtmlChartText.ReportOneMatch] = "نتيجة {count}";
        ui[HtmlChartText.ReportMatches] = "نتائج {count}";
        ui[HtmlChartText.ReportPageStatus] = "صفحة {page} من {count}";
        ui[HtmlChartText.ReportOverviewStatus] = "نظرة عامة";
    }

    // Compare the native scene, not translated chrome. Canvas2D drawing facts protect authored
    // text, transforms and bidi state; full PNGs remain evidence. Untouched/WebGL surfaces keep
    // exact bitmap comparison, and SVG glyph extents below retain physical geometry coverage.
    private static async Task<JsonElement> NativeSnapshotAsync(IPage page, string selector) {
        await page.EvaluateAsync("()=>document.fonts.ready.then(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))))");
        var snapshot = await page.Locator(selector).EvaluateAsync<JsonElement>("""
            root=>{const surfaces=[...root.querySelectorAll('svg[data-cfx-role=graph-scene],canvas,.cfx-topology-viewport>svg')],
                painted=node=>{for(let owner=node;owner;owner=owner.parentElement){const style=getComputedStyle(owner);if(style.display==='none'||style.visibility!=='visible'||Number(style.opacity)===0)return false}return true},
                textFacts=node=>({text:node.textContent,direction:getComputedStyle(node).direction});
                return{pixelRatio:devicePixelRatio,surfaces:surfaces.map(node=>({tag:node.localName,role:node.dataset.cfxRole,direction:getComputedStyle(node).direction,
                    drawing:node.localName==='canvas'?window.__cfxLocalizationPaint?.get(node)??null:null,
                    bitmap:node.localName==='canvas'?node.toDataURL():null})),
                nativeText:surfaces.filter(node=>node.localName==='svg').flatMap(svg=>[...svg.querySelectorAll('text')]).map(textFacts),
                texts:surfaces.filter(node=>node.localName==='svg'&&painted(node)).flatMap(svg=>{
                    const origin=svg.getBoundingClientRect();return [...svg.querySelectorAll('text')].map(node=>{
                        const matrix=node.getScreenCTM(),point=value=>[matrix.a*value.x+matrix.c*value.y+matrix.e-origin.left,
                            matrix.b*value.x+matrix.d*value.y+matrix.f-origin.top];
                        return {...textFacts(node),linear:[matrix.a,matrix.b,matrix.c,matrix.d],
                            characters:Array.from({length:node.getNumberOfChars()},(_,index)=>{
                                const box=node.getExtentOfChar(index);return [point(node.getStartPositionOfChar(index)),point(node.getEndPositionOfChar(index)),
                                    point({x:box.x,y:box.y}),point({x:box.x+box.width,y:box.y}),
                                    point({x:box.x,y:box.y+box.height}),point({x:box.x+box.width,y:box.y+box.height})]})}})})}}
            """);
        var json = snapshot.GetRawText();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var context = await page.Locator(selector).EvaluateAsync<JsonElement>("""
                root=>{const rect=node=>{if(!node)return null;const box=node.getBoundingClientRect();return [box.x,box.y,box.width,box.height]};
                    return {viewport:[root.dataset.cfxViewportX,root.dataset.cfxViewportY,root.dataset.cfxViewportScale],
                        selected:root.__cfxGraphState?.nodes.filter(node=>node.el.getAttribute('aria-pressed')==='true').map(node=>node.id),
                        hover:root.__cfxGraphHoverKey,stage:rect(root.querySelector('.cfx-graph-stage')),
                        controls:[...root.querySelectorAll('.cfx-graph-toolbar,.cfx-graph-view-controls')].map(rect),
                        surfaces:[...root.querySelectorAll('svg[data-cfx-role=graph-scene],canvas,.cfx-topology-viewport>svg')].map(node=>({role:node.dataset.cfxRole,
                            box:rect(node),width:node.width?.baseVal?.value??node.width,height:node.height?.baseVal?.value??node.height,
                            display:getComputedStyle(node).display,visibility:getComputedStyle(node).visibility}))};}
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, "localization-native-" + hash + ".json"),
                JsonSerializer.Serialize(new { snapshot, context }));
        }
        return snapshot;
    }

    private static void AssertNativeSnapshotEqual(JsonElement expected, JsonElement actual) {
        var expectedSurfaces = expected.GetProperty("surfaces").EnumerateArray().ToArray();
        var actualSurfaces = actual.GetProperty("surfaces").EnumerateArray().ToArray();
        Assert.Equal(expectedSurfaces.Length, actualSurfaces.Length);
        for (var index = 0; index < expectedSurfaces.Length; index++) {
            var before = expectedSurfaces[index]; var after = actualSurfaces[index];
            foreach (var property in new[] { "tag", "role", "direction" })
                Assert.Equal(before.GetProperty(property).GetRawText(), after.GetProperty(property).GetRawText());
            var drawing = before.GetProperty("drawing"); var nextDrawing = after.GetProperty("drawing");
            Assert.Equal(drawing.ValueKind, nextDrawing.ValueKind);
            if (drawing.ValueKind != JsonValueKind.Null)
                Assert.Equal(drawing.GetRawText(), nextDrawing.GetRawText());
            else
                Assert.Equal(before.GetProperty("bitmap").GetString(), after.GetProperty("bitmap").GetString());
        }
        Assert.Equal(expected.GetProperty("nativeText").GetRawText(), actual.GetProperty("nativeText").GetRawText());
        Assert.Equal(expected.GetProperty("pixelRatio").GetDouble(), actual.GetProperty("pixelRatio").GetDouble());
        var expectedTexts = expected.GetProperty("texts").EnumerateArray().ToArray();
        var actualTexts = actual.GetProperty("texts").EnumerateArray().ToArray();
        Assert.Equal(expectedTexts.Length, actualTexts.Length);
        for (var index = 0; index < expectedTexts.Length; index++) {
            Assert.Equal(expectedTexts[index].GetProperty("text").GetString(), actualTexts[index].GetProperty("text").GetString());
            Assert.Equal(expectedTexts[index].GetProperty("direction").GetString(), actualTexts[index].GetProperty("direction").GetString());
            Assert.Equal(expectedTexts[index].GetProperty("linear").GetRawText(), actualTexts[index].GetProperty("linear").GetRawText());
            var expectedCharacters = expectedTexts[index].GetProperty("characters").EnumerateArray().ToArray();
            var actualCharacters = actualTexts[index].GetProperty("characters").EnumerateArray().ToArray();
            Assert.Equal(expectedCharacters.Length, actualCharacters.Length);
            for (var character = 0; character < expectedCharacters.Length; character++) {
                // Compare the start, end and all extent corners relative to the SVG surface.
                // This includes ancestor translations and each glyph's width/height, while
                // allowing font-cache quantization below half a physical pixel.
                var expectedPoints = expectedCharacters[character].EnumerateArray().ToArray();
                var actualPoints = actualCharacters[character].EnumerateArray().ToArray();
                Assert.Equal(expectedPoints.Length, actualPoints.Length);
                for (var point = 0; point < expectedPoints.Length; point++) {
                    var dx = expectedPoints[point][0].GetDouble() - actualPoints[point][0].GetDouble();
                    var dy = expectedPoints[point][1].GetDouble() - actualPoints[point][1].GetDouble();
                    Assert.InRange(Math.Sqrt(dx * dx + dy * dy) * expected.GetProperty("pixelRatio").GetDouble(), 0, 0.5);
                }
            }
        }
    }

    private static async Task AssertLeadingEnglishNumberOrderAsync(ILocator message) {
        var order = await message.EvaluateAsync<JsonElement>("""
            node=>{const text=node.textContent,letter=text.search(/[A-Za-z]/),range=document.createRange();
                range.setStart(node.firstChild,0);range.setEnd(node.firstChild,1);const first=range.getBoundingClientRect();
                range.setStart(node.firstChild,letter);range.setEnd(node.firstChild,letter+1);const next=range.getBoundingClientRect();
                return {text,first:first.x,next:next.x,rootDirection:getComputedStyle(node.closest('[data-cfx-ui]')).direction};}
            """);
        Assert.True(char.IsDigit(order.GetProperty("text").GetString()![0]));
        Assert.Equal("rtl", order.GetProperty("rootDirection").GetString());
        Assert.True(order.GetProperty("first").GetDouble() < order.GetProperty("next").GetDouble(), order.GetRawText());
    }

    private static Task ObserveInputAsync(IPage page) => page.EvaluateAsync("""
        ()=>{window.localizationInput=[];for(const type of ['pointerup','keydown'])document.addEventListener(type,event=>localizationInput.push({type,key:event.key,trusted:event.isTrusted,
            role:event.target.closest?.('[data-cfx-role]')?.dataset.cfxRole,tag:event.target.localName}));}
        """);

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "localized-" + name + ".png"), FullPage = true });
        var facts = await page.EvaluateAsync<JsonElement>("""
            () => ({language:document.documentElement.lang,direction:document.documentElement.dir,
                messages:[...document.querySelectorAll('[role=status],output')].map(node=>({text:node.textContent,visible:!!node.getClientRects().length})),
                roots:[...document.querySelectorAll('[data-cfx-ui]')].map(root=>({language:root.lang,direction:getComputedStyle(root).direction,
                    bundle:JSON.parse(root.dataset.cfxUi),renderer:root.dataset.cfxGraphRendererActive})),
                nativeDirections:[...document.querySelectorAll('[data-cfx-role=graph-scene],canvas,.cfx-topology-viewport>svg,.diagram svg')].map(node=>({role:node.dataset.cfxRole,direction:getComputedStyle(node).direction})),
                input:window.localizationInput||[],readouts:window.readoutObservations||[],pageOverflow:document.documentElement.scrollWidth-innerWidth})
            """);
        await File.WriteAllTextAsync(Path.Combine(directory, "localized-" + name + ".json"), facts.GetRawText());
    }
}
