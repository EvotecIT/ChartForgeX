namespace ChartForgeX.Tests;

internal static class UseCaseJsonTableFixture {
    internal static string Read(bool compact = false) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "Build", "PackageConsumers", "Adapters", "usecase-json-table-content.mmd"));
        return compact ? source.Replace("direction LR", "direction TB", StringComparison.Ordinal) : source;
    }
}
