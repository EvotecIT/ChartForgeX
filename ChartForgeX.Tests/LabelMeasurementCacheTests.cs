using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Text measurement is memoized in lock-striped shards of two generations each and computed outside the shard locks; the cache must never change a
/// measurement, whether a value comes from either generation, is recomputed after eviction or is measured concurrently.
/// </summary>
public sealed class LabelMeasurementCacheTests {
    [Fact]
    public void Measure_MoreDistinctTextsThanBothGenerations_ReturnsTheSameMetricsAsAFreshService() {
        var style = new TextStyle { Font = FontSpec.SystemSans(), FontSize = 12, LineHeight = 1 };
        var cached = new LabelPlacementService();
        var texts = Enumerable.Range(0, 10_000).Select(i => "Label " + i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var first = texts.Select(text => cached.Measure(text, style)).ToArray();
        // Re-measure in reverse, so values come from the current generation, the previous one and recomputation.
        for (var i = texts.Length - 1; i >= 0; i--) {
            var again = cached.Measure(texts[i], style);
            var fresh = new LabelPlacementService().Measure(texts[i], style);
            Assert.Equal(first[i], again);
            Assert.Equal(fresh, again);
        }
    }

    [Fact]
    public void Measure_Concurrently_ReturnsTheSequentialMetrics() {
        var style = new TextStyle { Font = FontSpec.SystemSans(), FontSize = 11, LineHeight = 1.2 };
        var texts = Enumerable.Range(0, 2_000).Select(i => "Concurrent label " + (i % 500).ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var expected = texts.Select(text => new LabelPlacementService().Measure(text, style)).ToArray();
        var shared = new LabelPlacementService();
        var actual = new TextMetrics[texts.Length];
        Parallel.For(0, texts.Length, i => actual[i] = shared.Measure(texts[i], style));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Measure_StylesDifferingOnlyInUnderline_DoNotShareACachedValue() {
        var plain = new TextStyle { Font = FontSpec.SystemSans(), FontSize = 12, LineHeight = 1 };
        var underlined = new TextStyle { Font = FontSpec.SystemSans(), FontSize = 12, LineHeight = 1, UnderlineStyle = TextDecorationStyle.Double };
        var expectedPlain = new LabelPlacementService().Measure("Underline", plain);
        var expectedUnderlined = new LabelPlacementService().Measure("Underline", underlined);
        var shared = new LabelPlacementService();
        Assert.Equal(expectedPlain, shared.Measure("Underline", plain));
        Assert.Equal(expectedUnderlined, shared.Measure("Underline", underlined));
        var reversed = new LabelPlacementService();
        Assert.Equal(expectedUnderlined, reversed.Measure("Underline", underlined));
        Assert.Equal(expectedPlain, reversed.Measure("Underline", plain));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("  Segoe UI  ")]
    [InlineData("Arial")]
    public void MeasureText_DefaultFamily_MeasuresAsTheStyleWithThatDefaultFamily(string family) {
        var style = new TextStyleOverride { FontWeight = "600" };
        var expected = ChartLabelScene.MeasureText("Default family", 13, style.WithDefaultFontFamily(family), 400);
        Assert.Equal(expected, ChartLabelScene.MeasureText("Default family", 13, style, 400, family));
    }

    [Fact]
    public void Measure_ConcurrentlyWithEvictionInEveryShard_ReturnsTheSequentialMetrics() {
        var style = new TextStyle { Font = FontSpec.SystemSans(), FontSize = 13, LineHeight = 1.1 };
        // More distinct texts than both generations of all shards hold, measured twice in a scrambled order.
        var texts = Enumerable.Range(0, 12_000).Select(i => "Sharded " + ((i * 7919) % 6_000).ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var reference = new LabelPlacementService();
        var expected = texts.Select(text => reference.Measure(text, style)).ToArray();
        var shared = new LabelPlacementService();
        var actual = new TextMetrics[texts.Length];
        Parallel.For(0, texts.Length, i => actual[i] = shared.Measure(texts[i], style));
        Assert.Equal(expected, actual);
    }
}
