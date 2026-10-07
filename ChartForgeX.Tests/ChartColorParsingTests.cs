using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// The non-throwing colour parsers decide most inputs without an exception; they must accept and reject exactly what
/// <see cref="ChartColor.FromHex"/> does, including channels the framework parser pads with white space.
/// </summary>
public sealed class ChartColorParsingTests {
    public static IEnumerable<object[]> Inputs => new[] {
        "#34D399", "34d399", "#34D39980", "#abc", "#abcd", "abc", " #34D399 ", "#", "##abc", "#12", "#12345", "#1234567",
        "none", "url(#cfx-area0)", "var(--brand, #112233)", "currentColor", "rgba(1,2,3,0.5)", "#gg0000", "#00gg00",
        "#1 2233", "# 12233", "#12 233", "#1\t2233", "#12\t\t33", "#1 2233", "#  2233", "#1\u00002233", "#\u00001\u00002233",
        "#a b", "#ab c", "#x00000", "#12345g", "12 34 56", "#0F172A", "\t#0f172a\n", "Emerald400"
    }.Select(value => new object[] { value });

    [Theory]
    [MemberData(nameof(Inputs))]
    public void TryParseAndTryFromHex_AnyInput_MatchFromHexExactly(string input) {
        var expectedHex = Reference(input, out var expected);
        Assert.Equal(expectedHex, ChartColor.TryFromHex(input, out var hexColor));
        if (expectedHex) Assert.Equal(expected.ToRgba(), hexColor.ToRgba());

        var named = ChartColors.TryGet(input.Trim(), out var namedColor);
        Assert.Equal(named || expectedHex, ChartColor.TryParse(input, out var parsed));
        if (named) Assert.Equal(namedColor.ToRgba(), parsed.ToRgba());
        else if (expectedHex) Assert.Equal(expected.ToRgba(), parsed.ToRgba());
    }

    [Fact]
    public void TryFromHex_EverySixDigitChannelPair_MatchesFromHex() {
        // Every printable ASCII pair in the first channel, with valid digits elsewhere.
        for (var first = 0x20; first < 0x7F; first++) {
            for (var second = 0x20; second < 0x7F; second++) {
                var input = "#" + (char)first + (char)second + "AABB";
                var expectedHex = Reference(input, out var expected);
                Assert.Equal(expectedHex, ChartColor.TryFromHex(input, out var color));
                if (expectedHex) Assert.Equal(expected.ToRgba(), color.ToRgba());
            }
        }
    }

    private static bool Reference(string input, out ChartColor color) {
        color = default;
        if (string.IsNullOrWhiteSpace(input)) return false;
        try {
            color = ChartColor.FromHex(input);
            return true;
        } catch (ArgumentException) {
            return false;
        }
    }
}
