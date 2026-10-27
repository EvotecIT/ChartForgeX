namespace ChartForgeX.Tests;

/// <summary>Reads the same canonical XY source used by the adapter package probe and Mermaid showcase.</summary>
internal static class XYChartSourceDepthFixture {
    internal static string Read(bool horizontal = false) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "Build", "PackageConsumers", "Adapters", "xychart-native-source-depth.mmd")).ReplaceLineEndings("\n");
        return horizontal ? source.Replace("xychart-beta\n", "xychart-beta horizontal\n", StringComparison.Ordinal) : source;
    }
}
