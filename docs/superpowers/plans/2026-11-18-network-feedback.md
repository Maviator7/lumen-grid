# Network Feedback Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development for the independent work and verify the integrated build.

**Goal:** Give players readable network problems, reversible experiments, and varied expansion puzzles.

**Architecture:** Keep simulation and save logic in Core. Observe live packet delays and clone saved state for experiments. Use the existing Unity map, inspector and modal input system for presentation.

**Tech Stack:** Unity C#, standalone Mono checks, Unity Web build.

- [x] Implement relay specialization per the November 13 spec. Verify limits, mixed traffic and old saves.
- [x] Introduce distinct expansion demand profiles with offer descriptions and simulation validation.
- [x] Capture packet blocking events and per-district routes. Implement isolated peak simulation and compare live state before and after.
- [x] Add the overview action, diagnostic overlays, trial and retrofit panels. Verify modal input and small viewports.
- [x] Run core regression and build into an isolated directory. Exercise the browser build on a separate origin and preserve the live save.
