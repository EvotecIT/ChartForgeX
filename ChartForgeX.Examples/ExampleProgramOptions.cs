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
        if (HasArg(args, "--swimlane-layout-depth-only")) { SwimlaneLayoutDepthExamples.Write(output); return true; }
        if (HasArg(args, "--topology-crossings-only")) { TopologyCrossingPaintExamples.Write(output); return true; }
        if (HasArg(args, "--native-value-number-culture-case-only")) { NativeNumberCultureCaseExamples.Write(output, args); return true; }
        if (HasArg(args, "--native-joint-values-visibility-only")) { NativeJointValuesVisibilityExamples.Write(output,args); return true; }
        if (HasArg(args, "--native-scatter-values-only")) { NativeScatterValueExamples.Write(output,args); return true; }
        if (HasArg(args, "--native-complete-cartesian-values-only")) { NativeCompleteCartesianValueExamples.Write(output,args); return true; }
        if (HasArg(args, "--native-joint-stack-values-only")) { NativeJointStackValueExamples.Write(output,args); return true; }
        if (HasArg(args, "--native-owned-flash-only")) { NativeOwnedValueFlashExamples.Write(output,args); return true; }
        if (HasArg(args, "--native-raw-values-only")) { NativeRawValueExamples.Write(output); return true; }
        if (HasArg(args, "--native-richer-number-only")) { NativeNumberCultureCaseExamples.Write(output, args, richer: true); return true; }
        if (HasArg(args, "--native-value-text-repertoire-only")) { NativeFallbackRadialExamples.Write(output, args, numericValueText: true); return true; }
        if (HasArg(args, "--native-fallback-radial-only")) { NativeFallbackRadialExamples.Write(output, args); return true; }
        if (HasArg(args, "--native-hierarchy-motion-only")) { NativeHierarchyMotionExamples.Write(output, args); return true; }
        if (HasArg(args, "--native-radial-motion-only")) { NativeRadialMotionExamples.Write(output); return true; }
        if (HasArg(args, "--native-radial-bands-only")) { NativeRadialBandMotionExamples.Write(output); return true; }
        if (HasArg(args, "--native-reflow-only")) { NativeReflowExamples.Write(output); return true; }
        if (HasArg(args, "--native-cartesian-values-only")) { NativeCartesianValueExamples.Write(output); return true; }
        if (HasArg(args, "--native-initial-fade-only")) { NativeInitialFadeExamples.Write(output); return true; }
        if (HasArg(args, "--native-transition-only")) { NativeTransitionExamples.Write(output); return true; }
        if (HasArg(args, "--native-preparation-backpressure-only")) { NativePreparationBackpressureExamples.Write(output); return true; }
        if (HasArg(args, "--polar-viewport-only")) {
            PolarViewportExamples.Write(output);
            return true;
        }
        if (HasArg(args, "--radial-forms-only")) {
            RadialFormsExamples.Write(output, pngOutputScale);
            return true;
        }
        if (HasArg(args, "--integer-axis-only")) {
            IntegerAxisExamples.Write(output, pngOutputScale);
            return true;
        }
        if (HasArg(args, "--journey-timeline-interchange-only")) {
            JourneyTimelineInterchangeExamples.Write(output);
            Console.WriteLine("Generated editable categorical interchange examples in: " + output);
            return true;
        }
        if (HasArg(args, "--svg-gaussian-import-only")) {
            SvgGaussianImportExamples.Write(output);
            Console.WriteLine("Generated explicit Gaussian import examples in: " + output);
            return true;
        }
        if (HasArg(args, "--composition-effects-only")) {
            CompositionEffectsExamples.Write(output);
            Console.WriteLine("Generated baked composition effect examples in: " + output);
            return true;
        }
        if (HasArg(args, "--composition-only")) {
            CompositionShapeExamples.Write(output);
            Console.WriteLine("Generated image composition examples in: " + output);
            return true;
        }
        if (HasArg(args, "--numeric-axis-only")) {
            NumericAxisExamples.Write(output, pngOutputScale);
            return true;
        }
        if (HasArg(args, "--grouped-categories-only")) {
            CartesianCategoryExamples.WriteGrouped(output, pngOutputScale);
            Console.WriteLine("Generated grouped category examples in: " + output);
            return true;
        }
        if (HasArg(args, "--tile-typography-only")) {
            VisualCanvasTypographyExamples.Write(output);
            GalleryWriter.Write(output);
            Console.WriteLine("Generated tile typography examples in: " + output);
            return true;
        }
        if (HasArg(args, "--consumer-rendering-only")) { ConsumerRenderingExamples.Write(output); return true; }
        if (HasArg(args, "--story-appearance-only")) {
            StoryAppearanceExamples.Write(output);
            Console.WriteLine("Generated desktop story examples in: " + output);
            return true;
        }
        if (HasArg(args, "--story-replay-only")) {
            StoryReplayExamples.Write(output);
            Console.WriteLine("Generated recorded replay examples in: " + output);
            return true;
        }
        if (HasArg(args, "--story-playback-only")) {
            StoryPlaybackExamples.Write(output);
            Console.WriteLine("Generated story playback examples in: " + output);
            return true;
        }
        if (HasArg(args, "--hierarchy-view-only")) {
            HierarchyViewExamples.Write(output);
            GalleryWriter.Write(output);
            Console.WriteLine("Generated native hierarchy view examples in: " + output);
            return true;
        }
        if (HasArg(args, "--sunburst-highlight-only")) {
            SunburstBranchHighlightExamples.Write(output);
            Console.WriteLine("Generated Sunburst highlight examples in: " + output);
            return true;
        }
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
        if (HasArg(args, "--tile-typography-proof-only")) {
            VisualCanvasTypographyExamples.WriteProofMatrix(output);
            Console.WriteLine("Generated tile typography proof matrix in: " + output);
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
