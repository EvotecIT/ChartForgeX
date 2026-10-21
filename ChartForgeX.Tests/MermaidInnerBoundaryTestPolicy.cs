using ChartForgeX.Mermaid;

namespace ChartForgeX.Tests;

/// <summary>Lets existing configuration and caption regressions reach their inner budgets.</summary>
internal static class MermaidInnerBoundaryTestPolicy {
    internal static MermaidParseOptions ParseOptions() => new() {
        ResourceLimits = new MermaidResourceLimits {
            MaximumSourceCharacters = 2 * 1024 * 1024,
            MaximumSourceLines = 100_000,
            MaximumLineCharacters = 1_100_000
        }
    };

    internal static MermaidRenderOptions RenderOptions() => new() { ParseOptions = ParseOptions() };
    internal static MermaidRenderResult Render(string source) => MermaidRenderer.Render(source, RenderOptions());
    internal static MermaidParseResult<MermaidDocument> Parse(string source) => new MermaidParser().Parse(source, ParseOptions());
}
