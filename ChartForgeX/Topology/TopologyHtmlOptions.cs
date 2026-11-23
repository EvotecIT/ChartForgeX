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
    /// <remarks>Turning it on also turns on interactions and viewport controls.</remarks>
    public bool ForceGraphControls {
        get => _forceGraphControls;
        set {
            _forceGraphControls = value;
            if (value) { Interactions = true; ViewportControls = true; }
        }
    }
    private bool _forceGraphControls;


    /// <summary>Gets or sets whether interactive HTML pages render a find bar that highlights nodes by text, group or kind and moves the viewport to them.</summary>
    /// <remarks>Force graph controls already carry their own search, so force-directed pages with those controls keep that panel instead.
    /// Turning it on also turns on interactions and viewport controls.</remarks>
    public bool FindControls {
        get => _findControls;
        set {
            _findControls = value;
            if (value) { Interactions = true; ViewportControls = true; }
        }
    }
    private bool _findControls;


    /// <summary>Gets or sets whether interactive HTML pages should synchronize selection and viewport state with wrappers in the same sync group.</summary>
    public bool SynchronizedState { get; set; }


    /// <summary>Gets or sets whether interactive HTML pages should render scenario picker controls when scenarios are present.</summary>
    /// <remarks>Turning it on also turns on interactions.</remarks>
    public bool ScenarioControls {
        get => _scenarioControls;
        set {
            _scenarioControls = value;
            if (value) { Interactions = true; }
        }
    }
    private bool _scenarioControls = true;


    /// <summary>Gets or sets how interactive HTML pages should render scenario controls.</summary>
    /// <remarks>Setting it also turns on interactions and scenario controls.</remarks>
    public TopologyHtmlScenarioControlMode ScenarioControlMode {
        get => _htmlScenarioControlMode;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _htmlScenarioControlMode = value;
            Interactions = true;
            ScenarioControls = true;
        }
    }

    private TopologyHtmlScenarioControlMode _htmlScenarioControlMode = TopologyHtmlScenarioControlMode.Buttons;


    /// <summary>Gets or sets whether interactive HTML pages should render a compact scenario detail panel when scenarios are present.</summary>
    /// <remarks>Turning it on also turns on interactions.</remarks>
    public bool ScenarioPanel {
        get => _scenarioPanel;
        set {
            _scenarioPanel = value;
            if (value) { Interactions = true; }
        }
    }
    private bool _scenarioPanel = true;


    /// <summary>Gets or sets whether interactive HTML pages show each scenario's matching sequence diagram and highlight the active step's message in it.</summary>
    /// <remarks>Turning it on also turns on interactions.</remarks>
    public bool ScenarioSequence {
        get => _scenarioSequence;
        set {
            _scenarioSequence = value;
            if (value) { Interactions = true; }
        }
    }
    private bool _scenarioSequence;


    /// <summary>Gets or sets whether interactive HTML pages should render a compact selected-record panel for clicked nodes, edges, and groups.</summary>
    /// <remarks>Turning it on also turns on interactions.</remarks>
    public bool SelectionPanel {
        get => _selectionPanel;
        set {
            _selectionPanel = value;
            if (value) { Interactions = true; }
        }
    }
    private bool _selectionPanel;


    /// <summary>Gets or sets whether interactive HTML pages should render a fullscreen viewport control.</summary>
    /// <remarks>Turning it on also turns on interactions.</remarks>
    public bool FullscreenControl {
        get => _fullscreenControl;
        set {
            _fullscreenControl = value;
            if (value) { Interactions = true; }
        }
    }
    private bool _fullscreenControl;


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
    /// <remarks>Turning it on also turns on interactions.</remarks>
    public bool ScenarioUrlState {
        get => _scenarioUrlState;
        set {
            _scenarioUrlState = value;
            if (value) { Interactions = true; }
        }
    }
    private bool _scenarioUrlState;


    /// <summary>Gets or sets the optional HTML synchronization group name used by topology wrappers on the same page.</summary>
    public string? SyncGroupName { get; set; }


    /// <summary>Gets or sets the content security policy nonce for topology HTML. When set, the HTML carries no <c>style</c>
    /// attributes: every inline style and style element moves into one <c>style</c> element with this nonce
    /// (see <see cref="ChartForgeX.Html.HtmlStrictStyles"/>), and interactive script elements carry it too.</summary>
    public string? Nonce { get; set; }
}
