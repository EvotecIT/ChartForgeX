using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Rerunning the examples replaces the dedicated output while protecting surrounding source.</summary>
public sealed class ExampleOutputDirectoryTests {
    [Fact]
    public void ResetRemovesPreviousArtifactsAndRetainsTheDedicatedDirectory() {
        var scratch = Path.Combine(Path.GetTempPath(), "CFX-example-output-" + Guid.NewGuid().ToString("N"));
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
        var output = Path.Combine(Path.GetTempPath(), "CFX-example-git-" + Guid.NewGuid().ToString("N"));
        try {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, ".git"), "synthetic metadata marker");
            File.WriteAllText(Path.Combine(output, "keep.html"), "valuable");
            Assert.Throws<ArgumentException>(() => ExampleOutputDirectory.Reset(output));
            Assert.Equal("valuable", File.ReadAllText(Path.Combine(output, "keep.html")));
        } finally {
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        }
    }
}
