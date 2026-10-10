using System.Reflection;

namespace ChartForgeX.Tests;

internal static class TestRepository {
    // Test artifacts can be routed outside the checkout. Fixture and source checks still observe the exact built repository.
    internal static string Root {
        get {
            var source = typeof(TestRepository).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "RepositoryRoot").Value!;
            if (!File.Exists(Path.Combine(source, "ChartForgeX.sln"))) throw new InvalidOperationException("The built test repository is unavailable: " + source);
            return source;
        }
    }
}
