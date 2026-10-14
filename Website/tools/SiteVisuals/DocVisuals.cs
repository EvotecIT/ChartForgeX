// Figures for the documentation: the docs index map and the visuals each page talks about.
// Docs place them with the cfx-figure shortcode; the surface table on "Choose a rendering surface" takes one per row.
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace SiteVisuals;

internal static class DocVisuals {
    public const string ServiceMap = """
        ```chartforgex topology v1
        id service-map
        title "Service dependency map"
        subtitle "Production dependencies and latency"
        layout layered lr

        group platform "Platform" status:healthy icon:service
        group data "Data layer" status:warning icon:database

        node web "Web front end" kind:service group:platform status:healthy icon:service
        node api "Public API" kind:service group:platform status:healthy icon:service badge:v2
        node worker "Billing worker" kind:process group:platform status:warning icon:worker
        node sql "SQL primary" kind:database group:data status:warning icon:database subtitle:"failover lag 2m"
        node cache "Redis cache" kind:database group:data status:healthy icon:database

        edge web -> api "HTTPS" kind:dependency status:healthy direction:forward
        edge api -> cache "3 ms" kind:dependency status:healthy direction:forward
        edge api -> worker "queue" kind:dataflow status:warning direction:forward
        edge worker -> sql "84 ms" kind:dependency status:warning direction:forward
        ```
        """;

    public static Dictionary<string, object> Write(VisualOutput output, Dictionary<string, object> examples) {
        Dictionary<string, object> Example(string slug) => VisualOutput.Reuse((Dictionary<string, object>)examples[slug]);
        var topology = output.Pair("docs/topology", (_, dark) => VisualOutput.Playground("chartforgex", ServiceMap, dark));
        var figures = new SortedDictionary<string, object>(StringComparer.Ordinal) {
            ["overview"] = output.Pair("docs/overview", (tokens, _) => Overview(tokens).ToSvg("cfx-doc-overview")),
            ["install"] = output.Pair("docs/install", (tokens, _) => Packages(tokens).ToSvg("cfx-doc-install")),
            ["topology"] = topology,
            ["hierarchy"] = Example("organization-hierarchy"),
            ["interactivity"] = Example("interactive-topology")
        };

        // A story is a timeline, so its figure is a strip of frames from the example on the Stories page.
        var frames = new[] { 2.4, 6.5, 9.5 };
        for (var index = 0; index < frames.Length; index++) {
            var at = TimeSpan.FromSeconds(frames[index]);
            figures["stories-" + (index + 1)] = output.Pair("docs/stories-" + (index + 1), (_, dark) => StoryFrame(dark, at));
        }

        // One output per row of the surface table, in table order.
        var surfaces = new SortedDictionary<string, object>(StringComparer.Ordinal) {
            ["chart"] = output.Pair("docs/surface-chart", (tokens, _) => SurfaceChart(tokens).ToSvg("cfx-sf-chart")),
            ["grid"] = Example("report-grid"),
            ["blocks"] = output.Pair("docs/surface-blocks", (tokens, _) => Blocks(tokens).ToSvg("cfx-sf-blocks")),
            ["canvas"] = output.Pair("docs/surface-canvas", (tokens, _) => Canvas(tokens).ToSvg("cfx-sf-canvas")),
            ["topology"] = VisualOutput.Reuse(topology),
            ["graph"] = Example("interactive-topology"),
            ["markup"] = output.Pair("docs/surface-markup", (_, dark) => VisualOutput.Fit(MermaidAtlas.Render("surface-markup", """
                ---
                title: Markdown to visuals
                ---
                flowchart LR
                  md[Markdown fence] --> parse{Parse}
                  parse --> chart[Chart]
                  parse --> diagram[Diagram]
                """, dark, new List<string>()), 1.6))
        };

        return new Dictionary<string, object> { ["figures"] = figures, ["surfaces"] = surfaces };
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();

    private static ChartGrid Overview(VisualDesignTokens tokens) {
        var trend = Chart.Create().WithDesignTokens(tokens).WithTitle("Deployment health").WithSubtitle("Checks per run")
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
            .AddSmoothArea("Passed", Points(820, 940, 980, 1040, 1120))
            .AddSmoothLine("Failed", Points(220, 300, 280, 210, 180));
        var share = Chart.Create().WithDesignTokens(tokens).WithTitle("Results").WithSubtitle("This week")
            .WithXLabels("Passed", "Warnings", "Failed").AddDonut("Checks", Points(1260, 68, 10));
        var coverage = Chart.Create().WithDesignTokens(tokens).WithTitle("MFA coverage").WithSubtitle("Against a 95% target")
            .AddGauge("Coverage", 87).ConfigureGauge(options => options.Target = 95);
        var teams = Chart.Create().WithDesignTokens(tokens).WithTitle("Open findings").WithSubtitle("By team")
            .WithXLabels("Identity", "Network", "Endpoints", "Mail", "Data")
            .AddBar("Open", Points(14, 9, 21, 6, 11));
        return ChartGrid.Create().WithColumns(3).WithGap(12).WithPanelSize(320, 240).WithPanelFit(VisualPanelFit.Stretch)
            .Add(trend, 2).Add(share).Add(coverage).Add(teams, 2);
    }

    private static ChartTable Packages(VisualDesignTokens tokens) {
        var table = ChartTable.Create()
            .WithTitle("Packages")
            .AddColumn("Package")
            .AddColumn("Adds")
            .AddColumn("Status")
            .AddRow("ChartForgeX", "Charts, grids, topology, SVG, PNG, HTML", "Core")
            .AddRow("ChartForgeX.Visuals", "Visual blocks, canvases, image composition", "Optional")
            .AddRow("ChartForgeX.Interactivity.Html", "Self-contained interactive pages", "Optional")
            .AddRow("ChartForgeX.Markup", "Charts and diagrams from Markdown fences", "Optional")
            .AddRow("ChartForgeX.Mermaid", "Native Mermaid diagrams", "Optional")
            .WithSize(760, 300);
        tokens.ApplyTo(table.Options.Theme);
        return table;
    }

    private static Chart SurfaceChart(VisualDesignTokens tokens) => Chart.Create().WithDesignTokens(tokens)
        .WithTitle("Orders by region").WithSize(480, 300)
        .WithXLabels("North", "South", "East", "West")
        .AddBar("This year", Points(48, 61, 57, 72))
        .AddBar("Last year", Points(41, 55, 60, 63));

    private static VisualGrid Blocks(VisualDesignTokens tokens) {
        var cards = new[] {
            MetricCard.Create().WithMetric("CPU load", "38%").WithMiniSparkline(new[] { 52d, 48d, 44d, 41d, 38d }),
            MetricCard.Create().WithMetric("Memory used", "71%").WithMiniBars(new[] { 55d, 59d, 63d, 68d, 71d }, maximum: 100)
        };
        foreach (var card in cards) tokens.ApplyTo(card.Options.Theme);
        return VisualGrid.CreateMetricStrip("Endpoint snapshot", cards, columns: 2, panelWidth: 260, panelHeight: 150);
    }

    private static VisualCanvas Canvas(VisualDesignTokens tokens) {
        var trend = Chart.Create().WithDesignTokens(tokens).WithSize(420, 220).WithTransparentBackground().WithHeader(false).WithCard(false)
            .WithLegend(false).AddSmoothArea("Sign-ins", Points(18, 31, 24, 45, 39, 52, 61));
        return VisualCanvas.CreateSocialPreview()
            .WithTitle("Release notes cover")
            .WithDesignTokens(tokens)
            .AddText(72, 96, 620, "Release 2.0", 64, emphasized: true)
            .AddText(72, 196, 620, "Charts, topology and Mermaid diagrams, rendered in .NET.", 28)
            .AddChart(640, 300, 500, 260, trend);
    }

    // A three-scene story: type a chart program, run it in an authored terminal, show the chart it describes.
    // The terminal transcript is authored; stories present a session, they never execute it.
    private static string StoryFrame(bool dark, TimeSpan at) {
        const string program = "var chart = Chart.Create()\n    .WithTitle(\"Sign-ins by method\")\n    .AddStackedArea(\"Passkey\", passkey)\n    .AddStackedArea(\"Password\", password);\nchart.SaveSvg(\"sign-ins.svg\");";
        var editor = StorySourceTimeline.Create(StorySourceText.Create("", "csharp"))
            .Type(program, TimeSpan.FromSeconds(3))
            .Pause(TimeSpan.FromSeconds(1));
        var terminal = TerminalStory.Create()
            .WithDialect(TerminalDialect.PowerShell)
            .WithTheme(dark ? TerminalTheme.GraphiteDark() : TerminalTheme.GraphiteLight())
            .WithWorkingDirectory(@"C:\Reports")
            .Command("dotnet run")
            .Output("Wrote sign-ins.svg", TerminalTextTone.Success);
        var tokens = dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight();
        var chart = Chart.Create().WithDesignTokens(tokens).WithTitle("Sign-ins by method").WithSize(800, 520)
            .WithXLabels("W1", "W2", "W3", "W4", "W5", "W6")
            .AddStackedArea("Passkey", Points(800, 1700, 3000, 4700, 6400, 8100))
            .AddStackedArea("Password", Points(5600, 4900, 4100, 3300, 2500, 1800));

        var story = VisualStory.Create("From source to chart")
            .WithTheme(dark ? VisualStoryTheme.GraphiteDark() : VisualStoryTheme.GraphiteLight())
            .WithFormat(VisualStoryFormat.Widescreen);
        story.Scene("write", "Write the chart", 4)
            .Panel("source", new VisualStorySourceSurface(editor, options: new VisualStorySourceOptions("Report.cs", fontSize: 30)));
        story.Scene("run", "Run it", 3)
            .Panel("terminal", new VisualStoryTerminalSurface(terminal, options: new VisualStoryTerminalOptions(fontSize: 30)));
        story.Scene("result", "Read the result", 3, VisualStorySceneLayout.Split)
            .Panel("code", new VisualStorySourceSurface(StorySourceText.Create(program, "csharp"), options: new VisualStorySourceOptions("Report.cs", fontSize: 22)))
            .Panel("chart", new VisualStoryMediaSurface(chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 520)),
                themeMode: dark ? VisualThemeMode.Dark : VisualThemeMode.Light)), "Sign-ins by method, passkey sign-ins rising"));
        story.Outcome("chart", "Sign-ins chart", "chart");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(endHold: TimeSpan.FromSeconds(2), playCount: 1));
        return prepared.ToSvg(at);
    }
}
