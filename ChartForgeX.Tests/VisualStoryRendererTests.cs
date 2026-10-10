using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualStoryRendererTests {
    [Fact]
    public void PortableFormatsRevealTheDeclaredOutcome() => SmokeTests.VisualStoriesRevealDeclaredOutcomesAcrossPortableFormats();
    [Fact]
    public void ResolvedInputAndViewportContractsRemainBounded() => SmokeTests.VisualStoriesRejectUnrevealedOutcomesAndInvalidSyntaxSpans();
    [Fact]
    public void RasterLayoutAndDensityRespectPanelBounds() => SmokeTests.VisualStoryRasterLayoutStaysBoundedAtEveryDensity();
}
