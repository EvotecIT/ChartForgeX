$nodes = @(
    New-ImageTopologyNode browser Browser -Kind Person -X 0 -Y 0
    New-ImageTopologyNode api 'Orders API' -Kind Service -X 260 -Y 0
    New-ImageTopologyNode db 'Orders DB' -Kind Database -X 520 `
        -Y 0 -Status Warning
)
$arrow = @{ Direction = 'Forward' }
$edges = @(
    New-ImageTopologyEdge browser api -Id call @arrow
    New-ImageTopologyEdge api db -Id query @arrow
)

# A scenario names the edges a request travels, in order.
$scenario = New-ImageTopologyScenario order 'Place an order' `
    -StepDefinition {
        New-ImageTopologyScenarioStep call Edge -Label 'POST order'
        New-ImageTopologyScenarioStep query Edge -Label 'Save order'
    }
$motion = New-ImageTopologyMotion -ScenarioId order `
    -DurationSeconds 3 -FramesPerSecond 10

$flow = @{
    Node = $nodes; Edge = $edges; Scenario = $scenario
    Motion = $motion; Layout = 'Manual'
    Width = 720; Height = 160; NoLegend = $true
    FitContentToViewport = $true
}
# SVG animates in the browser; .gif writes the same motion for chat.
New-ImageTopology @flow -FilePath request-flow.svg

$scenario.Steps | Format-Table Id, Kind, Label -AutoSize
