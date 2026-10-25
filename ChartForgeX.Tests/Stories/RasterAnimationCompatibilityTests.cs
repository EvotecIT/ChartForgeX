using ChartForgeX.Raster;
using ChartForgeX.Stories;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterAnimationCompatibilityTests {
    [Fact]
    public void PublishedStoriesTypeReferencesResolveToCore() {
        var stories = typeof(VisualStory).Assembly;
        var expected = new[] { typeof(RasterAnimationFormat), typeof(RasterAnimationOptions),
            typeof(RasterAnimationFrame), typeof(RasterAnimationSource), typeof(RasterAnimationEncoder) };
        Assert.Equal(expected.OrderBy(type => type.FullName), stories.GetForwardedTypes().OrderBy(type => type.FullName));
        foreach (var type in expected) {
            Assert.Equal(typeof(RgbaImage).Assembly, type.Assembly);
            Assert.Same(type, Type.GetType(type.FullName + ", ChartForgeX.Stories, Version=3.0.1.0, Culture=neutral, PublicKeyToken=null", true));
        }
    }

}
