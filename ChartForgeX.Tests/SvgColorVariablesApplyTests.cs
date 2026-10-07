using System.Reflection;
using System.Text.RegularExpressions;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// <see cref="SvgColorVariables.Apply"/> scans start tags by hand and skips tags that cannot hold a mapped paint. The
/// result must equal the pattern-based replacement over every tag, which these tests rebuild from the class's own
/// patterns and tag replacement as the reference.
/// </summary>
public sealed class SvgColorVariablesApplyTests {
    private static readonly string[] Fragments = {
        "<rect", "<text", "<tspan", "<g", "<style>", "</style>", "</g>", "/>", ">", "<", " ", "\n", "\"", "'",
        " fill=\"#1E40AF\"", " fill='#1e40af'", " stroke=\"rgb(30, 64, 175)\"", " stop-color=\"rgba(30,64,175,0.5)\"", " fill=\"#ffffff\"",
        " style=\"fill:#1E40AF;stroke:rgb(255,255,255)\"", " data-fill=\"#1E40AF\"", " fill=\"var(--x, #1E40AF)\"", " fill=\"﷐P11E40AFFF﷑\"",
        " id=\"a#b\"", " title=\"a<b>c\"", " fill=\"RGB(1,2,3)\"", " color=\"#1E40AF\"", " x=\"1\"", "<ét fill=\"#1E40AF\"", "<aé fill=\"#1E40AF\"",
        "<!-- <rect fill=\"#1E40AF\"> -->", "#text{fill:#1E40AF}", "<p:q fill=\"#1E40AF\"", "<a.b-c_d fill=\"#ffffff\""
    };

    [Fact]
    public void Apply_RandomMarkup_MatchesThePatternReplacement() {
        var variables = Variables();
        var random = new Random(20261007);
        for (var sample = 0; sample < 4000; sample++) {
            var parts = new string[random.Next(1, 24)];
            for (var i = 0; i < parts.Length; i++) parts[i] = Fragments[random.Next(Fragments.Length)];
            var markup = string.Concat(parts);
            Assert.Equal(Reference(variables, markup), variables.Apply(markup));
        }
    }

    [Fact]
    public void Apply_RenderedChart_MatchesThePatternReplacement() {
        var variables = Variables();
        var svg = ChartForgeX.Core.Chart.Create().WithSize(500, 300).WithTitle("Apply")
            .AddBar("A", new[] { new ChartPoint(1, 2), new ChartPoint(2, 4) }, ChartColor.FromHex("#1E40AF"))
            .AddLine("B", new[] { new ChartPoint(1, 3), new ChartPoint(2, 1) }, ChartColor.FromHex("#FFFFFF")).ToSvg();
        var applied = variables.Apply(svg);
        Assert.Equal(Reference(variables, svg), applied);
        Assert.Contains("var(--bench-blue", applied, StringComparison.Ordinal);
    }

    private static SvgColorVariables Variables() => new SvgColorVariables()
        .Add("--bench-blue", ChartColor.FromHex("#1E40AF"), SvgColorRole.Series)
        .Add("--bench-white", ChartColor.FromHex("#FFFFFF"), SvgColorRole.Surface);

    // The replacement before the scanner: the style-element pass, then StartTag.Replace over every tag.
    private static string Reference(SvgColorVariables variables, string svg) {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var type = typeof(SvgColorVariables);
        var startTag = (Regex)type.GetField("StartTag", Flags)!.GetValue(null)!;
        var styleElement = (Regex)type.GetField("StyleElement", Flags)!.GetValue(null)!;
        var declarations = type.GetMethod("Declarations", Flags)!;
        var replaceTag = type.GetMethod("ReplaceTag", Flags)!;
        var result = styleElement.Replace(svg, match => match.Groups["open"].Value
            + (string)declarations.Invoke(variables, new object[] { match.Groups["css"].Value, false })! + match.Groups["close"].Value);
        return startTag.Replace(result, match => match.Groups["attrs"].Value.Length == 0 ? match.Value
            : (string)replaceTag.Invoke(variables, new object[] { match.Groups["tag"].Value, match.Groups["attrs"].Value })!);
    }
}
