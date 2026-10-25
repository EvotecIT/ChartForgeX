using System;
using ChartForgeX.Accessibility;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class FlowArtifact {
    internal FlowPreparedPresentation? PreparedPresentation { get; set; }
}

/// <summary>One immutable effective appearance and optional exact layout, guarded only by actual Flow request inputs.</summary>
internal sealed class FlowPreparedPresentation {
    private readonly double _width, _height, _padding;
    private readonly VisualLayoutOptions _layout;
    private readonly VisualAccessibility? _accessibility;
    internal FlowPreparedPresentation(FlowArtifact flow, VisualLayoutOptions layout, VisualTheme theme, VisualThemeMode mode, PreparedDiagramFont? font, VisualFrame? frame, VisualAccessibility? accessibility = null) {
        _width = flow.Width; _height = flow.Height; _padding = flow.Padding; _layout = layout;
        Theme = theme; Mode = mode; Font = font; Frame = frame?.RequestValues(retainOwnedFonts: true);
        _accessibility = accessibility?.Clone();
    }
    internal VisualTheme Theme { get; }
    internal VisualThemeMode Mode { get; }
    internal PreparedDiagramFont? Font { get; }
    internal VisualFrame? Frame { get; }
    internal void ApplyAccessibility(VisualAccessibility target) {
        if (_accessibility == null) return;
        target.Name = _accessibility.Name; target.Description = _accessibility.Description;
        target.Language = _accessibility.Language; target.IsDecorative = _accessibility.IsDecorative;
    }
    internal VisualRenderContext Request(FlowArtifact flow) {
        var layout = flow.Width == _width && flow.Height == _height && flow.Padding == _padding
            ? _layout : new VisualLayoutOptions(new VisualSize(flow.Width, flow.Height), flow.Padding);
        return new VisualRenderContext(layout, Theme, Mode, Frame ?? new VisualFrame(showLegend: false,
            transparentBackground: Theme.Resolve(Mode).Background.A == 0), Font?.ForPreparation() ?? Theme.Typography.ToFontSpec());
    }
}
