---
name: unity-scriptdesign
description: "Script quality advisor for Unity gameplay code. Use when users want to review or improve coupling, maintainability, performance, responsibility boundaries, or script structure before or after generating code. Triggers: coupling, maintainability, performance, refactor script, clean code, script quality, 低耦合, 可维护, 性能优化."
---

# Unity Script Design Review

Use this skill before creating gameplay scripts, or after scripts are generated and need a design pass.

## Review Checklist

- Responsibility: does the script have one clear job?
- Role: should it really be a `MonoBehaviour`, `ScriptableObject`, or plain C# class?
- Coupling: are dependencies explicit instead of hidden globals or deep scene lookups?
- Communication: should this be a direct reference, interface call, or event?
- Performance: is there unnecessary `Update`, repeated `Find`, avoidable allocation, or reflection in hot paths?
- Lifecycle: are subscriptions, timers, and async work cleaned up clearly?
- Inspector UX: are serialized fields private, grouped, and explained?
- Testability: can the core logic move into a plain C# class?
- Naming: do class and field names explain intent without cryptic abbreviations?

## Guardrails

- Prefer descriptive names over local shorthand.
- Do not “optimize” readability away for imagined productivity gains.
- Do not recommend complex patterns if a smaller refactor fixes the real problem.

## Output Format

- Keep: what is already good
- Simplify: what should stay straightforward
- Refactor: the highest-value structural change
- Performance notes: only real hotspots, not theoretical micro-optimizations
- Maintainability notes: naming, ownership, coupling, editor usability
