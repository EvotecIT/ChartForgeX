param(
    [Parameter(Mandatory = $true)]
    [string] $OutputRoot,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
if (-not [IO.Path]::IsPathRooted($OutputRoot)) { throw 'OutputRoot must be an explicit absolute task-owned path.' }
$repository = Split-Path -Parent $PSScriptRoot
$output = [IO.Path]::GetFullPath($OutputRoot)
$gallery = Join-Path $output 'gallery'
$captures = Join-Path $output 'captures'
$results = Join-Path $output 'results'
New-Item -ItemType Directory -Path $output, $captures, $results -Force | Out-Null
$project = Join-Path $repository 'ChartForgeX.Tests/ChartForgeX.Tests.csproj'
if (-not $SkipBuild) {
    dotnet build $project -c Release
    if ($LASTEXITCODE) { throw 'Browser test project build failed.' }
}
dotnet run --project (Join-Path $repository 'ChartForgeX.Examples/ChartForgeX.Examples.csproj') -c Release --no-build -- --v2-only --output $gallery
if ($LASTEXITCODE) { throw 'Fresh browser gallery generation failed.' }
if (-not (Test-Path -LiteralPath (Join-Path $gallery 'manifest.json'))) { throw 'The generated gallery has no manifest.' }

$addressFile = Join-Path $output 'gallery-address.txt'
$serverLog = Join-Path $output 'gallery-server.log'
[IO.File]::WriteAllText($addressFile, '')
$start = [Diagnostics.ProcessStartInfo]::new('node')
$start.CreateNoWindow = $true
$start.UseShellExecute = $false
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.ArgumentList.Add((Join-Path $PSScriptRoot 'Serve-BrowserGallery.cjs'))
$start.ArgumentList.Add($gallery)
$start.ArgumentList.Add($addressFile)
$server = $null
$variables = @('HFX_TEST_ENABLE_PLAYWRIGHT', 'CFX_TEST_REQUIRE_BROWSER', 'CFX_TEST_BROWSER', 'CFX_V2_GALLERY_URL', 'CFX_V2_CAPTURE_ROOT', 'CFX_BROWSER_CAPTURE_DIRECTORY')
$saved = @{}
foreach ($name in $variables) { $saved[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    $server = [Diagnostics.Process]::Start($start)
    $standardOutput = $server.StandardOutput.ReadToEndAsync()
    $standardError = $server.StandardError.ReadToEndAsync()
    $startupTimeoutSeconds = 60
    $startupTimer = [Diagnostics.Stopwatch]::StartNew()
    while ([IO.File]::ReadAllText($addressFile).Length -eq 0) {
        if ($server.HasExited) { throw "Gallery server exited with code $($server.ExitCode) before readiness. See $serverLog." }
        if ($startupTimer.Elapsed.TotalSeconds -ge $startupTimeoutSeconds) {
            throw "Gallery server did not become ready within $startupTimeoutSeconds seconds (PID $($server.Id); readiness file: $addressFile). See $serverLog."
        }
        Start-Sleep -Milliseconds 100
    }
    $env:HFX_TEST_ENABLE_PLAYWRIGHT = '1'
    $env:CFX_TEST_REQUIRE_BROWSER = '1'
    $env:CFX_TEST_BROWSER = 'chromium'
    $env:CFX_V2_GALLERY_URL = [IO.File]::ReadAllText($addressFile)
    $env:CFX_V2_CAPTURE_ROOT = $captures
    $env:CFX_BROWSER_CAPTURE_DIRECTORY = $captures
    Invoke-WebRequest -Uri $env:CFX_V2_GALLERY_URL -TimeoutSec 10 | Out-Null
    # HtmlTinkerX installs and opens Chromium through the shared test session when it is first needed.
    dotnet test $project -c Release --no-build --no-restore --filter 'Category=Browser' `
        --settings (Join-Path $PSScriptRoot 'browser.runsettings') --logger 'trx;LogFileName=browser.trx' --results-directory $results
    if ($LASTEXITCODE) { throw 'Required browser contracts failed. See the retained TRX, captures and console evidence.' }
    [xml] $report = Get-Content -LiteralPath (Join-Path $results 'browser.trx') -Raw
    $counters = $report.TestRun.ResultSummary.Counters
    if ([int]$counters.total -eq 0 -or [int]$counters.notExecuted -ne 0 -or [int]$counters.executed -ne [int]$counters.total) {
        throw 'Required browser proof must discover and execute tests without skips.'
    }
    $requiredContracts = @(
        'V2GalleryBrowserMatrixTests.EveryFamilyKeepsReadableNativeSvgAndHealthyPairedPng',
        'GaugeScalePrecisionBrowserTests.NeedleAtScaleEdgesClearsMeasuredSummaryInRenderedSvg',
        'GaugeScalePrecisionBrowserTests.CloseGaugeBoundsRemainDistinctAndContainedInNativeSvgAndPairedPng',
        'InteractiveTopologyPresentationTests.PointerReleaseOutsideViewportDoesNotResumePanningOnReentry',
        'BrowserColorComparisonTests.EquivalentPaintKeepsExactRgbaAndDifferentOrInvalidPaintStillFails',
        'InteractiveChartLifecycleBrowserTests.DefaultResetAndLoopingPlaybackReleaseResourcesAfterUpdateAndDisposalOrRemoval',
        'InteractiveChartLifecycleBrowserTests.PointLegendMuteAndIsolationFollowAuthoredIdentityAcrossNativeReordering',
        'InteractiveChartLifecycleBrowserTests.DisposalClearsComparisonBrushAndResetPresentationBeforeFreshMount',
        'InteractiveChartLifecycleBrowserTests.NativeWheelZoomsAndCanceledTouchBrushReleasesUncommittedPresentation',
        'InteractiveChartLifecycleBrowserTests.KeyedPointTransactionsRetainSelectedFactsAndCanceledUpdatesLeaveCommittedBrushUntouched',
        'SvgHostProjectionBrowserTests.HostZoomKeepsPartialPointerReadoutsFocusAndGuidesOnActualSvgMarks',
        'SvgHostProjectionBrowserTests.HostZoomKeepsBrushDataAndNativeViewportAnchorsInTheRenderedPlot',
        'InteractiveNativeXViewportBrowserTests.ConnectedXWindowRepreparesNativeTicksRetainsIdentityAndExportsCommittedScene',
        'InteractiveNativeXViewportBrowserTests.ConnectedNativePanConfinesTheRequestedSpanAndPointerCancellationDoesNotCommit',
        'InteractiveNativeXViewportBrowserTests.NativeWindowWithoutObservationsKeepsCrossingPaintWithoutInventingTargets',
        'InteractiveNativeXViewportBrowserTests.NativeRepliesCommitOnlyTheLatestRequestForTheMatchingSceneRevision',
        'InteractiveNativeXViewportBrowserTests.PendingNativeRepliesAbortOnSceneConnectionAndLifetimeChanges',
        'InteractiveNativeXViewportBrowserTests.FailedNativeRepliesLeaveTheCommittedSceneAndExportWorking',
        'InteractiveNativeXViewportBrowserTests.UnsupportedAndEmptyConnectedScenesDisableNativeControlsUntilSupportedReplacement',
        'InteractiveNativeXViewportBrowserTests.StandaloneControlsKeepGeometricZoomAndDisconnectDoesNotPretendToResetANativeDomain',
        'InteractiveNativeXViewportBrowserTests.NativeSourceWindowsReprepareSmoothAreaAndBubbleOnLinearAndTimeAxes',
        'InteractiveNativeXViewportBrowserTests.NativeWheelKeepsThePointerSourceAnchorWhileButtonsUseThePlotCentre',
        'InteractiveChartExportBrowserTests.StandaloneSvgAndBrowserPngRetainPersistentPaintWithoutHoverOrKeyboardChrome',
        'RuntimeSelectionCueBrowserTests.PersistentSelectionAndHostHighlightsPaintWithoutFiltersAndFollowCurrentNativeGeometry',
        'RuntimeSelectionCueBrowserTests.CollapsedFilledMarksHaveNoCueWhileNativeStrokesAndConnectedPointCuesRemainAvailable',
        'RuntimeSelectionCueBrowserTests.BranchSelectionAndHostHighlightKeepIndependentNativeCuesAcrossPeerHover',
        'InteractiveChordColorBrowserTests.IndependentFlowColorRetainsNativeReadoutsIdentityAndCurrentExportAcrossReplacement',
        'HighlightObservationOwnershipBrowserTests.OmittedSourceObservationsRetainTheirCatalogOwnerAcrossReplacement',
        'MapObservationIdentityBrowserTests.SelectionAndReadoutsRetainObservationIdsAfterTypedUpdates',
        'RuntimeSelectionCueBrowserTests.NativeTextOnlySelectionKeepsItsPaintedCueAndIdentityWithoutFilters',
        'WordCloudTargetOwnershipBrowserTests.PublicSelectionResolvesOnePaintedOwnerPerTermInMultipleAndSingleModes',
        'WordCloudTargetOwnershipBrowserTests.LimitedAndZeroWeightTermsRetainExactInventoryWithoutBecomingInteractiveDuplicates',
        'AnnotationTargetIdentityBrowserTests.RepeatedAnnotationCaptionsAndValuesRoundTripIndividualCommandAndActivationTargets',
        'AnnotationTargetIdentityBrowserTests.AnnotationScenarioOpaqueIdentityWinsBeforeLegacyCaptionAliases',
        'AnnotationTargetIdentityBrowserTests.AutomaticAnnotationIdentitiesStayLocalToPeersAndClearOnFullReplacement',
        'AnnotationTargetIdentityBrowserTests.SharedObservationHighlightFollowsItsCurrentAggregateAcrossPeersAndRepeatedReplacement',
        'NamedTargetIdentityBrowserTests.NamedNativeTargetsSynchronizeOnlyWithinTheirExplicitSeries',
        'PeerHoverOwnershipBrowserTests.MissingSharedObservationPreservesPeersOwnKeyboardAffordancesThroughPointerAndClear',
        'PeerHoverOwnershipBrowserTests.ContinuousPlotTransitionsRetireOnlyTheSendersAcceptedPeerHover',
        'GroupedCategoryAxisBrowserTests.GroupedCategoryContextStaysDetachedPassiveAndCurrentAcrossResizeAndReplacement',
        'GroupedCategoryAxisBrowserTests.NativeCategoryWindowRetainsFullSourceContextThroughAcceptedViewportReplacement',
        'InteractiveChartLocalizationBrowserTests.TranslatedChromeAndDynamicAnnouncementsFollowNativeUpdatesWithoutReversingAxes',
        'InteractiveChartLocalizationBrowserTests.ChromeInheritsTheHostDirectionUnlessTheAuthorChoosesOne',
        'InteractiveSelectionDockBrowserTests.CompareActivationInspectsTheRequestedObservationWithoutChangingSelectionOrCoveringTheDock',
        'InteractiveSelectionDockBrowserTests.ReadableCompareInspectionRevealsEachSelectedSourceBeforeShowingItsReadout',
        'InteractiveSelectionDockBrowserTests.PointerPressOnAPartlyVisibleChipKeepsItsClickWhileKeyboardFocusRevealsTheWholeChip',
        'InteractiveSelectionDockBrowserTests.PagedComparisonInspectsEverySelectionAndPreservesExportsResizeAndReplacement',
        'InteractiveLegendOverflowBrowserTests.OmittedLegendEntriesExposeEveryFactAndCanonicalActionsWithoutChangingNativeExports',
        'InteractiveLegendOverflowBrowserTests.ThousandPointKeysMaterializeOneBoundedPageAndRetainEveryObservationIdentity',
        'InteractiveLegendOverflowBrowserTests.OmittedPointAndNamedNodeCommandsRetainTheirNativeTargetIdentity',
        'InteractiveLegendOverflowBrowserTests.LinkedOverflowCommandsUpdateOffPagePeersWithoutEchoingOrUsingSeriesPositions',
        'InteractiveLegendOverflowBrowserTests.PassiveStateKeysAndDisabledLegendCommandsRemainDescriptive',
        'InteractiveLegendOverflowBrowserTests.OverflowCommandsFollowDurableIdentityAndReleaseDetachedPagesAcrossUpdateAndDisposal',
        'InteractiveLegendOverflowBrowserTests.AdaptiveOverflowRestoresAnIndividuallyMutedKeyAndKeepsDisclosurePreferenceWhenTheWideLegendFits',
        'StandaloneAdapterLifecycleBrowserTests.TopologyRemovalAndExplicitDisposalStopPlaybackAndReleaseListenersBeforeRemount',
        'StandaloneAdapterLifecycleBrowserTests.GraphRemovalReleasesPreferencesAndRemountRestoresOneSelectionHandler',
        'StandaloneAdapterLifecycleBrowserTests.GraphBadgeMeasurementsReusePaintWorkAndRefreshWithFontLoadingAndRemount',
        'StandaloneAdapterLifecycleBrowserTests.GraphHierarchyDoubleClickAndBreadcrumbsRemainOperableAfterRemount',
        'StandaloneAdapterLifecycleBrowserTests.AcceleratedGraphRemountPreservesDraggedPositionsSelectionAndExportsAndClearsHostHover',
        'StandaloneAdapterLifecycleBrowserTests.PendingPngImageCompletesAcrossDocumentMutationsWithoutItsTimeoutFallback',
        'StandaloneAdapterLifecycleBrowserTests.GraphPrintMaterializesTheCommittedSceneAndReleasesItsSnapshotWithoutChangingTheCamera',
        'StandaloneAdapterLifecycleBrowserTests.GraphPrintFitsActualVisibleLabelsSecondaryBadgesAndEdgeCaptions',
        'StandaloneAdapterLifecycleBrowserTests.UnmodifiedGraphFragmentsShareOneRuntimeAcrossParsingAndLateInsertion',
        'StandaloneAdapterLifecycleBrowserTests.GraphImagesFollowCurrentScenesAndShareUntilTheirLastConsumerLeaves',
        'StandaloneAdapterLifecycleBrowserTests.GraphImageExportsLeaseReplacedImagesAndIgnoreAbandonedLoadCallbacks',
        'StandaloneAdapterLifecycleBrowserTests.NonSquareImagesStretchConsistentlyBeforeAndAfterSelectedAuthoredUpdates',
        'StandaloneAdapterLifecycleBrowserTests.NativeGraphSvgAndPngPreserveTheSameNonSquareImageContentAndCircularClipping',
        'InteractiveChartTouchScrollingBrowserTests.NativeTouchScrollsTheReportAndExplicitBrushRetainsItsGesture',
        'DottedMapPointLegendBrowserTests.PointKeysKeepWeightedFactsAndPointOnlyLegendStateAcrossNativeReplacement',
        'MermaidFlowchartCaptionLayoutTests.ShapeOnlyFlowchartsCenterMeasuredCaptionsWithoutInfrastructureGlyphs',
        'MermaidFlowchartLengthTests.AuthoredSpacingRendersInNativeAndResponsiveStaticHosts',
        'MermaidRadarThemeTests.StyledStaticFencesRenderInBothThemesWithoutScripts',
        'RadarStrokeColorVariableTests.PlotAndLegendUseTheSameExplicitPaintUnderHostColorVariables',
        'PreparedTopologyNodeLayoutTests.WrappingLimitsDoNotReserveUnusedRowsForShortShapeCaptions',
        'PreparedTopologyNodeLayoutTests.ExplicitBadgesKeepTheirCaptionClearOfTheBadgeSurface',
        'PreparedTopologyNodeLayoutTests.DotModeOmitsOrdinaryTitlesAndKeepsItsAuthoredSymbol',
        'DottedMapPointLegendBrowserTests.UnweightedLocationAndZeroWeightHaveDistinctLegendReadouts',
        'MermaidMindMapColorModeTests.StaticMindmapHostsDisplaySvgAndNativePng',
        'TopologyPaletteTrunkTests.EqualRgbIncomingSlotsKeepIndependentHostColorsAndMarkers',
        'InteractiveHtmlPagePolishBrowserTests.StandalonePagesPrintTheCompleteNativeChartAndRestoreTheScreenView',
        'ShadowHostEventsBrowserTests.NativeCommandsBrushAndUpdateReachOuterShadowHosts',
        'ShadowHostEventsBrowserTests.GraphSelectionAndCancelableChangesReachOuterShadowHosts',
        'ShadowHostLegacyEventsBrowserTests.TopologyPublicNotificationsCrossShadowHostWhilePeerSyncStaysLocal',
        'ShadowHostLegacyEventsBrowserTests.IconBrowserSelectionCrossesShadowHostWithItsStableIconIdentity'
    )
    foreach ($contract in $requiredContracts) {
        $observed = @($report.TestRun.Results.UnitTestResult | Where-Object { $_.testName.Contains($contract) })
        if ($observed.Count -eq 0 -or @($observed | Where-Object outcome -ne 'Passed').Count) {
            throw "Required browser contract did not execute successfully: $contract"
        }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $captures 'native-family-matrix.json'))) { throw 'Native family matrix evidence is missing.' }
    Write-Host "Required browser proof passed: $($counters.total) executed tests. Evidence: $output"
} finally {
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
    if ($server) {
        try {
            if (-not $server.HasExited) { $server.Kill(); [void] $server.WaitForExit(10000) }
            if (-not $server.HasExited) { Write-Warning "Gallery server PID $($server.Id) did not exit within 10 seconds after termination." }
            $capturedOutput = if ($standardOutput.Wait(5000)) { $standardOutput.Result } else { '[stdout capture did not finish]' }
            $capturedError = if ($standardError.Wait(5000)) { $standardError.Result } else { '[stderr capture did not finish]' }
            [IO.File]::WriteAllText($serverLog, $capturedOutput + $capturedError)
            if ($capturedOutput -or $capturedError) { Write-Host ($capturedOutput + $capturedError) }
            else { Write-Host 'Gallery server produced no stdout or stderr.' }
        } finally { $server.Dispose() }
    }
}
