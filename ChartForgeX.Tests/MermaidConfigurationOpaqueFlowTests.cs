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
}
