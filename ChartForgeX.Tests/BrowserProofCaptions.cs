using Microsoft.Playwright;

namespace ChartForgeX.Tests;

/// <summary>Observes ordinary text and captured native glyph ink without treating metadata alone as paint.</summary>
internal static class BrowserProofCaptions {
    internal static Task InstallAsync(IPage page) => page.EvaluateAsync("""
        () => {
          const normalize = text => text.replace(/\\n/g, ' ').replace(/\s+/g, ' ').trim();
          const text = node => {
            if (node.hasAttribute('data-cfx-caption')) {
              const ink = node.querySelector('[data-cfx-native-text-ink]');
              if (ink) {
                const box = ink.getBoundingClientRect();
                if (!ink.querySelector('use,path') || !(box.width > 0 && box.height > 0))
                  throw new Error('Native caption has no nonempty glyph ink: ' + node.dataset.cfxCaption);
                return normalize(node.dataset.cfxCaption);
              }
            }
            const copy = node.cloneNode(true);
            for (const br of copy.querySelectorAll('br')) br.replaceWith(' ');
            for (const line of copy.querySelectorAll('tspan[dy]'))
              if (parseFloat(line.getAttribute('dy')) !== 0) line.before(' ');
            return normalize(copy.textContent);
          };
          window.cfxReferenceCaptionText = root => normalize([...root.querySelectorAll('text,foreignObject,[data-cfx-caption]')].map(text).join(' '));
          window.cfxReferenceLabelText = text;
        }
        """);
}
