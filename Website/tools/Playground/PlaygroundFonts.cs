using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Playground;

/// <summary>
/// Registers Carlito before the first render. The default Graphite stack starts with Calibri, then Carlito, which shares
/// Calibri's metrics. A browser exposes no installed fonts to WebAssembly, so without this the engine measures labels with
/// fallback metrics and fits text differently from a .NET host. The SIL Open Font License travels with the fonts as the
/// embedded resource Fonts.Carlito-OFL.txt.
/// </summary>
internal static class PlaygroundFonts {
    private static readonly Lazy<bool> Registered = new(Register, true);

    internal static void EnsureRegistered() => _ = Registered.Value;

    private static bool Register() {
        Add("Fonts.Carlito-Regular.ttf", 400);
        Add("Fonts.Carlito-Bold.ttf", 700);
        return true;
    }

    private static void Add(string resource, int weight) {
        using var stream = typeof(PlaygroundFonts).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("Embedded font is missing: " + resource);
        FontRegistry.Register("Carlito", stream, weight);
    }
}
