# Topology Physics Experiments

This folder is intentionally separate from the release-ready topology examples.

Static SVG/PNG exports are useful for readable topology diagrams: replication paths, site links, routing, firewall paths, bridgehead relationships, hierarchy fan-out, and focused dependency maps. They are not a good medium for thousands of individual users or devices rendered as dots.

Rejected experiment direction:

- Rendering 5000 real users as SVG/PNG dots.
- Treating a static dot cloud as a readable substitute for interactive graph exploration.
- Adding large-graph physics optimizations only to produce an unreadable report snapshot.

Current product direction:

- Keep static exports readable, usually dozens of labeled objects and carefully bounded dense diagrams.
- Use aggregate cards, sampled cohorts, issue tables, drill-through links, and health summaries for large populations.
- Reserve 500/5000+ individual-object exploration for a future interactive renderer with zoom, filtering, clustering, selection, and level-of-detail, likely canvas/WebGL rather than pure SVG.
