using Microsoft.Playwright;

namespace ChartForgeX.Tests;

/// <summary>Observes ordinary text and captured native glyph ink without treating metadata alone as paint.</summary>
internal static partial class BrowserProofCaptions {
    internal static async Task<string> TextAsync(ILocator owner) {
        await InstallNativeAsync(owner.Page);
        return await owner.EvaluateAsync<string>("node=>window.cfxCaptionText(node)");
    }
    internal static async Task<string[]> TextsAsync(ILocator owners) {
        await InstallNativeAsync(owners.Page);
        return await owners.EvaluateAllAsync<string[]>("nodes=>nodes.map(node=>window.cfxCaptionText(node))");
    }

    internal static Task InstallAsync(IPage page) => page.EvaluateAsync("""
        async () => {
              const pixels = window.cfxCaptionRasterPixels ||= new WeakMap();
              window.cfxCaptionPrepareRaster = async scope => Promise.all([...scope.querySelectorAll('[data-cfx-native-raster-text-ink] image[data-cfx-image-alpha-bounds]')].map(async node => {
                const href = node.getAttribute('href') || '';
                if (!href.startsWith('data:image/png;base64,')) throw Error('Native caption requires its captured PNG');
                if (pixels.get(node)?.href === href) return;
                const image = new Image(); image.src = href; await image.decode();
                const width=image.naturalWidth,height=image.naturalHeight;
                if (!(width>0&&height>0) || width*height>16777216) throw Error('Invalid bounded native caption pixels');
                const canvas=document.createElement('canvas'); canvas.width=width; canvas.height=height;
                const context=canvas.getContext('2d',{willReadFrequently:true}); context.drawImage(image,0,0);
                const rgba=context.getImageData(0,0,width,height).data;
                pixels.set(node,{href,width,height,rgba,hasPaint:rgba.some((value,index)=>(index&3)===3&&value!==0)});
              }));
              await window.cfxCaptionPrepareRaster(document);
          const normalize = text => text.replace(/\\n/g, ' ').replace(/\s+/g, ' ').trim();
          const text = node => {
            if (node.hasAttribute('data-cfx-caption')) {
              const ink = node.querySelector('[data-cfx-native-text-ink]');
              if (ink) {
                const box = ink.getBoundingClientRect();
                if ([...ink.querySelectorAll('use,path,image[data-cfx-image-alpha-bounds]')].some(shape => shape.localName !== 'image' || window.cfxCaptionRasterPixels?.get(shape)?.hasPaint) && box.width > 0 && box.height > 0 && getComputedStyle(ink).display !== 'none')
                  return normalize(node.dataset.cfxCaption);
                const live = node.querySelector('[data-cfx-owned-font-text]'), liveBox = live?.getBoundingClientRect();
                if (live && liveBox.width > 0 && liveBox.height > 0 && getComputedStyle(live).display !== 'none') return normalize(live.textContent);
                throw new Error('Native caption has no nonempty painted ink: ' + node.dataset.cfxCaption);
              }
            }
            const copy = node.cloneNode(true);
            for (const br of copy.querySelectorAll('br')) br.replaceWith(' ');
            for (const line of copy.querySelectorAll('tspan[dy]'))
              if (parseFloat(line.getAttribute('dy')) !== 0) line.before(' ');
            return normalize(copy.textContent);
          };
          window.cfxReferenceCaptionText = root => normalize([...root.querySelectorAll('text,foreignObject,[data-cfx-caption]')].filter(node => { const parent=node.parentElement?.closest('[data-cfx-caption]'); return !parent || !root.contains(parent); }).map(text).join(' '));
          window.cfxReferenceLabelText = text;
        }
        """);
}
