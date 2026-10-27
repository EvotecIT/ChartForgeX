using ChartForgeX.Typography;
using HtmlTinkerX;

namespace ChartForgeX.Tests;

/// <summary>Uses the same redistributable faces for native metrics and browser geometry assertions.</summary>
internal static class BrowserProofFonts {
    private const string Name = "CFX Browser Proof Carlito";
    private static readonly Lazy<string> Style = new(() => "<style>"
        + Face("Regular", 400) + Face("Bold", 700) + "</style>");

    internal static string Family {
        get {
            FontRegistry.Register(Name, PathFor("Regular"), 400);
            FontRegistry.Register(Name, PathFor("Bold"), 700);
            return Name;
        }
    }

    /// <summary>Declares the bounded font inputs for notation/portable-scene fixtures, independently of installed host collections.</summary>
    internal static ChartForgeX.Mermaid.MermaidRenderOptions MermaidOptions() => new() {
        Presentation = new ChartForgeX.Mermaid.MermaidPresentationOptions { FontFamily = Family }
    };

    internal static async Task<HtmlBrowserSession> OpenAsync(string body, int width, int height) {
        var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><head>" + Style.Value
            + "</head><body style='margin:0'>" + body + "</body></html>", width, height, waitForOwnedFonts: false);
        try {
            var loaded = await session.Page.EvaluateAsync<bool>("""
                async family => {
                    const faces = await Promise.all([400, 600, 700].map(weight =>
                        document.fonts.load(weight + ' 16px "' + family + '"')));
                    await document.fonts.ready;
                    return faces.every(items => items.length > 0 && items.every(face => face.status === 'loaded'));
                }
                """, Family);
            if (!loaded) throw new InvalidOperationException("The browser did not load the native proof fonts.");
            return session;
        } catch {
            await session.DisposeAsync();
            throw;
        }
    }

    private static string PathFor(string weight) => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-" + weight + ".ttf");
    private static string Face(string weight, int numeric) => "@font-face{font-family:'" + Name
        + "';font-weight:" + numeric + ";src:url(data:font/ttf;base64,"
        + Convert.ToBase64String(File.ReadAllBytes(PathFor(weight))) + ") format('truetype')}";
}
