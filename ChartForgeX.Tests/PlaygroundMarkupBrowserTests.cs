using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class PlaygroundMarkupBrowserTests {
    [BrowserTheory]
    [InlineData(false, "\n")]
    [InlineData(true, "\n")]
    [InlineData(false, "\r\n")]
    public async Task OptionsTableControlsUpdateTheParsedChartWithoutChangingItsData(bool comments, string newline) {
        if (!Enabled) return;
        var docs = File.ReadAllText(Path.Combine(Repository(), "docs", "markup.md"));
        var source = Regex.Match(docs, "```chartforgex chart v1 \\{#trend[^\\r\\n]*[\\s\\S]*?\\r?\\n```(?=\\r?\\n)").Value;
        Assert.NotEmpty(source);
        source = source.Replace("\r\n", "\n").Replace("\n", newline);
        if (comments) source = source.Replace("options:", "options: // render settings");
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>");
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            async ({module, source}) => {
                const controls = await import(module);
                return { before: controls.readChartOptions(source),
                    wide: controls.applyChartOption(source, 'size', 'wide'),
                    legend: controls.applyChartOption(source, 'legend', 'Top'),
                    labels: controls.applyChartOption(source, 'labels', false) };
            }
            """, new { module = Module(), source });
        Assert.Equal("off", result.GetProperty("before").GetProperty("legend").GetString());
        Assert.True(result.GetProperty("before").GetProperty("labels").GetBoolean());
        var original = Parse(source);
        var wide = Parse(result.GetProperty("wide").GetString()!);
        Assert.Equal(1200, wide.Options.Size.Width);
        Assert.Equal(520, wide.Options.Size.Height);
        var legend = Parse(result.GetProperty("legend").GetString()!);
        Assert.True(legend.Options.ShowLegend);
        Assert.Equal(ChartLegendPosition.Top, legend.Options.LegendPosition);
        var labels = Parse(result.GetProperty("labels").GetString()!);
        Assert.False(labels.Options.ShowDataLabels);
        foreach (var chart in new[] { wide, legend, labels })
            Assert.Equal(original.Series[0].Points.ToArray(), chart.Series[0].Points.ToArray());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ControlsPreserveDataRowsNamedAfterCommands() {
        if (!Enabled) return;
        const string data = """
            data:
            | Label | Portal | API |
            | ----- | ------ | --- |
            legend | 3 | 4
            dataLabels | 5 | 6
            size | 7 | 8
            type | 9 | 10
            """;
        var source = "```chartforgex chart v1 {type=\"bar\"}\n" + data + "\n```";
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>");
        var edited = await session.Page.EvaluateAsync<string>("""
            async ({module, source}) => {
                const {applyChartOption} = await import(module);
                for (const [option, value] of [['type','line'], ['legend','Top'], ['labels',true], ['size','wide']])
                    source = applyChartOption(source, option, value);
                return source;
            }
            """, new { module = Module(), source });
        Assert.Contains(data, edited);
        var original = Parse(source);
        var chart = Parse(edited);
        Assert.Equal(original.Series.Count, chart.Series.Count);
        for (var index = 0; index < original.Series.Count; index++)
            Assert.Equal(original.Series[index].Points.ToArray(), chart.Series[index].Points.ToArray());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("````", "")]
    [InlineData("~~~~", "")]
    [InlineData("````", "    ")]
    [InlineData("````", "\t")]
    public async Task ControlsSkipChartExamplesInsideOtherFences(string marker, string falseCloseIndent) {
        if (!Enabled) return;
        var falseClose = falseCloseIndent.Length == 0 ? "" : falseCloseIndent + marker + "\n";
        var example = marker + "markdown\n" + falseClose + "```chartforgex chart v1 {type=\"bar\"}\nlabels Example\nvalues 99\n```\n" + marker;
        var source = example + "\n```chartforgex chart v1 {type=\"line\"}\nlabels A B\nvalues 3 4\n```";
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>");
        var edited = await session.Page.EvaluateAsync<string>("""
            async ({module, source}) => (await import(module)).applyChartOption(source, 'size', 'wide')
            """, new { module = Module(), source });
        Assert.Contains(example, edited);
        var chart = Parse(edited);
        Assert.Equal(1200, chart.Options.Size.Width);
        Assert.Equal(520, chart.Options.Size.Height);
        Assert.Equal(Parse(source).Series[0].Points.ToArray(), chart.Series[0].Points.ToArray());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("    ")]
    [InlineData("\t")]
    public async Task ControlsSkipIndentedCodeExamples(string indent) {
        if (!Enabled) return;
        var example = string.Join("\n", new[] { "```chartforgex chart v1 {type=\"bar\"}", "labels Example", "values 99", "```" }
            .Select(line => indent + line));
        var source = example + "\n```chartforgex chart v1 {type=\"line\"}\nlabels A B\nvalues 3 4\n```";
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>");
        var edited = await session.Page.EvaluateAsync<string>("""
            async ({module, source}) => (await import(module)).applyChartOption(source, 'size', 'wide')
            """, new { module = Module(), source });
        Assert.Contains(example, edited);
        var chart = Parse(edited);
        Assert.Equal(1200, chart.Options.Size.Width);
        Assert.Equal(520, chart.Options.Size.Height);
        Assert.Equal(Parse(source).Series[0].Points.ToArray(), chart.Series[0].Points.ToArray());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("size=\"1200x520\" type=\"bar\"")]
    [InlineData("width=1200 height=520 type=\"bar\"")]
    [InlineData("type=\"bar\" title=\"Canvas width=1200\" size=\"1200x520\"")]
    public async Task StandardSizeRemovesLeadingFenceDimensions(string attributes) {
        if (!Enabled) return;
        const string body = "\nlabels A B\nvalues 1 2\n```";
        var source = "```chartforgex chart v1 {" + attributes + "}" + body;
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>");
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            async ({module, source}) => {
                const {applyChartOption, readChartOptions} = await import(module);
                return [applyChartOption(source, 'size', ''),
                    applyChartOption(applyChartOption(source, 'size', 'compact'), 'size', '')]
                    .map(text => ({source: text, size: readChartOptions(text).size}));
            }
            """, new { module = Module(), source });
        var standard = Parse("```chartforgex chart v1 {type=\"bar\"}" + body);
        Assert.Equal(2, result.GetArrayLength());
        foreach (var edited in result.EnumerateArray()) {
            Assert.Equal("", edited.GetProperty("size").GetString());
            var chart = Parse(edited.GetProperty("source").GetString()!);
            Assert.Equal(standard.Options.Size, chart.Options.Size);
            Assert.Equal(Parse(source).Title, chart.Title);
            Assert.Equal(standard.Series[0].Points.ToArray(), chart.Series[0].Points.ToArray());
        }
        AssertNoConsoleErrors(session);
    }

    private static Chart Parse(string source) {
        var result = new MarkupChartParser().Parse(source);
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        return Assert.IsType<MarkupChartDocument>(result.Document).Chart;
    }

    private static string Repository() => typeof(PlaygroundMarkupBrowserTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().Single(item => item.Key == "RepositoryRoot").Value!;

    private static string Module() => "data:text/javascript;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
        File.ReadAllText(Path.Combine(Repository(), "Website", "themes", "chartforgex", "assets", "playground-markup.js"))));
}
