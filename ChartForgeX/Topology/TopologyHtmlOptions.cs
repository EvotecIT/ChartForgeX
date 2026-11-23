using System;
using PowerForge.Fluent;

namespace ChartForgeX.Topology;

/// <summary>Interactive HTML page controls, panels and content security settings. Configure it through <see cref="TopologyRenderOptions.Html"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class TopologyHtmlOptions {
    internal TopologyHtmlOptions Clone() => (TopologyHtmlOptions)MemberwiseClone();
    /// <summary>Gets or sets whether complete HTML pages should include lightweight selection interactions. Defaults to static, script-free HTML.</summary>
    public bool Interactions { get; set; }


    /// <summary>Gets or sets whether interactive HTML pages should include lightweight zoom and pan controls.</summary>
    public bool ViewportControls { get; set; }


    private double _htmlNarrowMinimumHeight;


    /// <summary>Gets or sets the minimum drawing height in pixels for interactive viewports in containers up to 640 pixels wide. Zero preserves proportional sizing.</summary>
    public double NarrowMinimumHeight {
        get => _htmlNarrowMinimumHeight;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _htmlNarrowMinimumHeight = value;
        }
    }


    /// <summary>Gets or sets whether interactive HTML pages should include lightweight SVG and PNG export controls.</summary>
    public bool ExportControls { get; set; }


    /// <summary>Gets or sets whether force-directed HTML pages should include graph exploration controls.</summary>
    public bool ForceGraphControls { get; set; }


    /// <summary>Gets or sets whether interactive HTML pages render a find bar that highlights nodes by text, group or kind and moves the viewport to them.</summary>
    /// <remarks>Force graph controls already carry their own search, so force-directed pages with those controls keep that panel instead.</remarks>
    public bool FindControls { get; set; }


    /// <summary>Gets or sets whether interactive HTML pages should synchronize selection and viewport state with wrappers in the same sync group.</summary>
    public bool SynchronizedState { get; set; }


    /// <summary>Gets or sets whether interactive HTML pages should render scenario picker controls when scenarios are present.</summary>
    public bool ScenarioControls { get; set; } = true;


    /// <summary>Gets or sets how interactive HTML pages should render scenario controls.</summary>
    public TopologyHtmlScenarioControlMode ScenarioControlMode {
        get => _htmlScenarioControlMode;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _htmlScenarioControlMode = value;
        }
    }

    private TopologyHtmlScenarioControlMode _htmlScenarioControlMode = TopologyHtmlScenarioControlMode.Buttons;


    /// <summary>Gets or sets whether interactive HTML pages should render a compact scenario detail panel when scenarios are present.</summary>
    public bool ScenarioPanel { get; set; } = true;


    /// <summary>Gets or sets whether interactive HTML pages show each scenario's matching sequence diagram and highlight the active step's message in it.</summary>
    public bool ScenarioSequence { get; set; }


    /// <summary>Gets or sets whether interactive HTML pages should render a compact selected-record panel for clicked nodes, edges, and groups.</summary>
    public bool SelectionPanel { get; set; }


    /// <summary>Gets or sets whether interactive HTML pages should render a fullscreen viewport control.</summary>
    public bool FullscreenControl { get; set; }


    /// <summary>Gets or sets where viewport, fullscreen, and export controls are placed in interactive HTML output.</summary>
    public TopologyHtmlControlPlacement ControlPlacement {
        get => _htmlControlPlacement;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _htmlControlPlacement = value;
        }
    }

    private TopologyHtmlControlPlacement _htmlControlPlacement = TopologyHtmlControlPlacement.TopLeft;


    /// <summary>Gets or sets the view an interactive HTML topology with viewport controls opens at.</summary>
    public TopologyHtmlOpeningView OpeningView {
        get => _htmlOpeningView;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _htmlOpeningView = value;
        }
    }

    private TopologyHtmlOpeningView _htmlOpeningView = TopologyHtmlOpeningView.Readable;


    /// <summary>Gets or sets how the mouse wheel acts on an interactive HTML topology with viewport controls.</summary>
    public TopologyHtmlWheelZoom WheelZoom {
        get => _htmlWheelZoom;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _htmlWheelZoom = value;
        }
    }

    private TopologyHtmlWheelZoom _htmlWheelZoom = TopologyHtmlWheelZoom.Always;


    /// <summary>Gets or sets whether interactive HTML pages should read and update scenario state from the page query string.</summary>
    public bool ScenarioUrlState { get; set; }


    /// <summary>Gets or sets the optional HTML synchronization group name used by topology wrappers on the same page.</summary>
    public string? SyncGroupName { get; set; }


    /// <summary>Gets or sets the content security policy nonce for topology HTML. When set, the HTML carries no <c>style</c>
    /// attributes: every inline style and style element moves into one <c>style</c> element with this nonce
    /// (see <see cref="ChartForgeX.Html.HtmlStrictStyles"/>), and interactive script elements carry it too.</summary>
    public string? Nonce { get; set; }
}
