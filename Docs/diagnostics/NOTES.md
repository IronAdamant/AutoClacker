# Diagnostics — 2026-09-07 hang

Saved copies:

- `debug.log` — live file from `~/Library/Application Support/AutoClacker/debug.log`
- `hang-2026-09-07-001810.excerpt.txt` — first 400 lines of the 6.3MB hang report
- Full hang: `/Library/Logs/DiagnosticReports/AutoClacker_2026-09-07-001810_Arons-MacBook-Pro.hang`

## Timeline

| When | What |
|------|------|
| 00:03:31 | AutoClacker launched from Terminal/zsh (PID 36630) |
| ~15 min later | UI became unresponsive |
| 00:18:02 | macOS sampled a **hang** (870s, unresponsive ~869s). Not a .crash. |
| 12:10 | After restart, keyboard session ran (A @ 1597ms ×7, then A @ 10ms ×456). Those lines are in `debug.log`. |

The hang itself is **not** in `debug.log` (file logging was off or not yet writing at 00:03).

## Hang shape

- Main thread: `NSApplication run` → `_handleEvent` / `sendEvent` (Avalonia Native OSX). UI freeze, not a managed exception.
- No `MacHotkeyListener` thread in the sample.
- `.NET Timer` last ran ~868s before the sample (stuck for the hang duration).
- Thread-pool workers still waking.

## Causes addressed in code

1. **Mac hotkey run loop dies immediately.** Every start logs `Run loop started` then `Run loop exited` in the same millisecond. The tap source was added under a homemade `"kCFRunLoopCommonModes"` string (a *mode name*, not Apple’s common-modes set) while `CFRunLoopRun()` runs `kCFRunLoopDefaultMode` — no sources in the run mode, loop returns, then the thread `CFRelease`s the tap while fields still hold the pointers. Later UI/CF use can PAC-check / hang.
2. **Injected keys hit AutoClacker if it is focused.** `CGEventPost(kCGHIDEventTap)` delivers to the frontmost app. Keyboard mode at 10ms can flood our own window (`sendEvent` on the main thread). Swallow window key/text input while a session is running (the global hotkey is outside the window).
3. **Settings JSON pollution.** Computed `HasFiniteActionLimit` / `*KeyToken` were serialized. `[JsonIgnore]` those.
4. **Log timestamps had no date**, which made the 00:18 vs 12:10 sessions hard to separate.

Unhandled exceptions are now written to the debug log when it is enabled.
