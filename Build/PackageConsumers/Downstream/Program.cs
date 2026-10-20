using System.IO.Compression;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using HtmlForgeX;
using OfficeIMO.ChartForgeX;
using OfficeIMO.Excel;
using OfficeIMO.Pdf;
using OfficeIMO.PowerPoint;
using OfficeIMO.Word;

internal static class Program {
    private static void Main() {
        var selectedOutput = Environment.GetEnvironmentVariable("CFX_CONSUMER_OUTPUT_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(selectedOutput) && !Path.IsPathRooted(selectedOutput))
            throw new InvalidOperationException("CFX_CONSUMER_OUTPUT_DIRECTORY must be absolute.");
        var output = Path.Combine(selectedOutput ?? AppContext.BaseDirectory, "net" + Environment.Version.Major);
        Directory.CreateDirectory(output);
        var names = Enumerable.Range(1, 16).Select(index => "Regional service " + index).ToArray();
        var chart = Chart.Create().WithSize(390, 280).WithTitle("Allocation by service").WithDataLabels()
            .WithXLabels(names).AddPyramid("Allocation", Enumerable.Range(1, 16).Select(index => new ChartPoint(index, 17 - index)));
        var artifact = chart.ToVisualArtifact("allocation");
        artifact.Accessibility.WithTextAlternative("Service allocation", "Sixteen service allocations with complete source facts.");
        var visual = artifact.ToOfficeVisual(new OfficeVisualConversionOptions {
            WidthPoints = 390, SvgPolicy = OfficeVisualSvgPolicy.RasterizeWhenNeeded
        });
        PackageAssertions.Require(visual.WidthPoints == 390 && visual.HeightPoints == 280 && visual.GetPlacementBytes().Length > 100,
            "The public OfficeIMO adapter lost candidate viewport dimensions or placement bytes.");
        PackageAssertions.Require(visual.AlternativeText == "Sixteen service allocations with complete source facts.",
            "The public OfficeIMO adapter lost the authored text alternative.");
        using (var document = WordDocument.Create(Path.Combine(output, "allocation.docx"))) {
            document.AddParagraph().AddVisualArtifact(visual); document.Save();
        }
        using (var workbook = ExcelDocument.Create(Path.Combine(output, "allocation.xlsx"))) {
            workbook.AddWorksheet("Allocation").AddVisualArtifact(2, 2, visual); workbook.Save();
        }
        using (var presentation = PowerPointPresentation.Create(Path.Combine(output, "allocation.pptx"))) {
            presentation.AddSlide().AddVisualArtifact(visual, 36, 54); presentation.Save();
        }
        PdfDocument.Create(_ => { }).AddVisualArtifact(visual).Save(Path.Combine(output, "allocation.pdf"));
        foreach (var extension in new[] { "docx", "xlsx", "pptx" }) {
            using var archive = ZipFile.OpenRead(Path.Combine(output, "allocation." + extension));
            var embedded = archive.Entries.FirstOrDefault(entry => entry.FullName.EndsWith(".svg", StringComparison.Ordinal));
            PackageAssertions.Require(embedded != null, "The public OfficeIMO adapter did not save SVG in " + extension + ".");
            using var reader = new StreamReader(embedded!.Open());
            var svg = System.Xml.Linq.XDocument.Parse(reader.ReadToEnd());
            var stages = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "pyramid-stage").ToArray();
            PackageAssertions.Require(stages.Length == 16 && stages.Select(stage => (string?)stage.Attribute("data-cfx-label")).SequenceEqual(names),
                "The saved " + extension + " does not contain every candidate stage and complete category fact.");
        }
        using (var document = WordDocument.Load(Path.Combine(output, "allocation.docx")))
            PackageAssertions.Require(document.Images.Count == 1, "The saved Word visual did not reopen.");
        var pdf = File.ReadAllBytes(Path.Combine(output, "allocation.pdf"));
        PackageAssertions.Require(pdf.Length > 100 && System.Text.Encoding.ASCII.GetString(pdf, 0, 5) == "%PDF-",
            "The public OfficeIMO adapter did not save a PDF document.");
        using (var document = new Document()) {
            document.Body.Add(new ChartForgeXChart(chart)); document.Save(Path.Combine(output, "allocation.html"));
        }
        var html = File.ReadAllText(Path.Combine(output, "allocation.html"));
        PackageAssertions.Require(html.Contains("pyramid-stage", StringComparison.Ordinal), "The public HTML adapter lost the candidate SVG.");
        PackageBrowserProbe.RunDocumentAsync(html).GetAwaiter().GetResult();
        Console.WriteLine("Public OfficeIMO.ChartForgeX 3.5.1 and HtmlForgeX.ChartForgeX 3.0.0 consumed the staged ChartForgeX package; DOCX/XLSX/PPTX/PDF and HTML saved.");
    }
}
