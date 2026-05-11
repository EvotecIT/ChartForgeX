using ChartForgeX.Core;

internal static class ExampleProgramOptions {
    public static bool HasArg(string[] args, string name) =>
        args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));

    public static bool TryHandle(string[] args, string output, ChartPngOutputScale pngOutputScale) {
        if (HasArg(args, "--expressive-only")) {
            ExpressiveExamples.Write(output, pngOutputScale);
            GalleryWriter.Write(output);
            Console.WriteLine("Generated expressive files in: " + output);
            return true;
        }

        if (HasArg(args, "--topology-only")) {
            TopologyExamples.Write(output);
            Console.WriteLine("Generated topology files in: " + Path.Combine(output, "topology-demo"));
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
