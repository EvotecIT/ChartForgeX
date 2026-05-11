using System;
using System.IO;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyIconCatalogsResolveBuiltinAndVendorIcons() {
        var vendorPack = new TopologyIconPack("veeam", "Veeam", vendor: "Veeam", version: "1.0")
            .AddIcon("backup-server", "Backup Server", TopologyNodeKind.Server, TopologyIconShape.Server, "VBR", "#00B336", "Backup")
            .AddIcon("repository", "Repository", TopologyNodeKind.Storage, TopologyIconShape.Storage, "REPO", "#007A5A", "Backup");
        var catalog = TopologyIconCatalog.Default().AddPack(vendorPack);
        var unqualifiedSubnet = catalog.Resolve("subnet");
        Assert(unqualifiedSubnet?.QualifiedId == "network:subnet", "Unqualified subnet should keep resolving to the generic network icon.");
        Assert(catalog.Resolve("microsoft-ad:ad-subnet") != null, "AD subnet should stay available through its qualified icon id.");
        Assert(catalog.Search(new TopologyIconCatalogQuery { SearchText = "subnet" }).Any(icon => icon.QualifiedId == "microsoft-ad:ad-subnet"), "AD subnet should remain discoverable by subnet search.");

        var chart = TopologyChart.Create()
            .WithId("icon-pack-demo")
            .WithViewport(520, 280, 24)
            .WithLegend(null)
            .AddGroup("directory", "Directory", 24, 70, 250, 150, TopologyHealthStatus.Healthy, iconId: "microsoft-ad:site")
            .WithGroupIcon("directory", "microsoft-ad:site", catalog)
            .AddIconNode("dc1", "DC1", "microsoft-ad:domain-controller", 58, 120, TopologyHealthStatus.Healthy, "directory", catalog: catalog)
            .AddIconNode("switch1", "Core Switch", "network:switch", 178, 120, TopologyHealthStatus.Warning, "directory", catalog: catalog)
            .AddIconNode("backup1", "VBR01", "veeam:backup-server", 360, 120, TopologyHealthStatus.Healthy, groupId: null, catalog: catalog)
            .AddEdge("dc-switch", "dc1", "switch1", "LDAP", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy)
            .AddEdge("switch-backup", "switch1", "backup1", "backup", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning, TopologyDirection.Forward);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IconCatalog = catalog,
            EnableHtmlInteractions = true
        };
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-node-icon-id=\"microsoft-ad:domain-controller\"", StringComparison.Ordinal), "Topology SVG should expose built-in node icon ids.");
        Assert(svg.Contains("data-node-icon-pack=\"microsoft-ad\"", StringComparison.Ordinal), "Topology SVG should expose built-in node icon packs.");
        Assert(svg.Contains("data-node-icon-id=\"veeam:backup-server\"", StringComparison.Ordinal), "Topology SVG should expose vendor node icon ids.");
        Assert(svg.Contains("data-node-icon-shape=\"Server\"", StringComparison.Ordinal), "Topology SVG should expose resolved icon shapes for host palettes.");
        Assert(svg.Contains("data-group-icon-id=\"microsoft-ad:site\"", StringComparison.Ordinal), "Topology SVG should expose group icon ids.");
        Assert(svg.Contains("data-node-kind=\"Server\"", StringComparison.Ordinal), "Icon nodes should inherit their generic node kind from the icon definition.");

        var html = chart.ToHtmlPage(options);
        Assert(html.Contains("iconId: attr(element, 'data-node-icon-id')", StringComparison.Ordinal), "Topology HTML selection payloads should include node icon ids.");
        Assert(html.Contains("iconShape: attr(element, 'data-group-icon-shape')", StringComparison.Ordinal), "Topology HTML selection payloads should include group icon shapes.");
        Assert(chart.ToPng(options).Length > 64, "Topology PNG should render built-in and vendor icon nodes.");
    }

    private static void TopologyIconCatalogsRenderPaletteCharts() {
        var vendorPack = new TopologyIconPack("fortinet", "Fortinet", vendor: "Fortinet", version: "2026.1")
            .AddIcon("firewall", "FortiGate Firewall", TopologyNodeKind.Gateway, TopologyIconShape.Firewall, "FG", "#DA291C", "Security")
            .AddIcon("switch", "FortiSwitch", TopologyNodeKind.Network, TopologyIconShape.NetworkSwitch, "FSW", "#DA291C", "Network");
        var catalog = TopologyIconCatalog.Default().AddPack(vendorPack);

        var palette = catalog.ToPaletteChart(new TopologyIconPaletteOptions {
            Id = "picker",
            Title = "Network Diagram Icons",
            Subtitle = "Choose reusable icon models for topology diagrams.",
            PacksPerRow = 3,
            ColumnsPerPack = 3
        });
        var options = new TopologyRenderOptions {
            IconCatalog = catalog,
            IncludeLegend = false,
            EnableHtmlInteractions = true
        };

        Assert(palette.Groups.Count == catalog.Packs.Count, "The palette chart should create one group per registered icon pack.");
        Assert(palette.Nodes.Count == catalog.Packs.Sum(pack => pack.Icons.Count), "The palette chart should create one node per registered icon.");
        Assert(palette.Nodes.Any(node => node.IconId == "fortinet:firewall" && node.DisplayMode == TopologyNodeDisplayMode.Tile), "Vendor palette icons should preserve qualified icon ids and tile display.");
        Assert(palette.Groups.Any(group => group.Metadata.TryGetValue("vendor", out var vendor) && vendor == "Fortinet"), "Vendor pack metadata should be available to host-side pickers.");

        var svg = palette.ToSvg(options);
        Assert(svg.Contains("Network Diagram Icons", StringComparison.Ordinal), "The palette chart title should render.");
        Assert(svg.Contains("data-node-icon-id=\"network:switch\"", StringComparison.Ordinal), "Built-in network icons should render in the palette.");
        Assert(svg.Contains("data-node-icon-id=\"fortinet:firewall\"", StringComparison.Ordinal), "Vendor icons should render in the palette.");
        Assert(svg.Contains("data-cfx-meta-category=\"Security\"", StringComparison.Ordinal), "Palette nodes should expose icon category metadata.");
        Assert(!svg.Contains(">ST<", StringComparison.Ordinal), "Storage icons should use renderer-owned glyphs instead of fallback text.");
        Assert(!svg.Contains(">TLS<", StringComparison.Ordinal), "Certificate icons should use renderer-owned glyphs instead of fallback text.");
        Assert(!svg.Contains(">PC<", StringComparison.Ordinal), "Endpoint icons should use renderer-owned glyphs instead of fallback text.");
        Assert(!svg.Contains(">FOR<", StringComparison.Ordinal), "Icon-pack headers should use group icon glyphs instead of fallback text.");
        Assert(!svg.Contains(">DOM<", StringComparison.Ordinal), "Domain icons should use renderer-owned glyphs instead of fallback text.");
        Assert(!svg.Contains(">FSW<", StringComparison.Ordinal), "Vendor switch icons should use renderer-owned switch glyphs instead of fallback text.");

        var html = palette.ToHtmlPage(options);
        Assert(html.Contains("iconId: attr(element, 'data-node-icon-id')", StringComparison.Ordinal), "Interactive palette HTML should expose selected icon ids.");
        Assert(palette.ToPng(options).Length > 64, "Palette charts should render to PNG.");
    }

    private static void TopologyIconArtworkRendersVendorSvgAssets() {
        var pack = new TopologyIconPack("azure-example", "Azure Example Icons", vendor: "Microsoft", version: "sample")
            .WithTags("azure", "cloud", "vendor", "sample")
            .AddIcon(new TopologyIconDefinition("azure-example", "data-factory", "Data Factory", TopologyNodeKind.Process, TopologyIconShape.Application) {
                Symbol = "ADF",
                Color = "#0078D4",
                Category = "Azure",
                DisplayMode = TopologyNodeDisplayMode.Tile,
                Artwork = TopologyIconArtwork.InlineSvg("<rect x=\"7\" y=\"10\" width=\"13\" height=\"28\" rx=\"2\" fill=\"#0078D4\"/><rect x=\"28\" y=\"6\" width=\"13\" height=\"32\" rx=\"2\" fill=\"#50E6FF\"/><path d=\"M20 17 H28 M20 31 H28\" stroke=\"#243A5E\" stroke-width=\"3\" stroke-linecap=\"round\"/><circle cx=\"13.5\" cy=\"17\" r=\"2.6\" fill=\"#FFFFFF\"/><circle cx=\"34.5\" cy=\"17\" r=\"2.6\" fill=\"#FFFFFF\"/><circle cx=\"34.5\" cy=\"29\" r=\"2.6\" fill=\"#FFFFFF\"/>", "0 0 48 48")
            }.WithTags("data-factory", "pipeline", "azure"));
        var catalog = TopologyIconCatalog.Default().AddPack(pack);

        var palette = catalog.ToPaletteChart(new TopologyIconPaletteOptions {
            Id = "azure-example-palette",
            Title = "Azure Example Icons",
            IncludeBuiltInPacks = false
        });
        var renderOptions = new TopologyRenderOptions {
            IconCatalog = catalog,
            IncludeLegend = false,
            EnableHtmlInteractions = true
        };

        var svg = palette.ToSvg(renderOptions);
        Assert(svg.Contains("data-node-icon-artwork=\"svg\"", StringComparison.Ordinal), "SVG palette nodes should expose artwork type metadata.");
        Assert(svg.Contains("data-cfx-role=\"topology-icon-artwork\"", StringComparison.Ordinal), "SVG renderer should embed trusted artwork fragments.");
        Assert(svg.Contains("<rect x=\"7\" y=\"10\" width=\"13\"", StringComparison.Ordinal), "SVG renderer should include the vendor artwork fragment instead of only the fallback glyph.");
        Assert(!svg.Contains(">ADF<", StringComparison.Ordinal), "Artwork-backed icons should not fall back to compact text symbols in SVG.");

        var html = palette.ToHtmlPage(renderOptions);
        Assert(html.Contains("iconArtwork: attr(element, 'data-node-icon-artwork')", StringComparison.Ordinal), "Interactive palette payloads should include artwork type metadata.");

        var json = pack.ToJsonManifest();
        Assert(json.Contains("\"artwork\"", StringComparison.Ordinal), "Icon pack manifests should serialize artwork definitions.");
        Assert(json.Contains("\"svgViewBox\": \"0 0 48 48\"", StringComparison.Ordinal), "Icon pack manifests should preserve artwork viewBox values.");

        var imported = TopologyIconPackJson.FromJson(json);
        var importedIcon = imported.Icons.First(icon => icon.Id == "data-factory");
        Assert(importedIcon.Artwork != null && importedIcon.Artwork.HasSvgBody, "Icon pack manifests should round-trip inline SVG artwork.");
        Assert(imported.Validate().IsValid, "Safe artwork should pass pack validation.");

        var unsafePack = new TopologyIconPack("unsafe", "Unsafe", vendor: "Acme")
            .AddIcon(new TopologyIconDefinition("unsafe", "script", "Script", TopologyNodeKind.Application) {
                Artwork = TopologyIconArtwork.InlineSvg("<script>alert(1)</script>")
        });
        Assert(!unsafePack.Validate().IsValid, "Unsafe inline SVG artwork should be rejected by pack validation.");
    }

    private static void TopologyIconStencilBrowserRendersSearchablePicker() {
        var pack = new TopologyIconPack("azure-example", "Azure Example Icons", vendor: "Microsoft", version: "sample")
            .WithTags("azure", "cloud", "vendor", "sample")
            .AddIcon(new TopologyIconDefinition("azure-example", "data-factory", "Data Factory", TopologyNodeKind.Process, TopologyIconShape.Application) {
                Symbol = "ADF",
                Color = "#0078D4",
                Category = "Azure",
                DisplayMode = TopologyNodeDisplayMode.Tile,
                Artwork = TopologyIconArtwork.InlineSvg("<rect x=\"7\" y=\"10\" width=\"13\" height=\"28\" rx=\"2\" fill=\"#0078D4\"/>", "0 0 48 48")
            }.WithTags("data-factory", "pipeline"));
        var catalog = TopologyIconCatalog.Default().AddPack(pack);

        var html = catalog.ToStencilBrowserHtmlPage(new TopologyIconStencilBrowserOptions {
            Id = "stencil-browser-test",
            Title = "Stencil Browser",
            Subtitle = "Pick icons",
            PacksPerRow = 3,
            ColumnsPerPack = 4
        });

        Assert(html.Contains("data-cfx-icon-browser=\"true\"", StringComparison.Ordinal), "Stencil browser pages should expose a stable root marker.");
        Assert(html.Contains("data-cfx-icon-search=\"true\"", StringComparison.Ordinal), "Stencil browser pages should include client-side search.");
        Assert(html.Contains("data-cfx-icon-filter=\"vendor\"", StringComparison.Ordinal), "Stencil browser pages should include vendor filters.");
        Assert(html.Contains("data-cfx-icon-filter=\"pack\"", StringComparison.Ordinal), "Stencil browser pages should include pack filters.");
        Assert(html.Contains("data-cfx-icon-filter=\"category\"", StringComparison.Ordinal), "Stencil browser pages should include category filters.");
        Assert(html.Contains("data-node-icon-id=\"azure-example:data-factory\"", StringComparison.Ordinal), "Stencil browser pages should render artwork-backed vendor icons.");
        Assert(html.Contains("data-node-icon-artwork=\"svg\"", StringComparison.Ordinal), "Stencil browser pages should preserve artwork metadata in the embedded palette.");
        Assert(html.Contains("data-cfx-meta-vendor=\"Microsoft\"", StringComparison.Ordinal), "Palette nodes should expose vendor metadata for host-side filtering.");
        Assert(html.Contains("topology-node-status", StringComparison.Ordinal), "Stencil browser pages should retain status badge metadata for client-side filtering.");
        Assert(html.Contains("cfx-icon-browser-select", StringComparison.Ordinal), "Stencil browser pages should emit a host-readable selection event.");

        var filtered = catalog.ToStencilBrowserHtmlPage(new TopologyIconStencilBrowserOptions {
            Id = "stencil-browser-filtered",
            Title = "Filtered Browser",
            IncludeBuiltInPacks = false
        });
        Assert(filtered.Contains("Azure Example Icons", StringComparison.Ordinal), "Filtered stencil browsers should retain matching vendor packs.");
        Assert(!filtered.Contains("Common Infrastructure", StringComparison.Ordinal), "Filtered stencil browsers should honor built-in pack filtering.");
    }

    private static void TopologyIconSvgPackImporterBuildsAuditablePacks() {
        var directory = Path.Combine(Path.GetTempPath(), "chartforgex-topology-svg-import-" + Guid.NewGuid().ToString("N"));
        try {
            Directory.CreateDirectory(Path.Combine(directory, "Azure", "SVG"));
            Directory.CreateDirectory(Path.Combine(directory, "Office 365", "SVG"));
            File.WriteAllText(Path.Combine(directory, "Azure", "SVG", "Data-Factory.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 48 48\"><title>Data Factory</title><path d=\"M6 10h14v28H6z\" fill=\"#0078D4\"/><path d=\"M28 8h14v32H28z\" fill=\"#50E6FF\"/></svg>");
            File.WriteAllText(Path.Combine(directory, "Office 365", "SVG", "Data-Factory.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"64\" height=\"64\"><desc>duplicate name</desc><circle cx=\"32\" cy=\"32\" r=\"22\" fill=\"#7FBA00\"/></svg>");
            File.WriteAllText(Path.Combine(directory, "Azure", "SVG", "Duplicate-Ids.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 48 48\"><defs><clipPath id='a'><rect width='20' height='20'/></clipPath><linearGradient id='a'><stop offset='0' stop-color='#fff'/></linearGradient></defs><rect width=\"48\" height=\"48\" clip-path=\"url(#a)\" fill=\"url(#a)\"/><use href='#a'/></svg>");
            File.WriteAllText(Path.Combine(directory, "Azure", "SVG", "Mixed-Style.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"root-color\" viewBox=\"0 0 48 48\"><defs><linearGradient id=\"ok\"><stop offset=\"0\" stop-color=\"#fff\"/></linearGradient></defs><rect width=\"48\" height=\"48\" fill=\"url('#root-color')\" style=\"fill:url('#ok');filter:url(#missing);stroke:#111\"/></svg>");
            File.WriteAllText(Path.Combine(directory, "Azure", "SVG", "Visio-Defs.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 48 48\"><userDefs><clipPath id=\"clip0\"><rect width=\"48\" height=\"48\"/></clipPath></userDefs><rect width=\"48\" height=\"48\" clip-path=\"url(#clip0)\" fill=\"#0078D4\"/></svg>");
            File.WriteAllText(Path.Combine(directory, "Azure", "SVG", "Doctype.svg"), "<!DOCTYPE svg [<!ELEMENT svg ANY>]><svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" clip-path=\"url(#missing)\" fill=\"#0078D4\"/></svg>");
            File.WriteAllText(Path.Combine(directory, "Azure", "SVG", "Unsafe.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 48 48\"><script>alert(1)</script><rect width=\"48\" height=\"48\" fill=\"#fff\"/></svg>");

            var result = TopologyIconSvgPackImporter.ImportSvgPackFromDirectory(directory, new TopologyIconSvgPackImportOptions {
                PackId = "microsoft-azure-stencils",
                PackLabel = "Microsoft Azure Stencils",
                Vendor = "Microsoft",
                Version = "import-test",
                SourceUrl = "https://github.com/sandroasp/Microsoft-Integration-and-Azure-Stencils-Pack-for-Visio",
                SourceRevision = "d332d1457fc8d43d972815eab59d0a2da3087c45",
                SourceLicense = "MIT",
                SourceLicenseUrl = "https://github.com/sandroasp/Microsoft-Integration-and-Azure-Stencils-Pack-for-Visio/blob/master/LICENSE",
                SourceLicensePath = "LICENSE",
                CategoryPrefix = "Microsoft",
                StripDoctypeDeclarations = true
            });

            Assert(result.ImportedCount == 6, "SVG pack imports should add safe SVG artwork files.");
            Assert(result.SkippedCount == 1 && result.HasSkippedFiles, "SVG pack imports should report skipped unsafe files.");
            Assert(result.Files.Any(file => !file.Imported && file.Message != null && file.Message.Contains("unsafe", StringComparison.OrdinalIgnoreCase)), "Skipped SVG files should expose a useful message.");
            Assert(result.Pack.Id == "microsoft-azure-stencils", "SVG imports should honor stable pack ids.");
            Assert(result.Pack.Metadata.TryGetValue("source.url", out var sourceUrl) && sourceUrl.Contains("sandroasp", StringComparison.OrdinalIgnoreCase), "Imported packs should retain source repository provenance.");
            Assert(result.Pack.Metadata.TryGetValue("source.revision", out var revision) && revision.StartsWith("d332d145", StringComparison.Ordinal), "Imported packs should retain source revision provenance.");
            Assert(result.Pack.Metadata.TryGetValue("source.license", out var license) && license == "MIT", "Imported packs should retain license provenance.");
            Assert(result.Pack.Icons.Any(icon => icon.Id == "data-factory"), "Imported SVG file names should become stable icon ids.");
            Assert(result.Pack.Icons.Any(icon => icon.Id == "data-factory-2"), "Duplicate SVG file names should receive deterministic suffixes.");
            Assert(result.Pack.Icons.Any(icon => icon.Category == "Microsoft / Azure"), "Imported icons should infer categories from source folders and optional prefixes.");
            Assert(result.Pack.Icons.Any(icon => icon.Category == "Microsoft / Office 365"), "Imported icons should preserve multi-word source folder categories.");

            var first = result.Pack.Icons.First(icon => icon.Id == "data-factory");
            Assert(first.Artwork != null && first.Artwork.HasSvgBody && first.Artwork.SvgViewBox == "0 0 48 48", "Imported SVG icons should keep inline artwork and source viewBox.");
            Assert(first.Metadata.TryGetValue("source.path", out var sourcePath) && sourcePath == "Azure/SVG/Data-Factory.svg", "Imported SVG icons should keep normalized relative source paths.");
            Assert(!first.Artwork!.SvgBody!.Contains("<title", StringComparison.OrdinalIgnoreCase), "Importer should strip non-artwork title metadata from inline fragments.");

            var second = result.Pack.Icons.First(icon => icon.Id == "data-factory-2");
            Assert(second.Artwork != null && second.Artwork.SvgViewBox == "0 0 64 64", "Imported SVG icons should infer viewBox from width and height when missing.");
            var duplicateIds = result.Pack.Icons.First(icon => icon.Id == "duplicate-ids");
            Assert(duplicateIds.Artwork != null && duplicateIds.Artwork.SvgBody!.Contains("id=\"cfxi-microsoft-azure-stencils-duplicate-ids-a\"", StringComparison.Ordinal), "Imported SVG ids should be prefixed for document-level isolation.");
            Assert(duplicateIds.Artwork!.SvgBody!.Contains("id=\"cfxi-microsoft-azure-stencils-duplicate-ids-a-2\"", StringComparison.Ordinal), "Duplicate source ids should get unique prefixed ids.");
            Assert(duplicateIds.Artwork!.SvgBody!.Contains("href=\"#cfxi-microsoft-azure-stencils-duplicate-ids-a\"", StringComparison.Ordinal), "Single-quoted href fragments should be rewritten to the prefixed id.");
            var mixedStyle = result.Pack.Icons.First(icon => icon.Id == "mixed-style");
            Assert(mixedStyle.Artwork != null && mixedStyle.Artwork.SvgBody!.Contains("fill=\"url('#root-color')\"", StringComparison.Ordinal), "Quoted root-level SVG url fragments should be included in dangling-reference validation.");
            Assert(mixedStyle.Artwork!.SvgBody!.Contains("fill:url('#cfxi-microsoft-azure-stencils-mixed-style-ok')", StringComparison.Ordinal), "Quoted style url fragments should be rewritten to prefixed ids.");
            Assert(!mixedStyle.Artwork!.SvgBody!.Contains("filter:url(#missing)", StringComparison.Ordinal) && mixedStyle.Artwork!.SvgBody!.Contains("stroke:#111", StringComparison.Ordinal), "Dangling style url references should remove only the invalid declaration.");
            var visioDefs = result.Pack.Icons.First(icon => icon.Id == "visio-defs");
            Assert(visioDefs.Artwork != null && visioDefs.Artwork.SvgBody!.Contains("clipPath", StringComparison.Ordinal), "Importer should preserve drawable definitions from Visio userDefs blocks.");
            Assert(result.Pack.Icons.Any(icon => icon.Id == "doctype"), "Importer should accept safe SVGs with internal-subset DOCTYPE declarations when stripping is enabled.");
            var doctype = result.Pack.Icons.First(icon => icon.Id == "doctype");
            Assert(doctype.Artwork != null && !doctype.Artwork.SvgBody!.Contains("url(#missing)", StringComparison.Ordinal), "Importer should remove dangling SVG fragment references from incomplete source artwork.");
            Assert(result.Pack.Validate().IsValid, "Imported safe SVG packs should validate cleanly.");

            var json = result.Pack.ToJsonManifest();
            Assert(json.Contains("\"source.url\"", StringComparison.Ordinal), "Imported pack manifests should serialize provenance metadata.");
            Assert(json.Contains("\"source.path\"", StringComparison.Ordinal), "Imported pack manifests should serialize per-icon source paths.");
            var reloaded = TopologyIconPackJson.FromJson(json);
            Assert(reloaded.Icons.Count == 6 && reloaded.Icons.All(icon => icon.Artwork != null), "Imported pack manifests should round-trip SVG artwork.");

            var sidecarDirectory = Path.Combine(directory, "sidecar-pack");
            Directory.CreateDirectory(Path.Combine(sidecarDirectory, "svg"));
            File.WriteAllText(Path.Combine(sidecarDirectory, "svg", "data-factory.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 48 48\"><title>Data Factory</title><rect x=\"4\" y=\"8\" width=\"40\" height=\"32\" fill=\"#0078D4\"/></svg>");
            var sidecarArtwork = TopologyIconArtwork.SvgFile("svg\\data-factory.svg", svgViewBox: "0 0 96 96", previewPath: "previews/data-factory.png");
            sidecarArtwork.PreserveAspectRatio = "none";
            new TopologyIconPack("sidecar", "Sidecar", vendor: "Acme")
                .AddIcon(new TopologyIconDefinition("sidecar", "data-factory", "Data Factory", TopologyNodeKind.Application) {
                    Artwork = sidecarArtwork
                })
                .SaveJsonManifest(Path.Combine(sidecarDirectory, "manifest.json"));
            var loadedSidecar = TopologyIconPackJson.LoadJsonManifest(Path.Combine(sidecarDirectory, "manifest.json"));
            var sidecarIcon = loadedSidecar.Icons.First(icon => icon.Id == "data-factory");
            Assert(sidecarIcon.Artwork != null && sidecarIcon.Artwork.HasSvgPath && sidecarIcon.Artwork.HasSvgBody, "Manifest loading should resolve pack-local SVG sidecar artwork for rendering.");
            Assert(sidecarIcon.Artwork!.SvgViewBox == "0 0 96 96", "Manifest loading should preserve an explicit sidecar viewBox over the SVG file viewport.");
            Assert(sidecarIcon.Artwork!.PreviewPath == "previews/data-factory.png", "Manifest loading should preserve preview sidecar paths for picker UIs.");
            Assert(sidecarIcon.Artwork!.PreserveAspectRatio == "none", "Manifest loading should preserve sidecar preserveAspectRatio settings.");
            var sidecarRoundTrip = loadedSidecar.ToJsonManifest();
            Assert(sidecarRoundTrip.Contains("\"svgPath\"", StringComparison.Ordinal), "Sidecar manifest round-trips should preserve the SVG path.");
            Assert(!sidecarRoundTrip.Contains("\"svg\"", StringComparison.Ordinal), "Sidecar manifest round-trips should not inline hydrated SVG bodies.");
            using var sidecarReader = new StringReader(File.ReadAllText(Path.Combine(sidecarDirectory, "manifest.json")));
            var loadedSidecarFromReader = TopologyIconPackJson.LoadJsonManifest(sidecarReader, sidecarDirectory);
            Assert(loadedSidecarFromReader.Icons[0].Artwork != null && loadedSidecarFromReader.Icons[0].Artwork!.HasSvgBody, "TextReader sidecar loading should resolve artwork when a manifest directory is supplied.");

            var browser = new TopologyIconCatalog().AddPack(result.Pack).ToStencilBrowserHtmlPage(new TopologyIconStencilBrowserOptions {
                IncludeBuiltInPacks = false
            });
            Assert(browser.Contains("data-cfx-icon-detail=\"source\"", StringComparison.Ordinal), "Stencil browser inspectors should include source provenance rows.");
            Assert(browser.Contains("data-cfx-meta-pack-source-url=", StringComparison.Ordinal), "Stencil browser nodes should expose pack source URLs.");
            Assert(browser.Contains("data-cfx-meta-icon-source-path=", StringComparison.Ordinal), "Stencil browser nodes should expose per-icon source paths.");
            Assert(browser.Contains("sourceLicense", StringComparison.Ordinal), "Stencil browser selection payloads should include source license metadata.");
        } finally {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void TopologyCuratedIconPacksLoadSidecarArtwork() {
        var root = Path.Combine(FindRepositoryRoot(), "assets", "topology-icons");
        var packIds = new[] {
            "chartforgex-ad-network-premium",
            "backup",
            "network-security",
            "cloud-productivity",
            "network-infrastructure"
        };
        var catalog = new TopologyIconCatalog();
        foreach (var packId in packIds) {
            var packRoot = Path.Combine(root, packId);
            var manifestPath = Path.Combine(packRoot, "manifest.json");
            Assert(File.Exists(manifestPath), "Curated icon pack should include a manifest: " + packId + ".");
            Assert(File.Exists(Path.Combine(packRoot, "SOURCE.md")), "Curated icon pack should include provenance notes: " + packId + ".");
            Assert(File.Exists(Path.Combine(packRoot, "LICENSE")), "Curated icon pack should include license notes: " + packId + ".");
            Assert(Directory.GetFiles(Path.Combine(packRoot, "previews"), "*.png").Length > 0, "Curated icon pack should include preview PNG thumbnails: " + packId + ".");
            var pack = TopologyIconPackJson.LoadJsonManifest(manifestPath);
            Assert(pack.Validate().IsValid, "Curated icon pack should validate cleanly: " + packId + ".");
            Assert(pack.Metadata.TryGetValue("artwork.origin", out var origin) && origin == "original-chartforgex", "Curated icon packs should carry original-artwork provenance.");
            Assert(pack.Metadata.TryGetValue("artwork.workflow", out var workflow) && workflow == "svg-authored", "Curated icon packs should keep SVG artwork as the source of truth.");
            Assert(File.Exists(Path.Combine(packRoot, "_reports", "refresh-report.json")), "Curated icon packs should include a refresh report: " + packId + ".");
            Assert(pack.Icons.All(icon => icon.Artwork != null && icon.Artwork.HasSvgBody && icon.Artwork.HasSvgPath && icon.Artwork.HasPreviewPath), "Curated icon packs should load SVG bodies while preserving sidecar paths: " + packId + ".");
            Assert(pack.Icons.All(icon => File.Exists(Path.Combine(packRoot, icon.Artwork!.PreviewPath!.Replace('/', Path.DirectorySeparatorChar)))), "Curated icon preview paths should resolve beside the manifest: " + packId + ".");
            catalog.AddPack(pack);
        }

        Assert(catalog.Resolve("chartforgex-ad-network-premium:bridgehead") != null, "Curated AD pack should include bridgehead artwork.");
        Assert(catalog.Resolve("chartforgex-ad-network-premium:read-only-domain-controller") != null, "Curated AD pack should include RODC artwork.");
        Assert(catalog.Resolve("chartforgex-ad-network-premium:user") != null, "Curated AD pack should include user artwork.");
        Assert(catalog.Resolve("chartforgex-ad-network-premium:security-group") != null, "Curated AD pack should include group artwork.");
        Assert(catalog.Resolve("chartforgex-ad-network-premium:computer") != null, "Curated AD pack should include computer artwork.");
        Assert(catalog.Resolve("chartforgex-ad-network-premium:contact") != null, "Curated AD pack should include contact artwork.");
        Assert(catalog.Resolve("chartforgex-ad-network-premium:group-managed-service-account") != null, "Curated AD pack should include gMSA artwork.");
        Assert(catalog.Resolve("chartforgex-ad-network-premium:password-settings-object") != null, "Curated AD pack should include fine-grained password policy artwork.");
        Assert(catalog.Resolve("backup:backup-proxy") != null, "Curated backup pack should include backup proxy artwork.");
        Assert(catalog.Resolve("network-security:firewall") != null, "Curated network security pack should include firewall artwork.");
        Assert(catalog.Resolve("cloud-productivity:conditional-access") != null, "Curated cloud productivity pack should include Conditional Access artwork.");
        Assert(catalog.Resolve("network-infrastructure:rack-switch") != null, "Curated network infrastructure pack should include rack switch artwork.");
        Assert(catalog.Resolve("network-infrastructure:edge-router") != null, "Curated network infrastructure pack should include router artwork.");
        Assert(catalog.Resolve("network-infrastructure:patch-panel") != null, "Curated network infrastructure pack should include patch panel artwork.");

        var palette = catalog.ToPaletteChart(new TopologyIconPaletteOptions {
            Id = "curated-pack-palette",
            Title = "Curated Icon Packs",
            IncludeBuiltInPacks = false,
            PacksPerRow = 2,
            ColumnsPerPack = 4
        });
        var svg = palette.ToSvg(new TopologyRenderOptions {
            IconCatalog = catalog,
            IncludeLegend = false
        });
        Assert(svg.Contains("data-node-icon-artwork=\"svg\"", StringComparison.Ordinal), "Curated sidecar artwork should render through the SVG palette.");
        Assert(svg.Contains("data-cfx-role=\"topology-icon-artwork\"", StringComparison.Ordinal), "Curated artwork-backed icons should render as SVG artwork rather than shape-only fallback nodes.");
    }

    private static void TopologyDefaultAdCatalogExposesAdvancedDirectoryAliases() {
        var catalog = TopologyIconCatalog.Default();
        Assert(catalog.Resolve("microsoft-ad:bridgehead") != null, "Built-in AD catalog should include a bridgehead icon.");
        Assert(catalog.Resolve("microsoft-ad:ad-subnet") != null, "Built-in AD catalog should include an AD subnet icon.");
        Assert(catalog.Search(new TopologyIconCatalogQuery { SearchText = "rodc" }).Any(item => item.QualifiedId == "microsoft-ad:read-only-domain-controller"), "Built-in AD catalog should make RODC searchable.");
        Assert(catalog.Search(new TopologyIconCatalogQuery { SearchText = "cidr" }).Any(item => item.QualifiedId == "microsoft-ad:ad-subnet" || item.QualifiedId == "network:subnet"), "Subnet aliases should be searchable by CIDR vocabulary.");
        Assert(catalog.Search(new TopologyIconCatalogQuery { SearchText = "inter-site" }).Any(item => item.QualifiedId == "microsoft-ad:bridgehead"), "Bridgehead aliases should support replication-oriented picker searches.");
    }

    private static void TopologyIconCatalogsSearchAndFilterPaletteCharts() {
        var vendorPack = new TopologyIconPack("fortinet", "Fortinet", vendor: "Fortinet", version: "2026.1")
            .AddIcon("firewall", "FortiGate Firewall", TopologyNodeKind.Gateway, TopologyIconShape.Firewall, "FG", "#DA291C", "Security")
            .AddIcon("switch", "FortiSwitch", TopologyNodeKind.Network, TopologyIconShape.NetworkSwitch, "FSW", "#DA291C", "Network");
        var catalog = TopologyIconCatalog.Default().AddPack(vendorPack);

        var switches = catalog.Search(new TopologyIconCatalogQuery { SearchText = "switch" });
        Assert(switches.Any(item => item.QualifiedId == "network:switch"), "Catalog search should return built-in switch icons.");
        Assert(switches.Any(item => item.QualifiedId == "fortinet:switch" && item.Vendor == "Fortinet"), "Catalog search should return vendor switch icons with pack metadata.");

        var paletteOptions = new TopologyIconPaletteOptions {
            Id = "fortinet-security",
            Title = "Fortinet Security Icons",
            IncludeBuiltInPacks = false
        };
        paletteOptions.Vendors.Add("Fortinet");
        paletteOptions.Categories.Add("Security");
        var palette = catalog.ToPaletteChart(paletteOptions);

        Assert(palette.Groups.Count == 1, "Filtered palette charts should include only matching packs.");
        Assert(palette.Nodes.Count == 1, "Filtered palette charts should include only matching icons.");
        Assert(palette.Nodes[0].IconId == "fortinet:firewall", "Filtered palette charts should preserve the matching icon id.");
        Assert(palette.Groups[0].Subtitle != null && palette.Groups[0].Subtitle!.Contains("1 icons", StringComparison.Ordinal), "Filtered pack subtitles should reflect the rendered icon count.");

        var svg = palette.ToSvg(new TopologyRenderOptions { IconCatalog = catalog, IncludeLegend = false });
        Assert(svg.Contains("data-node-icon-id=\"fortinet:firewall\"", StringComparison.Ordinal), "Filtered palette SVG should include matching vendor icons.");
        Assert(!svg.Contains("data-node-icon-id=\"fortinet:switch\"", StringComparison.Ordinal), "Filtered palette SVG should exclude non-matching vendor icons.");
        Assert(!svg.Contains("data-node-icon-id=\"network:firewall\"", StringComparison.Ordinal), "Filtered palette SVG should exclude built-in packs when requested.");
        Assert(palette.ToPng(new TopologyRenderOptions { IconCatalog = catalog, IncludeLegend = false }).Length > 64, "Filtered palette charts should render to PNG.");
    }

    private static void TopologyIconCatalogsExposePickerFacetsAndMetadata() {
        var vendorPack = new TopologyIconPack("veeam", "Veeam", vendor: "Veeam", version: "13")
            .WithMetadata("website", "https://www.veeam.com")
            .WithTags("backup", "vendor")
            .AddIcon(new TopologyIconDefinition("veeam", "proxy", "Backup Proxy", TopologyNodeKind.Server, TopologyIconShape.Server) {
                Symbol = "PRX",
                Color = "#00B336",
                Category = "Backup"
            }.WithMetadata("product", "Backup and Replication").WithTags("proxy", "worker"))
            .AddIcon(new TopologyIconDefinition("veeam", "repository", "Backup Repository", TopologyNodeKind.Storage, TopologyIconShape.Storage) {
                Symbol = "REPO",
                Color = "#007A5A",
                Category = "Storage"
            }.WithMetadata("product", "Backup and Replication").WithTags("repository", "repo"));
        var catalog = TopologyIconCatalog.Default().AddPack(vendorPack);

        Assert(catalog.GetPackIds().Contains("veeam"), "Catalog facet helpers should include vendor pack ids.");
        Assert(catalog.GetVendors().Contains("Veeam"), "Catalog facet helpers should include vendor names.");
        Assert(catalog.GetCategories(new TopologyIconCatalogQuery { SearchText = "backup" }).Contains("Backup"), "Catalog category facets should honor search text.");
        Assert(catalog.GetTags().Contains("repository"), "Catalog tag facets should include icon-level tags.");
        Assert(catalog.Search(new TopologyIconCatalogQuery { SearchText = "loadbalancer" }).Any(item => item.QualifiedId == "network:load-balancer"), "Built-in icon tags should provide searchable aliases.");
        Assert(catalog.GetPackSummaries().Any(summary => summary.PackId == "veeam" && summary.IconCount == 2), "Catalog pack summaries should include vendor packs and icon counts.");
        Assert(catalog.GetVendorSummaries().Any(summary => summary.Label == "Veeam" && summary.PackCount == 1 && summary.IconCount == 2), "Catalog vendor summaries should group packs into picker folders.");

        var search = catalog.Search(new TopologyIconCatalogQuery { SearchText = "replication" });
        Assert(search.Any(item => item.QualifiedId == "veeam:proxy"), "Catalog search should include icon metadata values.");
        Assert(search.Any(item => item.QualifiedId == "veeam:repository"), "Catalog search should include metadata from multiple vendor icons.");

        var options = new TopologyIconPaletteOptions { IncludeBuiltInPacks = false };
        options.Vendors.Add("Veeam");
        options.Tags.Add("repo");
        var palette = catalog.ToPaletteChart(options);
        Assert(palette.Groups[0].Metadata.TryGetValue("pack.website", out var website) && website.Contains("veeam", StringComparison.OrdinalIgnoreCase), "Palette groups should preserve pack metadata.");
        Assert(palette.Groups[0].Metadata.TryGetValue("pack.tags", out var packTags) && packTags.Contains("backup", StringComparison.OrdinalIgnoreCase), "Palette groups should preserve pack tags.");
        Assert(palette.Nodes.Count == 1 && palette.Nodes[0].IconId == "veeam:repository", "Palette tag filters should include only matching icons.");
        Assert(palette.Nodes.Any(node => node.Metadata.TryGetValue("icon.product", out var product) && product.Contains("Replication", StringComparison.OrdinalIgnoreCase)), "Palette nodes should preserve icon metadata.");
        Assert(palette.Nodes.Any(node => node.Metadata.TryGetValue("icon.tags", out var iconTags) && iconTags.Contains("repo", StringComparison.OrdinalIgnoreCase)), "Palette nodes should preserve icon tags.");

        var svg = palette.ToSvg(new TopologyRenderOptions { IconCatalog = catalog, IncludeLegend = false });
        Assert(svg.Contains("data-cfx-meta-pack-website=", StringComparison.Ordinal), "SVG palette groups should expose pack metadata.");
        Assert(svg.Contains("data-cfx-meta-pack-tags=", StringComparison.Ordinal), "SVG palette groups should expose pack tags.");
        Assert(svg.Contains("data-cfx-meta-icon-product=", StringComparison.Ordinal), "SVG palette nodes should expose icon metadata.");
        Assert(svg.Contains("data-cfx-meta-icon-tags=", StringComparison.Ordinal), "SVG palette nodes should expose icon tags.");
    }

    private static void TopologyIconPackManifestsRoundTripVendorPacks() {
        var sourcePack = new TopologyIconPack("veeam", "Veeam", vendor: "Veeam", version: "13")
            .WithMetadata("website", "https://www.veeam.com")
            .WithMetadata("source", "vendor-manifest")
            .WithTags("backup", "vendor", "repository")
            .AddIcon(new TopologyIconDefinition("veeam", "backup-server", "Backup Server", TopologyNodeKind.Server, TopologyIconShape.Server) {
                Symbol = "VBR",
                Color = "#00B336",
                Category = "Backup",
                DisplayMode = TopologyNodeDisplayMode.Tile
            }.WithMetadata("product", "Backup and Replication").WithTags("vbr", "backup-server"))
            .AddIcon(new TopologyIconDefinition("veeam", "repository", "Backup Repository", TopologyNodeKind.Storage, TopologyIconShape.Storage) {
                Symbol = "REPO",
                Color = "#007A5A",
                Category = "Storage",
                DisplayMode = TopologyNodeDisplayMode.Tile
            }.WithMetadata("product", "Backup and Replication").WithTags("repo", "object-storage"));

        var json = sourcePack.ToJsonManifest();
        Assert(json.Contains("\"schema\": \"chartforgex.topology.iconPack\"", StringComparison.Ordinal), "Icon pack manifests should include a stable schema id.");
        Assert(json.Contains("\"packVersion\": \"13\"", StringComparison.Ordinal), "Icon pack manifests should preserve vendor pack versions.");
        Assert(json.Contains("\"displayMode\": \"Tile\"", StringComparison.Ordinal), "Icon pack manifests should preserve icon display defaults.");

        var imported = TopologyIconPackJson.FromJson(json);
        Assert(imported.Id == "veeam" && imported.Label == "Veeam", "Imported icon pack identity should round-trip.");
        Assert(imported.Vendor == "Veeam" && imported.Version == "13", "Imported icon pack vendor metadata should round-trip.");
        Assert(imported.Tags.Contains("repository"), "Imported icon pack tags should round-trip.");
        Assert(imported.Metadata.TryGetValue("website", out var website) && website.Contains("veeam", StringComparison.OrdinalIgnoreCase), "Imported icon pack metadata should round-trip.");
        Assert(imported.Icons.Count == 2, "Imported icon packs should preserve all icons.");
        Assert(imported.Icons.Any(icon => icon.QualifiedId == "veeam:repository" && icon.DisplayMode == TopologyNodeDisplayMode.Tile && icon.Tags.Contains("object-storage")), "Imported icon metadata and tags should stay attached to the icon.");

        var catalog = TopologyIconCatalog.Default().AddJsonPack(json);
        Assert(catalog.Resolve("veeam:backup-server") != null, "Catalogs should resolve icon packs imported from JSON manifests.");
        Assert(catalog.Search(new TopologyIconCatalogQuery { SearchText = "object-storage" }).Any(item => item.QualifiedId == "veeam:repository"), "Catalog search should include tags from imported manifests.");
        Assert(catalog.Search(new TopologyIconCatalogQuery { SearchText = "vendor-manifest" }).Any(item => item.PackId == "veeam"), "Catalog search should include pack metadata from imported manifests.");

        var paletteOptions = new TopologyIconPaletteOptions {
            Id = "veeam-pack",
            Title = "Veeam Pack",
            IncludeBuiltInPacks = false
        };
        paletteOptions.Vendors.Add("Veeam");
        var palette = catalog.ToPaletteChart(paletteOptions);
        Assert(palette.Groups.Count == 1, "Imported manifest packs should render as picker palette groups.");
        Assert(palette.Nodes.Count == 2, "Imported manifest icons should render as picker palette nodes.");

        var renderOptions = new TopologyRenderOptions { IconCatalog = catalog, IncludeLegend = false };
        var svg = palette.ToSvg(renderOptions);
        Assert(svg.Contains("data-node-icon-id=\"veeam:repository\"", StringComparison.Ordinal), "Imported manifest icons should expose stable SVG icon ids.");
        Assert(svg.Contains("data-cfx-meta-pack-website=", StringComparison.Ordinal), "Imported manifest pack metadata should render into SVG data attributes.");
        Assert(svg.Contains("data-cfx-meta-icon-tags=", StringComparison.Ordinal), "Imported manifest icon tags should render into SVG data attributes.");
        Assert(palette.ToPng(renderOptions).Length > 64, "Imported manifest palette charts should render to PNG.");

        AssertThrows<ArgumentException>(() => TopologyIconPackJson.FromJson("{\"schema\":\"chartforgex.topology.iconPack\",\"id\":\"bad\",\"label\":\"Bad\",\"icons\":[{\"id\":\"x\",\"label\":\"X\",\"nodeKind\":\"MissingKind\"}]}"), "Icon pack manifests should reject unknown node kinds.");
    }

    private static void TopologyIconPackManifestsLoadFromFilesAndStreams() {
        var sourcePack = new TopologyIconPack("fortinet", "Fortinet", vendor: "Fortinet", version: "2026.1")
            .WithMetadata("website", "https://www.fortinet.com")
            .WithTags("network", "security", "vendor")
            .AddIcon(new TopologyIconDefinition("fortinet", "firewall", "FortiGate Firewall", TopologyNodeKind.Gateway, TopologyIconShape.Firewall) {
                Symbol = "FG",
                Color = "#DA291C",
                Category = "Security",
                DisplayMode = TopologyNodeDisplayMode.Tile
            }.WithTags("fortigate", "firewall"))
            .AddIcon(new TopologyIconDefinition("fortinet", "switch", "FortiSwitch", TopologyNodeKind.Network, TopologyIconShape.NetworkSwitch) {
                Symbol = "FSW",
                Color = "#DA291C",
                Category = "Network",
                DisplayMode = TopologyNodeDisplayMode.Tile
            }.WithTags("fortiswitch", "switch"));

        using var writer = new StringWriter();
        sourcePack.WriteJsonManifest(writer, indented: false);
        var compactJson = writer.ToString();
        Assert(compactJson.Contains("\"id\":\"fortinet\"", StringComparison.Ordinal), "TextWriter manifest output should support compact JSON.");

        using var reader = new StringReader(compactJson);
        var streamedPack = TopologyIconPackJson.LoadJsonManifest(reader);
        Assert(streamedPack.Id == "fortinet" && streamedPack.Icons.Count == 2, "TextReader manifest input should load complete icon packs.");

        var path = Path.Combine(Path.GetTempPath(), "chartforgex-topology-icon-pack-" + Guid.NewGuid().ToString("N") + ".json");
        try {
            sourcePack.SaveJsonManifest(path);
            var loadedPack = TopologyIconPackJson.LoadJsonManifest(path);
            Assert(loadedPack.Icons.Any(icon => icon.Id == "firewall"), "File manifest loading should preserve local icon ids.");
            Assert(loadedPack.Metadata.TryGetValue("manifest.fileName", out var fileName) && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase), "File manifest loading should expose the manifest file name.");
            Assert(loadedPack.Metadata.TryGetValue("manifest.path", out var sourcePath) && Path.GetFullPath(path).Equals(sourcePath, StringComparison.OrdinalIgnoreCase), "File manifest loading should expose the manifest source path.");
            Assert(!loadedPack.ToJsonManifest().Contains("manifest.path", StringComparison.Ordinal), "Manifest export should not persist runtime source-path metadata.");

            var catalog = TopologyIconCatalog.Default().AddJsonPackFile(path);
            Assert(catalog.Resolve("fortinet:firewall") != null, "Catalogs should add icon packs from manifest files.");
            Assert(catalog.Search(new TopologyIconCatalogQuery { SearchText = "fortigate" }).Any(item => item.QualifiedId == "fortinet:firewall"), "Catalog search should index tags loaded from manifest files.");
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }

        AssertThrows<ArgumentException>(() => TopologyIconPackJson.LoadJsonManifest(" "), "Manifest file loaders should reject empty paths.");
        AssertThrows<ArgumentNullException>(() => sourcePack.WriteJsonManifest(null!), "Manifest writer overloads should reject null writers.");
    }

    private static void TopologyIconPackManifestsLoadFromDirectories() {
        var directory = Path.Combine(Path.GetTempPath(), "chartforgex-topology-icon-packs-" + Guid.NewGuid().ToString("N"));
        var childDirectory = Path.Combine(directory, "security");
        Directory.CreateDirectory(childDirectory);
        try {
            new TopologyIconPack("acme-backup", "Acme Backup", vendor: "Acme", version: "1")
                .WithTags("backup", "vendor")
                .AddIcon("repository", "Backup Repository", TopologyNodeKind.Storage, TopologyIconShape.Storage, "REPO", "#007A5A", "Backup")
                .SaveJsonManifest(Path.Combine(directory, "02-acme-backup.json"));

            new TopologyIconPack("acme-network", "Acme Network", vendor: "Acme", version: "1")
                .WithTags("network", "vendor")
                .AddIcon("firewall", "Security Firewall", TopologyNodeKind.Gateway, TopologyIconShape.Firewall, "FW", "#DA291C", "Security")
                .SaveJsonManifest(Path.Combine(childDirectory, "01-acme-network.json"));

            var topOnly = TopologyIconPackJson.LoadJsonManifestsFromDirectory(directory);
            Assert(topOnly.Count == 1 && topOnly[0].Id == "acme-backup", "Directory manifest loading should default to the top directory only.");

            var allPacks = TopologyIconPackJson.LoadJsonManifestsFromDirectory(directory, recursive: true);
            Assert(allPacks.Count == 2, "Recursive directory manifest loading should include child vendor folders.");
            Assert(allPacks[0].Id == "acme-backup", "Directory manifest loading should use deterministic path order.");

            var catalog = TopologyIconCatalog.Default().AddJsonPacksFromDirectory(directory, recursive: true);
            Assert(catalog.Resolve("acme-network:firewall") != null, "Catalogs should add all icon packs from manifest directories.");
            Assert(catalog.GetVendors().Contains("Acme"), "Directory-loaded packs should participate in catalog facets.");
        } finally {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }

        AssertThrows<ArgumentException>(() => TopologyIconPackJson.LoadJsonManifestsFromDirectory(" "), "Directory manifest loaders should reject empty directory paths.");
        AssertThrows<ArgumentException>(() => TopologyIconPackJson.LoadJsonManifestsFromDirectory(Path.GetTempPath(), " "), "Directory manifest loaders should reject empty search patterns.");
    }

    private static void TopologyIconPackManifestResultsReportInvalidFiles() {
        var directory = Path.Combine(Path.GetTempPath(), "chartforgex-topology-icon-pack-results-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            new TopologyIconPack("valid-pack", "Valid Pack", vendor: "Acme", version: "1")
                .WithTags("valid", "vendor")
                .AddIcon("server", "Server", TopologyNodeKind.Server, TopologyIconShape.Server, "SRV", "#2563EB", "Compute")
                .SaveJsonManifest(Path.Combine(directory, "01-valid.json"));
            File.WriteAllText(Path.Combine(directory, "02-broken.json"), "{\"schema\":\"chartforgex.topology.iconPack\",\"id\":\"broken\",\"label\":\"Broken\",\"icons\":[{\"id\":\"bad\",\"label\":\"Bad\",\"nodeKind\":\"NoSuchKind\"}]}");

            var results = TopologyIconPackJson.LoadJsonManifestResultsFromDirectory(directory);
            Assert(results.Count == 2, "Manifest load reports should include every matching file.");
            var loadedPack = results[0].Pack;
            Assert(results[0].Succeeded && loadedPack != null && loadedPack.Id == "valid-pack", "Manifest load reports should include successful packs.");
            Assert(results[0].SourcePath.EndsWith("01-valid.json", StringComparison.OrdinalIgnoreCase), "Manifest load reports should expose source paths.");
            Assert(!results[1].Succeeded && results[1].Pack == null, "Manifest load reports should keep broken manifests as failures instead of throwing.");
            var errorMessage = results[1].ErrorMessage;
            Assert(errorMessage != null && errorMessage.Contains("NoSuchKind", StringComparison.Ordinal), "Manifest load reports should expose clear per-file errors.");
            Assert(results[1].Exception is ArgumentException, "Manifest load reports should retain the original exception for diagnostics.");

            var catalog = TopologyIconCatalog.Default();
            foreach (var result in results.Where(result => result.Succeeded)) catalog.AddPack(result.Pack!);
            Assert(catalog.Resolve("valid-pack:server") != null, "Hosts should be able to add successful packs from a mixed load report.");
            Assert(catalog.Resolve("broken:bad") == null, "Failed pack manifests should not be added by load-report consumers.");
        } finally {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void TopologyIconCatalogLoadResultsBuildCatalogsWithDiagnostics() {
        var directory = Path.Combine(Path.GetTempPath(), "chartforgex-topology-icon-catalog-results-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            new TopologyIconPack("acme-apps", "Acme Apps", vendor: "Acme", version: "1")
                .WithTags("application", "vendor")
                .AddIcon("portal", "Portal", TopologyNodeKind.Application, TopologyIconShape.Application, "APP", "#7C3AED", "Application")
                .SaveJsonManifest(Path.Combine(directory, "01-acme-apps.json"));
            new TopologyIconPack("common", "Duplicate Common", vendor: "Acme", version: "1")
                .AddIcon("duplicate", "Duplicate", TopologyNodeKind.Server, TopologyIconShape.Server, "DUP", "#2563EB", "Compute")
                .SaveJsonManifest(Path.Combine(directory, "02-duplicate-common.json"));
            File.WriteAllText(Path.Combine(directory, "03-broken.json"), "{\"schema\":\"chartforgex.topology.iconPack\",\"id\":\"broken\",\"label\":\"Broken\",\"icons\":[{\"id\":\"bad\",\"label\":\"Bad\",\"nodeKind\":\"NoSuchKind\"}]}");

            var withBuiltIns = TopologyIconPackJson.LoadJsonCatalogFromDirectory(directory);
            Assert(withBuiltIns.Catalog.Resolve("common:server") != null, "Catalog load results should include built-in packs by default.");
            Assert(withBuiltIns.Catalog.Resolve("acme-apps:portal") != null, "Catalog load results should add successful manifest packs.");
            Assert(withBuiltIns.LoadedCount == 1, "Catalog load results should count successfully added manifest packs.");
            Assert(withBuiltIns.FailedCount == 2 && withBuiltIns.HasErrors, "Catalog load results should report broken or duplicate manifest files.");
            Assert(withBuiltIns.FailedResults.Any(result => result.FileName == "02-duplicate-common.json" && result.ErrorMessage != null && result.ErrorMessage.Contains("already registered", StringComparison.OrdinalIgnoreCase)), "Catalog load results should report duplicate pack ids as add failures.");
            Assert(withBuiltIns.FailedResults.Any(result => result.FileName == "03-broken.json" && result.ErrorMessage != null && result.ErrorMessage.Contains("NoSuchKind", StringComparison.Ordinal)), "Catalog load results should preserve parse failures.");

            var customOnly = TopologyIconPackJson.LoadJsonCatalogFromDirectory(directory, includeBuiltInPacks: false);
            Assert(customOnly.Catalog.Resolve("common:duplicate") != null, "Catalog load results should allow custom-only catalogs without built-in duplicate conflicts.");
            Assert(customOnly.Catalog.Resolve("common:server") == null, "Custom-only catalog load results should omit built-in packs.");
            Assert(customOnly.LoadedCount == 2 && customOnly.FailedCount == 1, "Custom-only catalog load results should count successful custom packs separately from parse failures.");
            Assert(customOnly.LoadedPacks.Any(pack => pack.Id == "common"), "Loaded pack summaries should include successfully added duplicate-id packs when no built-in conflict exists.");
        } finally {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void TopologyIconCatalogLoadOptionsControlDuplicatePacks() {
        var directory = Path.Combine(Path.GetTempPath(), "chartforgex-topology-icon-catalog-conflicts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            new TopologyIconPack("common", "Custom Common", vendor: "Acme", version: "1")
                .AddIcon("custom-server", "Custom Server", TopologyNodeKind.Server, TopologyIconShape.Server, "CSR", "#2563EB", "Compute")
                .SaveJsonManifest(Path.Combine(directory, "01-custom-common.json"));
            new TopologyIconPack("acme-network", "Acme Network", vendor: "Acme", version: "1")
                .AddIcon("router", "Router", TopologyNodeKind.Gateway, TopologyIconShape.Router, "RTR", "#2563EB", "Network")
                .SaveJsonManifest(Path.Combine(directory, "02-acme-network.json"));

            var skip = TopologyIconPackJson.LoadJsonCatalogFromDirectory(directory, new TopologyIconCatalogLoadOptions {
                ConflictBehavior = TopologyIconPackConflictBehavior.Skip
            });
            Assert(skip.LoadedCount == 1 && skip.SkippedCount == 1 && !skip.HasErrors && skip.HasSkipped, "Skip conflict behavior should keep loading without reporting duplicate pack errors.");
            var skipReason = skip.SkippedResults[0].SkipReason;
            Assert(skipReason != null && skipReason.Contains("common", StringComparison.OrdinalIgnoreCase), "Skipped duplicate packs should expose a skip reason.");
            Assert(skip.Catalog.Resolve("common:server") != null, "Skip conflict behavior should keep the existing built-in pack.");
            Assert(skip.Catalog.Resolve("common:custom-server") == null, "Skip conflict behavior should not add duplicate manifest icons.");
            Assert(skip.Catalog.Resolve("acme-network:router") != null, "Skip conflict behavior should still add non-conflicting packs.");

            var replace = TopologyIconPackJson.LoadJsonCatalogFromDirectory(directory, new TopologyIconCatalogLoadOptions {
                ConflictBehavior = TopologyIconPackConflictBehavior.Replace
            });
            Assert(replace.LoadedCount == 2 && replace.SkippedCount == 0 && !replace.HasErrors, "Replace conflict behavior should count duplicate manifests as loaded.");
            Assert(replace.Catalog.Resolve("common:custom-server") != null, "Replace conflict behavior should replace existing packs with manifest packs.");
            Assert(replace.Catalog.Resolve("common:server") == null, "Replace conflict behavior should remove replaced built-in icons.");

            var strict = TopologyIconPackJson.LoadJsonCatalogFromDirectory(directory);
            Assert(strict.LoadedCount == 1 && strict.FailedCount == 1 && strict.HasErrors, "Default conflict behavior should report duplicate pack ids as failures.");
        } finally {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void TopologyIconCatalogExportsVendorManifests() {
        var directory = Path.Combine(Path.GetTempPath(), "chartforgex-topology-icon-catalog-export-" + Guid.NewGuid().ToString("N"));
        try {
            var catalog = TopologyIconCatalog.Default()
                .AddPack(new TopologyIconPack("veeam", "Veeam", vendor: "Veeam", version: "13")
                    .WithTags("backup", "vendor")
                    .AddIcon("repository", "Backup Repository", TopologyNodeKind.Storage, TopologyIconShape.Storage, "REPO", "#007A5A", "Storage"))
                .AddPack(new TopologyIconPack("fortinet", "Fortinet", vendor: "Fortinet", version: "2026.1")
                    .WithTags("security", "vendor")
                    .AddIcon("firewall", "FortiGate Firewall", TopologyNodeKind.Gateway, TopologyIconShape.Firewall, "FG", "#DA291C", "Security"));

            var exported = catalog.SaveJsonManifestsToDirectory(directory);
            Assert(exported.Count == 2, "Catalog export should default to custom/vendor packs only.");
            Assert(exported.Any(path => Path.GetFileName(path) == "topology-icon-pack.veeam.json"), "Catalog export should use deterministic pack file names.");
            Assert(exported.All(File.Exists), "Catalog export should create manifest files.");
            Assert(!File.Exists(Path.Combine(directory, "topology-icon-pack.common.json")), "Catalog export should skip built-in packs by default.");

            var loaded = TopologyIconPackJson.LoadJsonCatalogFromDirectory(directory, includeBuiltInPacks: false);
            Assert(loaded.LoadedCount == 2 && !loaded.HasErrors, "Exported catalog manifests should load back cleanly.");
            Assert(loaded.Catalog.Resolve("veeam:repository") != null, "Exported and reloaded catalogs should preserve vendor icons.");

            AssertThrows<IOException>(() => catalog.SaveJsonManifestsToDirectory(directory, new TopologyIconCatalogExportOptions { Overwrite = false }), "Catalog export should reject existing files when overwrite is disabled.");

            var filteredDirectory = Path.Combine(directory, "filtered");
            var query = new TopologyIconCatalogQuery { IncludeBuiltInPacks = false };
            query.Vendors.Add("Fortinet");
            var filtered = catalog.SaveJsonManifestsToDirectory(filteredDirectory, new TopologyIconCatalogExportOptions {
                Query = query,
                FileNamePrefix = "vendor.",
                FileExtension = "manifest.json",
                Indented = false
            });
            Assert(filtered.Count == 1 && Path.GetFileName(filtered[0]) == "vendor.fortinet.manifest.json", "Catalog export should honor query and file-name options.");
            Assert(!File.ReadAllText(filtered[0]).Contains(Environment.NewLine, StringComparison.Ordinal), "Catalog export should honor compact JSON output.");

            var withBuiltInsDirectory = Path.Combine(directory, "builtins");
            var withBuiltIns = TopologyIconCatalog.Default().SaveJsonManifestsToDirectory(withBuiltInsDirectory, new TopologyIconCatalogExportOptions { IncludeBuiltInPacks = true });
            Assert(withBuiltIns.Any(path => Path.GetFileName(path) == "topology-icon-pack.common.json"), "Catalog export should include built-in packs when requested.");
        } finally {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void TopologyIconPackValidationReportsAuthoringIssues() {
        var pack = new TopologyIconPack("acme", "Acme")
            .WithTags("network", "vendor");
        pack.Tags.Add("network");
        pack.AddIcon(new TopologyIconDefinition("acme", "server", "Server", TopologyNodeKind.Server, TopologyIconShape.Server) {
            Symbol = "SERVER-LONG",
            Color = "acme-blue",
            Category = "Compute"
        });
        var duplicate = new TopologyIconDefinition("acme", "repository", "Repository", TopologyNodeKind.Storage, TopologyIconShape.Storage) {
            Color = "#007A5A",
            Category = "Storage"
        };
        pack.AddIcon(duplicate);
        duplicate.Id = "server";
        duplicate.PackId = "other";

        var result = pack.Validate();
        Assert(!result.IsValid, "Pack validation should report blocking icon authoring issues.");
        Assert(result.Errors.Any(issue => issue.Message.Contains("duplicated", StringComparison.OrdinalIgnoreCase)), "Pack validation should report duplicate icon ids.");
        Assert(result.Errors.Any(issue => issue.Path.Contains(".packId", StringComparison.Ordinal)), "Pack validation should report icons whose pack id no longer matches the containing pack.");
        Assert(result.Warnings.Any(issue => issue.Message.Contains("vendor", StringComparison.OrdinalIgnoreCase)), "Pack validation should warn when custom packs omit vendor labels.");
        Assert(result.Warnings.Any(issue => issue.Path.Contains(".color", StringComparison.Ordinal)), "Pack validation should warn about non-portable icon colors.");
        Assert(result.Warnings.Any(issue => issue.Path.Contains(".symbol", StringComparison.Ordinal)), "Pack validation should warn about long fallback symbols.");
        Assert(result.Warnings.Any(issue => issue.Message.Contains("Tag", StringComparison.Ordinal)), "Pack validation should warn about duplicate tags.");

        var catalog = new TopologyIconCatalog()
            .AddPack(new TopologyIconPack("one", "One").AddIcon("server", "Server", TopologyNodeKind.Server))
            .AddPack(new TopologyIconPack("two", "Two").AddIcon("storage", "Storage", TopologyNodeKind.Storage));
        catalog.Packs[1].Id = "one";
        var catalogResult = catalog.Validate();
        Assert(!catalogResult.IsValid, "Catalog validation should report duplicate pack ids even after pack mutation.");
        Assert(catalogResult.CatalogIssues.Any(issue => issue.Path.Contains("catalog.packs", StringComparison.Ordinal)), "Catalog validation should expose catalog-level issue paths.");
        Assert(TopologyIconCatalog.Default().Validate().IsValid, "Built-in topology icon packs should validate cleanly.");
    }

    private static void TopologyIconCatalogSummariesGroupPickerFolders() {
        var catalog = TopologyIconCatalog.Default()
            .AddPack(new TopologyIconPack("veeam", "Veeam", vendor: "Veeam", version: "13")
                .WithTags("backup", "vendor")
                .AddIcon(new TopologyIconDefinition("veeam", "repository", "Backup Repository", TopologyNodeKind.Storage, TopologyIconShape.Storage) {
                    Symbol = "REPO",
                    Color = "#007A5A",
                    Category = "Storage"
                }.WithTags("repo", "repository"))
                .AddIcon(new TopologyIconDefinition("veeam", "proxy", "Backup Proxy", TopologyNodeKind.Server, TopologyIconShape.Server) {
                    Symbol = "PRX",
                    Color = "#00B336",
                    Category = "Backup"
                }.WithTags("proxy")))
            .AddPack(new TopologyIconPack("fortinet", "Fortinet", vendor: "Fortinet", version: "2026.1")
                .WithTags("network", "security", "vendor")
                .AddIcon(new TopologyIconDefinition("fortinet", "firewall", "FortiGate Firewall", TopologyNodeKind.Gateway, TopologyIconShape.Firewall) {
                    Symbol = "FG",
                    Color = "#DA291C",
                    Category = "Security"
                }.WithTags("firewall", "fortigate")));

        var packSummaries = catalog.GetPackSummaries();
        var veeam = packSummaries.First(summary => summary.PackId == "veeam");
        Assert(veeam.IconCount == 2, "Pack summaries should report matching icon counts.");
        Assert(veeam.Categories.Contains("Backup") && veeam.Categories.Contains("Storage"), "Pack summaries should expose matching categories.");
        Assert(veeam.Tags.Contains("repository"), "Pack summaries should merge pack and icon tags.");

        var customSummaries = catalog.GetPackSummaries(new TopologyIconCatalogQuery { IncludeBuiltInPacks = false });
        Assert(customSummaries.Count == 2 && customSummaries.All(summary => !summary.IsBuiltIn), "Pack summaries should honor built-in/custom filters.");

        var storageQuery = new TopologyIconCatalogQuery { IncludeBuiltInPacks = false };
        storageQuery.Categories.Add("Storage");
        var storageSummaries = catalog.GetPackSummaries(storageQuery);
        Assert(storageSummaries.Count == 1 && storageSummaries[0].PackId == "veeam" && storageSummaries[0].IconCount == 1, "Pack summaries should count only matching icons.");

        var vendorFolders = catalog.GetVendorSummaries(new TopologyIconCatalogQuery { IncludeBuiltInPacks = false });
        Assert(vendorFolders.Count == 2, "Vendor summaries should produce one picker folder per vendor.");
        Assert(vendorFolders.Any(summary => summary.Label == "Fortinet" && summary.PackIds.Contains("fortinet") && summary.Categories.Contains("Security")), "Vendor summaries should expose vendor pack ids and categories.");
        Assert(vendorFolders.Any(summary => summary.Label == "Veeam" && summary.IconCount == 2 && summary.Tags.Contains("repo")), "Vendor summaries should expose matching icon counts and tags.");

        var builtInFolders = TopologyIconCatalog.Default().GetVendorSummaries();
        Assert(builtInFolders.Any(summary => summary.Label == "Microsoft" && summary.HasBuiltInPacks), "Vendor summaries should keep vendor labels for built-in vendor packs.");
        Assert(builtInFolders.Any(summary => summary.Label == "ChartForgeX" && summary.HasBuiltInPacks), "Vendor summaries should group generic built-in packs under ChartForgeX.");
    }
}
