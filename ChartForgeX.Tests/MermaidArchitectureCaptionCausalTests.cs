using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidArchitectureCaptionCausalTests {
    [Fact]
    public void OfficialOptionalGroupAndServiceCaptionsAreAccepted() {
        var parsed = new MermaidParser().ParseArchitecture("architecture-beta\ngroup scope(cloud)\nservice node(server) in scope");
        Assert.False(parsed.HasErrors); Assert.Equal("", parsed.Document!.Groups[0].Title);
    }

    [Fact]
    public void OfficialDatabaseBuiltinUsesTheExistingNativeCatalogIdentity() {
        var parsed = new MermaidParser().ParseArchitecture("architecture-beta\nservice node(database)[Node]");
        Assert.Empty(parsed.Diagnostics); Assert.Equal("common:database", parsed.Document!.ToTopologyChart().Nodes[0].IconId);
    }
}
