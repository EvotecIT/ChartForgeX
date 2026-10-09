using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PublicApiKeepsOneTypePerReusableConcept() {
        var assembly = typeof(Chart).Assembly;
        var publicAssemblies = new[] {
            assembly,
            typeof(ChartForgeX.Composition.VisualCanvas).Assembly,
            typeof(ChartForgeX.Stories.VisualStory).Assembly,
            typeof(ChartForgeX.Interactivity.ChartInteractionFeatures).Assembly,
            typeof(ChartForgeX.Interactivity.Html.HtmlInteractiveChartRenderer).Assembly,
            typeof(ChartForgeX.Markup.VisualMarkupKind).Assembly,
            typeof(ChartForgeX.Markup.Mermaid.MermaidVisualMarkupParser).Assembly,
            typeof(ChartForgeX.Mermaid.MermaidDiagramKind).Assembly
        };
        var exportedTypes = publicAssemblies.SelectMany(candidate => candidate.GetExportedTypes()).ToArray();
        var exportedNames = exportedTypes.Select(type => type.FullName ?? type.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var required in new[] {
            "ChartForgeX.Accessibility.VisualAccessibility",
            "ChartForgeX.Core.ChartAxis",
            "ChartForgeX.Data.ChartDataset`1",
            "ChartForgeX.Primitives.VisualLinkDirection",
            "ChartForgeX.Primitives.VisualPanelFit",
            "ChartForgeX.Themes.VisualDesignTokens",
            "ChartForgeX.Typography.FontSpec",
            "ChartForgeX.Typography.TextAlignment",
            "ChartForgeX.Typography.TextStyle",
            "ChartForgeX.Typography.TextStyleOverride",
            "ChartForgeX.Diagnostics.VisualDiagnosticSeverity",
            "ChartForgeX.VisualArtifacts.SequenceArtifactParticipantKind"
        }) {
            Assert(exportedNames.Contains(required), "The 1.0 public API should expose " + required + ".");
        }

        foreach (var removed in new[] {
            "ChartForgeX.Core.ChartGridPanelFit",
            "ChartForgeX.Core.ChartTextStyle",
            "ChartForgeX.Topology.TopologyDirection",
            "ChartForgeX.VisualArtifacts.FlowArtifactConnectorDirection",
            "ChartForgeX.VisualArtifacts.VisualArtifactRect",
            "ChartForgeX.VisualBlocks.VisualGridPanelFit",
            "ChartForgeX.VisualBlocks.VisualTextAlignment",
            "ChartForgeX.VisualBlocks.WardleyMapFlow",
            "ChartForgeX.VisualCanvas.VisualCanvasTextAlignment",
            "ChartForgeX.Markup.MarkupDiagnosticSeverity",
            "ChartForgeX.Mermaid.MermaidDiagnosticSeverity",
            "ChartForgeX.Mermaid.MermaidSequenceParticipantKind"
        }) {
            Assert(!exportedNames.Contains(removed), "Removed pre-1.0 duplicate type should not return: " + removed + ".");
        }

        Assert(!exportedTypes.Any(type => type.Namespace != null && type.Namespace.StartsWith("ChartForgeX.Simple", StringComparison.Ordinal)), "ChartForgeX.Simple should not return as a second chart-construction surface.");

        var duplicateEnums = exportedTypes
            .Where(type => type.IsEnum)
            .GroupBy(type => string.Join("|", Enum.GetNames(type)), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => string.Join(", ", group.Select(type => type.FullName).OrderBy(name => name, StringComparer.Ordinal)))
            .ToArray();
        Assert(duplicateEnums.Length == 0, "Public enums with identical member sets require one shared owner: " + string.Join("; ", duplicateEnums));

        var drawingReferences = assembly.GetReferencedAssemblies()
            .Where(reference => reference.Name != null && reference.Name.StartsWith("System.Drawing", StringComparison.OrdinalIgnoreCase))
            .Select(reference => reference.FullName)
            .ToArray();
        Assert(drawingReferences.Length == 0, "The dependency-free core must not reference System.Drawing: " + string.Join(", ", drawingReferences));
    }

    private static void SourceFilesStayUnderArchitectureLineBudget() {
        const int lineBudget = 800;
        var root = FindRepositoryRoot();
        var oversized = new[] {
                "ChartForgeX",
                "ChartForgeX.Visuals",
                "ChartForgeX.Stories",
                "ChartForgeX.AotSmoke",
                "ChartForgeX.Examples",
                "ChartForgeX.Interactivity",
                "ChartForgeX.Interactivity.Html",
                "ChartForgeX.Markup",
                "ChartForgeX.Markup.Cli",
                "ChartForgeX.Markup.Mermaid",
                "ChartForgeX.Mermaid",
                "ChartForgeX.Tests",
                "ChartForgeX.Tools.IconImport"
            }
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
        var testProject = Path.Combine(root, "ChartForgeX.Tests", "ChartForgeX.Tests.csproj");
        Assert(HasXmlProperty(testProject, "IsTestProject", "true"), "Smoke suite should be discoverable by dotnet test.");
        var aotSmokeProject = Path.Combine(root, "ChartForgeX.AotSmoke", "ChartForgeX.AotSmoke.csproj");
        Assert(File.Exists(aotSmokeProject), "Repository should include a Native AOT consumer smoke project.");
        Assert(HasXmlProperty(aotSmokeProject, "PublishAot", "true"), "Native AOT smoke project should publish with Native AOT enabled.");
        Assert(HasXmlProperty(aotSmokeProject, "TrimMode", "full"), "Native AOT smoke project should use full trimming.");

        foreach (var packageReference in GetXmlElements(libraryProject, "PackageReference")) {
            var include = packageReference.Attribute("Include")?.Value ?? string.Empty;
            var privateAssets = packageReference.Attribute("PrivateAssets")?.Value ?? string.Empty;
            var allowedBuildPackage = string.Equals(include, "Microsoft.NETFramework.ReferenceAssemblies.net472", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(privateAssets, "all", StringComparison.OrdinalIgnoreCase);
            Assert(allowedBuildPackage, "Runtime package dependencies are not allowed in the core library: " + include);
        }

        foreach (var projectFile in projectFiles.Where(file => !string.Equals(file, libraryProject, StringComparison.OrdinalIgnoreCase))) {
            foreach (var packageReference in GetXmlElements(projectFile, "PackageReference")) {
                var privateAssets = packageReference.Attribute("PrivateAssets")?.Value ?? string.Empty;
                Assert(string.Equals(privateAssets, "all", StringComparison.OrdinalIgnoreCase), "Non-library package references should stay private: " + Path.GetRelativePath(root, projectFile));
            }
        }
    }

    private static void VsCodeMarkupExtensionKeepsCliBackedContract() {
        var root = FindRepositoryRoot();
        var extensionRoot = Path.Combine(root, "ChartForgeX.Markup.VSCode");
        var packagePath = Path.Combine(extensionRoot, "package.json");
        Assert(File.Exists(packagePath), "VS Code markup extension should include package.json.");
        Assert(File.Exists(Path.Combine(extensionRoot, "package-lock.json")), "VS Code markup extension should commit package-lock.json for deterministic packaging.");
        Assert(File.Exists(Path.Combine(extensionRoot, "src", "extension.ts")), "VS Code markup extension should include extension source.");
        Assert(File.Exists(Path.Combine(extensionRoot, "scripts", "package-vsix.cjs")), "VS Code markup extension should include a VSIX packaging script.");
        Assert(File.Exists(Path.Combine(extensionRoot, "scripts", "package-vsix.ps1")), "VS Code markup extension should include a PowerShell VSIX packaging script.");
        Assert(File.Exists(Path.Combine(extensionRoot, "scripts", "dev-install.ps1")), "VS Code markup extension should include a linked dev install script.");
        Assert(File.Exists(Path.Combine(extensionRoot, "scripts", "install-insiders.ps1")), "VS Code markup extension should include a packaged Insiders install script.");
        var workflowPath = Path.Combine(root, ".github", "workflows", "vscode-extension.yml");
        Assert(File.Exists(workflowPath), "Repository should include VS Code extension packaging workflow.");

        using var packageDocument = JsonDocument.Parse(File.ReadAllText(packagePath));
        var packageRoot = packageDocument.RootElement;
        Assert(JsonString(packageRoot, "main") == "./out/extension.js", "VS Code markup extension should bundle to out/extension.js.");
        Assert(JsonString(packageRoot.GetProperty("scripts"), "package") == "node scripts/package-vsix.cjs", "VS Code markup extension package script should run the CLI-backed VSIX packager.");

        var contributes = packageRoot.GetProperty("contributes");
        var commands = JsonStringSet(contributes.GetProperty("commands"), "command");
        foreach (var command in new[] {
            "chartforgexMarkup.preview",
            "chartforgexMarkup.validate",
            "chartforgexMarkup.exportSvg",
            "chartforgexMarkup.exportPng",
            "chartforgexMarkup.exportHtml",
            "chartforgexMarkup.emitCSharp",
            "chartforgexMarkup.emitCSharpToFile",
            "chartforgexMarkup.openOutputFolder"
        }) {
            Assert(commands.Contains(command), "VS Code markup extension should expose command: " + command);
        }

        var settings = contributes.GetProperty("configuration").GetProperty("properties");
        foreach (var setting in new[] {
            "chartforgexMarkup.cliPath",
            "chartforgexMarkup.validateDebounceMs",
            "chartforgexMarkup.previewAutoRefresh",
            "chartforgexMarkup.previewDebounceMs",
            "chartforgexMarkup.outputDirectoryMode",
            "chartforgexMarkup.outputSubfolderName",
            "chartforgexMarkup.defaultExportFormat"
        }) {
            Assert(settings.TryGetProperty(setting, out _), "VS Code markup extension should expose setting: " + setting);
        }

        var files = JsonStringSet(packageRoot.GetProperty("files"));
        Assert(files.Contains("tools/**"), "VS Code markup extension VSIX should include bundled CLI tools.");
        Assert(files.Contains("syntaxes/**") && files.Contains("snippets/**"), "VS Code markup extension VSIX should include language assets.");
        Assert(!files.Contains("node_modules/**") && !files.Contains("dist/**"), "VS Code markup extension VSIX should not list generated development folders.");

        var packageScript = File.ReadAllText(Path.Combine(extensionRoot, "scripts", "package-vsix.cjs"));
        Assert(packageScript.Contains("ChartForgeX.Markup.Cli", StringComparison.Ordinal), "VS Code markup extension packager should publish the ChartForgeX.Markup.Cli.");
        Assert(packageScript.Contains("'win-x64'", StringComparison.Ordinal) && packageScript.Contains("'linux-x64'", StringComparison.Ordinal) && packageScript.Contains("'osx-arm64'", StringComparison.Ordinal), "VS Code markup extension packager should include common desktop runtime identifiers.");
        Assert(packageScript.Contains("VSCE_PAT", StringComparison.Ordinal), "VS Code markup extension packager should require VSCE_PAT for Marketplace publishing.");

        var extensionSource = File.ReadAllText(Path.Combine(extensionRoot, "src", "extension.ts"));
        Assert(extensionSource.Contains("spawnError", StringComparison.Ordinal) && extensionSource.Contains("fallbacks", StringComparison.Ordinal), "VS Code markup extension should fall back from unusable RID executables to portable CLI assets.");
        Assert(extensionSource.Contains("document.getText()", StringComparison.Ordinal) && extensionSource.Contains("mkdtempSync", StringComparison.Ordinal), "VS Code markup extension should run CLI commands against current buffer text without force-saving user files.");
        Assert(extensionSource.Contains("mermaid", StringComparison.Ordinal), "VS Code markup extension should let the CLI preview and validate Markdown documents that contain supported Mermaid fences.");

        var powerShellPackageScript = File.ReadAllText(Path.Combine(extensionRoot, "scripts", "package-vsix.ps1"));
        Assert(powerShellPackageScript.Contains("ChartForgeX.Markup.Cli", StringComparison.Ordinal), "VS Code markup extension PowerShell packager should publish the ChartForgeX.Markup.Cli.");
        Assert(powerShellPackageScript.Contains("'win-x64'", StringComparison.Ordinal) && powerShellPackageScript.Contains("'linux-x64'", StringComparison.Ordinal) && powerShellPackageScript.Contains("'osx-arm64'", StringComparison.Ordinal), "VS Code markup extension PowerShell packager should include common desktop runtime identifiers.");
        Assert(powerShellPackageScript.Contains("PublishMarketplace", StringComparison.Ordinal) && powerShellPackageScript.Contains("VSCE_PAT", StringComparison.Ordinal), "VS Code markup extension PowerShell packager should support Marketplace publishing.");
        Assert(powerShellPackageScript.Contains("tools/ChartForgeX.Markup.Cli", StringComparison.Ordinal), "VS Code markup extension PowerShell packager should copy CLI assets to the extension tools folder.");

        var workflow = File.ReadAllText(workflowPath);
        Assert(workflow.Contains("ChartForgeX-v", StringComparison.Ordinal), "VS Code markup workflow should stamp release versions from ChartForgeX release tags.");
        Assert(workflow.Contains("origin/main", StringComparison.Ordinal), "VS Code markup workflow should validate release commits against origin/main.");
        Assert(workflow.Contains("VSCE_PAT", StringComparison.Ordinal), "VS Code markup workflow should publish with the Marketplace token secret.");
        Assert(workflow.Contains("ChartForgeX.Markup.VSCode/scripts/package-vsix.ps1", StringComparison.Ordinal), "VS Code markup workflow should package through the PowerShell packager.");
        Assert(workflow.Contains("chartforgex-markup-vsix", StringComparison.Ordinal), "VS Code markup workflow should upload a stable VSIX artifact.");
        Assert(workflow.Contains("return \"$($utc.Year).$monthDay.$time\"", StringComparison.Ordinal), "VS Code release versions should use unpadded numeric SemVer components.");
        Assert(workflow.Contains("Expected = '2026.102.30405'", StringComparison.Ordinal) && workflow.Contains("Expected = '2026.721.105809'", StringComparison.Ordinal), "VS Code release workflow should self-test single-digit and multi-digit UTC version components.");
        Assert(workflow.Contains("^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)$", StringComparison.Ordinal), "VS Code release workflow should reject leading-zero numeric SemVer components.");

        var gitignore = File.ReadAllText(Path.Combine(extensionRoot, ".gitignore"));
        foreach (var ignored in new[] { "node_modules/", "out/", "dist/", "tools/" }) {
            Assert(gitignore.Contains(ignored, StringComparison.Ordinal), "VS Code markup extension should ignore generated folder: " + ignored);
        }
    }

    private static string JsonString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static HashSet<string> JsonStringSet(JsonElement array, string? property = null) {
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in array.EnumerateArray()) {
            var value = property == null ? item : item.GetProperty(property);
            if (value.ValueKind == JsonValueKind.String) values.Add(value.GetString() ?? string.Empty);
        }

        return values;
    }

    private static void ExampleAppIncludesInteractiveDemos() {
        var program = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "Program.cs"));
        Assert(program.Contains("SaveInteractiveHtml", StringComparison.Ordinal), "Example generation should include a visible interactive HTML adapter demo.");
        Assert(program.Contains("SaveInteractiveHtmlDashboard", StringComparison.Ordinal), "Example generation should include a visible synchronized dashboard adapter demo.");
        Assert(program.Contains("GraphExplorerExamples.Write", StringComparison.Ordinal), "Example generation should include a visible graph explorer adapter demo.");
        Assert(program.Contains("ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts", StringComparison.Ordinal), "Interactive example should exercise the competitive review toolbar features.");
        var graphExamples = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "GraphExplorerExamples.cs"));
        Assert(graphExamples.Contains("Identity Risk Graph Explorer", StringComparison.Ordinal) && graphExamples.Contains("GraphPhysicsSolver.ForceAtlas2", StringComparison.Ordinal) && graphExamples.Contains("CanvasPreferredNodeThreshold = 160", StringComparison.Ordinal), "Graph explorer examples should open product-real clustered runtime physics in an inspectable SVG state before Canvas LOD is needed.");
        Assert(graphExamples.Contains("CanvasPreferredNodeThreshold = 160", StringComparison.Ordinal) && graphExamples.Contains("MaxInteractiveCanvasNodes = 2000", StringComparison.Ordinal), "Graph explorer examples should exercise the Canvas fallback path and Canvas-specific performance budget.");
        Assert(graphExamples.Contains("Enterprise Access Graph Benchmark", StringComparison.Ordinal) && graphExamples.Contains("360 nodes, 720 directed edges", StringComparison.Ordinal) && graphExamples.Contains("CanvasPreferredNodeThreshold = 160", StringComparison.Ordinal), "Graph explorer examples should include a large-object benchmark beyond toy scenes.");
        Assert(graphExamples.Contains("xmlns='http://www.w3.org/2000/svg'", StringComparison.Ordinal), "Graph explorer image-node examples should use portable self-contained SVG data URLs.");
        var premiumGraphExample = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "GraphExplorerPremiumTopologyExample.cs"));
        Assert(premiumGraphExample.Contains("Global Estate Premium Topology", StringComparison.Ordinal) && premiumGraphExample.Contains("InitialRootNodeId = \"estate\"", StringComparison.Ordinal) && premiumGraphExample.Contains("SecondaryLabel", StringComparison.Ordinal) && premiumGraphExample.Contains("BadgeText", StringComparison.Ordinal), "Graph explorer examples should include a premium image-backed hierarchy with real drill-down details.");
        var scaleGraphExamples = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "GraphExplorerScaleExamples.cs"));
        var exampleOptions = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "ExampleProgramOptions.cs"));
        Assert(scaleGraphExamples.Contains("1000, 5000, 10000", StringComparison.Ordinal) && scaleGraphExamples.Contains("HtmlGraphRenderBackend.WebGl", StringComparison.Ordinal) && scaleGraphExamples.Contains("FrameBudgetMilliseconds = 16", StringComparison.Ordinal) && exampleOptions.Contains("--graph-scale-only", StringComparison.Ordinal), "Graph explorer examples should expose repeatable 1k, 5k, and 10k WebGL scale baselines behind an explicit generation command.");
        var graphRenderer = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "HtmlGraphExplorerRenderer.cs"));
        Assert(!graphRenderer.Contains("WriteFilter(writer, \"status\", scene.Nodes.Select(node => node.Status).Concat(scene.Edges.Select(edge => edge.Status)).Concat(scene.Clusters.Select(cluster => cluster.Kind)))", StringComparison.Ordinal), "Graph explorer status filters should not mix cluster kinds into the status facet.");
        var scenarios = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "ExampleInteractiveScenarios.cs"));
        Assert(program.Contains("ExampleInteractiveScenarios.ConfigureDomainSecurity", StringComparison.Ordinal) && scenarios.Contains(".AddScenario(\"healthy-trend\"", StringComparison.Ordinal) && scenarios.Contains(".WithDeepLinkState()", StringComparison.Ordinal), "Interactive example should visibly exercise reusable scenario and deep-link controls.");
        var topologyExamples = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "TopologyExamples.cs"));
        Assert(topologyExamples.Contains("Scenario Route Explorer", StringComparison.Ordinal) && topologyExamples.Contains("visual-replication-mesh-explorer.html?scenario=client-request-europe&amp;scenarioStep=4", StringComparison.Ordinal), "Topology examples should surface the scenario route explorer as an easy deep-linkable demo.");
        Assert(topologyExamples.Contains("topology-demo.html", StringComparison.Ordinal), "Topology examples should copy their focused demo index into the root generated artifact set.");
        var forceTopologyExamples = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "TopologyVisualExamples.Force.cs"));
        Assert(forceTopologyExamples.Contains("SaveInteractiveHtml", StringComparison.Ordinal) && !forceTopologyExamples.Contains("chart.SaveHtml", StringComparison.Ordinal), "Force-graph-only examples should route interactive HTML through the adapter package.");
        var coreProjectText = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "ChartForgeX.csproj"));
        var htmlAdapterProjectText = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "ChartForgeX.Interactivity.Html.csproj"));
        Assert(!coreProjectText.Contains("topology-interaction.js", StringComparison.Ordinal) && htmlAdapterProjectText.Contains("topology-interaction.js", StringComparison.Ordinal), "Topology browser runtimes should be embedded only by ChartForgeX.Interactivity.Html.");
        Assert(!File.Exists(Path.Combine(FindRepositoryRoot(), "ChartForgeX", "Topology", "TopologyIconStencilBrowser.cs")) && File.Exists(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "TopologyIconStencilBrowser.cs")), "The host-facing topology stencil browser should stay in the HTML interactivity adapter.");
        var catalog = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "GalleryWriter.Catalog.cs"));
        Assert(catalog.Contains("Topology Interactive Demos", StringComparison.Ordinal), "Catalog should separate topology-first interactive demos from regular chart interactivity examples.");
        Assert(catalog.Contains("\"identity-risk-graph-explorer\"", StringComparison.Ordinal), "Catalog should include the generated product-real graph explorer example.");
        Assert(catalog.Contains("\"enterprise-access-graph-benchmark\"", StringComparison.Ordinal), "Catalog should include the generated large graph benchmark example.");
        Assert(CountOccurrences(catalog, "\"visual-replication-mesh-explorer\"") == 1, "Scenario topology demo should live in one catalog family instead of being duplicated across topology visual groups.");
    }

    private static void GeneratedWebsiteLinksResolveToSeedAssets() {
        var generatedPath = Path.Combine(FindRepositoryRoot(), "Website", "static", "examples", "generated");
        var missing = new List<string>();
        foreach (var htmlPath in Directory.EnumerateFiles(generatedPath, "*.html")) {
            var html = File.ReadAllText(htmlPath);
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(html, "(?:href|src)=\"([^\"]+)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                var value = match.Groups[1].Value;
                // Embedded data URLs can exceed System.Uri's length limit; they are not local asset paths.
                if (value.StartsWith("#", StringComparison.Ordinal) || value.StartsWith("/", StringComparison.Ordinal)
                    || System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-z][a-z0-9+.-]*:", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;
                var relativePath = value.Split('?', '#')[0].Replace('/', Path.DirectorySeparatorChar);
                if (string.IsNullOrWhiteSpace(relativePath)) continue;
                var assetPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(htmlPath) ?? generatedPath, relativePath));
                if (!File.Exists(assetPath)) missing.Add(Path.GetFileName(htmlPath) + " -> " + value);
            }
        }

        Assert(missing.Count == 0, "Checked-in generated website pages should not link missing seed assets: " + string.Join(", ", missing.OrderBy(value => value, StringComparer.Ordinal)));
    }

    private static void EuropeRevenueMapRoutesTargetRenderedMarkers() {
        var maps = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "MapExamples.cs"));
        Assert(!maps.Contains("London to Warsaw", StringComparison.Ordinal) && !maps.Contains("Madrid to Berlin", StringComparison.Ordinal), "Europe revenue map routes should not use capital-city coordinates when the rendered markers are country market points.");
        Assert(maps.Contains(".AddMapRouteBetweenPoints(\"United Kingdom to Poland\", \"United Kingdom\", \"Poland\"", StringComparison.Ordinal), "Europe revenue map should route from the United Kingdom marker to the Poland marker.");
        Assert(maps.Contains(".AddMapRouteBetweenPoints(\"Spain to Germany\", \"Spain\", \"Germany\"", StringComparison.Ordinal), "Europe revenue map should route from the Spain marker to the Germany marker.");
        Assert(!maps.Contains(".AddMapRoute(route.Label", StringComparison.Ordinal), "Viewport map examples should use point-bound route helpers instead of duplicating raw route coordinates.");
        Assert(maps.Contains(".AddMapRouteBetweenPoints(route.Label, route.FromPointLabel, route.ToPointLabel", StringComparison.Ordinal), "Viewport map examples should bind route overlays to the rendered dotted-map markers.");
    }

    private static void NuGetPackageMetadataStaysPublishReady() {
        var libraryProject = Path.Combine(FindRepositoryRoot(), "ChartForgeX", "ChartForgeX.csproj");
        Assert(HasXmlProperty(libraryProject, "PackageId", "ChartForgeX"), "PackageId should remain stable.");
        Assert(HasXmlProperty(libraryProject, "PackageReadmeFile", "README.md"), "Package should include the README.");
        Assert(HasXmlProperty(libraryProject, "PackageLicenseExpression", "MIT"), "Core package should declare the repository license.");
        Assert(HasXmlProperty(libraryProject, "PackageProjectUrl", "https://github.com/EvotecIT/ChartForgeX"), "Package should expose the project URL.");
        Assert(HasXmlProperty(libraryProject, "RepositoryUrl", "https://github.com/EvotecIT/ChartForgeX"), "Package should expose the repository URL.");
        Assert(HasXmlProperty(libraryProject, "RepositoryType", "git"), "Package repository type should be git.");
        Assert(HasXmlProperty(libraryProject, "PublishRepositoryUrl", "true"), "Package should publish repository metadata.");
        Assert(HasXmlProperty(libraryProject, "Deterministic", "true"), "Package builds should be deterministic.");
        Assert(HasXmlProperty(libraryProject, "IncludeSymbols", "true"), "Package should include symbol package generation.");
        Assert(HasXmlProperty(libraryProject, "SymbolPackageFormat", "snupkg"), "Package symbols should use snupkg format.");
        var productVersion = CurrentProductVersion();
        foreach (var dependentProject in new[] {
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Visuals", "ChartForgeX.Visuals.csproj"),
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Stories", "ChartForgeX.Stories.csproj"),
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity", "ChartForgeX.Interactivity.csproj"),
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "ChartForgeX.Interactivity.Html.csproj"),
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Markup", "ChartForgeX.Markup.csproj"),
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Mermaid", "ChartForgeX.Mermaid.csproj"),
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Markup.Mermaid", "ChartForgeX.Markup.Mermaid.csproj")
        }) {
            var dependentReleaseNotes = GetXmlValue(dependentProject, "PackageReleaseNotes");
            Assert(dependentReleaseNotes.Contains(productVersion, StringComparison.Ordinal),
                "Dependent package release notes should describe the synchronized " + productVersion + " release lane: " + Path.GetRelativePath(FindRepositoryRoot(), dependentProject));
        }
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "CONTRIBUTING.md")), "Repository should include contribution guidance.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "TODO.md")), "Repository should include centralized follow-up guidance.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "AGENTS.md")), "Repository should include agent guidance.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "LICENSE")), "Repository should include a root license file.");
        Assert(!File.Exists(Path.Combine(FindRepositoryRoot(), "CHANGELOG.md")), "GitHub Releases should be the release-note source of truth instead of a second repository changelog.");
        Assert(!File.Exists(Path.Combine(FindRepositoryRoot(), "docs", "dashboard-pattern-expansion-plan.md")), "Completed dashboard implementation plans should be folded into TODO or focused docs instead of staying as stale plan files.");
        Assert(!Directory.Exists(Path.Combine(FindRepositoryRoot(), "experiments")), "Release branches should not ship loose experiment folders; keep durable decisions in docs or TODO.");
        foreach (var packageProject in new[] {
            libraryProject,
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity", "ChartForgeX.Interactivity.csproj"),
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "ChartForgeX.Interactivity.Html.csproj")
        }) {
            Assert(HasXmlProperty(packageProject, "PackageLicenseExpression", "MIT"), "Package should declare the MIT license: " + Path.GetRelativePath(FindRepositoryRoot(), packageProject));
            Assert(HasXmlProperty(packageProject, "IsAotCompatible", "true"), "Modern package assets should declare AOT compatibility: " + Path.GetRelativePath(FindRepositoryRoot(), packageProject));
            Assert(HasXmlProperty(packageProject, "EnableTrimAnalyzer", "true"), "Modern package assets should enable trim analysis: " + Path.GetRelativePath(FindRepositoryRoot(), packageProject));
            Assert(HasXmlProperty(packageProject, "EnableAotAnalyzer", "true"), "Modern package assets should enable AOT analysis: " + Path.GetRelativePath(FindRepositoryRoot(), packageProject));
        }
        var tags = GetXmlValue(libraryProject, "PackageTags");
        foreach (var tag in new[] { "charts", "svg", "reports", "zero-dependency", "aot", "nativeaot", "trimming" }) {
            Assert(tags.Contains(tag, StringComparison.OrdinalIgnoreCase), "Package tags should include " + tag + ".");
        }
    }

    private static void GitHubActionsUsePublicRunnerMatrix() {
        var workflowRoot = Path.Combine(FindRepositoryRoot(), ".github", "workflows");
        Assert(Directory.Exists(workflowRoot), "Repository should include GitHub Actions workflows.");
        var workflows = Directory.EnumerateFiles(workflowRoot, "*.yml", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(workflowRoot, "*.yaml", SearchOption.TopDirectoryOnly))
            .ToArray();
        Assert(workflows.Length > 0, "Repository should include at least one GitHub Actions workflow.");
        foreach (var workflow in workflows) {
            var workflowName = Path.GetFileName(workflow);
            var text = File.ReadAllText(workflow);
            Assert(!text.Contains("self-hosted", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should use public hosted runners now that the repository is public: " + workflowName);
            if (string.Equals(workflowName, "vscode-extension.yml", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workflowName, "website-api-release.yml", StringComparison.OrdinalIgnoreCase)) continue;

            Assert(ContainsAny(text, "ubuntu-latest") && ContainsAny(text, "windows-latest") && ContainsAny(text, "macos-latest"), "GitHub Actions workflows should cover public Linux, Windows, and macOS runners: " + workflowName);
            Assert(text.Contains("actions/setup-dotnet", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should install the expected .NET SDK: " + workflowName);
            Assert(text.Contains("actions/upload-artifact", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should preserve their validation artifacts: " + workflowName);
            if (string.Equals(workflowName, "rendering-history.yml", StringComparison.OrdinalIgnoreCase)) {
                Assert(text.Contains("GIT_CONFIG_KEY_0: core.autocrlf", StringComparison.Ordinal) &&
                       text.Contains("GIT_CONFIG_VALUE_0: 'false'", StringComparison.Ordinal),
                    "Benchmark owner checkout should preserve source bytes for its controlled source guard.");
                Assert(text.Contains("global-json-file: Ignore/PowerForgeSource/global.json", StringComparison.Ordinal),
                    "Benchmark builds should use the canonical owner's selected SDK.");
                Assert(text.Contains("rendering-history-proof-${{ matrix.name }}", StringComparison.Ordinal) &&
                       text.Contains("rendering-history-reference-${{ matrix.name }}", StringComparison.Ordinal),
                    "Benchmark workflows should retain platform-specific raw proof and accepted references.");
                continue;
            }
            Assert(text.Contains("artifacts/packages/Release", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should upload packages from Build.ps1 artifact output: " + workflowName);
            Assert(text.Contains("if: runner.os == 'Linux'", StringComparison.Ordinal), "GitHub Actions workflows should keep Linux package provisioning off Windows and macOS runners: " + workflowName);
            Assert(text.Contains("chartforgex-packages-${{ matrix.runner }}", StringComparison.Ordinal) && text.Contains("chartforgex-example-gallery-${{ matrix.runner }}", StringComparison.Ordinal), "GitHub Actions matrix jobs should upload OS-specific artifact names: " + workflowName);
        }
    }

}
