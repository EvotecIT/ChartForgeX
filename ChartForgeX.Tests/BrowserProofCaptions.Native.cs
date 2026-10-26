using System.Xml.Linq;
using Microsoft.Playwright;

namespace ChartForgeX.Tests;

/// <summary>Reads native caption facts and targets actual glyph paint without making hidden semantic text interactive.</summary>
internal static partial class BrowserProofCaptions {
    internal static async Task InstallNativeAsync(IPage page) {
        await InstallAsync(page);
        await page.EvaluateAsync("""
            () => {
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
                const boxes = roots(node).map(element => element.getBoundingClientRect()).filter(box => box.width > 0 && box.height > 0);
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
              window.cfxCaptionPoint = node => {
                for (const root of roots(node)) {
                  const glyphs = root.localName === 'text' ? [root] : [...root.querySelectorAll('use,path')];
                  for (const glyph of glyphs) {
                    const box = glyph.getBoundingClientRect();
                    if (!(box.width > 0 && box.height > 0)) continue;
                    for (const fy of [.5,.25,.75,.125,.875]) for (const fx of [.5,.25,.75,.125,.875]) {
                      const x = box.left + box.width * fx, y = box.top + box.height * fy;
                      const hit = document.elementFromPoint(x,y);
                      if (hit && root.contains(hit)) return {x,y};
                    }
                  }
                }
                throw Error('No actual painted caption point can receive the real pointer');
              };
            }
            """);
    }

    internal static async Task<float[]> PointAsync(IPage page, ILocator owner) {
        await InstallNativeAsync(page);
        await owner.ScrollIntoViewIfNeededAsync();
        return await owner.EvaluateAsync<float[]>("node => { const point=window.cfxCaptionPoint(node); return [point.x,point.y]; }");
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
            if (!hasInk) throw new InvalidOperationException("Exported native caption has no nonempty glyph ink.");
        }
        return string.Join(" ", captions.Select(element => (string)element.Attribute("data-cfx-caption")!));
    }
}
