using ChartForgeX.Typography;

/// <summary>Reference-only OFL faces; production consumers continue to supply their own theme fonts.</summary>
internal static class TopologyRegionTypography {
    internal const string Family = "IBM Plex Sans";
    private static readonly (string Name, int Weight)[] Faces = { ("Regular", 400), ("Medium", 500), ("SemiBold", 600), ("Bold", 700) };
    private static string DirectoryPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "IBMPlexSans");
    internal static void Register() {
        if (FontRegistry.Families.Contains(Family, StringComparer.OrdinalIgnoreCase)) return;
        foreach (var face in Faces) FontRegistry.Register(Family, Path.Combine(DirectoryPath, "IBMPlexSans-" + face.Name + ".ttf"), face.Weight);
    }
    internal static string EmbeddedStyle => "<style>/*" + File.ReadAllText(Path.Combine(DirectoryPath, "OFL.txt")) + "*/"
        + string.Join("", Faces.Select(face => "@font-face{font-family:'" + Family + "';font-weight:" + face.Weight
            + ";font-display:block;src:url(data:font/ttf;base64," + Convert.ToBase64String(File.ReadAllBytes(Path.Combine(DirectoryPath, "IBMPlexSans-" + face.Name + ".ttf"))) + ") format('truetype')}")) + "</style>";
}
