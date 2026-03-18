---
name: unity-adr
description: "Architecture decision record helper for Unity projects. Use when users want to compare options, lock in a design choice, or keep future AI/codegen steps consistent across multiple turns. Triggers: ADR, architecture decision, tradeoff, choose pattern, design choice, 设计决策, 架构决策."
---

# Unity ADR

Use this when architecture choices may be revisited later or when multiple plausible options exist.

## Output Format

- Decision
- Context
- Options considered
- Chosen option
- Why this option won
- Consequences
- Revisit triggers

## Example Use Cases

- Coroutine vs UniTask
- Direct reference vs event-driven communication
- ScriptableObject config vs in-scene authoring
- One assembly vs multiple `asmdef`
- Runtime logic in `MonoBehaviour` vs pure C# service

## Guardrails

- Keep ADRs short.
- Record only decisions that materially affect code generation or architecture direction.
