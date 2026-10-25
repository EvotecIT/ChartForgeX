namespace ChartForgeX.Tests;

/// <summary>Uses the same authored source as the public package probe and parsed-source showcase.</summary>
internal static class UseCaseSourceDepthFixture {
    internal static string Read(bool compact = false) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "Build", "PackageConsumers", "Adapters", "usecase-native-source-depth.mmd"));
        return compact ? source.Replace("direction LR", "direction TB", StringComparison.Ordinal) : source;
    }
}
