using System.Threading;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;

internal static partial class Program {
    // Public rendering/markup entrypoints share explicit fixture fonts; parsing and source precedence stay native.
    private static MermaidRenderOptions FixtureOptions(MermaidRenderOptions? options = null) {
        options ??= new MermaidRenderOptions();
        options.Presentation ??= new MermaidPresentationOptions();
        options.Presentation.FontFamily ??= PackageAssertions.OutlineFontFamily;
        return options;
    }

    private static MermaidRenderResult RenderFixture(string source, MermaidRenderOptions? options = null, CancellationToken cancellationToken = default) =>
        MermaidRenderer.Render(source, FixtureOptions(options), cancellationToken);

    private static MermaidVisualMarkupParser CreateFixtureParser(MermaidRenderOptions? options = null, VisualThemeMode? fallback = null) =>
        new(FixtureOptions(options), fallback);
}
