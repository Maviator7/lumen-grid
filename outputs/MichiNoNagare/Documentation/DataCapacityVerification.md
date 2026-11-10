# Data capacity verification — 2026-10-30

Mac build (Unity 6000.3.11f1): Success. Artifact: ../LumenGrid-Balanced-Mac/LumenGrid.app. The previous ../LumenGrid-Mac application was left running and unmodified to preserve the user's unsaved city.

Core validation: existing network/tutorial/pointer/reset/legacy checks pass. Seeded generator validates 64 legacy reference maps. Continuous mode validates 6 seeds through 6 north/south expansions each. New data suite: 93 checks.

Regression red: before this change, seed291026 north expansion with the extra data center disconnected still showed recent data reliability 1.0, failing the new <0.9 assertion.

Regression green: new role demand is 6.5/s initially, 9.5/s after the first north medical district. Existing center dispatch limit is 7/s. With every reference wire except the north center connection, power remains100%, steady recent reliability is72.9%, with33 requests waiting. Reconnecting the north center restores recent reliability100%, average latency1.835s, north output5/s, and45 consecutive stable seconds. Recent output and request accounting remain bounded; waiting births survive edits, expired requests are counted, nearest-source saturation falls back to reachable spare capacity, independent centers do not block each other, reset clears queued requests.

Native UI: initial reference city reaches100% electricity/data with one center (1.93s mean latency observed). North expansion preview shows +3/s medical demand. Expanded reference city displays demand9.5/s, connected output14/s, both centers actually sending (4.5/s and5/s), success100%, mean latency1.81s. Resource and status text fits the native window. Captured ../lumen-grid-data-capacity.jpg. Player log contains no exceptions.

Native synthetic pointer actions did not reliably select the center cable, so disconnect/reconnect quality is verified by the core regression above, rather than claimed as a mouse-based end-to-end test. Fixed tutorial logic and map generator geometry are unchanged. Save support remains outside scope.
