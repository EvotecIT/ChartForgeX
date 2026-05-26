using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ChartColorConversionsAndKnownNamesStayDeterministic() {
        Assert(ChartColors.Emerald400.WithAlpha(172).A == 172, "Named chart colors should support explicit alpha values.");
        Assert(ChartColors.Emerald400.WithOpacity(0.5).A == 128, "Named chart colors should support opacity helpers.");
        Assert(ChartColors.Emerald400.WithAlpha(172).ToHexRgba() == "#34D399AC", "Named chart colors should round-trip to RGBA hex.");
        Assert(ChartColor.FromArgb(0x8034D399u).ToHexRgba() == "#34D39980", "Chart colors should support packed ARGB conversion.");
        Assert(ChartColor.FromArgb(unchecked((int)0x8034D399u)).ToArgb() == 0x8034D399u, "Chart colors should round-trip signed packed ARGB conversion.");
        Assert(ChartColor.FromRgba(0x34D39980u).ToArgb() == 0x8034D399u, "Chart colors should support packed RGBA conversion.");
        Assert(ChartColor.FromArgb(128, 52, 211, 153).ToRgba() == 0x34D39980u, "Chart colors should expose packed RGBA output.");
        Assert(ChartColor.TryFromHex("#34D39980", out var rgbaHex) && rgbaHex.ToArgb() == 0x8034D399u, "Chart colors should expose non-throwing hex parsing.");
        Assert(!ChartColor.TryFromHex("#12", out _), "Chart colors should reject invalid hex without throwing when using TryFromHex.");

        var (deR, deG, deB, deA) = ChartColors.Emerald400.WithAlpha(128);
        Assert(deR == 52 && deG == 211 && deB == 153 && deA == 128, "Chart colors should deconstruct into RGBA channels.");
        Assert(ChartColors.GetNamedColors().Count >= 142, "ChartForgeX should expose the stable System.Drawing/CSS named color set.");
        Assert(ChartColors.TryGet("RebeccaPurple", out var rebecca) && rebecca.ToHex() == "#663399", "Named color lookup should include modern CSS colors.");
        Assert(ChartColor.Parse("DarkSlateGrey").ToHex() == ChartColors.DarkSlateGray.ToHex(), "Named color parsing should support grey aliases.");
        Assert(!ChartColors.TryGet("ActiveCaption", out _), "Dynamic Windows system colors should stay out of the deterministic ChartForgeX named color map.");
        Assert(!ChartColors.TryGet("ButtonFace", out _), "Dynamic Windows system color aliases should be resolved by host adapters, not the core renderer.");
        Assert(ChartColors.TryGet("Slate950", out var slate) && slate.ToHex() == ChartColors.Slate950.ToHex(), "Named color lookup should include ChartForgeX design tokens.");
        Assert(ChartColor.Parse("emerald400").ToHex() == ChartColors.Emerald400.ToHex(), "Named color parsing should support ChartForgeX design tokens.");
        Assert(ChartColors.GetTokenColors().Count >= 29, "ChartForgeX should expose its design token color set.");

        var tokenPalette = ChartPalettes.FromHex("Slate950", "Blue400", "Emerald400");
        Assert(tokenPalette[0].ToHex() == ChartColors.Slate950.ToHex() && tokenPalette[2].ToHex() == ChartColors.Emerald400.ToHex(), "Palette parsing should accept ChartForgeX design token names.");
        Assert(ChartColor.Parse("#34D399").ToHex() == ChartColors.Emerald400.ToHex(), "Color parsing should keep hex support.");
    }
}
