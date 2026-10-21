namespace ChartForgeX.Tests;

/// <summary>Dedicated physical temporary paths for executable example fixtures.</summary>
internal static class ExampleTestOutput {
    internal static string CreateScratchPath(string prefix) {
        // System temp paths may contain aliases. Resolve their existing ancestors so
        // generated output reaches the example owner's ordinary safety checks.
        var directory = new DirectoryInfo(Path.GetTempPath());
        var segments = new Stack<string>();
        while (directory.Parent is { } parent) {
            segments.Push(directory.Name);
            directory = parent;
        }
        var physical = directory.FullName;
        while (segments.Count > 0) {
            var child = new DirectoryInfo(Path.Combine(physical, segments.Pop()));
            physical = child.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? child.FullName;
        }
        return Path.Combine(physical, prefix + Guid.NewGuid().ToString("N"));
    }
}
