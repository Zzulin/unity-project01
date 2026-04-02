---
name: unity-patterns
description: "Unity pattern selector. Use when users want advice on whether to use ScriptableObject, events, interfaces, generics, state machines, object pools, services, or other architecture patterns. Triggers: ScriptableObject, event channel, observer, interface, generic, attribute, state machine, pool, service, pattern, 模式, ScriptableObject架构."
---

# Unity Pattern Selector

Use this skill to decide whether a pattern is justified. Do not recommend every pattern at once.

## Rule

Recommend at most 1-3 patterns, and explain why simpler options are not enough.

## Pattern Guide

- `ScriptableObject`
  - Use for authored config, shared static data, event channels, and reusable data assets.
  - Avoid as the default home for per-run mutable gameplay state.

- `C# events / delegates`
  - Use for one-to-many notifications with clear ownership and unsubscribe points.
  - Avoid for imperative flows that need ordering, return values, or complex debugging.

- `Global event bus / observer hub`
  - Use sparingly and only when many systems truly need broad decoupled notifications.
  - Avoid as the default answer to coupling. It often hides ownership and makes debugging harder.

- `Interfaces`
  - Use when multiple implementations or clearer dependency boundaries are needed.
  - Avoid adding interfaces around every class without a real seam.

- `State machine`
  - Use for actors with mutually exclusive states and explicit transitions.
  - Avoid when a few booleans or a small command flow is enough.

- `Object pool`
  - Use for frequent spawn/despawn of bullets, VFX, enemies, UI items.
  - Avoid for rare objects or when lifetime is simple.

- `Service layer`
  - Use for a small number of cross-scene systems with explicit bootstrap and interfaces.
  - Avoid turning everything into hidden singletons or service locators.

- `Generics / custom attributes`
  - Use when they remove repeated boilerplate with clear type safety or editor metadata value.
  - Avoid when they make gameplay code harder to read than duplicated simple code.

## Output Format

- Recommended pattern(s)
- Why they fit this case
- Why not the simpler alternative
- Minimal implementation boundary
- Known tradeoffs
