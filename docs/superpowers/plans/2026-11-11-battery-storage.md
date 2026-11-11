# Battery Storage Implementation Plan

> Execute inline on a feature branch in the existing suitable Unity checkout; retain Unity cache and canonical save server. Use test-driven-development and verification-before-completion. One advisory reviewer checks this spec and plan.

**Goal:** Add finite, automatically charged relay batteries with useful placement trade-offs and durable saves.
**Architecture:** Core/BatteryStorage owns battery states and power allocation after ordinary reactor supply. Existing relay node kinds and routes remain compatible. Runtime partials own HUD/actions and pooled-per-scene battery cells. Optional save fields preserve classic snapshots.
**Tech Stack:** Unity 6000.3.27f2, C# core tests, IMGUI, existing Web save bridge.

- [x] 1. Core failing tests in Editor/BatteryChecks.cs and Tools/check-battery.sh: installing/removing and slots; reactor-priority charging; exact 90% energy; peak discharge; finite depletion; no battery-to-battery; data-only, disconnected and saturated paths; no energy change with edits; snapshot isolation and resumed simulation. Run RED before Core/BatteryStorage.cs implementation.
- [x] 2. Implement Core/BatteryStorage.cs. States keyed by relay ID, readonly public state and dictionary ownership; caps120MJ/2MW/4MW/.9. Reuse fair .125 flow increments and common capacity/load recording, with discharge after district reactor allocation and charge using only remaining reactor power. Energy changes only in positive simulation steps. NetworkSimulation.AllocatePower accepts dt=0 preview, Step calls it if batteries exist; no-battery behavior untouched. ResetExpansionState clears equipment. PlanningClone copies equipment independently with empty charge for reference validation.
- [x] 3. NetworkSave optional Batteries, deep capture and restore with range/type/duplicate/count validation, then nonmutating flow preview. Add Unity JSON BatterySaveChecks and core save resume tests. Wire battery tests into check-core explicit source lists and ProjectTools.Validate.
- [x] 4. Runtime/BatteryGame.cs: install/removal guard and two-click removal confirmation, save after changes. Runtime/BatteryHud.cs: bounded selected-relay inspector and summary, state explanations/peak countdown and action affordances. Preserve old inspector when tutorial active. Labels and PowerInspection show storage as an actual supply source. Runtime/BatteryView.cs: scene-owned compact metal cells and independent green lamps based on charge and sim time, no per-frame allocation; clear on scene disposal. Refresh equipment visuals without resetting camera/selection.
- [x] 5. Run battery then full core tests. Staged Web build runs JSON checks. Test at separate8769 using only test fixture; manual equipment install/charge/discharge and reload, invalid connection and removal confirmation. Verify1280x720 and larger viewport. Review implementation before publication and fix issues with regression tests.
- [x] 6. Document and update AGENTS. Preserve screenshot/log proof. Atomically publish staged Web preserving old build and live8766 server. Commit as Maviator7 using `✨: 蓄電所と需要ピークへの電力補助を追加`, normal integration/push only; never include saves. Check git diff and remote state.

## Advisory review resolution

Own discharge counters are separate from relay transit. Aggregate energy/headroom limits cover all destinations/sources. dt=0 previews never integrate. Charging traces are separate from district delivery traces; inspect transit includes both. Tests cover multiple targets/sources and save resume near empty/full. Removal confirmation is bound to current selection and simulation identity.

Public runtime properties now separate from SavedBattery DTOs. Editor-only fixtures seed internal state by reflection; no runtime debug input. Preview dt=0 preserves saved quality/stability; only positive steps can cancel stability for depleted batteries. Unity JSON comparison tolerates1e-9 MJ for serializer rounding (observed4e-15 MJ); simulation round-trips remain within tolerance.

## Final review and verification

Power diagnostics and read-only live state were preserved after advisory review. Added failures and fixes for zero-step quality preservation, next-tick bypass discharge, and overlapping two-battery charging/discharging. 5,520 standalone checks and125Unity JSON checks pass. Cache trajectories match fresh allocation over1,000multi-battery steps and the uncached3-seed300second comparison. A full140simulated-second UI run after reinstall demonstrates charging, a complete residential peak, discharge, then recharge and45seconds stable; final energy55.59MJ. Candidate picker action completed in361ms in the final Web test.
