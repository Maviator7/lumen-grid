# Supply control HUD revision

The HUD uses a 1440×900 reference space, uniformly fitted to the available window. All input exclusion regions and projected world labels convert through the same scale. This addresses fixed pixel controls becoming tiny in Retina or large windows.

Top: persistent Japanese supply metrics, segmented status bars, launch/pause/redesign. Bottom: capacity meters, five icon tools, undo/speed and camera controls. Chamfered frames, cyan selection rails and amber power coding replace conventional filled buttons. Tool names remain visible alongside icons and shortcuts. Editing is disabled during simulation/report; inspection remains available via the world.

Validation: native window visual checks, keyboard tool selection, running/report states, shader/runtime log scan, and core checks through the Mac build.

Facility name/role labels use text with a subtle dark shadow and no background plates. Tutorial title, body and hints are measured with GUIStyle.CalcHeight including Japanese glyph metrics, newlines and wrapping; the footer buttons and panel extent follow content height. The input exclusion rectangle follows the rendered guide panel.

Inspector follow-up: node and conduit panels now measure every heading, data row and paragraph, add spacing cumulatively, and derive their frame/hit region from the rows. The shared Text renderer enforces a measured minimum label height for all HUD text. Removed inspector headline rectangles of 28/32 units that clipped Japanese glyphs.

Dedicated quota clarity: persistent footer states that power-only and data-only share a total limit of three, shows used/remaining numbers, and distinguishes power/data in the slot indicators. Selecting either dedicated tool shows counts by type, total allowance and recovery instructions. Tools remain usable at the cap because repainting an existing dedicated line does not spend another slot.

Facility palette (2026-11-04): names keep a stable role color—reactor amber, data core cyan, relay neutral white, industrial violet, housing pink, medical mint. Supply warnings appear on a separate amber line prefixed with ! and never recolor the name. The inspector code, title and border reuse the facility color. A six-entry legend appears in the footer without name background plates. Tested overview, unmet supply warning, selected housing panel and footer layout at 1280×720; Web console errors none. Web/Mac builds successful.
