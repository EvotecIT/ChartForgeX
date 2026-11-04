using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using System.Globalization;
using System.Text;

public static partial class XZoomBenchmarkCases {
    private const string Family = "CFX XZoom Benchmark Carlito";
    private static VisualTheme _theme = null!;
    private static readonly ChartColor Blue = new(35, 100, 220);
    private static readonly ChartColor Purple = new(150, 65, 190);

    private static void InitializeInputs(string tokens, string font, string bold) {
        FontRegistry.Register(Family, font, 400);
        FontRegistry.Register(Family, bold, 700);
        _theme = VisualTheme.FromPaletteJson(File.ReadAllText(tokens), new VisualTypography(Family, 22, 13, 11, 12, 11));
    }

    private sealed class ZoomInput {
        internal string Descriptor = "", Fixture = "";
        internal Chart Chart = null!;
        internal VisualRenderContext Context = null!;
        internal ChartXZoomLevels Levels = null!;
        internal int LevelIndex, Count;
        internal bool Reversed;
        internal double Pan;
        internal long XCalls, YCalls, SecondaryCalls, ValueCalls;
        internal Dictionary<double, int> XPerValue = new();
        internal (long X, long Y, long Secondary, long Value) Counters => (XCalls, YCalls, SecondaryCalls, ValueCalls);
        internal (long X, long Y, long Secondary, long Value) Frozen;
        internal PreparedChartXZoom? Bank;
        internal PreparedVisual? View;
        internal string LastSvgDigest = "", SourceSha = "";
    }

    // Descriptor is an explicit matrix record: fixture|bank|level|width|height|theme|reversed|pan.
    private static ZoomInput CreateInput(string descriptor) {
        var fields = descriptor.Split('|');
        if (fields.Length != 8) throw new ArgumentException("Expected eight explicit X zoom input fields.", nameof(descriptor));
        var input = new ZoomInput { Descriptor = descriptor, Fixture = fields[0],
            LevelIndex = int.Parse(fields[2], CultureInfo.InvariantCulture), Reversed = bool.Parse(fields[6]),
            Pan = double.Parse(fields[7], CultureInfo.InvariantCulture) };
        input.Levels = fields[1] switch { "Standard" => new ChartXZoomLevels(1, 2, 4),
            "High" => new ChartXZoomLevels(1, 2, 4, 8, 16), _ => throw new ArgumentOutOfRangeException(nameof(descriptor)) };
        if (input.LevelIndex < 0 || input.LevelIndex >= input.Levels.Count) throw new ArgumentOutOfRangeException(nameof(descriptor));
        var width = int.Parse(fields[3], CultureInfo.InvariantCulture); var height = int.Parse(fields[4], CultureInfo.InvariantCulture);
        var mode = Enum.Parse<VisualThemeMode>(fields[5]);
        input.Context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), padding: 24), _theme, mode,
            new VisualFrame("Prepared X zoom cost fixture", "Native bank, contained pan and static export", showLegend: false), FontSpec.FromFamily(Family));
        var shape = input.Fixture.Split('-');
        if (shape.Length != 2 || shape[0] is not ("line" or "step")) throw new ArgumentOutOfRangeException(nameof(descriptor));
        input.Count = shape[1] switch { "small" => 8, "regular" => 256, "default" => 256, "dense" => 2048, "labeled" => 128, "references" => 64,
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor)) };
        var labeled = shape[1] is "labeled" or "references";
        var chart = Chart.Create().WithLegend(false).WithXAxis("Sample").WithYAxis("Primary")
            .WithXAxisBounds(0, input.Count - 1).WithYAxisBounds(0, 100);
        if (labeled) chart.WithDataLabels();
        AddSeries(chart, shape[0] == "step", "Primary", Points(input.Count, false), Blue, labeled || shape[1] == "small");
        if (shape[1] == "references") {
            AddSeries(chart, shape[0] == "step", "Secondary", Points(input.Count, true), Purple, true);
            chart.Series[1].UseSecondaryValueAxis();
            chart.WithSecondaryYAxis("Secondary").WithSecondaryYAxisBounds(1, 1000).WithSecondaryYAxisScale(ChartScaleKind.Logarithmic);
            chart.AddHorizontalLine(40, "Fixed Y reference").AddVerticalLine((input.Count - 1) * .37, "Moving X reference")
                .AddHorizontalBand(10, 20, "Fixed Y band").AddVerticalBand((input.Count - 1) * .55, (input.Count - 1) * .7, "Moving X band")
                .AddPlotRegion(new ChartPlotRegion(new ChartColor(200, 160, 45), xMinimum: (input.Count - 1) * .2, xMaximum: (input.Count - 1) * .4,
                    yMinimum: 60, yMaximum: 90, label: "Moving X region"))
                .AddPlotRegion(new ChartPlotRegion(new ChartColor(65, 170, 100), yMinimum: 5, yMaximum: 15, label: "Fixed Y region"));
        }
        chart.Options.XAxis.Reversed = input.Reversed;
        chart.Options.XAxis.LabelFormatter = value => {
            input.XCalls++; input.XPerValue[value] = input.XPerValue.GetValueOrDefault(value) + 1;
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        };
        chart.Options.YAxis.LabelFormatter = value => { input.YCalls++; return value.ToString("0.##", CultureInfo.InvariantCulture); };
        chart.Options.SecondaryYAxis.LabelFormatter = value => { input.SecondaryCalls++; return value.ToString("0.##", CultureInfo.InvariantCulture); };
        if (shape[1] != "default") chart.Options.ValueFormatter = value => { input.ValueCalls++; return value.ToString("0.0", CultureInfo.InvariantCulture); };
        input.Chart = chart; input.SourceSha = SourceDigest(input);
        return input;
    }

    private static ChartPoint[] Points(int count, bool secondary) => Enumerable.Range(0, count).Select(index => {
        var value = secondary ? Math.Pow(10, .5 + 2 * (1 + Math.Sin(index * .13)) / 2) : index % 19 == 0 ? 0 : 50 + 25 * Math.Sin(index * .13);
        return new ChartPoint(index, value, index > 0 && index % Math.Max(4, count / 4) == 0);
    }).ToArray();

    private static void AddSeries(Chart chart, bool step, string name, ChartPoint[] points, ChartColor color, bool markers) {
        if (step) chart.AddStepLine(name, points, color); else chart.AddLine(name, points, color);
        var series = chart.Series[^1];
        if (step) series.WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
        series.StrokeWidth = 1.5;
        series.ConfigureMarkers(options => { options.Enabled = markers; options.Radius = 2.5; options.StrokeWidth = 1; });
    }

    private static string SourceDigest(ZoomInput input) {
        var chart = input.Chart;
        var source = new StringBuilder();
        void Add(object? value) => source.Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append('|');
        void Number(double? value) => source.Append(value?.ToString("R", CultureInfo.InvariantCulture) ?? "null").Append('|');
        Add(chart.Options.XAxis.Reversed); Number(chart.Options.XAxis.Minimum); Number(chart.Options.XAxis.Maximum);
        Number(chart.Options.YAxis.Minimum); Number(chart.Options.YAxis.Maximum); Add(chart.Options.ShowDataLabels);
        Number(chart.Options.SecondaryYAxis.Minimum); Number(chart.Options.SecondaryYAxis.Maximum); Add(chart.Options.SecondaryYAxis.Scale);
        foreach (var series in chart.Series) {
            Add(series.Name); Add(series.Kind); Add(series.Interpolation); Add(series.StepPosition); Add(series.ValueAxis);
            Add(series.Color?.ToHexRgba()); Number(series.StrokeWidth); Add(series.Markers.Enabled); Number(series.Markers.Radius);
            Number(series.Markers.StrokeWidth); Add(series.Markers.Shape); Add(series.Markers.Fill?.ToHexRgba()); Add(series.Markers.Stroke?.ToHexRgba()); Add(series.ShowDataLabels); Add(series.DataLabelPlacement); Add(series.FillPattern);
            foreach (var point in series.Points) { Number(point.X); Number(point.Y); Add(point.BreakBefore); Number(point.ColorValue); }
        }
        foreach (var annotation in chart.Annotations) { Add(annotation.Kind); Number(annotation.Value); Number(annotation.EndValue); Add(annotation.Label); Add(annotation.ShowLabel); Add(annotation.Color.ToHexRgba()); }
        foreach (var region in chart.PlotRegions) { Number(region.XMinimum); Number(region.XMaximum); Number(region.YMinimum); Number(region.YMaximum); Add(region.YAxis); Add(region.Color?.ToHexRgba()); Add(region.PaletteIndex); Number(region.Opacity); Add(region.Label); Add(region.ShowLabel); }
        return Hash(Encoding.UTF8.GetBytes(source.ToString()));
    }
}
