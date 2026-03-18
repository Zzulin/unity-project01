---
name: unity-testability
description: "Unity testability advisor. Use when users want to improve testability, isolate pure logic from Unity APIs, decide what belongs in EditMode or PlayMode tests, or reduce hard-to-test MonoBehaviour logic. Triggers: testability, unit test, editmode, playmode, isolate logic, 测试性, 单元测试."
---

# Unity Testability Advisor

Use this skill when deciding what logic should remain in Unity-facing classes and what should move into pure C# code.

## Review Questions

- Can the rule/algorithm run without `Transform`, `GameObject`, or scene state?
- Can config be injected instead of read through static globals?
- Can runtime decisions be moved to a plain C# class and called from a thin `MonoBehaviour`?
- Does this need PlayMode coverage, or is EditMode enough?

## Output Format

- Logic that should move to pure C#
- Logic that should stay Unity-facing
- Suggested seams/interfaces
- Candidate EditMode tests
- Candidate PlayMode tests

## Guardrails

- Do not force test seams everywhere if the script is tiny and scene-bound.
- Prefer a few meaningful seams over abstraction for its own sake.
