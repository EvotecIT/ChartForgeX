# Architecture diagrams and use case playback

`chartforgex architecture v1` describes a system and the request flows that run through it in one text block:
typed nodes, groups and boundaries, labelled connections, and use cases whose ordered steps carry their request and
response payloads. The parser produces an ordinary `TopologyChart`, so the same model drives:

- the static diagram (SVG, PNG, static HTML);
- the interactive player: step list, previous/play/next, a scrubber, keyboard keys, the active step's payload, and the
  matching sequence diagram kept in step, drawn in the topology's visual language (node cards with icons, subtitles and
  status dots as participants, groups as participant bands, neutral message lines with label pills, dashed replies, and
  the critical colour only for failed calls);
- one highlighted frame per step (SVG or PNG), a step-by-step visual story (SVG, PNG, GIF, APNG, HTML);
- a terminal drawing that text output and ConsoleMonster share.

The idea of playing request flows over an architecture diagram comes from [Proschi](https://github.com/gvart/proschi).
ChartForgeX keeps its own grammar style (one statement per line, `key:value` attributes, `end`-closed blocks) and has no
load or cost simulation.

## Example

````markdown
```chartforgex architecture v1 {#orders title="Order flow"}
direction right

user client "Customer"
group backend "Backend"
  service api "Orders API"
  database db "Orders DB" status:healthy
end

client -> api "HTTPS"
api -> db "SQL"

usecase create "Create an order"
  client -> api "POST /orders" json {
    "sku": "A1",
    "quantity": 2
  }
  api -> db "INSERT order"
  db --> api "1 row"
  api --> client "201 Created" json { "id": 42 }
end
```
````

Two complete examples ship with the gallery and the tests:
[`ad-replication.md`](../ChartForgeX.Examples/Assets/Architecture/ad-replication.md) (an attribute change replicating
across two AD sites, plus a site-link failure) and
[`tierbridge-file-journey.md`](../ChartForgeX.Examples/Assets/Architecture/tierbridge-file-journey.md) (a file crossing
from a Tier 2 share to a Tier 0 share through inspection, quarantine and approval).

## Statements

| Statement | Syntax | Notes |
| --- | --- | --- |
| Title | `title "Text"`, `subtitle "Text"` | Fence attributes `title=` and `subtitle=` work too. |
| Direction | `direction right\|down\|left\|up` | Default `right`. `lr`, `td`, `rl` and `bt` are accepted. |
| Layout | `layout <mode> [direction]` | Any `TopologyLayoutMode`; default `layered`. |
| Viewport | `viewport 1200x700` | Optional. Without it the diagram takes its natural size: the layout spaces layers by their labels and the canvas grows to fit. |
| Legend | `legend auto\|none` | `auto` infers a legend from node kinds and statuses. |
| Node | `<type> <id> ["Label"] [attributes]` | Types below. Attributes: `status`, `icon`, `symbol`, `subtitle` (or `note`, `description`), `color`, `badge`, `tooltip`, `href`, `display`, `kind`. |
| Group | `group <id> ["Label"] [attributes]` ... `end` | Nodes and groups inside belong to it. Attributes: `status`, `color`, `icon`, `subtitle`, `direction`. |
| Boundary | `boundary <id> ["Label"]` ... `end` | A group drawn with a dashed border, for trust zones and networks. |
| Connection | `<from> -> <to> ["label"] [attributes]` | `->` forward, `<-` backward, `<->` both, `--` none. Attributes: `id`, `status`, `kind`, `style` (`solid`, `dashed`, `dotted`), `color`. `connect a -> b` also works. |
| Use case | `usecase <id> ["Label"] [attributes]` ... `end` | Top level only. `usecase "Label"` derives the id. Attributes: `description`, `color`, `delay` (ms per step), `loop`, `autoplay`, `spotlight`. |
| Step | `<from> <arrow> <to> ["label"] [duration:<ms>] [payload]` | Inside a use case; see below. |
| Node step | `visit <node> ["what happens"] [payload]` | A step that stays on one node. |
| Comment | `// text`, or a line starting with `#` | `//` inside quotes is text, so JSON strings such as URLs are safe. |

Node types: `node` (generic), `user`/`person`/`actor`, `team`, `client`/`app`/`application`, `service`/`api`/`function`,
`server`/`host`/`vm`, `database`/`db`, `storage`/`share`/`bucket`, `queue`/`topic`/`bus`, `gateway`/`proxy`/`firewall`,
`network`, `cloud`/`external`, `endpoint`/`device`/`workstation`, `process`/`job`/`worker`, `certificate`,
`site`/`location`, `hub`. Each maps to a `TopologyNodeKind`, which chooses the node's icon; `icon:<id>` picks any icon
from the built-in catalog.

Arrows in a connection must be separate words (`a -> b`), so ids may contain hyphens.

## Steps and payloads

| Arrow | Meaning |
| --- | --- |
| `a -> b` | A request: `a` waits for the answer. |
| `a ->> b` | A message that `a` does not wait for. |
| `b --> a` | The answer to an earlier request from `a` to `b`. |
| `a -x b` | A failed call that never gets an answer. Drawn in red. |

A step travels over the connection that joins its two nodes, in either direction, so a response over `api -> db` is
written `db --> api`. A step between nodes that no connection joins adds a dashed connection and reports `CFXA006`.

A payload follows the label: `json {...}`, `xml <...>`, `text "..."`, or a bare `{`, `[` or `<` (read as JSON or XML).
A JSON payload may span several lines; it continues until its brackets balance, and its indentation is kept relative to
the closing bracket.

## Diagnostics

The parser reports every problem with its line and column and keeps going. Codes are stable:

| Code | Severity | Meaning |
| --- | --- | --- |
| `CFXA001` | Warning | Unknown statement. |
| `CFXA002` | Error | Missing or misplaced parts, or unclosed quotes. |
| `CFXA003` | Error | Duplicate id. |
| `CFXA004` | Error | A step or connection refers to something that does not exist. |
| `CFXA005` | Warning | A connection to an undeclared node; a generic node is added. |
| `CFXA006` | Warning | A step between nodes that no connection joins; a dashed connection is added. |
| `CFXA007` | Error | A group or use case without `end`. |
| `CFXA008` | Error | An `end` with nothing open. |
| `CFXA009` | Error | A payload that cannot be read, such as unbalanced JSON. |
| `CFXA010` | Warning | A response that answers no earlier request. |
| `CFXA011` | Warning | An attribute the statement does not use. |
| `CFXA012` | Error | No nodes. |
| `CFXA013` | Warning | A use case without steps. |
| `CFXA014` | Warning | An icon the built-in catalog does not know. |
| `CFXA015` | Error | A value that cannot be read, such as an unknown status. |

The CLI writes them as `warning CFXA006(12): ...`, and the VS Code extension shows them on the line with the code.

## Rendering from the command line

`chartforgex-markup render` renders one diagram. It reads Markdown with fences, or a file without fences as one
architecture diagram (for example `orders.cfx`).

```powershell
chartforgex-markup render ./orders.md                                  # text drawing plus the list of use cases
chartforgex-markup render ./orders.md --scenario create --step 2       # text with step 2 highlighted and its payload
chartforgex-markup render ./orders.md --format png --output orders.png
chartforgex-markup render ./orders.md --format png --scenario create --step all --output ./frames
chartforgex-markup render ./orders.md --format html --output orders.html
```

`--format` is `text`, `svg`, `png` or `html`, or comes from the `--output` extension. `--columns` sets the preferred text
width (default 120), `--ascii` draws with plain ASCII, and `--diagram <id>` picks one diagram from a file with several.
Text output covers architecture, topology and flow diagrams. HTML opens the interactive player; add
`?scenario=<id>&scenarioStep=<n>` to the page URL to open a step (zero-based).

## From C#

```csharp
var chart = new MarkupArchitectureParser().Parse(markdown).Document!;

var scenario = chart.ResolveScenario("create");          // source, target, label and payload per step
var sequence = chart.ToSequenceArtifact("create");       // the matching sequence diagram, as topology-style cards
var frame = chart.PrepareScenarioStep("create", 1);      // step 2 highlighted, same layout as chart.Prepare()
File.WriteAllBytes("step-2.png", frame.ToPng());

var page = chart.ToInteractiveHtmlPage(new TopologyRenderOptions { ActiveScenarioId = "create" }
    .WithHtmlScenarioPanel()
    .WithHtmlScenarioSequence());

var story = chart.ToScenarioStory("create");             // ChartForgeX.Stories: one scene per step
File.WriteAllBytes("create.gif", story.Prepare().ToGif());

var layout = TopologyCellLayout.Create(chart.Prepare(), new TopologyCellLayoutOptions { MaximumColumns = 100 });
Console.WriteLine(layout.Paint(TopologyCellPaintOptions.ForStep(scenario.Steps[1])).ToText());
Console.WriteLine(scenario.ToText(1));
```

On the `ToInteractiveHtmlPage` page with `WithHtmlScenarioSequence()`, the topology and the sequence diagrams share one type scale. Each fits the column while that costs at most an eighth of its size; past that it keeps its text size and scrolls inside its own frame, so a phone shows readable text instead of a thumbnail. Pages with zoom, export or fullscreen controls, or the selection panel, keep the fitted view.

Scenarios built in code use the same model: `AddMessageStep(edgeId, TopologyScenarioInteraction.Response, "201", reverse: true)`
and `WithPayload(json, TopologyScenarioPayloadFormat.Json)` on a step.

## Interactive keys

When the scenario panel has focus: `n` or Right for the next step, `b` or Left for the previous one, `p` to play or pause,
`r` or Home to restart, `e` or End for the last step.

## Terminal drawing

`TopologyCellLayout` maps the prepared layout into terminal cells. Layout runs once in the topology engine; the
projection only maps that geometry to a character grid, keeping routes orthogonal and nodes wide enough for their
labels. `Paint` returns a `TopologyCellCanvas` whose cells carry their glyph, role (node border, label, edge, arrow,
group border, legend), element id, status and emphasis (highlighted, dimmed, focused, failed). `ToText()` gives plain
text; terminal hosts such as ConsoleMonster map roles and emphasis to their own styles, so they draw the same diagram
without a second layout engine. `TopologyCellPaintOptions` takes live node values and statuses, a focused node, and the
active step.
