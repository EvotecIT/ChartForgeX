using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class HtmlOwnedFontDeliveryTests : IDisposable {
    [Theory]
    [InlineData(HtmlChartTextPresentation.Automatic)]
    [InlineData(HtmlChartTextPresentation.NativeContours)]
    [InlineData(HtmlChartTextPresentation.Browser)]
    public void TextSelectionOnlyChangesTheDetachedHtmlEmissionAndKeepsNativeSvgPngAndSemantics(HtmlChartTextPresentation mode) {
        var model = Model(); var renderer = new HtmlInteractiveChartRenderer();
        var section = renderer.PrepareSection(model, configure: options => options.TextPresentation = mode);
        var drawing = section.PreparedVisual; var svg = drawing.ToSvg(); var png = drawing.ToPng(); var json = drawing.ToArtifact().ToInterchangeJson();
        Capture(mode.ToString(), section);
        Assert.Equal(svg, drawing.ToSvg()); Assert.Equal(png, drawing.ToPng()); Assert.Equal(json, drawing.ToArtifact().ToInterchangeJson());
        Assert.Equal(svg, drawing.ToSvg(new VisualSvgOptions().WithTextPresentation(VisualSvgTextPresentation.Browser)));
        Assert.Equal(mode == HtmlChartTextPresentation.Automatic, section.Html.Contains("data-cfx-font-root=", StringComparison.Ordinal));
        if (mode != HtmlChartTextPresentation.Browser) Assert.Contains("data-cfx-native-text-resources", section.WideEmission!.Svg, StringComparison.Ordinal);
        if (mode == HtmlChartTextPresentation.Automatic) {
            Assert.Contains("data-cfx-owned-font-native", section.Html, StringComparison.Ordinal);
            Assert.Contains("data-cfx-owned-font-text", section.Html, StringComparison.Ordinal);
            Assert.DoesNotContain("src:url(data:font", section.Html, StringComparison.Ordinal);
            Assert.DoesNotContain("blob:", section.Html, StringComparison.Ordinal);
        }
    }
    [Fact]
    public void AdaptiveBindingsShareOneActualRootPayloadAcrossBothCapturedEmissions() {
        var section = new HtmlInteractiveChartRenderer().PrepareSection(Model(), configure: options => { options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.TextPresentation = HtmlChartTextPresentation.Automatic; });
        Capture("adaptive", section);
        Assert.NotNull(section.NarrowEmission); Assert.Equal(VisualSvgTextPresentation.Automatic, section.NarrowEmission!.Options.TextPresentation);
        var payloads = Regex.Matches(section.Html, "data-cfx-font-root=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();
        Assert.Equal(payloads.Distinct(StringComparer.Ordinal).Count(), payloads.Length);
        var aliases = Regex.Matches(section.Html, "data-cfx-font-face=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();
        Assert.Equal(aliases.Distinct(StringComparer.Ordinal).Count(), aliases.Length);
        Assert.All(section.WideEmission!.Fonts!.Faces.Concat(section.NarrowEmission.Fonts!.Faces), face => Assert.Contains(face.Alias, aliases));
        Assert.Equal(section.NarrowEmission.Svg, section.NarrowPreparedVisual!.ToSvg(section.NarrowEmission.Options));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void MixedFallbackAndSelectedCollectionMembersUseCanonicalNativeInkWithoutAFalseBrowserFace(bool collection) {
        const string family = "CFX S10 Selected Font";
        if (collection) FontRegistry.Register(family, OpenTypeTestFonts.Collection(TopologyOwnedFontPortableTests.Bytes(), TopologyOwnedFontPortableTests.Bytes(weight:700)), 700, collectionIndex:1);
        else TopologyOwnedFontPortableTests.Register();
        var model = Model(collection ? family : TopologyOwnedFontPortableTests.Primary + ", " + TopologyOwnedFontPortableTests.Fallback);
        model.WithTitle("").WithXLabels(collection ? "HH" : "H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter), "OH");
        var section = new HtmlInteractiveChartRenderer().PrepareSection(model); Capture("native-fallback-" + collection, section);
        var resources = section.WideEmission!.Fonts!;
        Assert.Contains("data-cfx-native-text-ink", section.Html, StringComparison.Ordinal);
        if (collection) Assert.Empty(resources.Faces);
        else Assert.Contains(section.PreparedVisual.Scene.Nodes.OfType<VisualSceneText>(), node => node.Text.Lines.Any(line => line.Glyphs.Select(glyph => glyph.Face.Root).Distinct().Count() > 1));
        Assert.All(resources.Faces, face => Assert.True(face.Font.IsSingleCapturedFace));
    }
    [Fact]
    public void OversizedOwnedRootsKeepStaticAndHtmlNativeInkWithoutEnteringPortableFontAdmission() {
        const string family = "CFX S10 Oversize Font"; var original = TopologyOwnedFontPortableTests.Bytes();
        var bytes = new byte[OwnedFontResources.MaximumRootBytes + 1]; Array.Copy(original, bytes, original.Length); FontRegistry.Register(family, bytes);
        var section = new HtmlInteractiveChartRenderer().PrepareSection(Model(family)); Capture("oversize-native", section);
        Assert.Empty(section.WideEmission!.Fonts!.Faces); Assert.DoesNotContain("data-cfx-font-root=", section.Html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-native-text-ink", section.Html, StringComparison.Ordinal); Assert.NotEmpty(section.PreparedVisual.ToPng());
    }
    [Fact]
    public void LegacyTopologyUsesAutomaticWhileAnExplicitPresentationCallbackKeepsItsOwnSvg() {
        var chart = TopologyOwnedFontPortableTests.Source(); chart.Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);
        // The default node 600 request deliberately stays native; the available regular 400 face qualifies owned browser text.
        chart.Nodes[0].TextStyle = new TextStyleOverride { FontWeight = "400" };
        var renderer = new HtmlInteractiveTopologyRenderer(); var automatic = renderer.RenderFragmentWithoutAssets(chart);
        var calls = 0; var explicitSvg = renderer.RenderPresentationFragmentWithoutAssets(chart, prepared => { calls++; return prepared.ToSvg(); });
        Save("legacy-auto.html", automatic); Save("legacy-callback.html", explicitSvg);
        Assert.Contains("data-cfx-font-root=", automatic, StringComparison.Ordinal); Assert.Contains("data-cfx-owned-font-native", automatic, StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-font-root=", explicitSvg, StringComparison.Ordinal); Assert.Equal(1, calls);
    }
    [Fact]
    public void UndefinedTextModeFailsClearlyAndTheStandaloneAssetIsTheOneBinaryRegistrationOwner() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlInteractiveChartRenderer().PrepareSection(Model(), configure: options => options.TextPresentation = (HtmlChartTextPresentation)999));
        var script = HtmlInteractiveAssetFiles.NativeTextScript.Content; Save("standalone.js", script);
        Assert.Single(Regex.Matches(script, @"new FontFace\("));
        Assert.DoesNotContain("fetch(", script, StringComparison.Ordinal); Assert.DoesNotContain("url(data:font", script, StringComparison.Ordinal);
        Assert.Contains("new Uint8Array(borrowed)", script, StringComparison.Ordinal); Assert.Contains("font-src 'self'", File.ReadAllText(Path.Combine(TestRepository.Root, "Website", "site.json")), StringComparison.Ordinal);
    }
    [Fact]
    public void ShippedFontBytesStayInNormalHtmlWhileInitialFadeKeepsTheUnchangedOneMiBPresentationBudget() {
        var renderer = new HtmlInteractiveChartRenderer();
        var section = renderer.PrepareSection(Model(), configure: options => options.Interaction.Enable(ChartForgeX.Interactivity.ChartInteractionFeatures.Export));
        Capture("large-font-transition", section);
        var payload = renderer.PrepareNativeTransitionJson(section, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, 2));
        Save("large-font-transition.json", payload); Save("large-font-transition.observation.html", section.TransitionHtml);
        Assert.True(Encoding.UTF8.GetByteCount(section.Html) > 1024 * 1024);
        Assert.InRange(Encoding.UTF8.GetByteCount(payload), 1, 1024 * 1024);
        var destination = JsonDocument.Parse(payload).RootElement.GetProperty("destination").GetString();
        HtmlFontRootObservation.AssertDestination(section.Html, destination!); Assert.NotEqual(section.Html, destination);
        foreach (var face in section.WideEmission!.Fonts!.Faces) {
            Assert.Contains(face.Font.CapturedByteIdentity, destination!, StringComparison.Ordinal);
            Assert.Contains(face.Alias, destination!, StringComparison.Ordinal);
        }
        Assert.Contains("data-cfx-font-byte-length", destination!, StringComparison.Ordinal);
        Assert.NotEmpty(section.PreparedVisual.ToPng());
    }
    [Fact]
    public void IndependentDestinationObservationRetainsEveryNonPayloadFieldAndFontDescriptor() {
        var renderer = new HtmlInteractiveChartRenderer(); var section = renderer.PrepareSection(Model());
        using var json = JsonDocument.Parse(renderer.PrepareNativeTransitionJson(section, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, 2)));
        var destination = json.RootElement.GetProperty("destination").GetString()!;
        HtmlFontRootObservation.AssertDestination(section.Html, destination);
        var foreignField = destination.Replace("cfx-interactive-chart", "cfx-foreign-section", StringComparison.Ordinal);
        Assert.NotEqual(destination, foreignField);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => HtmlFontRootObservation.AssertDestination(section.Html, foreignField));
        var alteredWeight = Regex.Replace(destination, "data-cfx-font-weight=\"[0-9]+\"", "data-cfx-font-weight=\"1\"");
        Assert.NotEqual(destination, alteredWeight);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => HtmlFontRootObservation.AssertDestination(section.Html, alteredWeight));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => HtmlFontRootObservation.AssertDestination(section.Html, destination.Replace("data-cfx-font-byte-length", "data-cfx-foreign-byte-length", StringComparison.Ordinal)));
    }
    internal static Chart Model(string? family = null) => Chart.Create().WithSize(720,420).WithTitle("Owned native text").WithXLabels("H", "O")
        .WithTheme(VisualTheme.Graphite().WithFontFamily(family ?? BrowserProofFonts.Family)).AddBar("Observed", new[] { new ChartPoint(1, 12), new ChartPoint(2, 18) });
    private static void Capture(string name, HtmlChartSection section) {
        Save(name + ".html", section.Html); Save(name + ".svg", section.PreparedVisual.ToSvg());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output,"owned-html-"+name+".png"), section.PreparedVisual.ToPng()); }
    }
    internal static void Save(string name,string text) { if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not{Length:>0} output)return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"owned-html-"+name),text); }
    public void Dispose() { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
}
