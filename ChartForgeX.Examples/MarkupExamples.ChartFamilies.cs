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
subtitle Ready work grew every quarter
type stackedbar
size 920x520
theme Graphite

| Quarter | Ready | In review |
| --- | --- | --- |
| 2025 Q3 | 42 | 18 |
| 2025 Q4 | 58 | 22 |
| 2026 Q1 | 72 | 16 |
| 2026 Q2 | 86 | 10 |
| 2026 Q3 | 96 | 8 |
| 2026 Q4 | 108 | 6 |
```
"""),
        new(
            "markup-chart-stacked-horizontal-bar",
            """
```chartforgex chart v1
title Validation by Stage
subtitle Build and test account for most validation work
type stackedhorizontalbar
size 920x520

| Stage | Automated | Manual |
| --- | --- | --- |
| Plan | 18 | 6 |
| Build | 62 | 8 |
| Test | 48 | 16 |
| Package | 36 | 12 |
| Deploy | 22 | 10 |
| Observe | 18 | 6 |
```
"""),
        new(
            "markup-chart-percent-stacked-bar",
            """
```chartforgex chart v1
title Release Channel Mix
subtitle Stable releases take a larger share each quarter
type percentstackedbar
size 920x520

| Quarter | Stable | Preview |
| --- | --- | --- |
| 2025 Q3 | 42 | 18 |
| 2025 Q4 | 58 | 22 |
| 2026 Q1 | 72 | 16 |
| 2026 Q2 | 86 | 10 |
| 2026 Q3 | 96 | 8 |
| 2026 Q4 | 108 | 6 |
```
"""),
        new(
            "markup-chart-percent-stacked-horizontal-bar",
            """
```chartforgex chart v1
title Review Outcome Mix
subtitle Acceptance rates range from 70% to 80%
type percentstackedhorizontalbar
size 920x520

| Team | Accepted | Changes requested |
| --- | --- | --- |
| Platform | 72 | 18 |
| Services | 48 | 12 |
| Reports | 56 | 24 |
| Apps | 64 | 16 |
| Data | 46 | 14 |
| Tools | 38 | 12 |
```
"""),
        new(
            "markup-chart-percent-stacked-area",
            """
```chartforgex chart v1
title Validation Workload Mix
subtitle Automated checks take a larger share each month
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
subtitle Documentation resolves a growing share of requests
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
subtitle Coverage is strongest at build and publish time
type heatmap
size 920x520
option heatmapValueName "Coverage %"

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
subtitle 78% of release checks are ready
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
subtitle 148 checks per minute against a target of 200
type lineargauge
size 920x520
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
subtitle Prices rose over twenty weeks, with a pullback in August
type candlestick
size 920x520
series "Price"
candle 2026-06-01 102 110 98 108
candle 2026-06-08 108 114 104 106
candle 2026-06-15 106 116 102 112
candle 2026-06-22 112 120 108 118
candle 2026-06-29 118 124 114 121
candle 2026-07-06 121 126 116 119
candle 2026-07-13 119 129 117 127
candle 2026-07-20 127 134 123 131
candle 2026-07-27 131 137 126 134
candle 2026-08-03 134 139 125 128
candle 2026-08-10 128 132 119 122
candle 2026-08-17 122 128 116 125
candle 2026-08-24 125 135 121 132
candle 2026-08-31 132 140 129 138
candle 2026-09-07 138 145 134 142
candle 2026-09-14 142 147 136 139
candle 2026-09-21 139 150 137 148
candle 2026-09-28 148 155 143 152
candle 2026-10-05 152 158 147 150
candle 2026-10-12 150 162 148 160
```
"""),
        new(
            "markup-chart-ohlc",
            """
```chartforgex chart v1
title Weekly OHLC Range
subtitle The August pullback gave way to new weekly highs
type ohlc
size 920x520
series "Price"

candles:
| x | open | high | low | close |
| --- | --- | --- | --- | --- |
| 2026-06-01 | 102 | 110 | 98 | 108 |
| 2026-06-08 | 108 | 114 | 104 | 106 |
| 2026-06-15 | 106 | 116 | 102 | 112 |
| 2026-06-22 | 112 | 120 | 108 | 118 |
| 2026-06-29 | 118 | 124 | 114 | 121 |
| 2026-07-06 | 121 | 126 | 116 | 119 |
| 2026-07-13 | 119 | 129 | 117 | 127 |
| 2026-07-20 | 127 | 134 | 123 | 131 |
| 2026-07-27 | 131 | 137 | 126 | 134 |
| 2026-08-03 | 134 | 139 | 125 | 128 |
| 2026-08-10 | 128 | 132 | 119 | 122 |
| 2026-08-17 | 122 | 128 | 116 | 125 |
| 2026-08-24 | 125 | 135 | 121 | 132 |
| 2026-08-31 | 132 | 140 | 129 | 138 |
| 2026-09-07 | 138 | 145 | 134 | 142 |
| 2026-09-14 | 142 | 147 | 136 | 139 |
| 2026-09-21 | 139 | 150 | 137 | 148 |
| 2026-09-28 | 148 | 155 | 143 | 152 |
| 2026-10-05 | 152 | 158 | 147 | 150 |
| 2026-10-12 | 150 | 162 | 148 | 160 |
end
```
"""),
        new(
            "markup-chart-treemap",
            """
```chartforgex chart v1
title Validation Portfolio
subtitle Runtime checks account for most of the validation portfolio
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
subtitle Automated review handles 72% of incoming changes
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
