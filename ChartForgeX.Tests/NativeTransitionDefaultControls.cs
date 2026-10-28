using System.Text;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Incumbent APIs only: retain every supported finite value plan and old one-edit payload before generic presentation changes.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeTransitionDefaultControls {
    public static IEnumerable<object[]> Cases() {
        foreach (var kind in new[] { "progress", "layered", "circle", "rings", "gauge-horizontal", "bullet-horizontal" }) {
            yield return new object[] { kind, 390, true }; yield return new object[] { kind, 760, false };
        }
    }
    [Theory, MemberData(nameof(Cases))]
    public void SixIncumbentTypedPlansAndOneEditPayloadsRetainOrdinaryBytes(string kind, int width, bool dark) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(kind, width, dark, .2); var to = Section(kind, width, dark, .8);
        var json = renderer.PrepareValueTransitionJson(from, to, 3); Save(kind + "-" + width + "-" + dark, from, to, json);
        Assert.Equal(json, renderer.PrepareValueTransitionJson(from, to, 3)); Assert.NotNull(from.NarrowPreparedVisual);
    }
    internal static HtmlChartSection Section(string kind, int width, bool dark, double ratio) => new HtmlInteractiveChartRenderer().PrepareSection(Model(kind, width, dark, ratio), configure: options => Configure(options, dark));
    internal static Chart Model(string kind, int width, bool dark, double ratio) => kind switch {
        "progress" => NativeProgressTransitionTests.Model(width, dark, 0, ratio, BrowserProofFonts.Family),
        "layered" => NativeLayeredValueTransitionTests.Model(width, dark, 1, ratio * 100, BrowserProofFonts.Family),
        "circle" => NativeCircleValueTransitionTests.Model(width, dark, ratio * 100, BrowserProofFonts.Family),
        _ => NativeRemainingValueTransitionTests.Model(kind, width, dark, ratio * 100, BrowserProofFonts.Family)
    };
    internal static void Configure(HtmlChartInteractionOptions options, bool dark) {
        options.IdScope = dark ? "Wyniki źródła:42" : "native-transition-values"; options.Interaction.ChartId = "native-transition-values";
        if (dark) options.Interaction.Enable(ChartInteractionFeatures.Export); else options.Interaction.Disable(ChartInteractionFeatures.Export);
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420;
    }
    private static void Save(string name, HtmlChartSection from, HtmlChartSection to, string payload) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Native transition controls require an absolute capture directory.");
        var prefix = Path.Combine(output, "native-transition-default", name); Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        File.WriteAllText(prefix + ".from.html", from.Html, new UTF8Encoding(false)); File.WriteAllText(prefix + ".to.html", to.Html, new UTF8Encoding(false));
        File.WriteAllText(prefix + ".legacy.json", payload, new UTF8Encoding(false));
        void Drawing(string suffix, PreparedVisual visual) { File.WriteAllText(prefix + suffix + ".svg", visual.ToSvg(), new UTF8Encoding(false)); File.WriteAllBytes(prefix + suffix + ".png", visual.ToPng()); }
        Drawing(".from.wide", from.PreparedVisual); Drawing(".from.narrow", from.NarrowPreparedVisual!);
        Drawing(".to.wide", to.PreparedVisual); Drawing(".to.narrow", to.NarrowPreparedVisual!);
        Drawing(".middle.wide", VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3)[1]);
        Drawing(".middle.narrow", VisualValueCapture.Frames(from.NarrowPreparedVisual!, to.NarrowPreparedVisual!, 3)[1]);
        File.WriteAllText(prefix + ".source.json", JsonSerializer.Serialize(new { name, oldPublicApi = "PrepareValueTransitionJson", frames = 3, adaptive = true }), new UTF8Encoding(false));
    }
}
