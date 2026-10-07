/// <summary>Prepares the dedicated generated-example directory for a complete replacement run.</summary>
public static class ExampleOutputDirectory {
    /// <summary>Removes the previous generated set without accepting roots, checkout ancestors or links.</summary>
    /// <param name="output">A dedicated directory whose previous contents may be replaced.</param>
    public static void Reset(string output) {
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(output));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.Equals(target, Path.GetPathRoot(target), comparison) ||
            Contains(target, AppContext.BaseDirectory, comparison) || Contains(target, Directory.GetCurrentDirectory(), comparison))
            throw new ArgumentException("Example output must be a dedicated directory, not a filesystem root or application/working-directory ancestor.", nameof(output));
        var repository = FindRepository(Directory.GetCurrentDirectory()) ?? FindRepository(AppContext.BaseDirectory);
        if (repository != null && Contains(target, repository, comparison))
            throw new ArgumentException("Example output cannot replace the repository or one of its ancestors.", nameof(output));
        for (var ancestor = new DirectoryInfo(target); ancestor != null; ancestor = ancestor.Parent)
            if (ancestor.Exists && (ancestor.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Example output cannot pass through a filesystem link.", nameof(output));
        if (Directory.Exists(target)) {
            var pending = new Stack<string>();
            pending.Push(target);
            while (pending.Count > 0) {
                foreach (var entry in Directory.EnumerateFileSystemEntries(pending.Pop())) {
                    var attributes = File.GetAttributes(entry);
                    if ((attributes & FileAttributes.ReparsePoint) != 0 || string.Equals(Path.GetFileName(entry), ".git", comparison))
                        throw new ArgumentException("Example output cannot contain filesystem links or Git metadata.", nameof(output));
                    if ((attributes & FileAttributes.Directory) != 0) pending.Push(entry);
                }
            }
            Directory.Delete(target, recursive: true);
        }
        Directory.CreateDirectory(target);
    }

    private static bool Contains(string parent, string child, StringComparison comparison) {
        child = Path.TrimEndingDirectorySeparator(Path.GetFullPath(child));
        return string.Equals(parent, child, comparison) || child.StartsWith(parent + Path.DirectorySeparatorChar, comparison);
    }

    private static string? FindRepository(string directory) {
        for (var ancestor = new DirectoryInfo(directory); ancestor != null; ancestor = ancestor.Parent)
            if (File.Exists(Path.Combine(ancestor.FullName, ".git")) || Directory.Exists(Path.Combine(ancestor.FullName, ".git"))) return ancestor.FullName;
        return null;
    }
}
