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
    /// <summary>Text for the count steps. Uses <c>{count}</c>.</summary>
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
    /// <summary>Disclosure title for native legend entries omitted by the drawing budget, using {count}.</summary>
    LegendMore,
    /// <summary>Accessible name for the previous page of omitted legend entries.</summary>
    LegendPrevious,
    /// <summary>Accessible name for the next page of omitted legend entries.</summary>
    LegendNext,
    /// <summary>Visible omitted-legend range, using {first}, {last} and {count}.</summary>
    LegendRange,
    /// <summary>Mute toggle name for an omitted legend entry, using {label}.</summary>
    LegendMute,
    /// <summary>Isolation toggle name for an omitted legend entry, using {label}.</summary>
    LegendIsolate,
    /// <summary>Visible text on an omitted legend entry's isolation button.</summary>
    LegendIsolateButton,
    /// <summary>UI text: Interactive topology</summary>
    GraphEyebrow,
    /// <summary>UI text: Find and appearance controls</summary>
    GraphFindAndAppearanceControls,
    /// <summary>UI text: Appearance</summary>
    GraphAppearance,
    /// <summary>UI text: System</summary>
    GraphThemeSystem,
    /// <summary>UI text: Light</summary>
    GraphThemeLight,
    /// <summary>UI text: Dark</summary>
    GraphThemeDark,
    /// <summary>UI text: Change color theme</summary>
    GraphChangeTheme,
    /// <summary>UI text: Neighborhood navigation</summary>
    GraphNeighborhoodNavigation,
    /// <summary>UI text: Back</summary>
    GraphBack,
    /// <summary>UI text: Previous neighbors</summary>
    GraphPreviousNeighbors,
    /// <summary>UI text: Next neighbors</summary>
    GraphNextNeighbors,
    /// <summary>UI text: Graph commands</summary>
    GraphCommands,
    /// <summary>UI text: Find and filter</summary>
    GraphFindAndFilter,
    /// <summary>UI text: Search graph</summary>
    GraphSearch,
    /// <summary>UI text: Search nodes and edges</summary>
    GraphSearchPlaceholder,
    /// <summary>UI text: Filter by status</summary>
    GraphFilterStatus,
    /// <summary>UI text: All status</summary>
    GraphAllStatus,
    /// <summary>UI text: Filter by kind</summary>
    GraphFilterKind,
    /// <summary>UI text: All kind</summary>
    GraphAllKind,
    /// <summary>UI text: Explore</summary>
    GraphExplore,
    /// <summary>UI text: Clusters</summary>
    GraphClusters,
    /// <summary>UI text: Clear selection</summary>
    GraphClearSelection,
    /// <summary>UI text: View</summary>
    GraphView,
    /// <summary>UI text: Fit</summary>
    GraphFit,
    /// <summary>UI text: Fit graph to view</summary>
    GraphFitToView,
    /// <summary>UI text: Select</summary>
    GraphSelect,
    /// <summary>UI text: Box select</summary>
    GraphBoxSelect,
    /// <summary>UI text: Toggle box selection</summary>
    GraphToggleBoxSelection,
    /// <summary>UI text: Edit</summary>
    GraphEdit,
    /// <summary>UI text: Open graph editor</summary>
    GraphOpenEditor,
    /// <summary>UI text: Undo</summary>
    GraphUndo,
    /// <summary>UI text: Undo graph change</summary>
    GraphUndoChange,
    /// <summary>UI text: Redo</summary>
    GraphRedo,
    /// <summary>UI text: Redo graph change</summary>
    GraphRedoChange,
    /// <summary>UI text: Save</summary>
    GraphSave,
    /// <summary>UI text: Publish current node positions</summary>
    GraphPublishPositions,
    /// <summary>UI text: Physics</summary>
    GraphPhysics,
    /// <summary>UI text: Stabilize</summary>
    GraphStabilize,
    /// <summary>UI text: Export</summary>
    GraphExport,
    /// <summary>UI text: Export graph</summary>
    GraphExportGraph,
    /// <summary>UI text: Export format</summary>
    GraphExportFormat,
    /// <summary>UI text: SVG document</summary>
    GraphSvgDocument,
    /// <summary>UI text: PNG image</summary>
    GraphPngImage,
    /// <summary>UI text: JSON data</summary>
    GraphJsonData,
    /// <summary>UI text: Export SVG document</summary>
    GraphExportSvg,
    /// <summary>UI text: Export PNG image</summary>
    GraphExportPng,
    /// <summary>UI text: Export JSON data</summary>
    GraphExportJson,
    /// <summary>UI text: Graph editor</summary>
    GraphEditor,
    /// <summary>UI text: Validated changes stay local until the host accepts or exports them.</summary>
    GraphEditorHint,
    /// <summary>UI text: Close graph editor</summary>
    GraphCloseEditor,
    /// <summary>UI text: Graph edit actions</summary>
    GraphEditActions,
    /// <summary>UI text: Add node</summary>
    GraphAddNode,
    /// <summary>UI text: Edit node</summary>
    GraphEditNode,
    /// <summary>UI text: Add edge</summary>
    GraphAddEdge,
    /// <summary>UI text: Edit edge</summary>
    GraphEditEdge,
    /// <summary>UI text: Delete selected</summary>
    GraphDeleteSelected,
    /// <summary>UI text: Node id</summary>
    GraphNodeId,
    /// <summary>UI text: Edge id</summary>
    GraphEdgeId,
    /// <summary>UI text: Source node</summary>
    GraphSourceNode,
    /// <summary>UI text: Target node</summary>
    GraphTargetNode,
    /// <summary>UI text: X</summary>
    GraphAxisX,
    /// <summary>UI text: Y</summary>
    GraphAxisY,
    /// <summary>UI text: Apply node</summary>
    GraphApplyNode,
    /// <summary>UI text: Apply edge</summary>
    GraphApplyEdge,
    /// <summary>UI text: Physics lab</summary>
    GraphPhysicsLab,
    /// <summary>UI text: Solver</summary>
    GraphSolver,
    /// <summary>UI text: Theta</summary>
    GraphTheta,
    /// <summary>UI text: Gravity constant</summary>
    GraphGravityConstant,
    /// <summary>UI text: Node distance</summary>
    GraphNodeDistance,
    /// <summary>UI text: Repulsion strength</summary>
    GraphRepulsionStrength,
    /// <summary>UI text: Central gravity</summary>
    GraphCentralGravity,
    /// <summary>UI text: Spring length</summary>
    GraphSpringLength,
    /// <summary>UI text: Spring strength</summary>
    GraphSpringStrength,
    /// <summary>UI text: Damping</summary>
    GraphDamping,
    /// <summary>UI text: Avoid overlap</summary>
    GraphAvoidOverlap,
    /// <summary>UI text: Minimum velocity</summary>
    GraphMinimumVelocity,
    /// <summary>UI text: Maximum velocity</summary>
    GraphMaximumVelocity,
    /// <summary>UI text: Timestep</summary>
    GraphTimestep,
    /// <summary>UI text: Iterations</summary>
    GraphIterations,
    /// <summary>UI text: Adaptive timestep</summary>
    GraphAdaptiveTimestep,
    /// <summary>UI text: Reheat</summary>
    GraphReheat,
    /// <summary>UI text: Reset</summary>
    GraphReset,
    /// <summary>UI text: barnesHut</summary>
    GraphSolverBarnesHut,
    /// <summary>UI text: forceAtlas2</summary>
    GraphSolverForceAtlas2,
    /// <summary>UI text: repulsion</summary>
    GraphSolverRepulsion,
    /// <summary>UI text: hierarchicalRepulsion</summary>
    GraphSolverHierarchicalRepulsion,
    /// <summary>UI text: {title} interactive graph. Uses <c>{title}</c>.</summary>
    GraphInteractiveName,
    /// <summary>UI text: Use the graph controls to search, filter, fit, or export. In the graph, use arrow keys to move between items and Enter or Space to select. Drag nodes to rearrange the topology.</summary>
    GraphInstructions,
    /// <summary>UI text: {title}. Interactive graph. Use arrow keys to move between nodes and Enter or Space to select. Uses <c>{title}</c>.</summary>
    GraphAcceleratedName,
    /// <summary>UI text: {source} to {target}. Uses <c>{source}</c>, <c>{target}</c>.</summary>
    GraphEndpoints,
    /// <summary>UI text: Graph canvas</summary>
    GraphCanvas,
    /// <summary>UI text: Graph WebGL canvas</summary>
    GraphWebGlCanvas,
    /// <summary>UI text: Graph item</summary>
    GraphItem,
    /// <summary>UI text: Graph</summary>
    GraphDefaultTitle,
    /// <summary>UI text: {title}. Current item: {label}. Use arrow keys to move and Enter or Space to activate. Uses <c>{title}</c>, <c>{label}</c>.</summary>
    GraphCurrentItem,
    /// <summary>UI text: {label} selected. {count} item selected. Uses <c>{label}</c>, <c>{count}</c>.</summary>
    GraphSelectedOne,
    /// <summary>UI text: {label} selected. {count} items selected. Uses <c>{label}</c>, <c>{count}</c>.</summary>
    GraphSelectedMany,
    /// <summary>UI text: {label} removed from selection. Uses <c>{label}</c>.</summary>
    GraphRemovedFromSelection,
    /// <summary>UI text: {count} items selected. Uses <c>{count}</c>.</summary>
    GraphItemsSelected,
    /// <summary>UI text: {count} items highlighted. Uses <c>{count}</c>.</summary>
    GraphItemsHighlighted,
    /// <summary>UI text: {count} selected. Uses <c>{count}</c>.</summary>
    GraphSelectedCount,
    /// <summary>UI text: Badge {badge}. Uses <c>{badge}</c>.</summary>
    GraphBadge,
    /// <summary>UI text: {count} match. Uses <c>{count}</c>.</summary>
    GraphOneMatch,
    /// <summary>UI text: {count} matches. Uses <c>{count}</c>.</summary>
    GraphMatches,
    /// <summary>UI text: Expand clusters</summary>
    GraphExpandClusters,
    /// <summary>UI text: Collapse clusters</summary>
    GraphCollapseClusters,
    /// <summary>UI text: Clear neighborhood focus</summary>
    GraphClearNeighborhood,
    /// <summary>UI text: Focus selected node neighborhood</summary>
    GraphFocusNeighborhood,
    /// <summary>UI text: Color theme: {current}. Activate to use {next} theme. Uses <c>{current}</c>, <c>{next}</c>.</summary>
    GraphThemeInstruction,
    /// <summary>UI text: Color theme: {current} ({active} colors). Uses <c>{current}</c>, <c>{active}</c>.</summary>
    GraphThemeTooltip,
    /// <summary>UI text: {active} color theme enabled. Uses <c>{active}</c>.</summary>
    GraphThemeEnabled,
    /// <summary>UI text: Priority overview: {shown} routes from {total} inspectable relationships. Uses <c>{shown}</c>, <c>{total}</c>.</summary>
    GraphPriorityOverview,
    /// <summary>UI text: {count} relationships: {source} both directions with {target}{kind}. Activate this route to expand both sites and inspect individual relationships. Uses <c>{count}</c>, <c>{source}</c>, <c>{target}</c>, <c>{kind}</c>.</summary>
    GraphBundleBoth,
    /// <summary>UI text: {count} relationships: {source} from {target}{kind}. Activate this route to expand both sites and inspect individual relationships. Uses <c>{count}</c>, <c>{source}</c>, <c>{target}</c>, <c>{kind}</c>.</summary>
    GraphBundleFrom,
    /// <summary>UI text: {count} relationships: {source} to {target}{kind}. Activate this route to expand both sites and inspect individual relationships. Uses <c>{count}</c>, <c>{source}</c>, <c>{target}</c>, <c>{kind}</c>.</summary>
    GraphBundleTo,
    /// <summary>UI text: {count} relationships: {source} and {target}{kind}. Activate this route to expand both sites and inspect individual relationships. Uses <c>{count}</c>, <c>{source}</c>, <c>{target}</c>, <c>{kind}</c>.</summary>
    GraphBundleUndirected,
    /// <summary>UI text: {count} relationships. Uses <c>{count}</c>.</summary>
    GraphBundleCount,
    /// <summary>UI text: Showing {shown} priority routes from {total} inspectable relationships. Select a route, expand a site, or filter to inspect all. Uses <c>{shown}</c>, <c>{total}</c>.</summary>
    GraphPriorityRoutes,
    /// <summary>UI text: {path}: {shown} of {total} neighborhood nodes · {edges} relationships · {omittedNodes} nodes and {omittedEdges} relationships omitted. Uses <c>{path}</c>, <c>{shown}</c>, <c>{total}</c>, <c>{edges}</c>, <c>{omittedNodes}</c>, <c>{omittedEdges}</c>.</summary>
    GraphNeighborhoodSummary,
    /// <summary>UI text: Reduced view; expand or filter.</summary>
    GraphReducedViewShort,
    /// <summary>UI text: Other relationships are hidden. Expand a site or filter to inspect all.</summary>
    GraphReducedView,
    /// <summary>UI text: Pause physics</summary>
    GraphPausePhysics,
    /// <summary>UI text: Start physics</summary>
    GraphStartPhysics,
    /// <summary>UI text: {solver} · {state} · {thread}. Uses <c>{solver}</c>, <c>{state}</c>, <c>{thread}</c>.</summary>
    GraphPhysicsStatus,
    /// <summary>UI text: ready</summary>
    GraphPhysicsReady,
    /// <summary>UI text: running</summary>
    GraphPhysicsRunning,
    /// <summary>UI text: paused</summary>
    GraphPhysicsPaused,
    /// <summary>UI text: stabilized</summary>
    GraphPhysicsStabilized,
    /// <summary>UI text: idle</summary>
    GraphPhysicsIdle,
    /// <summary>UI text: stopped</summary>
    GraphPhysicsStopped,
    /// <summary>UI text: worker</summary>
    GraphPhysicsWorker,
    /// <summary>UI text: main</summary>
    GraphPhysicsMain,
    /// <summary>UI text: Select a node or edge first.</summary>
    GraphSelectFirst,
    /// <summary>UI text: The selected items cannot be deleted.</summary>
    GraphCannotDelete,
    /// <summary>UI text: Delete {count} selected graph item?. Uses <c>{count}</c>.</summary>
    GraphDeleteOne,
    /// <summary>UI text: Delete {count} selected graph items?. Uses <c>{count}</c>.</summary>
    GraphDeleteMany,
    /// <summary>UI text: Graph updated.</summary>
    GraphUpdated,
    /// <summary>UI text: The graph change was rejected.</summary>
    GraphChangeRejected,
    /// <summary>UI text: Select one node to edit.</summary>
    GraphSelectNodeToEdit,
    /// <summary>UI text: Select one edge to edit.</summary>
    GraphSelectEdgeToEdit,
    /// <summary>UI text: Overview</summary>
    ExplorerOverview,
    /// <summary>UI text: Focus</summary>
    ExplorerFocus,
    /// <summary>UI text: Status</summary>
    ExplorerStatus,
    /// <summary>UI text: Kind</summary>
    ExplorerKind,
    /// <summary>UI text: Node</summary>
    ExplorerNode,
    /// <summary>UI text: Edge</summary>
    ExplorerEdge,
    /// <summary>UI text: Group</summary>
    ExplorerGroup,
    /// <summary>UI text: Label</summary>
    ExplorerLabel,
    /// <summary>UI text: Selection</summary>
    ExplorerSelection,
    /// <summary>UI text: No item selected</summary>
    ExplorerNoItemSelected,
    /// <summary>UI text: ID</summary>
    ExplorerId,
    /// <summary>UI text: Related nodes</summary>
    ExplorerRelatedNodes,
    /// <summary>UI text: Related edges</summary>
    ExplorerRelatedEdges,
    /// <summary>UI text: Topology controls</summary>
    TopologyControls,
    /// <summary>UI text: Fit topology</summary>
    TopologyFit,
    /// <summary>UI text: Reset viewport</summary>
    TopologyResetViewport,
    /// <summary>UI text: Open topology fullscreen</summary>
    TopologyOpenFullscreen,
    /// <summary>UI text: Export SVG</summary>
    TopologyExportSvg,
    /// <summary>UI text: Export PNG</summary>
    TopologyExportPng,
    /// <summary>UI text: Force graph controls</summary>
    TopologyForceControls,
    /// <summary>UI text: Find node, edge, group</summary>
    TopologyFindPlaceholder,
    /// <summary>UI text: Find node, edge, or group</summary>
    TopologyFind,
    /// <summary>UI text: Healthy</summary>
    TopologyHealthy,
    /// <summary>UI text: Warning</summary>
    TopologyWarning,
    /// <summary>UI text: Critical</summary>
    TopologyCritical,
    /// <summary>UI text: Unknown</summary>
    TopologyUnknown,
    /// <summary>UI text: Disabled</summary>
    TopologyDisabled,
    /// <summary>UI text: Edges</summary>
    TopologyEdges,
    /// <summary>UI text: Labels</summary>
    TopologyLabels,
    /// <summary>UI text: Groups</summary>
    TopologyGroups,
    /// <summary>UI text: Quiet pan</summary>
    TopologyQuietPan,
    /// <summary>UI text: Graph ready</summary>
    TopologyGraphReady,
    /// <summary>UI text: All groups</summary>
    TopologyAllGroups,
    /// <summary>UI text: Topology scenarios</summary>
    TopologyScenarios,
    /// <summary>UI text: Show all</summary>
    TopologyShowAll,
    /// <summary>UI text: All topology routes visible</summary>
    TopologyAllRoutesVisible,
    /// <summary>UI text: Play route</summary>
    TopologyPlayRoute,
    /// <summary>UI text: Copy route link</summary>
    TopologyCopyRouteLink,
    /// <summary>UI text: Topology route timeline step</summary>
    TopologyTimelineStep,
    /// <summary>UI text: {nodes} nodes / {edges} edges visible. Uses <c>{nodes}</c>, <c>{edges}</c>.</summary>
    TopologyVisibleSummary,
    /// <summary>UI text: TOPOLOGY REPORT</summary>
    ReportEyebrow,
    /// <summary>UI text: {objects} objects · {pages} detail pages · {relationships} relationships. Uses <c>{objects}</c>, <c>{pages}</c>, <c>{relationships}</c>.</summary>
    ReportSummary,
    /// <summary>UI text: Report pages</summary>
    ReportPages,
    /// <summary>UI text: Previous</summary>
    ReportPrevious,
    /// <summary>UI text: Page</summary>
    ReportPage,
    /// <summary>UI text: Page {page}. Uses <c>{page}</c>.</summary>
    ReportPageNumber,
    /// <summary>UI text: Fit page</summary>
    ReportFitPage,
    /// <summary>UI text: Find an object</summary>
    ReportFindObject,
    /// <summary>UI text: Name or stable ID</summary>
    ReportFindPlaceholder,
    /// <summary>UI text: Enable JavaScript to navigate this report, or use the separately exported SVG pages.</summary>
    ReportNoScript,
    /// <summary>UI text: Diagram</summary>
    ReportDiagram,
    /// <summary>UI text: Detail pages</summary>
    ReportDetailPages,
    /// <summary>UI text: Relationships to other pages</summary>
    ReportOtherRelationships,
    /// <summary>UI text: Page {page} · {objects} objects. Uses <c>{page}</c>, <c>{objects}</c>.</summary>
    ReportDetailPageLink,
    /// <summary>UI text:  · page {page}. Uses <c>{page}</c>.</summary>
    ReportPageSuffix,
    /// <summary>UI text: Previous detail pages</summary>
    ReportPreviousDetailPages,
    /// <summary>UI text: Previous relationships</summary>
    ReportPreviousRelationships,
    /// <summary>UI text: Next detail pages</summary>
    ReportNextDetailPages,
    /// <summary>UI text: Next relationships</summary>
    ReportNextRelationships,
    /// <summary>UI text: {first}–{last} of {count}. Uses <c>{first}</c>, <c>{last}</c>, <c>{count}</c>.</summary>
    ReportRange,
    /// <summary>UI text: Overview · choose a detail page to read individual objects</summary>
    ReportOverviewStatus,
    /// <summary>UI text: Page {page} of {count} · scroll at natural size, or choose Fit page. Uses <c>{page}</c>, <c>{count}</c>.</summary>
    ReportPageStatus,
    /// <summary>UI text: {count} match. Uses <c>{count}</c>.</summary>
    ReportOneMatch,
    /// <summary>UI text: {count} matches. Uses <c>{count}</c>.</summary>
    ReportMatches,
    /// <summary>UI text:  · showing first 20; refine your search</summary>
    ReportRefineSearch,
    /// <summary>UI text: Graph hierarchy</summary>
    GraphHierarchy,
    /// <summary>UI text: Up one level</summary>
    GraphUpOneLevel,
    /// <summary>UI text: {count} object. Uses <c>{count}</c>.</summary>
    GraphOneObject,
    /// <summary>UI text: {count} objects. Uses <c>{count}</c>.</summary>
    GraphObjects,
    /// <summary>Lowercase theme display name for generated instructions.</summary>
    GraphThemeSystemLowercase,
    /// <summary>Lowercase theme display name for generated instructions.</summary>
    GraphThemeLightLowercase,
    /// <summary>Lowercase theme display name for generated instructions.</summary>
    GraphThemeDarkLowercase,
    /// <summary>Built-in graph validation UI: Graph patch {kind} hierarchy contains a parent cycle at '{id}'. Uses <c>{kind}</c>, <c>{id}</c>.</summary>
    GraphPatchParentCycle,
    /// <summary>Built-in graph validation UI: Graph patch clusters require stable ids.</summary>
    GraphPatchClusterIds,
    /// <summary>Built-in graph validation UI: Graph patch node '{nodeId}' references missing cluster '{clusterId}'. Uses <c>{nodeId}</c>, <c>{clusterId}</c>.</summary>
    GraphPatchNodeCluster,
    /// <summary>Built-in graph validation UI: Graph patch cluster '{clusterId}' references missing parent '{parentId}'. Uses <c>{clusterId}</c>, <c>{parentId}</c>.</summary>
    GraphPatchClusterParent,
    /// <summary>Built-in graph validation UI: Graph patch cluster '{clusterId}' references missing node '{nodeId}'. Uses <c>{clusterId}</c>, <c>{nodeId}</c>.</summary>
    GraphPatchClusterNode,
    /// <summary>Built-in graph validation UI: Graph patch nodes require stable ids.</summary>
    GraphPatchNodeIds,
    /// <summary>Built-in graph validation UI: Graph patch node '{nodeId}' references missing parent '{parentId}'. Uses <c>{nodeId}</c>, <c>{parentId}</c>.</summary>
    GraphPatchNodeParent,
    /// <summary>Built-in graph validation UI: Graph patch node '{nodeId}' belongs to multiple clusters. Uses <c>{nodeId}</c>.</summary>
    GraphPatchMultipleClusters,
    /// <summary>Built-in graph validation UI: Graph patch node '{nodeId}' has conflicting cluster memberships. Uses <c>{nodeId}</c>.</summary>
    GraphPatchConflictingClusters,
    /// <summary>Built-in graph validation UI: Graph patch edges require stable ids.</summary>
    GraphPatchEdgeIds,
    /// <summary>Built-in graph validation UI: Graph patch edge '{edgeId}' width must be finite and greater than zero. Uses <c>{edgeId}</c>.</summary>
    GraphPatchEdgeWidth,
    /// <summary>Built-in graph validation UI: Graph patch edge '{edgeId}' references a missing endpoint. Uses <c>{edgeId}</c>.</summary>
    GraphPatchEdgeEndpoint,
    /// <summary>Built-in graph validation UI: Graph manipulation does not allow {capability}. Uses <c>{capability}</c>.</summary>
    GraphManipulationCapability,
    /// <summary>Built-in graph validation UI: User-level graph manipulation does not allow cluster structure changes. Use the host update API.</summary>
    GraphManipulationClusters,
    /// <summary>Built-in graph validation UI: This graph scene does not enable manipulation.</summary>
    GraphManipulationDisabled,
    /// <summary>Heading and accessible name for native X navigation.</summary>
    XNavigation,
    /// <summary>Control selecting the full source-coordinate range.</summary>
    XFullRange,
    /// <summary>Accessible name and visible label of the lower range handle.</summary>
    XRangeStart,
    /// <summary>Accessible name and visible label of the upper range handle.</summary>
    XRangeEnd,
    /// <summary>Accessible name and visible label of the source-window position scrollbar.</summary>
    XWindowPosition,
    /// <summary>Live status while native range preparation is pending.</summary>
    XRangeUpdating,
    /// <summary>Live status after a native range preparation fails.</summary>
    XRangeFailed,
    /// <summary>Annotation editor UI: Annotation editor</summary>
    AnnotationEditor,
    /// <summary>Annotation editor UI: Drawing</summary>
    AnnotationChoose,
    /// <summary>Annotation editor UI: Add note</summary>
    AnnotationNote,
    /// <summary>Annotation editor UI: Add line</summary>
    AnnotationLine,
    /// <summary>Annotation editor UI: Add channel</summary>
    AnnotationChannel,
    /// <summary>Annotation editor UI: Edit</summary>
    AnnotationEdit,
    /// <summary>Annotation editor UI: Delete</summary>
    AnnotationDelete,
    /// <summary>Annotation editor UI: Caption</summary>
    AnnotationCaption,
    /// <summary>Annotation editor UI: Apply</summary>
    AnnotationApply,
    /// <summary>Annotation editor UI: Cancel</summary>
    AnnotationCancel,
    /// <summary>Annotation editor UI: Undo</summary>
    AnnotationUndo,
    /// <summary>Annotation editor UI: Redo</summary>
    AnnotationRedo,
    /// <summary>Annotation editor UI: Close editor</summary>
    AnnotationClose,
    /// <summary>Annotation editor UI: Start anchor</summary>
    AnnotationStart,
    /// <summary>Annotation editor UI: End anchor</summary>
    AnnotationEnd,
    /// <summary>Annotation editor UI: Channel offset</summary>
    AnnotationOffset,
    /// <summary>Annotation editor UI: Updating annotation…</summary>
    AnnotationUpdating,
    /// <summary>Annotation editor UI: The annotation could not be updated.</summary>
    AnnotationFailed,
    /// <summary>Annotation editor UI: Draft: move an anchor, then apply or cancel.</summary>
    AnnotationDraft,
    /// <summary>Annotation editor UI: Connect native preparation to edit annotations.</summary>
    AnnotationDisconnected,
    /// <summary>Accessible name for native hierarchy navigation.</summary>
    HierarchyNavigation,
    /// <summary>Moves to the selected hierarchy item's parent.</summary>
    HierarchyUp,
    /// <summary>Restores the complete source hierarchy.</summary>
    HierarchyHome,
    /// <summary>Reports a pending native hierarchy request.</summary>
    HierarchyUpdating,
    /// <summary>Reports a rejected native hierarchy request.</summary>
    HierarchyFailed,
    /// <summary>Explains that hierarchy navigation requires the host's native preparation binding.</summary>
    HierarchyDisconnected,
    /// <summary>Collapses one organization branch. Template: {label}.</summary>
    HierarchyCollapse,
    /// <summary>Expands one organization branch. Template: {label}.</summary>
    HierarchyExpand,
    /// <summary>UI text: Topology diagram</summary>
    TopologyDiagram,
}
