using System.Collections.Generic;

namespace ChartForgeX.Playground;

/// <summary>One bundled playground sample.</summary>
public sealed class PlaygroundSample {
    /// <summary>Creates a sample.</summary>
    public PlaygroundSample(string id, string title, string language, string source, string? group = null) {
        Id = id;
        Title = title;
        Language = language;
        Source = source;
        Group = group ?? (language == PlaygroundEngine.MermaidLanguage ? "Mermaid" : "Diagrams and documents");
    }

    /// <summary>Stable sample id.</summary>
    public string Id { get; }
    /// <summary>Display title.</summary>
    public string Title { get; }
    /// <summary>Picker group the sample is listed under.</summary>
    public string Group { get; }
    /// <summary>Playground language name.</summary>
    public string Language { get; }
    /// <summary>Sample source.</summary>
    public string Source { get; }
}

/// <summary>Samples shown in the playground picker. Website/tools/PlaygroundCheck renders every one of them in both themes.</summary>
public static class PlaygroundSamples {
    private const string Mermaid = PlaygroundEngine.MermaidLanguage;
    private const string Cfx = PlaygroundEngine.ChartForgeXLanguage;
    private const string Charts = "Charts";

    /// <summary>All samples: Mermaid first, then ChartForgeX markup. The page lists charts first and opens on <c>cfx-line</c>.</summary>
    public static IReadOnlyList<PlaygroundSample> All { get; } = new[] {
        new PlaygroundSample("mermaid-flowchart", "Flowchart", Mermaid, """
            flowchart TD
              alert([Alert raised]) --> triage{Known issue?}
              triage -->|yes| runbook[Run the runbook]
              triage -->|no| owner[Page the service owner]
              runbook --> verify[Verify recovery]
              owner --> fix[Ship a fix]
              fix --> verify
              verify --> close((Close))
            """),
        new PlaygroundSample("mermaid-sequence", "Sequence diagram", Mermaid, """
            sequenceDiagram
              autonumber
              actor User
              participant App
              participant IdP as Identity provider
              participant API
              User->>App: Open dashboard
              App->>IdP: Authorization request
              IdP-->>App: ID token + access token
              App->>API: GET /reports (Bearer token)
              alt token valid
                API-->>App: 200 report data
              else token expired
                API-->>App: 401 Unauthorized
              end
              Note over App,API: Tokens refresh silently before expiry
              App-->>User: Render charts
            """),
        new PlaygroundSample("mermaid-gantt", "Gantt chart", Mermaid, """
            gantt
              title Release train
              dateFormat YYYY-MM-DD
              section Engine
              Layout pass      :done,    l1, 2026-09-01, 12d
              Label placement  :active,  l2, after l1, 10d
              section Surfaces
              PNG parity       :         s1, 2026-09-10, 14d
              Email preview    :         s2, after s1, 7d
              section Release
              Ship 2.0         :milestone, m1, after l2 s2, 0d
            """),
        new PlaygroundSample("mermaid-pie", "Pie chart", Mermaid, """
            pie showData
              title Sign-ins by method
              "Passkey" : 8900
              "Authenticator app" : 6200
              "SMS code" : 1300
              "Password only" : 1500
            """),
        new PlaygroundSample("mermaid-xychart", "XY chart", Mermaid, """
            xychart-beta
              title "Tickets closed per sprint"
              x-axis [S1, S2, S3, S4, S5, S6, S7, S8]
              y-axis "Tickets" 0 --> 120
              bar [48, 61, 57, 72, 80, 77, 94, 103]
              line [48, 61, 57, 72, 80, 77, 94, 103]
            """),
        new PlaygroundSample("mermaid-sankey", "Sankey diagram", Mermaid, """
            sankey-beta
            Office network,Passkey,5200
            Office network,Authenticator app,3100
            Remote,Passkey,3700
            Remote,Authenticator app,3100
            Remote,SMS code,1300
            Remote,Password only,1500
            """),
        new PlaygroundSample("mermaid-mindmap", "Mind map", Mermaid, """
            mindmap
              root((ChartForgeX))
                Outputs
                  SVG
                  PNG
                  HTML
                Authoring
                  Fluent C#
                  Markdown fences
                  Mermaid
                Hosts
                  Reports
                  Email
                  Dashboards
            """),
        new PlaygroundSample("mermaid-timeline", "Timeline", Mermaid, """
            timeline
              title Rendering history
              2024 : Fluent chart API : SVG and PNG output
              2025 : Topology diagrams : Markdown fences
              2026 : Mermaid families : Native WebAssembly playground
            """),
        new PlaygroundSample("mermaid-state", "State diagram", Mermaid, """
            stateDiagram-v2
              [*] --> Draft
              Draft --> Review : submit
              Review --> Draft : request changes
              Review --> Approved : approve
              Approved --> Published : release
              Published --> [*]
            """),
        new PlaygroundSample("mermaid-class", "Class diagram", Mermaid, """
            classDiagram
              class Chart {
                +string Title
                +WithSize(width, height)
                +ToSvg() string
              }
              class ChartSeries {
                +string Name
                +ChartPoint[] Points
              }
              class VisualTheme {
                +VisualTypography Typography
              }
              Chart "1" *-- "many" ChartSeries : contains
              Chart --> VisualTheme : uses
            """),
        new PlaygroundSample("mermaid-er", "Entity relationship", Mermaid, """
            erDiagram
              CUSTOMER ||--o{ ORDER : places
              ORDER ||--|{ LINE_ITEM : contains
              PRODUCT ||--o{ LINE_ITEM : "ordered in"
              CUSTOMER {
                string id PK
                string name
                string email
              }
              ORDER {
                string id PK
                string customerId FK
                date placed
              }
            """),
        new PlaygroundSample("mermaid-journey", "User journey", Mermaid, """
            journey
              title Report delivery
              section Build
                Collect data: 5: Collector
                Render charts: 4: ChartForgeX
              section Deliver
                Send email: 3: Mailer
                Read on phone: 5: Reader
            """),
        new PlaygroundSample("mermaid-gitgraph", "Git graph", Mermaid, """
            gitGraph
              commit id: "init"
              branch feature
              checkout feature
              commit id: "renderer"
              commit id: "tests"
              checkout main
              commit id: "docs"
              merge feature id: "merge" tag: "v2.0"
            """),
        new PlaygroundSample("mermaid-quadrant", "Quadrant chart", Mermaid, """
            quadrantChart
              title Feature priorities
              x-axis Low effort --> High effort
              y-axis Low impact --> High impact
              quadrant-1 Plan carefully
              quadrant-2 Do now
              quadrant-3 Maybe later
              quadrant-4 Avoid
              Dark theme: [0.25, 0.85]
              PNG export: [0.7, 0.8]
              Tooltips: [0.35, 0.4]
              Custom fonts: [0.8, 0.3]
            """),
        new PlaygroundSample("mermaid-radar", "Radar chart", Mermaid, """
            radar-beta
              title Security posture
              axis mail["Mail auth"], dnssec["DNSSEC"], tls["TLS"], ct["CT"], policy["Policy"]
              curve current["Current"]{91, 72, 88, 95, 79}
              curve target["Target"]{95, 90, 95, 95, 90}
              min 0
              max 100
            """),
        new PlaygroundSample("mermaid-treemap", "Treemap", Mermaid, """
            treemap-beta
            "Infrastructure"
                "Identity": 42
                "Messaging": 18
                "Endpoints"
                    "Windows": 24
                    "macOS": 9
                    "Linux": 6
            """),
        new PlaygroundSample("mermaid-kanban", "Kanban board", Mermaid, """
            kanban
              todo[To do]
                docs[Write playground docs]
                share[Share links]
              doing[In progress]
                engine[WebAssembly engine]@{ priority: "High" }
              done[Done]
                site[chartforgex.com]
            """),
        new PlaygroundSample("cfx-line", "Line chart", Cfx, """
            ```chartforgex chart v1 {#latency title="API latency by region" type="smoothLine"}
            subtitle "p95 response time over the last 12 weeks"
            | Week | Europe | Americas | Asia Pacific |
            | ---- | ------ | -------- | ------------ |
            | W1   | 182    | 214      | 236          |
            | W2   | 176    | 206      | 229          |
            | W3   | 190    | 199      | 241          |
            | W4   | 168    | 203      | 222          |
            | W5   | 157    | 192      | 210          |
            | W6   | 149    | 181      | 204          |
            | W7   | 143    | 175      | 193          |
            | W8   | 139    | 168      | 186          |
            | W9   | 134    | 162      | 171          |
            | W10  | 131    | 158      | 166          |
            | W11  | 127    | 151      | 158          |
            | W12  | 124    | 147      | 152          |
            annotation hLine 160 "Target 160 ms"
            option yAxisTitle "Milliseconds"
            option yAxisMinimum 100
            option yAxisMaximum 260
            ```
            """, Charts),
        new PlaygroundSample("cfx-bar", "Bar chart", Cfx, """
            ```chartforgex chart v1 {#releases title="Releases per quarter" type="bar"}
            subtitle "Stable and preview builds shipped"
            | Quarter | Stable | Preview |
            | ------- | ------ | ------- |
            | Q1      | 4      | 9       |
            | Q2      | 6      | 11      |
            | Q3      | 5      | 14      |
            | Q4      | 8      | 10      |
            option yAxisTitle "Releases"
            ```
            """, Charts),
        new PlaygroundSample("cfx-hbar", "Ranked bars", Cfx, """
            ```chartforgex chart v1 {#mfa title="MFA coverage by department" type="horizontalBar"}
            subtitle "Share of active accounts with a strong second factor"
            series "Coverage %"
            | Label       | Value |
            | ----------- | ----- |
            | Engineering | 97    |
            | Finance     | 92    |
            | Legal       | 88    |
            | Sales       | 81    |
            | Support     | 74    |
            | Facilities  | 63    |
            dataLabels true
            annotation vLine 90 "Target"
            option xAxisMinimum 0
            option xAxisMaximum 100
            ```
            """, Charts),
        new PlaygroundSample("cfx-area", "Stacked area chart", Cfx, """
            ```chartforgex chart v1 {#sign-ins title="Sign-ins by method" type="stackedArea"}
            subtitle "Weekly sign-ins across the tenant"
            | Week | Passkey | Authenticator app | SMS code | Password only |
            | ---- | ------- | ----------------- | -------- | ------------- |
            | W1   | 800     | 7200              | 4200     | 5600          |
            | W2   | 1700    | 7500              | 3800     | 4900          |
            | W3   | 3000    | 7500              | 3200     | 4100          |
            | W4   | 4700    | 7300              | 2600     | 3300          |
            | W5   | 6400    | 6900              | 2000     | 2500          |
            | W6   | 8100    | 6400              | 1500     | 1800          |
            option yAxisTitle "Sign-ins"
            ```
            """, Charts),
        new PlaygroundSample("cfx-scatter", "Scatter plot", Cfx, """
            ```chartforgex chart v1 {#builds title="Build duration per commit" type="scatter"}
            subtitle "Last 14 commits on main, by runner"
            | Commit | Windows | Linux | macOS |
            | ------ | ------- | ----- | ----- |
            | C1     | 412     | 288   | 351   |
            | C2     | 398     | 301   | 342   |
            | C3     | 441     | 276   | 368   |
            | C4     | 405     | 292   | 337   |
            | C5     | 389     | 269   | 329   |
            | C6     | 376     | 281   | 345   |
            | C7     | 452     | 310   | 372   |
            | C8     | 361     | 258   | 318   |
            | C9     | 348     | 262   | 309   |
            | C10    | 355     | 249   | 314   |
            | C11    | 337     | 244   | 301   |
            | C12    | 329     | 251   | 296   |
            | C13    | 341     | 238   | 288   |
            | C14    | 318     | 231   | 279   |
            option xAxisTitle "Commit"
            option yAxisTitle "Seconds"
            ```
            """, Charts),
        new PlaygroundSample("cfx-donut", "Donut chart", Cfx, """
            ```chartforgex chart v1 {#methods title="Sign-ins by method" type="donut"}
            subtitle "All interactive sign-ins this month"
            series "Sign-ins"
            | Label             | Value |
            | ----------------- | ----- |
            | Passkey           | 8900  |
            | Authenticator app | 6200  |
            | Password only     | 1500  |
            | SMS code          | 1300  |
            ```
            """, Charts),
        new PlaygroundSample("cfx-radar", "Radar chart", Cfx, """
            ```chartforgex chart v1 {#posture title="Security posture" type="radar"}
            subtitle "Domain scores against the target profile"
            | Area         | Current | Target |
            | ------------ | ------- | ------ |
            | Mail auth    | 91      | 95     |
            | DNSSEC       | 72      | 90     |
            | TLS          | 88      | 95     |
            | Certificates | 95      | 95     |
            | Policy       | 79      | 90     |
            | Monitoring   | 84      | 92     |
            ```
            """, Charts),
        new PlaygroundSample("cfx-waterfall", "Waterfall chart", Cfx, """
            ```chartforgex chart v1 {#licences title="Licence count this quarter" type="waterfall"}
            subtitle "From opening balance to closing balance"
            series "Licences"
            | Label        | Value |
            | ------------ | ----- |
            | Opening      | 1200  |
            | New hires    | 180   |
            | Contractors  | 65    |
            | Leavers      | -140  |
            | Clean-up     | -95   |
            | Pilot        | 40    |
            option yAxisTitle "Licences"
            ```
            """, Charts),
        new PlaygroundSample("cfx-funnel", "Funnel chart", Cfx, """
            ```chartforgex chart v1 {#onboarding title="Device onboarding" type="funnel"}
            subtitle "Devices reaching each stage this month"
            series "Devices"
            | Label      | Value |
            | ---------- | ----- |
            | Discovered | 4200  |
            | Enrolled   | 3650  |
            | Compliant  | 3120  |
            | Encrypted  | 2890  |
            | Healthy    | 2610  |
            ```
            """, Charts),
        new PlaygroundSample("cfx-treemap", "Treemap", Cfx, """
            ```mermaid
            ---
            title: Storage by workload (TB)
            ---
            treemap-beta
            "Infrastructure"
                "Identity": 42
                "Messaging": 18
                "Endpoints"
                    "Windows": 24
                    "macOS": 9
                    "Linux": 6
            "Applications"
                "Reporting": 21
                "Billing": 14
                "Portal": 8
            ```
            """, Charts),
        new PlaygroundSample("cfx-sankey", "Sankey flow", Cfx, """
            ```mermaid
            ---
            title: Sign-ins by network and method
            ---
            sankey-beta
            Office network,Passkey,5200
            Office network,Authenticator app,3100
            Remote,Passkey,3700
            Remote,Authenticator app,3100
            Remote,SMS code,1300
            Remote,Password only,1500
            ```
            """, Charts),
        new PlaygroundSample("cfx-gantt", "Gantt plan", Cfx, """
            ```chartforgex timeline v1 {#launch title="Launch plan" type="gantt" today="2026-10-12"}
            | kind      | label          | start      | end        | progress | dependsOn |
            | --------- | -------------- | ---------- | ---------- | -------- | --------- |
            | task      | Design         | 2026-10-01 | 2026-10-08 | 1        |           |
            | task      | Build engine   | 2026-10-06 | 2026-10-20 | 0.6      | 0         |
            | task      | Playground UI  | 2026-10-14 | 2026-10-24 | 0.2      | 1         |
            | milestone | Launch         | 2026-10-27 |            |          | 2         |
            ```
            """, Charts),
        new PlaygroundSample("cfx-topology", "Topology diagram", Cfx, """
            ```chartforgex topology v1
            id service-map
            title "Service dependency map"
            subtitle "Production dependencies and latency"
            layout layered lr

            group platform "Platform" status:healthy icon:service
            group data "Data layer" status:warning icon:database

            node web "Web front end" kind:service group:platform status:healthy icon:service
            node api "Public API" kind:service group:platform status:healthy icon:service badge:v2
            node worker "Billing worker" kind:process group:platform status:warning icon:worker
            node sql "SQL primary" kind:database group:data status:warning icon:database subtitle:"failover lag 2m"
            node cache "Redis cache" kind:database group:data status:healthy icon:database

            edge web -> api "HTTPS" kind:dependency status:healthy direction:forward
            edge api -> cache "3 ms" kind:dependency status:healthy direction:forward
            edge api -> worker "queue" kind:dataflow status:warning direction:forward
            edge worker -> sql "84 ms" kind:dependency status:warning direction:forward
            ```
            """),
        new PlaygroundSample("cfx-sequence", "Sequence (native)", Cfx, """
            ```chartforgex sequence v1 {#render title="Render request"}
            actor author "Author"
            participant page "Playground"
            participant engine "WebAssembly engine"
            message author -> page "Edit source"
            message page -> engine "Render(language, source, theme)"
            message engine -> page "SVG + diagnostics" style:dashed
            note rightOf engine "Runs locally, no server"
            message page -> author "Live preview" style:dashed
            ```
            """),
        new PlaygroundSample("cfx-flow", "Process flow", Cfx, """
            ```chartforgex flow v1 {#pipeline title="Report pipeline"}
            layout layered lr
            lane collect "Collect"
            lane publish "Publish"
            start intake "Collect data" lane:collect status:healthy
            process render "Render charts" lane:collect status:healthy
            decision review "Review" lane:publish status:warning
            output send "Send report" lane:publish status:healthy
            connect intake -> render "rows" status:healthy
            connect render -> review "SVG + PNG" status:healthy
            connect review -> send "approved" status:healthy
            ```
            """),
        new PlaygroundSample("cfx-report", "Markdown report", Cfx, """
            # Weekly identity report

            Charts and tables are ordinary Markdown fences, so one document can hold several visuals.

            ```chartforgex chart v1 {#mfa title="MFA coverage" type="bar"}
            labels Finance Sales Engineering Support
            series "Coverage %" values 92 81 97 74
            annotation hLine 90 "Target" color:#16A34A
            option yAxisMaximum 100
            ```

            ```chartforgex table v1 {#gaps title="Accounts without MFA"}
            columns:
            | id     | label      | type   | alignment |
            | ------ | ---------- | ------ | --------- |
            | team   | Team       | text   | left      |
            | count  | Accounts   | number | right     |
            | status | Status     | status | left      |

            rows:
            | team    | count | status  |
            | ------- | ----- | ------- |
            | Support | 18    | warning |
            | Sales   | 11    | warning |
            | Finance | 4     | healthy |
            ```
            """)
    };
}
