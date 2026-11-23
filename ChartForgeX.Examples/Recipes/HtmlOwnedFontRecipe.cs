using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>A native report chart whose HTML adapter can deliver the caller's registered face.</summary>
public static class HtmlOwnedFontRecipe {
    /// <summary>Creates the same native chart for static exports and configurable HTML text.</summary>
    public static Chart Create(string variant, ThemeMode mode, string? family = null) {
        if (variant is not ("wide" or "compact")) throw new ArgumentException("Unknown font report size.", nameof(variant));
        var theme = VisualTheme.Graphite(); if (family != null) theme = theme.WithFontFamily(family);
        return Chart.Create().Size(variant == "compact" ? 390 : 760, 440).Theme(theme).ThemeMode(mode)
            .Title("Review progress").Subtitle("One prepared chart across report formats").XLabels("Received", "Checked", "Approved")
            .DataLabels(true).AddBar("Requests", new[] { new ChartPoint(1, 32), new ChartPoint(2, 26), new ChartPoint(3, 21) });
    }
}
