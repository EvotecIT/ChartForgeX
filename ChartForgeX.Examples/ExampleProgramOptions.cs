using ChartForgeX.Core;

internal static class ExampleProgramOptions {
    public static string OutputDirectory(string[] args) {
        var index = Array.FindIndex(args, arg => string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase));
        if (index < 0) return Path.Combine(AppContext.BaseDirectory, HasArg(args, "--v2-only") ? "output-v2" : "output");
        if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1])) throw new ArgumentException("--output requires a directory.");
        return Path.GetFullPath(args[index + 1]);
    }
    public static bool HasArg(string[] args, string name) =>
        args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));

    public static bool TryHandle(string[] args, string output, ChartPngOutputScale pngOutputScale) {
        if (HasArg(args, "--wallpaper-only")) {
            WallpaperOwnerExamples.Write(output);
            Console.WriteLine("Generated wallpaper examples in: " + output);
            return true;
        }
        if (HasArg(args, "--v2-only")) {
            V2Examples.Write(output, HasArg(args, "--v2-curated"));
            Console.WriteLine("Generated direct-scene proof in: " + output);
            return true;
        }
        if (HasArg(args, "--mermaid-only")) {
            MermaidExamples.Write(output);
            GalleryWriter.Write(output);
            return true;
        }

        if (HasArg(args, "--graphite-only")) { GraphiteExamples.Write(output, pngOutputScale); GalleryWriter.Write(output); return true; }
        if (HasArg(args, "--dense-legends-only")) {
            DenseLegendExamples.Write(output, pngOutputScale);
            return true;
        }

        if (HasArg(args, "--paged-facets-only")) {
            PagedFacetExamples.Write(output, pngOutputScale);
            return true;
        }

        if (HasArg(args, "--dense-signals-only")) {
            DenseSignalExamples.Write(output, pngOutputScale);
            Console.WriteLine("Generated dense-signal files in: " + output);
            return true;
        }

        if (HasArg(args, "--reporting-only")) {
            ReportingExamples.Write(output, pngOutputScale);
            Console.WriteLine("Generated reporting files in: " + output);
            return true;
        }

        if (HasArg(args, "--readable-topology-only")) {
            ReadableTopologyExamples.Write(output);
            return true;
        }

        if (HasArg(args, "--visual-story-only")) {
            VisualStoryExamples.Write(output);
            Console.WriteLine("Generated visual-story files in: " + output);
            return true;
        }

        if (HasArg(args, "--expressive-only")) {
            ExpressiveExamples.Write(output, pngOutputScale);
            GalleryWriter.Write(output);
            Console.WriteLine("Generated expressive files in: " + output);
            return true;
        }

        if (HasArg(args, "--topology-typography-only")) {
            TopologyTypographyExamples.Write(output);
            Console.WriteLine("Generated topology typography examples in: " + output);
            return true;
        }

        if (HasArg(args, "--topology-only")) {
            TopologyExamples.Write(output);
            Console.WriteLine("Generated topology files in: " + Path.Combine(output, "topology-demo"));
            return true;
        }

        if (HasArg(args, "--force-graph-only")) {
            TopologyVisualExamples.WriteForceGraph(output);
            Console.WriteLine("Generated force graph files in: " + output);
            return true;
        }

        if (HasArg(args, "--graph-neighborhood-only")) {
            GraphNeighborhoodExample.Write(output);
            Console.WriteLine("Generated bounded graph neighborhoods in: " + output);
            return true;
        }

        if (HasArg(args, "--graph-rich-rendering-only")) {
            GraphRichRenderingExample.Write(output);
            Console.WriteLine("Generated rich graph renderer examples in: " + output);
            return true;
        }

        if (HasArg(args, "--graph-scale-only")) {
            GraphExplorerScaleExamples.Write(output);
            Console.WriteLine("Generated graph explorer scale baselines in: " + output);
            return true;
        }

        if (HasArg(args, "--graph-explorer-only")) {
            GraphExplorerExamples.Write(output);
            Console.WriteLine("Generated graph explorer examples in: " + output);
            return true;
        }

        if (HasArg(args, "--dashboard-patterns-only")) {
            DashboardPatternExamples.Write(output, pngOutputScale);
            GalleryWriter.Write(output);
            Console.WriteLine("Generated dashboard pattern files in: " + output);
            return true;
        }

        if (HasArg(args, "--wellness-only")) {
            WellnessDashboardExamples.Write(output, pngOutputScale);
            GalleryWriter.Write(output);
            Console.WriteLine("Generated wellness files in: " + output);
            return true;
        }

        if (HasArg(args, "--visual-canvas-only")) {
            WellnessDashboardExamples.WritePowerBgInfoSocialPreview(output, (int)pngOutputScale);
            Console.WriteLine("Generated visual canvas files in: " + output);
            return true;
        }

        if (HasArg(args, "--dashboard-shipment-only")) {
            DashboardPatternExamples.WriteShipmentActivityPanel(output, pngOutputScale);
            Console.WriteLine("Generated dashboard shipment panel in: " + output);
            return true;
        }

        if (HasArg(args, "--dashboard-project-progress-only")) {
            DashboardPatternExamples.WriteProjectProgressCard(output, pngOutputScale);
            Console.WriteLine("Generated dashboard project progress card in: " + output);
            return true;
        }

        return false;
    }
}
