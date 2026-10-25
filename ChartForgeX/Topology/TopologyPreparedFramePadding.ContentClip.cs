using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

/// <summary>An immutable observation of the existing final clip, only when natural layout extended its canonical plot.</summary>
internal sealed class TopologyPreparedContentClip {
    internal TopologyPreparedContentClip(ChartRect plot, ChartRect clip) { Plot = plot; Clip = clip; }
    internal ChartRect Plot { get; }
    internal ChartRect Clip { get; }
    internal static TopologyPreparedContentClip? Capture(ChartRect plot, ChartRect clip) => Same(plot, clip) ? null : new(plot, clip);
    internal static bool Same(ChartRect a, ChartRect b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
    internal void Validate(double width, double height) {
        Bounds(Plot); Bounds(Clip);
        if (Plot.X < -.001 || Plot.Y < -.001 || Clip.X < -.001 || Clip.Y < -.001 || Plot.Right > width + .001 || Clip.Right > width + .001
            || Plot.Bottom > height + .001 || Clip.Bottom > height + .001 || Clip.Top != Plot.Top || Clip.Bottom != Plot.Bottom
            || Clip.Left > Plot.Left || Clip.Right < Plot.Right)
            throw new ArgumentException("Captured topology content clips require an in-canvas horizontal extension of their original plot.");
        static void Bounds(ChartRect r) { foreach (var value in new[] { r.X, r.Y, r.Width, r.Height, r.Right, r.Bottom })
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > TopologyPathGeometry.MaximumCoordinate)
                throw new ArgumentException("Captured topology clip coordinates must be finite and bounded.");
            if (r.Width <= 0 || r.Height <= 0) throw new ArgumentException("Captured topology clips require positive extents."); }
    }
}

internal sealed partial class TopologyPreparedFramePadding {
    private FrameWitness? _clipWitness;
    // Attach only to a newly detached restored chart. The immutable wire observation is shared; the witness stores no graph.
    internal static void AttachPreparedFrame(TopologyChart chart) {
        var fact = Resolve(chart); if (fact == null || fact.ContentClip == null && !chart.Annotations.Exists(note => note.PreparedDefaultUnits != 1)) return;
        var copy = new TopologyPreparedFramePadding(fact.Layout, fact.ContentClip);
        var options = chart.ResolveRenderOptions(null);
        copy._clipWitness = new FrameWitness(chart, options, ResolveContext(chart, options));
        chart.PreparedFramePadding = copy;
    }
    internal static void ApplyAnnotationLifecycle(TopologyChart chart, TopologyRenderOptions options, VisualRenderContext context) {
        if (!chart.Annotations.Exists(note => note.PreparedDefaultUnits != 1)) return;
        var fact = ForRequest(chart);
        if (options.FitContentToViewport || fact?._clipWitness == null || !fact._clipWitness.Matches(chart, options, context))
            foreach (var note in chart.Annotations) { note.PreparedDefaultUnits = 1; note.PreparedRoutingViewport = null; }
    }
    internal static TopologyPreparedContentClip? WireContentClip(TopologyChart chart) => Resolve(chart)?.ContentClip;
    internal static ChartRect? ResolveContentClip(TopologyChart chart, TopologyRenderOptions options, VisualRenderContext context, ChartRect plot) {
        var fact = ForRequest(chart); var clip = fact?.ContentClip;
        return clip != null && fact!._clipWitness is {} witness && witness.Matches(chart, options, context)
            && TopologyPreparedContentClip.Same(clip.Plot, plot) ? clip.Clip : (ChartRect?)null;
    }
    internal static VisualRenderContext ResolveContext(TopologyChart chart, TopologyRenderOptions options) {
        var context = VisualTopologyCompiler.ResolveFrame(VisualExportRequest.ForTopology(chart, options).Context, chart, options);
        return VisualTopologyCompiler.ResolveLegendFrame(context, options.IncludeLegend ? TopologyLegend.Resolve(chart, options.LegendMode) : null);
    }
    internal sealed class FrameWitness {
        private readonly VisualRenderContext _context; // Immutable frame/font request only; no source graph or callbacks.
        private readonly string? _title, _subtitle;
        private readonly VisualTheme _theme;
        private readonly VisualThemeMode _mode;
        private readonly string?[] _legendLabels;
        internal FrameWitness(TopologyChart chart, TopologyRenderOptions options, VisualRenderContext context) {
            _context = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, context.Frame.RequestValues(), context.Font.RequestValues()); _title = chart.Title; _subtitle = chart.Subtitle; _theme = chart.Theme; _mode = chart.ThemeMode;
            _legendLabels = Labels(chart, options);
        }
        private static string?[] Labels(TopologyChart chart, TopologyRenderOptions options) {
            var legend = options.IncludeLegend ? TopologyLegend.Resolve(chart, options.LegendMode) : null;
            return legend == null ? Array.Empty<string?>() : new[] { legend.Title }.Concat(legend.Items.Select(item => (string?)item.Label)).ToArray();
        }
        internal bool Matches(TopologyChart chart, TopologyRenderOptions options, VisualRenderContext context) {
            var a = context.Layout; var b = _context.Layout;
            return chart.Title == _title && chart.Subtitle == _subtitle && ReferenceEquals(chart.Theme, _theme) && chart.ThemeMode == _mode
                && ReferenceEquals(context.Theme, _context.Theme) && context.ThemeMode == _context.ThemeMode
                && a.Size.Width == b.Size.Width && a.Size.Height == b.Size.Height
                && a.PaddingEdges.Left == b.PaddingEdges.Left && a.PaddingEdges.Top == b.PaddingEdges.Top
                && a.PaddingEdges.Right == b.PaddingEdges.Right && a.PaddingEdges.Bottom == b.PaddingEdges.Bottom
                && context.Font.SameRequest(_context.Font) && context.Frame.SameRequest(_context.Frame)
                && Labels(chart, options).SequenceEqual(_legendLabels);
        }
    }
}
