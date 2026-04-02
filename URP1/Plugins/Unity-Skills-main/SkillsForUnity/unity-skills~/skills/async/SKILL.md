---
name: unity-async
description: "Unity async and lifecycle strategy advisor. Use when users want to choose between Update, events, coroutine, UniTask, timers, IDisposable, or cancellation/lifecycle cleanup. Triggers: UniTask, coroutine, async, await, Update, timer, polling, IDisposable, cancellation, 生命周期, 协程, 异步."
---

# Unity Async Strategy

Use this skill when the user is deciding how runtime work should be scheduled or cleaned up.

## Decision Ladder

1. First ask whether the task needs per-frame work at all.
2. If not, prefer events, callbacks, or explicit method calls.
3. If a short Unity-bound sequence is needed, prefer coroutine.
4. Recommend `UniTask` only when:
   - the project already uses it, or
   - the user explicitly wants it and accepts the dependency.
5. Use `Update` only for true continuous simulation, polling, or input loops that cannot be event-driven.

Do not recommend `UniTask` just because it looks more advanced than coroutine.

## Specific Guidance

- Avoid many unrelated `Update` methods if a more event-driven flow works.
- Cache references used in hot paths.
- Always define lifecycle ownership:
  - who starts the work
  - who cancels or stops it
  - when it is cleaned up
- In `MonoBehaviour`, prefer `OnEnable` / `OnDisable` / `OnDestroy` for subscribe-unsubscribe symmetry.
- Use `IDisposable` mainly for pure C# lifetimes, temporary subscriptions, or scope-based cleanup helpers, not as a cargo-cult replacement for Unity lifecycle methods.

## Output Format

- Recommended scheduling model
- Why it fits
- Lifecycle / cancellation owner
- Hot-path risks
- Why the heavier alternative is unnecessary, if applicable
