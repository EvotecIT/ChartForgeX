using ChartForgeX.Mermaid;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static class UseCaseAdvancedActorFixture {
    internal static string Read(bool compact = false) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "Build", "PackageConsumers", "Adapters", "usecase-advanced-actors.mmd")).ReplaceLineEndings("\n");
        return compact ? source.Replace("direction TB", "direction LR", StringComparison.Ordinal) : source;
    }
    internal static MermaidRenderOptions Options(int width, bool dark) => new() {
        WarningPolicy = MermaidWarningPolicy.Reject,
        UseCase = new MermaidFlowchartRenderOptions { Width = width, Height = width == 390 ? 1160 : 680,
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family,
                ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, TopologySizing = MermaidTopologySizing.FixedViewport } }
    };
}
