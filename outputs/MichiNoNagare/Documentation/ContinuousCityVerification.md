# Continuous city verification — 2026-10-30

- Unity 6000.3.11f1 Mac build: success. BuildMac runs the complete core validation suite before building. See Verification.txt.
- New continuous suite: 6 seeds (291026, 42, -19, 0, 12345, int.MinValue), 6 alternating north/south expansions each. Validates stable unlock, both candidate reference networks, non-mutating planning, preserved wiring/time/delivery counters, disconnected new demand, increased resource caps, repeat stabilization, live edit/undo, no terminal result, and root reset.
- Existing network, seeded-city (64 generated reference rounds), tutorial, reset, pointer and legacy traffic checks pass.
- Native Mac keyboard QA: main runs past 45 seconds; X opens expansion offers with demand/resource preview; 1 opens north, subsequent 2 opens south; district count/resources increase and uptime continues; F2 restores supply for all open sectors, unlocking another expansion.
- Native reset QA: F1 opens confirmation, plain Enter cancels, Cmd+Enter resets to original 3 districts. Fixed confirm-key leakage into pause/resume and verified LIVE continues after reset.
- Expanded geometry uses growing foundation/canal and dynamic facility roles. V / bottom-right control frames the whole city. New-sector labels show facility names; selecting them shows role details.
- Mouse cable editing and dragging were not re-tested end-to-end in this session because synthetic pointer coordinates in Unity were unreliable; existing gesture checks pass. No production input was changed to accommodate automation.
- Saving progress, free placement of hubs, and final difficulty balancing are outside this increment. Closing the app does not preserve progress.
