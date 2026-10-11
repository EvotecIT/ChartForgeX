using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationOpaqueFlowTests {
    [Theory]
    [InlineData("  other: {", "  }")]
    [InlineData("  other: [ {", "  } ]")]
    [InlineData("  other: &defaults {", "  }")]
    [InlineData("  other: !custom {", "  }")]
    [InlineData("  other: !<tag:example.com,2026:test> {", "  }")]
    [InlineData("  foo.bar: {", "  }")]
    [InlineData("  [foo, bar]: {", "  }")]
    [InlineData("  other:\n  - {", "  }")]
    [InlineData("  other:\n    nested: {", "  }")]
    public void OpaqueFlowMembersRemainOwnedUntilTheClosingCollection(string opening, string closing) {
        var source = "---\nconfig:\n" + opening + "\n  theme: dark,\n  fontFamily: Hidden\n" + closing +
            "\n  look: classic\n---\nflowchart LR\nA --> B";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Artifact);
        Assert.Null(result.Document!.Configuration.Theme);
        Assert.Null(result.Document.Configuration.FontFamily);
        Assert.Equal("classic", result.Document.Configuration.Look);
        Assert.Equal(source, result.Document.SourceText);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
    }

    [Fact]
    public void QuotedClosingBracesAndNestedCollectionsCannotEndAnOpaqueFlowEarly() {
        const string source = "---\nconfig:\n  other: {\n  nested: [ { note: \"} ]\" } ],\n  # A presentation comment.\n  theme: dark\n  }\n  look: classic\n---\nflowchart LR\nA --> B";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Artifact);
        Assert.Null(result.Document!.Configuration.Theme);
        Assert.Equal("classic", result.Document.Configuration.Look);
    }

    [Theory]
    [InlineData("  other: |\n    {unmatched literal text")]
    [InlineData("  other: >\n    [unmatched folded text")]
    [InlineData("  other:\n    nested: |\n      {unmatched literal text")]
    [InlineData("  foo.bar: |\n    [unmatched literal text")]
    [InlineData("  other:\n  - |\n      {unmatched sequence scalar")]
    public void BlockScalarBodiesCannotConsumeFollowingSupportedSiblings(string yaml) {
        var result = MermaidRenderer.Render("---\nconfig:\n" + yaml + "\n  theme: dark\n---\nflowchart LR\nA --> B");
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Artifact);
        Assert.Equal("dark", result.Document!.Configuration.Theme);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
    }
}
