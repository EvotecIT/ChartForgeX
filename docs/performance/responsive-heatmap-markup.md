# Responsive heatmap markup budget

Goal: at most 262,144 UTF-8 bytes of mounted host markup for the 12-by-30 linked
fixture, including the inline SVG, specification and runtime. The fixture has
28 missing observations and 332 links. The 150-by-30 fixture is measured without
an imposed ceiling.

On Win-PC, 28 October 2026, the original source at `8ff843511` produced 1,212,469
serialized bytes for 360 positions and 13,897,172 for 4,500 positions. The
`ResponsiveHeatmapMarkupBudgetTests` fixture and browser test reproduce the
measurements and assert the mounted budget, link names and responsive widths.

| Attempt | 360 serialized | 360 mounted | 4,500 serialized | 4,500 mounted | Disposition |
| --- | ---: | ---: | ---: | ---: | --- |
| Two complete SVGs | 1,212,469 | not measured | 13,897,172 | not measured | Replaced |
| One base SVG | 545,156 | not measured | not measured | not measured | Insufficient alone |
| One SVG, compact linked cells and shared patterns | 244,449 | 255,864 | 2,351,413 | 2,471,801 | Kept; browser proof preserves patterned-edge pixels and link names |
| Responsive row pitch, missing-cell outlines and key | 242,896 | 254,105 | 2,401,665 | 2,512,911 | 29 October; mounted at 1440 px, with the 360-position budget also checked at 390 and 1024 px |

Preparing the base at print width made this fixture's fallback text larger, but
lost responsive legend information and shifted sparse bar captions in the broader
browser suite. That attempt was reverted; the maximum-width base remains necessary
for those existing contracts.

The budget measures UTF-8 bytes of the mounted host's outerHTML. The browser
serializes the live SVG after reflow; these numbers are not compressed transfer
sizes. Keep all rows and missing observations in the fixture. Future optimization
should retain accessible links, native tooltips, the print fallback and SVG/PNG
parity; stop when those contracts and the budget pass.
