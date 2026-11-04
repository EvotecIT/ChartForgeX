using System;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

public sealed partial class VisualDesignTokens {
    private static readonly Lazy<string> CanonicalDocument = new(() => {
        using var stream = typeof(VisualDesignTokens).Assembly.GetManifestResourceStream("ChartForgeX.Themes.Tokens.evotec.chartforgex.tokens.json")
            ?? throw new InvalidOperationException("The canonical theme resource is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }, true);
    private static readonly Lazy<ChartColor[]> CanonicalLight = new(() => CanonicalSeries("light"), true);
    private static readonly Lazy<ChartColor[]> CanonicalDark = new(() => CanonicalSeries("dark"), true);

    internal static string CanonicalJson => CanonicalDocument.Value;
    internal static ChartColor[] CanonicalPalette(bool dark) => (ChartColor[])(dark ? CanonicalDark.Value : CanonicalLight.Value).Clone();

    private static ChartColor[] CanonicalSeries(string mode) {
        // Read the embedded series without constructing default tokens: defaults themselves use this palette.
        var root = GeoJsonValue.Parse(CanonicalJson, StringComparer.Ordinal, TokenJsonLimits).AsObject("visual tokens");
        return Series(Member(root, mode, mode), mode + ".series");
    }
}
