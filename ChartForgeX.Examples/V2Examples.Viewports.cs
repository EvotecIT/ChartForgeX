using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

public static partial class V2Examples {
    private static void WriteViewports(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var kind in new[] { ChartSeriesKind.Line, ChartSeriesKind.StepLine })
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
                foreach (var compact in new[] { false, true }) {
                    var steps = kind == ChartSeriesKind.StepLine;
                    var family = FamilyName(kind);
                    var variant = compact ? "compact-x-viewport" : "x-viewport";
                    var id = "family-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                    var width = compact ? 360 : 800; var height = compact ? 360 : 440;
                    var title = steps ? "Reversed steps, X 2–8" : "Requests, X 2–8";
                    var subtitle = compact ? "Full-data Y; gap at 6; labels in X 2–8" : "Utilization labels within X 2–8; full-data Y scales; gap at sample 6";
                    var chart = CreateViewportChart(steps, compact);
                    var viewport = new ChartXViewport(2, 8);
                    var model = new ViewportExample(chart, viewport);
                    var prepared = model.Prepare(GalleryContext(width, height, mode, title, subtitle, !compact));
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
                    File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
                    WriteThumbnail(output, model, id, title, family, mode, !compact);
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".csharp.txt"),
                        ViewportSnippet(chart, id, title, subtitle, mode, width, height, !compact));
                    artifacts.Add(new ProofArtifact(id, family, title, variant, mode.ToString().ToLowerInvariant(), width, height,
                        prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count, new[] { kind.ToString() },
                        prepared.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray(), Compact: compact));
                }
    }

    private static Chart CreateViewportChart(bool steps, bool compact) {
        // Just-outside observations retain their source facts without painting labels into the window.
        ChartPoint[] Points(params double[] values) => values.Select((value, index) =>
            new ChartPoint(index == 1 ? 1.99 : index == 9 ? 8.01 : index, value, index == 6)).ToArray();
        var chart = Chart.Create().WithXAxis("Sample").WithYAxis("Requests").WithXAxisBounds(0, 10).WithDataLabels();
        var observed = Points(88, 44, 34, 56, 46, 64, 42, 58, 50, 38, 82);
        if (steps) chart.AddStepLine("Observed", observed);
        else chart.AddLine("Observed", observed);
        if (!compact) {
            chart.Series[0].WithDataLabels(false);
            var utilization = Points(96, 72, 48, 61, 55, 76, 49, 67, 59, 84, 100);
            if (steps) chart.AddStepLine("Utilization", utilization);
            else chart.AddLine("Utilization", utilization);
            chart.Series[1].UseSecondaryYAxis();
            chart.WithSecondaryYAxis("Utilization (%)");
            chart.Options.SecondaryYAxis.TickCount = 4;
        }
        chart.Options.XAxis.WithReversal(steps);
        chart.Options.XAxis.TickCount = 4; chart.Options.YAxis.TickCount = 4;
        foreach (var series in chart.Series) {
            series.WithMarkerRadius(3);
            if (steps) series.WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
        }
        return chart;
    }

    // Example glue keeps the same immutable viewport when the ordinary thumbnail writer prepares a new display size.
    // Downloaded programs call Chart.Prepare(context, viewport) directly and do not depend on this private wrapper.
    private sealed class ViewportExample : IVisualRenderable {
        private readonly Chart _chart;
        private readonly ChartXViewport _viewport;

        public ViewportExample(Chart chart, ChartXViewport viewport) { _chart = chart; _viewport = viewport; }
        public PreparedVisual Prepare(VisualRenderContext context) => _chart.Prepare(context, _viewport);
    }

    private static string ViewportSnippet(Chart chart, string id, string title, string subtitle, VisualThemeMode mode, int width, int height, bool legend) {
        var source = new StringBuilder("using ChartForgeX.Core;\nusing ChartForgeX.Primitives;\nusing ChartForgeX.Rendering;\nusing ChartForgeX.Themes;\nusing ChartForgeX.Typography;\nusing ChartForgeX.VisualArtifacts;\n\n");
        source.Append("// The gallery includes these licensed font fixtures beside the outputs.\nFontRegistry.Register(")
            .Append(Literal(ProofFont)).Append(", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(")
            .Append(Literal(ProofFont)).Append(", \"fonts/Carlito-Bold.ttf\", 700);\n")
            .Append("var chart = Chart.Create().WithXAxis(\"Sample\").WithYAxis(\"Requests\").WithXAxisBounds(0, 10).WithDataLabels();\n");
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            var points = string.Join(", ", series.Points.Select(point => "new ChartPoint(" + point.X.ToString(CultureInfo.InvariantCulture) + ", "
                + point.Y.ToString(CultureInfo.InvariantCulture) + (point.BreakBefore ? ", true" : "") + ")"));
            source.Append("chart.").Append(series.Kind == ChartSeriesKind.StepLine ? "AddStepLine" : "AddLine").Append('(')
                .Append(Literal(series.Name)).Append(", new ChartPoint[] { ").Append(points).Append(" });\n")
                .Append("chart.Series[").Append(index).Append("].WithMarkerRadius(3);\n");
            if (series.Kind == ChartSeriesKind.StepLine)
                source.Append("chart.Series[").Append(index).Append("].WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);\n");
            if (series.ShowDataLabels == false)
                source.Append("chart.Series[").Append(index).Append("].WithDataLabels(false);\n");
            if (series.YAxis == ChartAxisSide.Secondary)
                source.Append("chart.Series[").Append(index).Append("].UseSecondaryYAxis();\n");
        }
        if (chart.Series.Count > 1)
            source.Append("chart.WithSecondaryYAxis(\"Utilization (%)\");\nchart.Options.SecondaryYAxis.TickCount = 4;\n");
        if (chart.Options.XAxis.Reversed) source.Append("chart.Options.XAxis.WithReversal();\n");
        source.Append("chart.Options.XAxis.TickCount = 4;\nchart.Options.YAxis.TickCount = 4;\n")
            .Append("// Only observations inside X 2–8 request labels; boundary observations remain eligible.\n")
            .Append("// Authored observations at X 1.99 and 8.01 retain their source facts; all points determine the Y scales.\nvar viewport = new ChartXViewport(2, 8);\n")
            .Append("var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(").Append(width).Append(", ").Append(height)
            .Append(")), VisualTheme.Graphite(), VisualThemeMode.").Append(mode).Append(",\n    new VisualFrame(").Append(Literal(title)).Append(", ")
            .Append(Literal(subtitle)).Append(", showLegend: ").Append(legend ? "true" : "false").Append("), FontSpec.FromFamily(").Append(Literal(ProofFont)).Append("));\n")
            .Append("var prepared = chart.Prepare(context, viewport);\nSystem.IO.File.WriteAllText(\"visual.svg\", prepared.ToSvg(").Append(Literal(id)).Append("));\n")
            .Append("System.IO.File.WriteAllBytes(\"visual.png\", prepared.ToPng());\nSystem.IO.File.WriteAllText(\"visual.html\", prepared.ToArtifact(")
            .Append(Literal(id)).Append(", VisualArtifactKind.Chart).ToHtmlPage());\n\n")
            .Append("// Prepare the same window at thumbnail size, rather than resizing the full scene.\n")
            .Append("var thumbnailTheme = VisualTheme.Graphite().WithTypography(new VisualTypography(axisSize: 13.5, legendSize: 13.5, dataLabelSize: 13.5));\n")
            .Append("var thumbnailContext = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 280), 16), thumbnailTheme, VisualThemeMode.")
            .Append(mode).Append(", new VisualFrame(\"\", \"\", showLegend: ").Append(legend ? "true" : "false").Append("), FontSpec.FromFamily(").Append(Literal(ProofFont)).Append("));\n")
            .Append("var thumbnail = chart.Prepare(thumbnailContext, viewport);\nvar artifact = thumbnail.ToArtifact(").Append(Literal(id + "-thumbnail"))
            .Append(", VisualArtifactKind.Chart);\nartifact.Accessibility.Name = ").Append(Literal(title)).Append(";\n")
            .Append("System.IO.File.WriteAllText(\"visual.thumbnail.svg\", artifact.ToSvg());\nSystem.IO.File.WriteAllBytes(\"visual.thumbnail.png\", thumbnail.ToPng());\n");
        return source.ToString();
    }
}
