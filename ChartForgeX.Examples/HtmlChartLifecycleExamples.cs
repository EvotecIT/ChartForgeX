using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

/// <summary>Shows native data replacement inside a retained interactive chart host.</summary>
public static class HtmlChartLifecycleExamples {
    /// <summary>Creates an example with source-backed updates and explicit browser disposal/remounting.</summary>
    /// <param name="dark">Whether to use the dark Graphite palette.</param>
    /// <returns>A self-contained HTML page.</returns>
    public static string CreatePage(bool dark = false) {
        Chart Create(bool updated) {
            var values = updated ? new[] { new ChartBubble(2, 9, 25), new ChartBubble(1, 17, 64) }
                : new[] { new ChartBubble(1, 5, 9), new ChartBubble(2, 8, 25) };
            var samples = ChartSeries.CreateBubble(new ChartSeriesId("samples"), "Samples", values)
                .WithObservationId(updated ? 1 : 0, new ChartObservationId("north"))
                .WithObservationId(updated ? 0 : 1, new ChartObservationId("south"));
            var reference = ChartSeries.CreateLine(new ChartSeriesId("reference"), "Reference",
                new[] { new ChartPoint(0, 20), new ChartPoint(3, 20) });
            var chart = Chart.Create().WithSize(800, 440).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
                .WithTitle("Measurements that keep their identity")
                .WithSubtitle("Select a bubble or mute Reference, then update the measurements")
                .WithXAxisBounds(0, 3).WithYAxisBounds(0, 25).WithLegend().WithDataLabels(false);
            return updated ? chart.AddSeries(reference).AddSeries(samples) : chart.AddSeries(samples).AddSeries(reference);
        }
        void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "measurement-lifecycle";
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        }
        var initial = Create(false);
        var fragments = JsonSerializer.Serialize(new[] {
            initial.ToInteractiveHtmlFragmentWithoutAssets(Configure), Create(true).ToInteractiveHtmlFragmentWithoutAssets(Configure)
        });
        const string controls = "<div class='cfx-toolbar cfx-lifecycle-controls' aria-label='Example host controls'><button class='cfx-tool' type='button' id='update-measurements'>Update measurements</button>"
            + "<button class='cfx-tool' type='button' id='dispose-chart'>Dispose chart</button><button class='cfx-tool' type='button' id='mount-chart' hidden>Mount again</button>"
            + "<span id='host-status' role='status'>Original measurements</span></div>";
        var colors = VisualTheme.Graphite().Resolve(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var style = "<style>.cfx-lifecycle-demo{width:min(100%,1000px);min-width:0}"
            + ".cfx-lifecycle-controls{margin:0 0 16px;justify-content:flex-start;align-items:center;background:" + colors.Surface.ToCss() + ";border-color:" + colors.Border.ToCss() + "}"
            + ".cfx-lifecycle-controls .cfx-tool{background:" + colors.Surface.ToCss() + ";color:" + colors.Foreground.ToCss() + ";border-color:" + colors.Border.ToCss() + "}"
            + ".cfx-lifecycle-controls [hidden]{display:none}.cfx-lifecycle-controls .cfx-tool:disabled{opacity:.5;cursor:default}"
            + ".cfx-lifecycle-controls [role=status]{font-size:13px}</style>";
        var script = "<script>const replacements=" + fragments + ";" + """
            const chartRoot=document.querySelector('.cfx-interactive-chart');
            let controller=ChartForgeXCharts.mount(chartRoot), updated=false;
            const update=document.querySelector('#update-measurements'), dispose=document.querySelector('#dispose-chart'), mount=document.querySelector('#mount-chart'), status=document.querySelector('#host-status');
            update.addEventListener('click',()=>{updated=!updated;controller.update(replacements[Number(updated)]);status.textContent=updated?'Updated measurements: North is 17':'Original measurements';});
            dispose.addEventListener('click',()=>{controller.dispose();update.disabled=true;dispose.hidden=true;mount.hidden=false;status.textContent='Chart disposed';});
            mount.addEventListener('click',()=>{controller=ChartForgeXCharts.mount(chartRoot);update.disabled=false;dispose.hidden=false;mount.hidden=true;status.textContent='Chart mounted';});
            </script>
            """;
        return initial.ToInteractiveHtmlPage(Configure).Replace("<section class=\"cfx-interactive-chart\"", "<div class='cfx-lifecycle-demo'>" + controls + "<section class=\"cfx-interactive-chart\"", StringComparison.Ordinal)
            .Replace("</main>", "</div></main>", StringComparison.Ordinal).Replace("</head>", style + "</head>", StringComparison.Ordinal)
            .Replace("</body>", script + "</body>", StringComparison.Ordinal);
    }
}
