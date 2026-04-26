using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SourceFilesStayUnderArchitectureLineBudget() {
        const int lineBudget = 800;
        var root = FindRepositoryRoot();
        var oversized = new[] { "ChartForgeX", "ChartForgeX.Examples", "ChartForgeX.Tests" }
            .Where(sourceRoot => Directory.Exists(Path.Combine(root, sourceRoot)))
            .SelectMany(sourceRoot => Directory.EnumerateFiles(Path.Combine(root, sourceRoot), "*.cs", SearchOption.AllDirectories))
            .Where(file => !IsGeneratedPath(file))
            .Select(file => new { File = file, Lines = File.ReadLines(file).Count() })
            .Where(item => item.Lines > lineBudget)
            .Select(item => Path.GetRelativePath(root, item.File) + " (" + item.Lines.ToString(System.Globalization.CultureInfo.InvariantCulture) + " lines)")
            .ToArray();
        Assert(oversized.Length == 0, "Source files should stay under " + lineBudget.ToString(System.Globalization.CultureInfo.InvariantCulture) + " lines. Split: " + string.Join(", ", oversized));
    }

    private static void ProjectFilesKeepStrictBuildSettings() {
        var root = FindRepositoryRoot();
        var projectFiles = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories).Where(file => !IsGeneratedPath(file)).ToArray();
        var projectSettingFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories).Where(IsProjectSettingFile).Where(file => !IsGeneratedPath(file)).ToArray();

        foreach (var file in projectSettingFiles) {
            Assert(!File.ReadAllText(file).Contains("<NoWarn", StringComparison.OrdinalIgnoreCase), "Project files should not suppress warnings with NoWarn: " + Path.GetRelativePath(root, file));
        }

        foreach (var projectFile in projectFiles) {
            Assert(HasXmlProperty(projectFile, "TreatWarningsAsErrors", "true"), "Project should treat warnings as errors: " + Path.GetRelativePath(root, projectFile));
        }

        var libraryProject = Path.Combine(root, "ChartForgeX", "ChartForgeX.csproj");
        Assert(HasXmlProperty(libraryProject, "GenerateDocumentationFile", "true"), "Library project should generate XML documentation.");

        foreach (var packageReference in GetXmlElements(libraryProject, "PackageReference")) {
            var include = packageReference.Attribute("Include")?.Value ?? string.Empty;
            var privateAssets = packageReference.Attribute("PrivateAssets")?.Value ?? string.Empty;
            var allowedBuildPackage = string.Equals(include, "Microsoft.NETFramework.ReferenceAssemblies.net472", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(privateAssets, "all", StringComparison.OrdinalIgnoreCase);
            Assert(allowedBuildPackage, "Runtime package dependencies are not allowed in the core library: " + include);
        }
    }

    private static void NuGetPackageMetadataStaysPublishReady() {
        var libraryProject = Path.Combine(FindRepositoryRoot(), "ChartForgeX", "ChartForgeX.csproj");
        Assert(HasXmlProperty(libraryProject, "PackageId", "ChartForgeX"), "PackageId should remain stable.");
        Assert(HasXmlProperty(libraryProject, "PackageReadmeFile", "README.md"), "Package should include the README.");
        Assert(HasXmlProperty(libraryProject, "PackageProjectUrl", "https://github.com/EvotecIT/ChartForgeX"), "Package should expose the project URL.");
        Assert(HasXmlProperty(libraryProject, "RepositoryUrl", "https://github.com/EvotecIT/ChartForgeX"), "Package should expose the repository URL.");
        Assert(HasXmlProperty(libraryProject, "RepositoryType", "git"), "Package repository type should be git.");
        var tags = GetXmlValue(libraryProject, "PackageTags");
        foreach (var tag in new[] { "charts", "svg", "reports", "zero-dependency" }) {
            Assert(tags.Contains(tag, StringComparison.OrdinalIgnoreCase), "Package tags should include " + tag + ".");
        }
    }

    private static void HtmlPageIsStatic() {
        var html = SampleChart().ToHtmlPage();
        Assert(html.Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase), "HTML page should include a document type.");
        Assert(html.Contains("<svg", StringComparison.Ordinal), "HTML page should include inline SVG.");
        Assert(!html.Contains("<script", StringComparison.OrdinalIgnoreCase), "Static HTML renderer should not emit JavaScript.");
    }

    private static void RenderedMarkupStaysSelfContained() {
        var chart = SampleChart();
        AssertSelfContainedMarkup(chart.ToSvg(), "SVG output");
        AssertSelfContainedMarkup(chart.ToHtmlPage(), "HTML page output");
        AssertSelfContainedMarkup(chart.ToHtmlFragment(), "HTML fragment output");
        AssertSelfContainedMarkup(Chart.Create().WithTitle("Radar").WithXLabels("A", "B", "C").AddRadar("Values", Points(80, 60, 90)).ToSvg(), "radar SVG output");
        AssertSelfContainedMarkup(Chart.Create().WithTitle("Funnel").WithXLabels("A", "B", "C").AddFunnel("Values", Points(90, 60, 30)).ToHtmlPage(), "funnel HTML output");
    }

    private static void PngIsValid() {
        var png = SampleChart().ToPng();
        Assert(png.Length > 64, "PNG output should not be empty.");
        Assert(png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "PNG signature should be valid.");
        Assert(ReadBigEndianInt32(png, 16) == 640, "PNG width should match chart width.");
        Assert(ReadBigEndianInt32(png, 20) == 360, "PNG height should match chart height.");
    }

    private static void ExampleGalleryIsStaticAndLinksGeneratedArtifacts() {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-gallery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            File.WriteAllText(Path.Combine(output, "alpha.html"), "<!doctype html><title>Alpha &amp; Beta</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "alpha.svg"), "<svg></svg>");
            File.WriteAllText(Path.Combine(output, "alpha.png"), string.Empty);
            File.WriteAllText(Path.Combine(output, "zeta.html"), "<!doctype html><title>Zeta</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "zeta.svg"), "<svg></svg>");
            File.WriteAllText(Path.Combine(output, "zeta.png"), string.Empty);

            GalleryWriter.Write(output);
            var gallery = File.ReadAllText(Path.Combine(output, "index.html"));
            Assert(gallery.Contains("<title>ChartForgeX Examples</title>", StringComparison.Ordinal), "Gallery should render a stable title.");
            Assert(CountOccurrences(gallery, "<article class=\"card\">") == 2, "Gallery should render one card per generated chart page.");
            Assert(CountOccurrences(gallery, "<iframe ") == 2, "Gallery should render chart previews.");
            Assert(!gallery.Contains("<script", StringComparison.OrdinalIgnoreCase), "Gallery should remain JavaScript-free.");
            AssertSelfContainedMarkup(gallery, "example gallery");
            Assert(gallery.Contains("alpha.html", StringComparison.Ordinal), "Gallery should link chart HTML output.");
            Assert(gallery.Contains("alpha.svg", StringComparison.Ordinal), "Gallery should link chart SVG output.");
            Assert(gallery.Contains("alpha.png", StringComparison.Ordinal), "Gallery should link chart PNG output.");
        } finally {
            Directory.Delete(output, true);
        }
    }
}
