namespace ChartForgeX.Tests;

/// <summary>Reads the same authored source consumed by the packed adapter and generated showcase.</summary>
internal static class QuadrantSourceDepthFixture {
    internal static string Read(bool dark = false) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "Build", "PackageConsumers", "Adapters", "quadrant-native-source-depth.mmd"));
        return dark ? source.Replace("theme: default", "theme: dark", StringComparison.Ordinal)
            .Replace("quadrantXAxisTextFill: \"#475569\"", "quadrantXAxisTextFill: \"#CBD5E1\"", StringComparison.Ordinal)
            .Replace("quadrantTitleFill: \"#0F172A\"", "quadrantTitleFill: \"#F1F5F9\"", StringComparison.Ordinal) : source;
    }
}
