using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRangeFlashBeforeControls {
    [Theory]
    [InlineData(false, "lower")] [InlineData(false, "upper")] [InlineData(false, "tuples")] [InlineData(false, "series")]
    [InlineData(true, "lower")] [InlineData(true, "upper")] [InlineData(true, "tuples")] [InlineData(true, "series")]
    public void ExistingPublicEntryCapturesTheRealChangedIntervalEnvelopeAndTupleFlash(bool area, string change) {
        var renderer = new HtmlInteractiveChartRenderer();
        var from = NativeRangeFlashFixture.Section(renderer, area, false, change); var to = NativeRangeFlashFixture.Section(renderer, area, true, change);
        var name = "range-flash-before-" + area + "-" + change;
        // Preserve real supported endpoints before attempting the missing flash admission.
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to");
        Assert.Equal("range-components", NativeRawValueSourceMotionTests.Metadata(from).GetProperty("nativeValueSource").GetProperty("kind").GetString());
        Assert.Equal("range-components", NativeRawValueSourceMotionTests.Metadata(to).GetProperty("nativeValueSource").GetProperty("kind").GetString());
        string? payload = null; var error = Record.Exception(() => payload = renderer.PrepareNativeTransitionJson(from, to,
            new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 5, ChartColor.FromHex("#FFC000"))));
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString() }));
        Assert.Null(error); Assert.NotNull(payload);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExistingIntervalValuesAndOrdinaryDefaultsRemainASeparatePublicControl(bool area) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeRangeFlashFixture.Model(area, false);
        var ordinary = renderer.PrepareSection(chart); var repeat = renderer.PrepareSection(chart);
        var from = renderer.PrepareSection(chart, configure: NativeRangeFlashFixture.Configure); var to = NativeRangeFlashFixture.Section(renderer, area, true);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3));
        NativeRadialMotionBeforeControls.Capture(ordinary, "range-flash-values-control-" + area);
        NativeRadialMotionBeforeControls.Save("range-flash-values-control-" + area + ".transition.json", payload);
        Assert.Equal(ordinary.Html, repeat.Html); Assert.Equal(ordinary.PreparedVisual.ToSvg(), from.PreparedVisual.ToSvg());
        Assert.Equal(ordinary.PreparedVisual.ToPng(), repeat.PreparedVisual.ToPng()); Assert.Equal(ordinary.PreparedVisual.ToPng(), from.PreparedVisual.ToPng());
        Assert.False(NativeRawValueSourceMotionTests.Metadata(ordinary).TryGetProperty("nativeValueSource", out _));
        using var json = JsonDocument.Parse(payload); Assert.Equal("values", json.RootElement.GetProperty("effect").GetString());
    }
}
