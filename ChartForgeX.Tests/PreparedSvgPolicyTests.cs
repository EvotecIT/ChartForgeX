using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedSvgPolicyTests {
    [Fact]
    public void SvgPolicySeparatesEqualRgbRolesAndDetachesHostVariablesWithoutChangingPixels() {
        var color = ChartColor.Parse("#2864b4");
        var variables = new SvgColorVariables().Add("--surface", color, SvgColorRole.Surface)
            .Add("--status", color, SvgColorRole.Status).Add("--series", color, SvgColorRole.Series);
        var builder = Builder();
        builder.Rect(new ChartRect(10, 10, 25, 25), color, role: "status", paint: new VisualScenePaintBinding(fill: SvgPaint.Of(color, SvgColorRole.Status)));
        builder.Rect(new ChartRect(40, 10, 25, 25), color, role: "series", paint: new VisualScenePaintBinding(fill: SvgPaint.Of(color, SvgColorRole.Series)));
        builder.Rect(new ChartRect(70, 10, 25, 25), color, role: "derived", paint: new VisualScenePaintBinding(fill: SvgPaint.Literal(color)));
        builder.Rect(new ChartRect(100, 10, 25, 25), color, role: "unbound");
        var prepared = new PreparedVisual(builder.Build()); var pixels = prepared.ToPng(); var original = prepared.ToSvg();
        var policy = new VisualSvgOptions("host-A", variables);
        variables.Add("--later", ChartColor.White); policy.ColorVariables!.Add("--copy", ChartColor.Black);
        var svg = XDocument.Parse(prepared.ToSvg(policy));
        Assert.StartsWith("var(--status,", Fill(svg, "status"), StringComparison.Ordinal);
        Assert.StartsWith("var(--series,", Fill(svg, "series"), StringComparison.Ordinal);
        Assert.Equal(color.ToCss(), Fill(svg, "derived")); Assert.Equal(color.ToCss(), Fill(svg, "unbound"));
        Assert.Equal(3, policy.ColorVariables!.Variables.Count);
        Assert.Equal(pixels, prepared.ToPng()); Assert.Equal(original, prepared.ToSvg());
    }

    [Fact]
    public void SvgPolicyRetainsMixContrastAndGradientBindingsThroughSceneComposition() {
        var series = ChartColor.Parse("#204080"); var surface = ChartColor.White;
        var blended = ChartForgeX.Rendering.ChartColorMath.Blend(series, surface, .5);
        var variables = new SvgColorVariables().Add("--series", series, SvgColorRole.Series)
            .Add("--surface", surface, SvgColorRole.Surface).AddInk("--series-ink", series, ChartColor.White, SvgColorRole.Series);
        var child = Builder();
        child.Rect(new ChartRect(5, 5, 60, 30), blended, role: "mix", paint: new VisualScenePaintBinding(fill: SvgPaint.Mix(blended, series, SvgColorRole.Series, surface, SvgColorRole.Surface, .5)));
        child.Text("Ink", 10, 25, 12, ChartColor.White, role: "ink", paint: SvgPaint.Contrast(series, SvgColorRole.Series));
        child.RectGradient(new ChartRect(5, 45, 60, 30), new ChartPoint(5, 45), new ChartPoint(65, 45), new[] {
            new VisualGradientStop(0, series, SvgPaint.Of(series, SvgColorRole.Series)),
            new VisualGradientStop(1, surface, SvgPaint.Of(surface, SvgColorRole.Surface)) });
        var parent = Builder(); parent.Append(child.Build(), 20, 0, "child");
        var prepared = new PreparedVisual(parent.Build()); var svg = prepared.ToSvg(new VisualSvgOptions(colorVariables: variables));
        Assert.Contains("color-mix(in srgb,", svg); Assert.Contains("var(--series-ink,", svg);
        Assert.Contains("stop-color=\"var(--series,", svg); Assert.Contains("stop-color=\"var(--surface,", svg);
        Assert.DoesNotContain("var(--", prepared.ToSvg());
        var other = new SvgColorVariables().Add("--other", series, SvgColorRole.Series);
        Assert.NotEqual(XDocument.Parse(svg).Descendants().First(element => element.Name.LocalName == "linearGradient").Attribute("id")!.Value,
            XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: other))).Descendants().First(element => element.Name.LocalName == "linearGradient").Attribute("id")!.Value);
    }

    [Fact]
    public void SvgPolicyScopesIdsAndPinsOnlyExplicitStatusGroupsWithSafeKeyboardLinks() {
        var builder = Builder();
        using (builder.PushGroup("status", "status-container", new System.Collections.Generic.Dictionary<string, string> { ["data-cfx-pin-state-colors"] = "true" }))
            builder.Rect(new ChartRect(0, 0, 20, 20), ChartColor.Black);
        using (builder.PushLink("https://example.org/details", "record", tooltip: "Open record"))
            builder.Rect(new ChartRect(30, 0, 20, 20), ChartColor.Black);
        var prepared = new PreparedVisual(builder.Build());
        var document = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions("host-B", linkTarget: VisualSvgLinkTarget.NewContext)));
        var anchor = document.Descendants().Single(element => element.Name.LocalName == "a");
        Assert.Equal("_blank", anchor.Attribute("target")!.Value); Assert.Equal("noopener noreferrer", anchor.Attribute("rel")!.Value);
        Assert.Equal("0", anchor.Attribute("tabindex")!.Value);
        Assert.All(document.Descendants().Attributes("id"), attribute => Assert.StartsWith("host-B-", attribute.Value, StringComparison.Ordinal));
        Assert.Single(document.Descendants().Attributes("style"), attribute => attribute.Value == "forced-color-adjust:none");
        Assert.DoesNotContain("target=", prepared.ToSvg());
        Assert.Throws<ArgumentException>(() => new VisualSvgOptions("bad prefix"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualSvgOptions(linkTarget: (VisualSvgLinkTarget)42));
        Assert.Throws<ArgumentException>(() => builder.PushLink("javascript:alert(1)"));
    }

    [Fact]
    public void PreparationDefaultPolicySurvivesNamespaceAndArtifactAccessibilityOverrides() {
        var color = ChartColor.Parse("#2864b4"); var builder = Builder();
        using (builder.PushLink("/record")) builder.Rect(new ChartRect(0, 0, 20, 20), color,
            paint: new VisualScenePaintBinding(fill: SvgPaint.Of(color, SvgColorRole.Status)));
        var variables = new SvgColorVariables().Add("--status", color, SvgColorRole.Status);
        var scope = VisualSvgOptions.NamespaceFromExternalId("host record:42");
        Assert.Equal(scope, VisualSvgOptions.NamespaceFromExternalId("host record:42"));
        Assert.NotEqual(scope, VisualSvgOptions.NamespaceFromExternalId("host-record:42"));
        var prepared = new PreparedVisual(builder.Build(), svgOptions: new VisualSvgOptions(scope, variables, VisualSvgLinkTarget.NewContext));
        Assert.Contains("var(--status,", prepared.ToSvg()); Assert.Contains("target=\"_blank\"", prepared.ToSvg());
        Assert.Contains("var(--status,", prepared.ToSvg("explicit-host"));
        var artifact = prepared.ToArtifact("record", VisualArtifactKind.Chart); artifact.Accessibility.Name = "Current host title";
        Assert.Contains("Current host title", artifact.ToSvg()); Assert.Contains("var(--status,", artifact.ToSvg());
        Assert.Contains("target=\"_blank\"", artifact.ToSvg());
        Assert.DoesNotContain("var(--", prepared.ToSvg(new VisualSvgOptions()));
        Assert.DoesNotContain("target=", prepared.ToSvg(new VisualSvgOptions()));
    }

    private static VisualSceneBuilder Builder() => new(new VisualSize(160, 100), new FontSpec());
    private static string Fill(XDocument document, string role) => document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == role).Attribute("fill")!.Value;
}
