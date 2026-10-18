namespace ChartForgeX.Interactivity.Html;

/// <summary>Messages supplied by the HTML interaction adapter. User-authored chart facts are formatted by their native owner.</summary>
public enum HtmlChartText {
    /// <summary>Text for chart controls.</summary>
    ChartControls,
    /// <summary>Text for zoom in.</summary>
    ZoomIn,
    /// <summary>Text for zoom out.</summary>
    ZoomOut,
    /// <summary>Text for zoom +.</summary>
    ZoomInButton,
    /// <summary>Text for zoom -.</summary>
    ZoomOutButton,
    /// <summary>Text for pan.</summary>
    Pan,
    /// <summary>Text for pan chart.</summary>
    PanChart,
    /// <summary>Text for brush.</summary>
    Brush,
    /// <summary>Text for brush select region.</summary>
    BrushRegion,
    /// <summary>Text for download svg.</summary>
    DownloadSvg,
    /// <summary>Text for download png.</summary>
    DownloadPng,
    /// <summary>Text for reset view.</summary>
    ResetView,
    /// <summary>Text for compare the count.</summary>
    Compare,
    /// <summary>Text for clear.</summary>
    Clear,
    /// <summary>Text for the count marks highlighted.</summary>
    Highlighted,
    /// <summary>Text for highlight cleared.</summary>
    HighlightCleared,
    /// <summary>Text for chart scenarios.</summary>
    ChartScenarios,
    /// <summary>Text for all.</summary>
    All,
    /// <summary>Text for show all scenarios.</summary>
    ShowAllScenarios,
    /// <summary>Text for all chart data visible.</summary>
    AllDataVisible,
    /// <summary>Text for the count steps.</summary>
    Steps,
    /// <summary>Text for step the current step / the count.</summary>
    StepProgress,
    /// <summary>Text for prev.</summary>
    Previous,
    /// <summary>Text for previous step.</summary>
    PreviousStep,
    /// <summary>Text for play.</summary>
    Play,
    /// <summary>Text for pause.</summary>
    Pause,
    /// <summary>Text for play scenario.</summary>
    PlayScenario,
    /// <summary>Text for pause scenario.</summary>
    PauseScenario,
    /// <summary>Text for next.</summary>
    Next,
    /// <summary>Text for next step.</summary>
    NextStep,
    /// <summary>Text for clear step.</summary>
    ClearStep,
    /// <summary>Text for link.</summary>
    Link,
    /// <summary>Text for copy scenario link.</summary>
    CopyScenarioLink,
    /// <summary>Text for scenario timeline step.</summary>
    ScenarioTimelineStep,
    /// <summary>The badge on a pinned observation tooltip.</summary>
    Pinned,
    /// <summary>Accessible name for the previous page of selected targets.</summary>
    ComparePrevious,
    /// <summary>Accessible name for the next page of selected targets.</summary>
    CompareNext,
    /// <summary>Visible selected-target range, using {first}, {last} and {count}.</summary>
    CompareRange,
    /// <summary>Default tooltip caption for a scalar value without a native dimension label.</summary>
    Value,
}
