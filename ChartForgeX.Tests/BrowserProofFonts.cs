using ChartForgeX.Themes;
using ChartForgeX.Typography;
using HtmlTinkerX;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Uses the same redistributable faces for native metrics and browser geometry assertions.</summary>
internal static class BrowserProofFonts {
    internal const string Family = ChartFontStacks.ProductSans;

    /// <summary>Checks a computed font-family against the proof stack; WebKit serializes family names without quotes.</summary>
    internal static void AssertComputedFamily(string? computed) => Assert.Contains(Unquoted(Family), Unquoted(computed ?? string.Empty));

    private static string Unquoted(string value) => value.Replace("\"", string.Empty).Replace("'", string.Empty);
    /// <summary>Names the Carlito fixture face for charts whose native metrics come from <see cref="CarlitoPath"/>.</summary>
    internal const string CarlitoFamily = "CFX Proof Carlito";

    internal static string CarlitoPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");

    /// <summary>Delivers the same Carlito bytes to the browser, so browser and native geometry measure one face.</summary>
    internal static string CarlitoStyle => "<style>@font-face{font-family:'" + CarlitoFamily + "';font-weight:400;src:url(data:font/ttf;base64,"
        + Convert.ToBase64String(File.ReadAllBytes(CarlitoPath)) + ") format('truetype')}</style>";

    /// <summary>Declares the bounded font inputs for notation/portable-scene fixtures, independently of installed host collections.</summary>
    internal static ChartForgeX.Mermaid.MermaidRenderOptions MermaidOptions() => new() {
        Presentation = new ChartForgeX.Mermaid.MermaidPresentationOptions { FontFamily = Family }
    };

    internal static async Task<HtmlBrowserSession> OpenAsync(string body, int width, int height) {
        var style = body.Contains(ChartFontAssets.StyleSheet, StringComparison.Ordinal)
            ? string.Empty : "<style>" + ChartFontAssets.StyleSheet + "</style>";
        var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><head>" + style
            + "</head><body style='margin:0'>" + body + "</body></html>", width, height, waitForOwnedFonts: false);
        try {
            var loaded = await session.Page.EvaluateAsync<bool>("""
                async family => {
                    const faces = await Promise.all([400, 500, 600, 650, 700].map(weight =>
                        document.fonts.load(weight + ' 16px "' + family + '"')));
                    await document.fonts.ready;
                    return faces.every(items => items.length > 0 && items.every(face => face.status === 'loaded'));
                }
                """, "Archivo");
            if (!loaded) throw new InvalidOperationException("The browser did not load bundled Archivo at weights 400, 500, 600, 650 and 700.");
            if (body.Contains(CarlitoFamily, StringComparison.Ordinal) && !await session.Page.EvaluateAsync<bool>(
                    "async family => (await document.fonts.load('400 16px \"' + family + '\"')).some(face => face.status === 'loaded')", CarlitoFamily))
                throw new InvalidOperationException("The browser did not load the Carlito fixture face.");
            return session;
        } catch {
            await session.DisposeAsync();
            throw;
        }
    }
}
