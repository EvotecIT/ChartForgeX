using System.Diagnostics;
using System.Globalization;
using System.Text;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

var iterations = ParseIterations(args);
var warmupIterations = Math.Max(5, iterations / 10);
var parseEditSample = BuildCircleMarkupWithRawStringBuilder();
var pathDataSample = BuildPathDataWithRawStringBuilder();
const string transformSample = "translate(10,-20) rotate(45 5 6) scale(2) skewX(-10) matrix(1 0 0 1 3 4)";
const string styleSample = "fill:#2563EB;stroke:#111;stroke-width:2;filter:url(\"data:image/svg+xml;a=b\");opacity:.8";
const string viewBoxSample = "-10,-20 900 500";
const string pointListSample = "0,0 10,0 10 12 0 12";
var scenarios = new BenchmarkScenario[] {
    new("markup circles raw StringBuilder", BuildCircleMarkupWithRawStringBuilder),
    new("markup circles SvgMarkupWriter", BuildCircleMarkupWithWriter),
    new("markup circles SvgElement AST", BuildCircleMarkupWithAst),
    new("markup circles SvgDocument parse edit save", () => ParseEditSaveCircleMarkup(parseEditSample)),
    new("path data raw StringBuilder", BuildPathDataWithRawStringBuilder),
    new("path data SvgPathDataBuilder", BuildPathDataWithBuilder),
    new("path data SvgPathData parse edit save", () => ParseEditSavePathData(pathDataSample)),
    new("transform SvgTransformList parse edit save", () => ParseEditSaveTransform(transformSample)),
    new("style SvgStyleDeclarationList parse edit save", () => ParseEditSaveStyle(styleSample)),
    new("viewBox SvgViewBox parse edit save", () => ParseEditSaveViewBox(viewBoxSample)),
    new("points SvgPointList parse edit save", () => ParseEditSavePointList(pointListSample)),
    new("chart line current SVG", () => BuildLineChart().ToSvg("bench-line")),
    new("chart grouped bar current SVG", () => BuildGroupedBarChart().ToSvg("bench-bars")),
    new("chart dotted map current SVG", () => BuildDottedMapChart().ToSvg("bench-map"))
};

Console.WriteLine("# ChartForgeX Rendering Engine Benchmarks");
Console.WriteLine();
Console.WriteLine("Iterations: " + iterations.ToString(CultureInfo.InvariantCulture));
Console.WriteLine("Warmup: " + warmupIterations.ToString(CultureInfo.InvariantCulture));
Console.WriteLine();
Console.WriteLine("| Scenario | Mean ms/op | Alloc KB/op | Output chars |");
Console.WriteLine("| --- | ---: | ---: | ---: |");

foreach (var scenario in scenarios) {
    var result = RunScenario(scenario, warmupIterations, iterations);
    Console.WriteLine(
        "| " + scenario.Name + " | " +
        result.MeanMilliseconds.ToString("0.000", CultureInfo.InvariantCulture) + " | " +
        result.AllocatedKilobytes.ToString("0.0", CultureInfo.InvariantCulture) + " | " +
        result.OutputCharacters.ToString(CultureInfo.InvariantCulture) + " |");
}

static int ParseIterations(string[] args) {
    if (args.Length == 0) return 200;
    if (int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0) {
        return value;
    }

    throw new ArgumentException("Pass a positive integer iteration count, for example: dotnet run --project ChartForgeX.Benchmarks -c Release -- 500");
}

static BenchmarkResult RunScenario(BenchmarkScenario scenario, int warmupIterations, int iterations) {
    string? output = null;
    for (var i = 0; i < warmupIterations; i++) output = scenario.Run();

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var stopwatch = Stopwatch.StartNew();
    for (var i = 0; i < iterations; i++) output = scenario.Run();
    stopwatch.Stop();
    var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

    return new BenchmarkResult(
        stopwatch.Elapsed.TotalMilliseconds / iterations,
        allocated / 1024.0 / iterations,
        output?.Length ?? 0);
}

static string BuildCircleMarkupWithRawStringBuilder() {
    var sb = new StringBuilder(64 * 1024);
    sb.Append("<svg viewBox=\"0 0 900 500\" role=\"img\">");
    sb.Append("<g data-cfx-role=\"synthetic-markers\" data-label=\"");
    sb.Append(EscapeAttribute("A < B & \"C\""));
    sb.Append("\">");
    for (var i = 0; i < 720; i++) {
        var x = 18 + i % 60 * 14.5;
        var y = 18 + i / 60 * 32.5;
        sb.Append("<circle data-cfx-role=\"point\" data-cfx-point=\"");
        sb.Append(i.ToString(CultureInfo.InvariantCulture));
        sb.Append("\" cx=\"");
        sb.Append(Format(x));
        sb.Append("\" cy=\"");
        sb.Append(Format(y));
        sb.Append("\" r=\"3.5\" fill=\"#2563EB\"><title>");
        sb.Append(EscapeText("Marker " + i.ToString(CultureInfo.InvariantCulture)));
        sb.Append("</title></circle>");
    }

    sb.Append("</g></svg>");
    return sb.ToString();
}

static string BuildCircleMarkupWithWriter() {
    var writer = new SvgMarkupWriter(64 * 1024);
    writer.StartElement("svg")
        .Attribute("viewBox", "0 0 900 500")
        .Attribute("role", "img")
        .EndStartElement();
    writer.StartElement("g")
        .Attribute("data-cfx-role", "synthetic-markers")
        .Attribute("data-label", "A < B & \"C\"")
        .EndStartElement();
    for (var i = 0; i < 720; i++) {
        var x = 18 + i % 60 * 14.5;
        var y = 18 + i / 60 * 32.5;
        writer.StartElement("circle")
            .Attribute("data-cfx-role", "point")
            .Attribute("data-cfx-point", i)
            .Attribute("cx", x)
            .Attribute("cy", y)
            .Attribute("r", 3.5)
            .Attribute("fill", "#2563EB")
            .EndStartElement();
        writer.StartElement("title")
            .EndStartElement()
            .Text("Marker " + i.ToString(CultureInfo.InvariantCulture))
            .EndElement();
        writer.EndElement();
    }

    writer.EndElement();
    writer.EndElement();
    return writer.Build();
}

static string BuildCircleMarkupWithAst() {
    var document = SvgDocument.Create(900, 500, "0 0 900 500");
    document.Root
        .RemoveAttribute("xmlns");
    document.Root
        .RemoveAttribute("width");
    document.Root
        .RemoveAttribute("height");
    document.Root.Attribute("role", "img");
    var group = document.Root.Element("g", element => element
        .Attribute("data-cfx-role", "synthetic-markers")
        .Attribute("data-label", "A < B & \"C\""));
    for (var i = 0; i < 720; i++) {
        var x = 18 + i % 60 * 14.5;
        var y = 18 + i / 60 * 32.5;
        group.Element("circle", circle => {
            circle.Attribute("data-cfx-role", "point")
                .Attribute("data-cfx-point", i)
                .Attribute("cx", x)
                .Attribute("cy", y)
                .Attribute("r", 3.5)
                .Attribute("fill", "#2563EB");
            circle.Element("title", title => title.Text("Marker " + i.ToString(CultureInfo.InvariantCulture)));
        });
    }

    return document.ToMarkup();
}

static string ParseEditSaveCircleMarkup(string markup) {
    var document = SvgDocument.Parse(markup);
    document.Root.FindByTag("g").First().Attribute("data-edited", true);
    return document.ToMarkup();
}

static string BuildPathDataWithRawStringBuilder() {
    var sb = new StringBuilder(32 * 1024);
    for (var i = 0; i < 360; i++) {
        if (sb.Length > 0) sb.Append(' ');
        var x = i * 2.25;
        var y = 150 + Math.Sin(i / 12.0) * 80;
        if (i == 0) sb.Append("M ");
        else if (i % 5 == 0) sb.Append("Q ").Append(Format(x - 2)).Append(' ').Append(Format(y - 16)).Append(' ');
        else sb.Append("L ");
        sb.Append(Format(x)).Append(' ').Append(Format(y));
    }

    return sb.ToString();
}

static string BuildPathDataWithBuilder() {
    var path = new SvgPathDataBuilder(32 * 1024);
    for (var i = 0; i < 360; i++) {
        var x = i * 2.25;
        var y = 150 + Math.Sin(i / 12.0) * 80;
        if (i == 0) path.MoveTo(x, y);
        else if (i % 5 == 0) path.QuadraticTo(x - 2, y - 16, x, y);
        else path.LineTo(x, y);
    }

    return path.Build();
}

static string ParseEditSavePathData(string pathData) {
    var path = SvgPathData.Parse(pathData);
    if (path.Commands.Count > 4) {
        var command = path.Commands[4];
        path.Replace(4, command.WithValues(command.Values.ToArray()));
    }

    return path.ToMarkup();
}

static string ParseEditSaveTransform(string transformValue) {
    var transform = SvgTransformList.Parse(transformValue);
    if (transform.Transforms.Count > 2) {
        transform.Replace(2, new SvgTransform("scale", new[] { 1.5, 0.75 }));
    }

    return transform.ToMarkup();
}

static string ParseEditSaveStyle(string styleValue) {
    var style = SvgStyleDeclarationList.Parse(styleValue);
    style.Set("fill", "#16A34A").Remove("stroke");
    return style.ToMarkup();
}

static string ParseEditSaveViewBox(string viewBoxValue) {
    var viewBox = SvgViewBox.Parse(viewBoxValue);
    return viewBox.Expand(4).ToMarkup();
}

static string ParseEditSavePointList(string pointListValue) {
    var points = SvgPointList.Parse(pointListValue);
    points.Replace(2, new SvgPoint(12, 14));
    return points.ToMarkup();
}

static Chart BuildLineChart() {
    var values = Enumerable.Range(0, 80)
        .Select(i => new ChartPoint(i, 120 + Math.Sin(i / 6.0) * 44 + i * 1.2))
        .ToArray();
    return Chart.Create()
        .WithSize(960, 520)
        .WithTitle("Rendering baseline line chart")
        .WithSubtitle("Dense axis labels, smooth line, and data labels off")
        .WithXAxis("Sample")
        .WithYAxis("Value")
        .AddSmoothArea("Latency", values, ChartColor.FromRgb(37, 99, 235));
}

static Chart BuildGroupedBarChart() {
    var pointsA = Enumerable.Range(0, 24).Select(i => new ChartPoint(i, 30 + i % 7 * 8)).ToArray();
    var pointsB = Enumerable.Range(0, 24).Select(i => new ChartPoint(i, 22 + i % 5 * 9)).ToArray();
    var labels = Enumerable.Range(1, 24).Select(i => "M" + i.ToString("00", CultureInfo.InvariantCulture)).ToArray();
    return Chart.Create()
        .WithSize(960, 520)
        .WithTitle("Rendering baseline grouped bars")
        .WithXLabels(labels)
        .AddBar("Current", pointsA, ChartColor.FromRgb(37, 99, 235))
        .AddBar("Previous", pointsB, ChartColor.FromRgb(16, 185, 129));
}

static Chart BuildDottedMapChart() {
    return Chart.Create()
        .WithSize(960, 560)
        .WithTitle("Rendering baseline dotted map")
        .WithMapViewport(ChartMapViewport.Europe())
        .WithDataLabels()
        .AddDottedMap("Revenue", new[] {
            new ChartMapPoint("United Kingdom", -1.1743, 52.3555, 188, ChartColor.FromRgb(37, 99, 235)),
            new ChartMapPoint("Poland", 19.1451, 51.9194, 142, ChartColor.FromRgb(220, 38, 38)),
            new ChartMapPoint("Spain", -3.7038, 40.4168, 96, ChartColor.FromRgb(245, 158, 11)),
            new ChartMapPoint("Germany", 10.4515, 51.1657, 214, ChartColor.FromRgb(34, 197, 94)),
            new ChartMapPoint("Norway", 8.4689, 60.4720, 74, ChartColor.FromRgb(14, 165, 233))
        })
        .AddMapRouteBetweenPoints("United Kingdom to Poland", "United Kingdom", "Poland")
        .AddMapRouteBetweenPoints("Spain to Germany", "Spain", "Germany");
}

static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

static string EscapeText(string value) =>
    value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

static string EscapeAttribute(string value) =>
    EscapeText(value).Replace("\"", "&quot;");

internal readonly record struct BenchmarkScenario(string Name, Func<string> Run);

internal readonly record struct BenchmarkResult(double MeanMilliseconds, double AllocatedKilobytes, int OutputCharacters);
