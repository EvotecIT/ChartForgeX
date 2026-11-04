using System.Linq;
using ChartForgeX.Markup;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MarkupNumericValueDiagnosticsTests {
    [Theory]
    [InlineData("sequence", "size widex400", "Option 'width' requires a numeric value.")]
    [InlineData("sequence", "size 1e999x400", "")]
    [InlineData("sequence", "size -5x400", "greater than zero")]
    [InlineData("table", "columns:\n| id | width |\n| --- | --- |\n| name | wide |\nend", "Option 'width' requires a numeric value.")]
    [InlineData("table", "columns:\n| id | width |\n| --- | --- |\n| name | NaN |\nend", "greater than zero")]
    [InlineData("topology", "group region \"Region\" width:wide", "Option 'width' requires a numeric value.")]
    [InlineData("topology", "group region \"Region\" width:-1", "width must be a non-negative finite number.")]
    [InlineData("topology", "groups:\n| id | width |\n| --- | --- |\n| region | 1e999 |\nend", "")]
    public void InvalidNumbersBecomeLineDiagnostics(string kind, string body, string message) {
        var result = new VisualMarkupParser().Parse("```chartforgex " + kind + " v1\n" + body + "\n```\n");

        // The offending value line carries the error; incomplete-document errors on the fence line may follow it.
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error
            && diagnostic.Line > 1 && diagnostic.Message.Contains(message));
    }
}
