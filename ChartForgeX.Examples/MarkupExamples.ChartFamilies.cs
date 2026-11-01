/// <summary>
/// Native numeric, financial, hierarchy and relationship chart markup examples.
/// </summary>
internal static partial class MarkupExamples {
    private static readonly MarkupExample[] ChartFamilyExamples = {
        new(
            "markup-chart-stacked-bar",
            """
```chartforgex chart v1
title Release Readiness by Quarter
subtitle Stacked bars from a label and two numeric series columns
type stackedbar
size 920x520
theme Graphite

| Quarter | Ready | In review |
| --- | --- | --- |
| Q1 | 42 | 18 |
| Q2 | 58 | 22 |
| Q3 | 72 | 16 |
| Q4 | 86 | 10 |
```
"""),
        new(
            "markup-chart-stacked-horizontal-bar",
            """
```chartforgex chart v1
title Validation by Stage
subtitle Horizontal stacks retain the same numeric table contract
type stackedhorizontalbar
size 920x520

| Stage | Automated | Manual |
| --- | --- | --- |
| Build | 62 | 8 |
| Test | 48 | 16 |
| Package | 36 | 12 |
| Publish | 28 | 6 |
```
"""),
        new(
            "markup-chart-percent-stacked-bar",
            """
```chartforgex chart v1
title Release Channel Mix
subtitle Counts normalized to a 100 percent stack in each quarter
type percentstackedbar
size 920x520

| Quarter | Stable | Preview |
| --- | --- | --- |
| Q1 | 42 | 18 |
| Q2 | 58 | 22 |
| Q3 | 72 | 16 |
| Q4 | 86 | 10 |
```
"""),
        new(
            "markup-chart-percent-stacked-horizontal-bar",
            """
```chartforgex chart v1
title Review Outcome Mix
subtitle Normalized horizontal stacks compare teams with different totals
type percentstackedhorizontalbar
size 920x520

| Team | Accepted | Changes requested |
| --- | --- | --- |
| Platform | 72 | 18 |
| Services | 48 | 12 |
| Reports | 56 | 24 |
```
"""),
        new(
            "markup-chart-percent-stacked-area",
            """
```chartforgex chart v1
title Validation Workload Mix
subtitle Numeric counts become normalized stacked areas
type percentstackedarea
size 920x520
labels Jan Feb Mar Apr May
series Automated values 34 48 62 74 88 color #2563EB
series Manual values 26 24 22 18 12 color #14B8A6
```
"""),
        new(
            "markup-chart-smooth-percent-stacked-area",
            """
```chartforgex chart v1 {theme=Graphite themeMode=dark}
title Support Channel Mix
subtitle Smooth normalized areas from native numeric table data
type smoothpercentstackedarea
size 920x520

| Month | Documentation | Guided support |
| --- | --- | --- |
| Jan | 34 | 26 |
| Feb | 48 | 24 |
| Mar | 62 | 22 |
| Apr | 74 | 18 |
| May | 88 | 12 |
```
"""),
        new(
            "markup-chart-heatmap",
            """
```chartforgex chart v1
title Validation Coverage
subtitle Each numeric series column becomes a heatmap row
type heatmap
size 920x520
theme Accessible

| Stage | Windows | Linux | macOS |
| --- | --- | --- | --- |
| Build | 96 | 92 | 90 |
| Test | 88 | 94 | 86 |
| Package | 92 | 86 | 82 |
| Publish | 98 | 96 | 94 |
```
"""),
        new(
            "markup-chart-gauge",
            """
```chartforgex chart v1
title Release Readiness
subtitle A gauge uses exactly one value per series on the default 0 to 100 range
type gauge
size 920x520
series "Ready" values 78 color #2563EB
```
"""),
        new(
            "markup-chart-linear-gauge",
            """
```chartforgex chart v1
title Validation Throughput
subtitle A linear gauge with an explicit native range
type lineargauge
size 920x520
theme Report
themeMode light
option gaugeMinimum 0
option gaugeMaximum 200
series "Checks per minute" values 148 color #14B8A6
```
"""),
        new(
            "markup-chart-candlestick",
            """
```chartforgex chart v1
title Weekly Price Range
subtitle Candles preserve x, open, high, low and close as typed facts
type candlestick
size 920x520
series "Price"
candle 1 102 110 98 108
candle 2 108 114 104 106
candle 3 106 116 102 112
candle 4 112 120 108 118
```
"""),
        new(
            "markup-chart-ohlc",
            """
```chartforgex chart v1
title Weekly OHLC Range
subtitle A candles table uses the same typed facts as candle commands
type ohlc
size 920x520
series "Price"

candles:
| x | open | high | low | close |
| --- | --- | --- | --- | --- |
| 1 | 102 | 110 | 98 | 108 |
| 2 | 108 | 114 | 104 | 106 |
| 3 | 106 | 116 | 102 | 112 |
| 4 | 112 | 120 | 108 | 118 |
end
```
"""),
        new(
            "markup-chart-treemap",
            """
```chartforgex chart v1
title Validation Portfolio
subtitle Empty value cells define groups; leaf values determine area
type treemap
size 920x520
series "Checks"

items:
| id | label | parent | value | colorValue |
| --- | --- | --- | --- | --- |
| portfolio | Portfolio | | | |
| runtime | Runtime | portfolio | | |
| windows | Windows | runtime | 42 | 96 |
| linux | Linux | runtime | 34 | 92 |
| macos | macOS | runtime | 24 | 90 |
| packages | Packages | portfolio | 28 | 88 |
end
```
"""),
        new(
            "markup-chart-sankey",
            """
```chartforgex chart v1
title Review Flow
subtitle Native nodes and weighted flows preserve relationship identity
type sankey
size 920x520
series "Changes"

nodes:
| id | label |
| --- | --- |
| intake | Intake |
| automated | Automated review |
| manual | Manual review |
| ready | Ready |
end

flows:
| id | source | target | value | colorValue |
| --- | --- | --- | --- | --- |
| intake-auto | intake | automated | 72 | 92 |
| intake-manual | intake | manual | 28 | 74 |
| auto-ready | automated | ready | 72 | 92 |
| manual-ready | manual | ready | 28 | 74 |
end
```
""")
    };
}
