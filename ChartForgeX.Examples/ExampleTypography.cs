using ChartForgeX.Typography;

/// <summary>Shares the licensed gallery faces between native metrics and example HTML.</summary>
internal static class ExampleTypography {
    internal const string FontFamily = "CFX Proof Carlito";
    private static readonly Lazy<string> Embedded = new(() => "<style>/*"
        + File.ReadAllText(Path.Combine(FontDirectory, "OFL.txt")) + "*/"
        + Face("Regular", 400) + Face("Bold", 700) + "</style>");

    internal static string EmbeddedStyle => Embedded.Value;

    internal static void Register() {
        FontRegistry.Register(FontFamily, FontPath("Regular"), 400);
        FontRegistry.Register(FontFamily, FontPath("Bold"), 700);
    }

    internal static void WriteAssets(string output) {
        Register();
        var destination = Path.Combine(output, "fonts");
        Directory.CreateDirectory(destination);
        foreach (var file in new[] { "Carlito-Regular.ttf", "Carlito-Bold.ttf", "OFL.txt" })
            File.Copy(Path.Combine(FontDirectory, file), Path.Combine(destination, file), true);
    }

    private static string FontDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
    private static string FontPath(string weight) => Path.Combine(FontDirectory, "Carlito-" + weight + ".ttf");
    private static string Face(string weight, int numeric) => "@font-face{font-family:'" + FontFamily
        + "';font-weight:" + numeric + ";font-display:block;src:url(data:font/ttf;base64,"
        + Convert.ToBase64String(File.ReadAllBytes(FontPath(weight))) + ") format('truetype')}";
}
