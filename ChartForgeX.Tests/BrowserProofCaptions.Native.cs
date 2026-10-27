using System.Xml.Linq;
using Microsoft.Playwright;

namespace ChartForgeX.Tests;

/// <summary>Reads native caption facts and targets actual glyph paint without making hidden semantic text interactive.</summary>
internal static partial class BrowserProofCaptions {
    internal static async Task InstallNativeAsync(IPage page) {
        await InstallAsync(page);
        await page.EvaluateAsync("""
            async () => {
              const pixels = window.cfxCaptionRasterPixels;
              const roots = node => {
                const owner = node.closest('[data-cfx-caption]') || node;
                const captured = owner.matches('[data-cfx-native-text-ink]') ? [owner]
                  : [...owner.querySelectorAll('[data-cfx-native-text-ink]')];
                const visible = elements => elements.filter(element => {
                  const style=getComputedStyle(element),box=element.getBoundingClientRect();
                  return style.display!=='none' && style.visibility!=='hidden' && style.visibility!=='collapse' && box.width>0 && box.height>0;
                });
                const ink = visible(captured);
                return ink.length ? ink : visible([owner, ...owner.querySelectorAll('text')].filter(element => element.localName === 'text'));
              };
              window.cfxCaptionText = node => node.hasAttribute('data-cfx-caption') ? window.cfxReferenceLabelText(node) : window.cfxReferenceCaptionText(node);
              window.cfxCaptionBox = node => {
                const boxes = roots(node).flatMap(element => {
                  const shapes=element.localName==='text'?[element]:[...element.querySelectorAll('use,path,image[data-cfx-image-alpha-bounds]')];
                  return shapes.filter(shape=>!shape.closest('[data-cfx-state-outline],[data-cfx-highlight-outline]')).map(shape=>{
                    if(shape.localName!=='image')return shape.getBoundingClientRect();
                    const source=pixels.get(shape),matrix=shape.getScreenCTM();if(!source?.hasPaint||!matrix)return null;
                    const b=source.bounds,x=shape.x.baseVal.value,y=shape.y.baseVal.value,w=shape.width.baseVal.value/source.width,h=shape.height.baseVal.value/source.height;
                    const points=[[b.left,b.top],[b.right,b.top],[b.left,b.bottom],[b.right,b.bottom]].map(([px,py])=>new DOMPoint(x+px*w,y+py*h).matrixTransform(matrix));
                    const left=Math.min(...points.map(p=>p.x)),right=Math.max(...points.map(p=>p.x)),top=Math.min(...points.map(p=>p.y)),bottom=Math.max(...points.map(p=>p.y));
                    return {left,right,top,bottom,width:right-left,height:bottom-top};
                  });
                }).filter(box => box && box.width > 0 && box.height > 0);
                if (!boxes.length) throw Error('Native caption has no nonempty painted bounds');
                const left = Math.min(...boxes.map(box => box.left)), right = Math.max(...boxes.map(box => box.right));
                const top = Math.min(...boxes.map(box => box.top)), bottom = Math.max(...boxes.map(box => box.bottom));
                return {x:left,y:top,left,right,top,bottom,width:right-left,height:bottom-top};
              };
              window.cfxCaptionSvgBox = node => {
                const box = window.cfxCaptionBox(node), inverse = node.ownerSVGElement.getScreenCTM().inverse();
                const points = [[box.left,box.top],[box.right,box.top],[box.left,box.bottom],[box.right,box.bottom]]
                  .map(([x,y]) => new DOMPoint(x,y).matrixTransform(inverse));
                const x = Math.min(...points.map(point => point.x)), y = Math.min(...points.map(point => point.y));
                const right = Math.max(...points.map(point => point.x)), bottom = Math.max(...points.map(point => point.y));
                return {x,y,left:x,top:y,right,bottom,width:right-x,height:bottom-y};
              };
              window.cfxCaptionPoint = (node, last = false) => {
                for (const root of (last ? roots(node).reverse() : roots(node))) {
                  const glyphs = root.localName === 'text' ? [root] : [...root.querySelectorAll('use,path,image[data-cfx-image-alpha-bounds]')];
                  if (last) glyphs.reverse();
                  const tree = root.getRootNode(), hitAt = (x,y) => tree.elementFromPoint(x,y);
                  for (const glyph of glyphs) {
                    if (glyph.localName === 'image') {
                      const source=pixels.get(glyph),matrix=glyph.getScreenCTM();
                      if (!source?.hasPaint || !matrix) continue;
                      for (let step=0;step<source.width*source.height;step++) {
                        const index=last ? source.width*source.height-1-step : step;
                        if (!source.rgba[index*4+3]) continue;
                        const x=glyph.x.baseVal.value+glyph.width.baseVal.value*((index%source.width)+.5)/source.width;
                        const y=glyph.y.baseVal.value+glyph.height.baseVal.value*(Math.floor(index/source.width)+.5)/source.height;
                        const point=new DOMPoint(x,y).matrixTransform(matrix),hit=hitAt(point.x,point.y);
                        if (hit&&(root.contains(hit)||node.contains(hit)&&hit.hasAttribute('data-cfx-browser-hit-area'))) return {x:point.x,y:point.y};
                      }
                      continue;
                    }
                    const box = glyph.getBoundingClientRect();
                    if (!(box.width > 0 && box.height > 0)) continue;
                    const fractions=last ? [.875,.125,.75,.25,.5] : [.5,.25,.75,.125,.875];
                    for (const fy of fractions) for (const fx of fractions) {
                      const x = box.left + box.width * fx, y = box.top + box.height * fy;
                      const hit = hitAt(x,y);
                      if (hit && (root.contains(hit)||node.contains(hit)&&hit.hasAttribute('data-cfx-browser-hit-area'))) return {x,y};
                    }
                  }
                }
                throw Error('No actual painted caption point can receive the real pointer');
              };
            }
            """);
    }

    internal static async Task<float[]> PointAsync(IPage page, ILocator owner, bool scrollIntoView = true) {
        await InstallNativeAsync(page);
        if (scrollIntoView) await owner.ScrollIntoViewIfNeededAsync();
        try {
            return await owner.EvaluateAsync<float[]>("async node => { await window.cfxCaptionPrepareRaster(node); const point=window.cfxCaptionPoint(node); return [point.x,point.y]; }");
        } catch {
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(directory)) {
                Directory.CreateDirectory(directory);
                var stem = Path.Combine(directory, "native-caption-point-failure-" + Guid.NewGuid().ToString("N"));
                var observed = await owner.EvaluateAsync<string>("""
                    node=>JSON.stringify({owner:node.outerHTML,viewport:{width:innerWidth,height:innerHeight},
                        nodes:[node,...node.querySelectorAll('[data-cfx-native-text-ink],text,image')].map(ink=>{
                            const b=ink.getBoundingClientRect(),style=getComputedStyle(ink),tree=ink.getRootNode();
                            return {tag:ink.localName,role:ink.dataset.cfxRole,box:{x:b.x,y:b.y,width:b.width,height:b.height},
                                display:style.display,visibility:style.visibility,opacity:style.opacity,pointerEvents:style.pointerEvents,
                                samples:[.1,.5,.9].flatMap(x=>[.1,.5,.9].map(y=>{const px=b.x+b.width*x,py=b.y+b.height*y,hit=tree.elementFromPoint(px,py);
                                    return {x:px,y:py,hit:hit?.outerHTML.slice(0,600),insideOwner:!!hit&&node.contains(hit)};}))};})})
                    """);
                await File.WriteAllTextAsync(stem + ".json", observed);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".png" });
            }
            throw;
        }
    }

    internal static async Task MoveAsync(IPage page, ILocator owner) {
        var point = await PointAsync(page, owner);
        await page.Mouse.MoveAsync(point[0], point[1], new MouseMoveOptions { Steps = 4 });
    }

    internal static async Task ClickAsync(IPage page, ILocator owner) {
        var point = await PointAsync(page, owner);
        await page.Mouse.ClickAsync(point[0], point[1]);
    }

    internal static string ReadXml(XElement owner) {
        var captions = owner.DescendantsAndSelf().Where(element => element.Attribute("data-cfx-caption") != null).ToArray();
        if (captions.Length == 0) return string.Join(" ", owner.DescendantsAndSelf().Where(element => element.Name.LocalName == "text")
            .SelectMany(element => element.Elements().Any() ? element.Elements().Select(line => line.Value) : new[] { element.Value }));
        var document = owner.Document?.Root ?? owner;
        foreach (var caption in captions) {
            if (string.IsNullOrWhiteSpace((string?)caption.Attribute("data-cfx-caption"))) continue;
            var ink = caption.Descendants().Where(element => element.Attribute("data-cfx-native-text-ink") != null).ToArray();
            var references = ink.SelectMany(element => element.Descendants()).Where(element => element.Name.LocalName == "use").ToArray();
            if (references.Any(reference => !document.DescendantsAndSelf().Any(target => "#" + (string?)target.Attribute("id") == (string?)reference.Attribute("href"))))
                throw new InvalidOperationException("Exported native caption has an unresolved glyph reference.");
            var hasInk = ink.SelectMany(element => element.Descendants()).Any(element => element.Name.LocalName == "path" && !string.IsNullOrEmpty((string?)element.Attribute("d")))
                || references.Any(reference => document.DescendantsAndSelf().Any(target => "#" + (string?)target.Attribute("id") == (string?)reference.Attribute("href")
                    && target.DescendantsAndSelf().Any(path => path.Name.LocalName == "path" && !string.IsNullOrEmpty((string?)path.Attribute("d")))));
            hasInk |= ink.SelectMany(element => element.Descendants()).Where(element => element.Name.LocalName == "image"
                && element.Attribute("data-cfx-image-alpha-bounds") != null).Any(HasRasterInk);
            if (!hasInk) throw new InvalidOperationException("Exported native caption has no nonempty glyph ink.");
        }
        return string.Join(" ", captions.Select(element => (string)element.Attribute("data-cfx-caption")!));
    }
    private static bool HasRasterInk(XElement image) {
        const string prefix = "data:image/png;base64,";
        var uri = (string?)image.Attribute("href") ?? "";
        if (!uri.StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidOperationException("Native caption requires its captured PNG.");
        var pixels = ChartForgeX.Raster.PngReader.Decode(Convert.FromBase64String(uri.Substring(prefix.Length)));
        return pixels.Pixels.Where((value,index)=>(index&3)==3).Any(alpha=>alpha!=0);
    }

}
