using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// <see cref="SvgPaint.Resolve"/> finds paint tokens with a hand-written scanner and resolves each distinct token once.
/// The result must equal the replacement through the token pattern for any text, with and without variables and with
/// literals kept or resolved.
/// </summary>
public sealed class SvgPaintResolveTests {
    private const string Start = "﷐";
    private const string End = "﷑";

    private static readonly string[] Fragments = {
        Start, End, "L", "P", "I", "M", "0", "7", "8", "9", "A", "F", "G", "a", "f", ".", "E", "e", "+", "-", " ", "<rect fill=\"", "\"/>",
        "1E40AF", "FFFFFFFF", "1E40AFFF", "0.5", "1e-3", new string('5', 31), new string('5', 33),
        Start + "L1E40AFFF" + End, Start + "P11E40AFFF" + End, Start + "P8FFFFFFFF" + End, Start + "I01E40AFFFFFFFFFFF" + End,
        Start + "M1E40AFFF11E40AFFFLFFFFFFFF0.25" + End, Start + "M1E40AFFF2FFFFFFFF31E40AFFF1" + End,
        Start + "M1E40AFFFLFFFFFFFFL000000FF" + new string('1', 32) + End, Start + "M1E40AFFFLFFFFFFFFL000000FF" + new string('1', 33) + End,
        Start + "L1e40afff" + End, Start + "L1E40AFF" + End, Start + Start + "L1E40AFFF" + End + End
    };

    [Fact]
    public void Resolve_RandomText_MatchesThePatternReplacement() {
        var variables = new SvgColorVariables()
            .Add("--paint-blue", ChartColor.FromHex("#1E40AF"), SvgColorRole.Series)
            .Add("--paint-white", ChartColor.FromHex("#FFFFFF"), SvgColorRole.Surface);
        var random = new Random(20261007);
        for (var sample = 0; sample < 6000; sample++) {
            var parts = new string[random.Next(1, 20)];
            for (var i = 0; i < parts.Length; i++) parts[i] = Fragments[random.Next(Fragments.Length)];
            var text = string.Concat(parts);
            foreach (var current in new[] { null, variables }) {
                Assert.Equal(SvgPaint.ResolveWithPattern(text, current), SvgPaint.Resolve(text, current));
                Assert.Equal(SvgPaint.ResolveWithPattern(text, current, keepLiterals: true), SvgPaint.Resolve(text, current, keepLiterals: true));
            }
        }
    }

    [Fact]
    public void Resolve_RepeatedTokens_ResolvesEveryOccurrence() {
        var token = SvgPaint.Of(ChartColor.FromHex("#1E40AF"), SvgColorRole.Series).Value!;
        var text = "<a fill=\"" + token + "\"/><b stroke=\"" + token + "\"/>" + token;
        var resolved = SvgPaint.Resolve(text, null);
        Assert.Equal(SvgPaint.ResolveWithPattern(text, null), resolved);
        Assert.DoesNotContain(Start, resolved, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_TextWithoutTokens_ReturnsTheSameInstance() {
        const string Text = "<svg><rect fill=\"#1E40AF\"/>" + Start + "not a token" + End + "</svg>";
        Assert.Same(Text, SvgPaint.Resolve(Text, null));
    }
}
