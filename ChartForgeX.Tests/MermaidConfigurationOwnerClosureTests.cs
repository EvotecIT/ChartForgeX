using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidConfigurationOwnerClosureTests {
    [Fact]
    public void MultilineRootFlowMappingsRetainNestedStringsAndLeaveFollowingFrontmatterSeparate() {
        var parsed = new MermaidParser().Parse("---\nconfig: {\n  \"theme\": \"dark\",\n  \"fontFamily\": \"A}B\",\n  \"flowchart\": {\"look\": \"classic\"}\n}\ntitle: Flow mapping\n---\nflowchart LR\nA --> B");
        Assert.False(parsed.HasErrors);
        Assert.Equal("dark", parsed.Document!.Configuration.Theme);
        Assert.Equal("A}B", parsed.Document.Configuration.FontFamily);
        Assert.Contains("title: Flow mapping", parsed.Document.FrontMatter);
    }

    [Fact]
    public void WardleyOverridesUseTheOfficialDiagramConfigurationScope() {
        var parsed = new MermaidParser().Parse("---\nconfig:\n  theme: default\n  layout: dagre\n  look: classic\n  wardley-beta:\n    theme: dark\n    look: handDrawn\n    layout: elk\n---\nwardley-beta\ncomponent Service [0.5, 0.5]");
        Assert.False(parsed.HasErrors);
        Assert.Equal("dark", parsed.Document!.Configuration.Theme);
        Assert.Equal("handDrawn", parsed.Document.Configuration.Look);
        Assert.Equal("elk", parsed.Document.Configuration.Layout);
        Assert.DoesNotContain(parsed.Diagnostics, item => item.Message.Contains("wardley-beta.theme"));
    }

    [Fact]
    public void AccessibilityDescriptionTextCannotBecomeAnInitializationDirective() {
        const string description = "literal description\n%%{init:\nthis is accessibility text";
        var parsed = new MermaidParser().Parse("flowchart LR\naccDescr {\n" + description + "\n}\nA --> B\n%%{init: {\"theme\":\"dark\"}}%%");
        Assert.False(parsed.HasErrors);
        Assert.Equal(description, parsed.Document!.Accessibility.Description);
        Assert.Equal("dark", parsed.Document.Configuration.Theme);
        Assert.Single(parsed.Document.Directives);
    }

    [Theory]
    [InlineData("{\n\"theme\":\"dark\",\n\"theme\":\"default\"\n}")]
    [InlineData("{\n\"theme\":\"dark\"\n")]
    public void InvalidMultilineFlowObjectsRemainLocatedConfigurationErrors(string mapping) {
        var rendered = MermaidRenderer.Render("---\nconfig: " + mapping + "\n---\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Span.Line == 2);
    }

    [Fact]
    public void MultilineRootFlowIndentedContentCountsTowardTheConfigurationBudget() {
        var rendered = MermaidRenderer.Render("---\nconfig: {\n" + new string(' ', 65536) + "\"theme\":\"dark\"\n}\n---\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Message.Contains("65536"));
    }

    [Theory]
    [InlineData("}")]
    [InlineData("\"")]
    public void OpeningLineCommentsCannotHideFlowContinuationFromTheBudget(string comment) {
        var rendered = MermaidRenderer.Render("---\nconfig: { # " + comment + "\n" + new string(' ', 65536) + "\"theme\":\"dark\"\n}\n---\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration && item.Message.Contains("65536"));
    }

    [Fact]
    public void ClosingLineCommentsCannotChargeUnrelatedFrontmatterToConfiguration() {
        var rendered = MermaidRenderer.Render("---\nconfig: {\"theme\":\"dark\"} # {\ndescription: " + new string('x', 70000) + "\n---\nflowchart LR\nA --> B");
        Assert.False(rendered.HasErrors);
        Assert.Equal("dark", rendered.Document!.Configuration.Theme);
    }
}
