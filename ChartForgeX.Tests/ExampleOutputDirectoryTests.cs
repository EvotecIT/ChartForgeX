using Xunit;

namespace ChartForgeX.Tests;

[CollectionDefinition(nameof(ExampleOutputDirectoryTests), DisableParallelization = true)]
public sealed class ExampleOutputDirectoryCollection { }

/// <summary>Rerunning the examples replaces the dedicated output while protecting surrounding source.</summary>
[Collection(nameof(ExampleOutputDirectoryTests))]
public sealed class ExampleOutputDirectoryTests {
    [Fact]
    public void ResetRemovesPreviousArtifactsAndRetainsTheDedicatedDirectory() {
        var scratch = CreateScratchPath("CFX-example-output-");
        var output = Path.Combine(scratch, "output");
        try {
            Directory.CreateDirectory(Path.Combine(output, "topology-demo"));
            File.WriteAllText(Path.Combine(output, "renamed-chart.html"), "old chart");
            File.WriteAllText(Path.Combine(output, "topology-demo", "removed-chart.svg"), "old diagram");
            File.WriteAllText(Path.Combine(scratch, "keep.txt"), "outside selected output");
            ExampleOutputDirectory.Reset(output);
            Assert.True(Directory.Exists(output));
            Assert.Empty(Directory.EnumerateFileSystemEntries(output));
            Assert.Equal("outside selected output", File.ReadAllText(Path.Combine(scratch, "keep.txt")));
            File.WriteAllText(Path.Combine(output, "current-chart.html"), "new chart");
            ExampleOutputDirectory.Reset(output);
            Assert.Empty(Directory.EnumerateFileSystemEntries(output));
        } finally {
            if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
        }
    }

    [Fact]
    public void ResetUsesPhysicalTemporaryRootWhenUnixTmpdirHasALinkedAncestor() {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
        var scratch = CreateScratchPath("CFX-example-tmpdir-");
        var real = Path.Combine(scratch, "real");
        var link = Path.Combine(scratch, "linked");
        var previous = Environment.GetEnvironmentVariable("TMPDIR");
        try {
            var physicalTemp = Path.Combine(real, "temp");
            Directory.CreateDirectory(physicalTemp);
            File.WriteAllText(Path.Combine(physicalTemp, "keep.txt"), "temporary-root sentinel");
            Directory.CreateSymbolicLink(link, real);
            var logicalTemp = Path.Combine(link, "temp");
            Environment.SetEnvironmentVariable("TMPDIR", logicalTemp);
            Assert.Equal(logicalTemp, Path.TrimEndingDirectorySeparator(Path.GetTempPath()));
            ResetRemovesPreviousArtifactsAndRetainsTheDedicatedDirectory();
            ResetRejectsGitMetadataBeforeDeletingNeighboringArtifacts();
            Assert.Equal("temporary-root sentinel", File.ReadAllText(Path.Combine(physicalTemp, "keep.txt")));
            Assert.NotNull(new DirectoryInfo(link).LinkTarget);
        } finally {
            Environment.SetEnvironmentVariable("TMPDIR", previous);
            if (Directory.Exists(link)) Directory.Delete(link);
            if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
        }
    }

    [Theory]
    [InlineData("root")]
    [InlineData("application")]
    [InlineData("working")]
    public void ResetRejectsProtectedDirectoriesBeforeDeletingAnything(string variant) {
        var output = variant == "root" ? Path.GetPathRoot(Path.GetTempPath())! :
            variant == "application" ? AppContext.BaseDirectory : Directory.GetCurrentDirectory();
        Assert.Throws<ArgumentException>(() => ExampleOutputDirectory.Reset(output));
        Assert.True(Directory.Exists(output));
    }

    [Fact]
    public void ResetRejectsGitMetadataBeforeDeletingNeighboringArtifacts() {
        var output = CreateScratchPath("CFX-example-git-");
        try {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, ".git"), "synthetic metadata marker");
            File.WriteAllText(Path.Combine(output, "keep.html"), "valuable");
            var error = Assert.Throws<ArgumentException>(() => ExampleOutputDirectory.Reset(output));
            Assert.Contains("Git metadata", error.Message);
            Assert.Equal("valuable", File.ReadAllText(Path.Combine(output, "keep.html")));
        } finally {
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetRejectsUnixSymbolicLinksBeforeDeletingArtifacts(bool linkIsAncestor) {
        // Windows link creation may require privileges; this fixture exercises ordinary Unix links only.
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
        var scratch = CreateScratchPath("CFX-example-link-");
        var real = Path.Combine(scratch, "real");
        var link = Path.Combine(scratch, "linked");
        var physicalOutput = linkIsAncestor ? Path.Combine(real, "output") : real;
        try {
            Directory.CreateDirectory(physicalOutput);
            File.WriteAllText(Path.Combine(physicalOutput, "keep.html"), "valuable");
            Directory.CreateSymbolicLink(link, real);
            var output = linkIsAncestor ? Path.Combine(link, "output") : link;
            var error = Assert.Throws<ArgumentException>(() => ExampleOutputDirectory.Reset(output));
            Assert.Contains("filesystem link", error.Message);
            Assert.Equal("valuable", File.ReadAllText(Path.Combine(physicalOutput, "keep.html")));
            Assert.NotNull(new DirectoryInfo(link).LinkTarget);
        } finally {
            if (Directory.Exists(link)) Directory.Delete(link);
            if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
        }
    }

    private static string CreateScratchPath(string prefix) {
        // Temp roots can contain system aliases. Resolve each existing ancestor before
        // creating the dedicated fixture so positive tests reach their intended guard.
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
