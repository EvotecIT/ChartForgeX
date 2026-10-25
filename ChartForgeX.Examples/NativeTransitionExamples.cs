using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

/// <summary>Public adapter consumers use captured native frames and finite typed sections; the host owns only commands and transport.</summary>
internal static class NativeTransitionExamples {
    internal static void Write(string output) {
        Directory.CreateDirectory(output); ExampleTypography.Register(); var renderer = new HtmlInteractiveChartRenderer();
        foreach (var width in new[] { 390, 760 }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            void Configure(HtmlChartInteractionOptions options) {
                options.Interaction.ChartId = "native-presentation-example";
                options.IdScope = mode == VisualThemeMode.Dark ? "Wyniki źródła:42" : "native-presentation-example";
                options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.LegendToggles);
                options.Interaction.Disable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
                options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420;
            }
            Chart Progress(double fraction, double count) => Chart.Create().WithSize(width, 400).WithTheme(VisualTheme.Graphite(), mode).WithFontFamily(ExampleTypography.FontFamily)
                .WithTitle("Native values in each row's units").WithLegend(false).AddSeries(ChartSeries.CreateProgressBars(new ChartSeriesId("delivery"), "Delivery", new[] {
                    new ChartProgressItem("Reviewed fraction", fraction, 0, 1), new ChartProgressItem("Verified jobs", count, 0, 2500)
                }).WithObservationId(0, new ChartObservationId("reviewed")).WithObservationId(1, new ChartObservationId("verified")));
            var before = renderer.PrepareSection(Progress(.2, 1000), configure: Configure); var after = renderer.PrepareSection(Progress(.8, 2000), configure: Configure);
            foreach (var effect in new[] { HtmlNativeTransitionEffect.Values, HtmlNativeTransitionEffect.InitialValues, HtmlNativeTransitionEffect.ValueFlash }) {
                var payload = renderer.PrepareNativeTransitionJson(before, after, new HtmlNativeTransitionOptions(effect, 3,
                    effect == HtmlNativeTransitionEffect.ValueFlash ? ChartColor.FromHex("#FFD900") : null));
                var captured = JsonSerializer.Serialize(payload); var destination = JsonSerializer.Serialize(after.Html);
                var script = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),play=document.querySelector('#play');play.onclick=()=>{";
                script += effect == HtmlNativeTransitionEffect.InitialValues
                    ? "chart.presentInitial(" + captured + ",{sceneRevision:chart.sceneRevision,motion:{duration:650}});"
                    : "chart.update(" + destination + ",{sceneRevision:chart.sceneRevision,transition:" + captured + ",motion:{duration:650}});play.disabled=true;";
                script += "};";
                WriteProgram("native-" + effect.ToString().ToLowerInvariant() + "-" + width + "-" + mode.ToString().ToLowerInvariant(), before, after, payload,
                    effect == HtmlNativeTransitionEffect.InitialValues ? after : before, script,
                    "The host supplies two real native value states with explicit domains. Initial presentation retains the authoritative final scene; the other actions accept one final scene.", new { width, mode = mode.ToString(), effect = effect.ToString(), frameCount = 3 });
            }
            foreach (var labels in new[] { false, true }) {
                var chart = Chart.Create().WithSize(width, 400).WithTheme(VisualTheme.Graphite(), mode).WithFontFamily(ExampleTypography.FontFamily)
                    .WithTitle("Retained native legend visibility").WithXLabels("Opened", "Reviewed", "Released").WithDataLabels(labels).WithLegend(true)
                    .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("reviewed"), "Reviewed", ChartPoints.FromValues(18, 70, 44)));
                var policy = labels ? ChartVisibilityLabelLayering.GroupWithSeries : ChartVisibilityLabelLayering.PreserveOverlay;
                HtmlChartSection Section(bool hidden) => renderer.PrepareVisibilitySection(chart,
                    new ChartVisibilityView(hidden ? new[] { new ChartSeriesId("reviewed") } : null, policy), configure: Configure);
                var visible = Section(false); var hidden = Section(true); var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3);
                var forward = renderer.PrepareNativeTransitionJson(visible, hidden, options); var reverse = renderer.PrepareNativeTransitionJson(hidden, visible, options);
                var catalog = JsonSerializer.Serialize(new { visible = new { html = visible.Html, basis = "visible", transition = reverse }, hidden = new { html = hidden.Html, basis = "hidden", transition = forward } });
                var script = "const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),catalog=" + catalog + ";chart.connectNative(request=>{";
                script += "if(!['visibility','reset'].includes(request.kind)||!request.sourceVisibility)throw new TypeError('This finite host accepts its authored absolute legend views and Reset.');";
                script += "const target=request.sourceVisibility.hiddenSeries.includes('reviewed')?catalog.hidden:catalog.visible;return Promise.resolve({...target,transition:request.basis===target.basis?undefined:target.transition});},{basis:'visible',axes:['x'],legend:true,motion:{duration:650}});document.querySelector('#play').hidden=true;";
                WriteProgram("native-legend-" + (labels ? "grouped-labels-" : "labels-off-") + width + "-" + mode.ToString().ToLowerInvariant(), visible, hidden, forward,
                    visible, script, labels ? "Each series plot and its already placed captions share one native composite. Later series or chart context can occlude those captions. Click a legend item, then Reset."
                    : "The native source catalog, legend and full-data domains stay retained when a series is hidden. Click a legend item, then Reset.", new { width, mode = mode.ToString(), labels, labelLayering = policy.ToString(), frameCount = 3 });
            }
        }
        void WriteProgram(string name, HtmlChartSection before, HtmlChartSection after, string payload, HtmlChartSection initial, string script, string explanation, object source) {
            var prefix = Path.Combine(output, name); File.WriteAllText(prefix + ".transition.json", payload);
            File.WriteAllText(prefix + ".from.svg", before.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".from.png", before.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".to.svg", after.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".to.png", after.PreparedVisual.ToPng());
            File.WriteAllText(prefix + ".source.json", JsonSerializer.Serialize(source));
            File.WriteAllText(prefix + ".interactive.html", "<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
                + ExampleTypography.EmbeddedStyle + "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "body{margin:16px;font:14px sans-serif}button{font:inherit;padding:8px 12px}</style></head><body>"
                + "<p>" + explanation + "</p><button id=\"play\">Show native presentation</button>" + initial.Html
                + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script><script>" + script + "</script></body></html>");
        }
    }
}
