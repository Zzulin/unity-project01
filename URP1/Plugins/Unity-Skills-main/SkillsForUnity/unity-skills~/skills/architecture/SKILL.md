---
name: unity-architecture
description: "Gameplay and system architecture advisor for Unity mini-games. Use when users want module boundaries, scene/bootstrap design, SOLID-oriented structure, decoupling, maintainability, or refactoring direction before writing lots of code. Triggers: architecture, system design, module, SOLID, decouple, maintainable, mini-game architecture, gameplay framework, Unity架构, 解耦, 可维护."
---

# Unity Architecture Advisor

Use this before generating lots of gameplay scripts or when the user asks for a cleaner architecture.

## Workflow

1. Identify scope: prototype, small game, or long-lived project.
2. Define the core loop and the minimum runtime systems needed.
3. Recommend the smallest architecture that fits the scope.
4. Separate:
   - scene/bootstrap layer
   - gameplay/domain logic
   - data/config assets
   - view/presentation layer
5. Call out what should stay simple now vs what is worth abstracting.

## Output Format

When using this skill, structure the advice as:

- Project tier: prototype / small-game / long-lived
- Recommended modules: 3-7 modules with one-line responsibilities
- Scene/bootstrap plan: where composition and initialization happen
- Data ownership: what belongs in scene objects, ScriptableObjects, or pure C# classes
- Communication rules: direct refs, interfaces, events, or commands
- Performance risks: only the hot paths that matter
- Do now / skip now: avoid over-engineering

## Default Guidance

- Prefer thin `MonoBehaviour` scripts as composition bridges.
- Put reusable gameplay rules in plain C# classes when possible.
- Use `ScriptableObject` for authored config and shared static data, not as a default dump for runtime state.
- Keep dependencies explicit. Avoid hidden global state unless the project size clearly justifies a small service layer.
- Favor simple module boundaries over framework-heavy architecture.

## Guardrails

- Do not start from a giant reusable framework unless the project truly needs it.
- Do not add layers just to satisfy textbook SOLID wording.
- Prefer a small architecture that can grow, not an impressive one that slows iteration.

## Load Related Advisory Modules When Needed

- Pattern choice: see [`../patterns/SKILL.md`](../patterns/SKILL.md)
- Async / Update / UniTask decisions: see [`../async/SKILL.md`](../async/SKILL.md)
- Inspector-facing field design: see [`../inspector/SKILL.md`](../inspector/SKILL.md)
- Script quality review: see [`../scriptdesign/SKILL.md`](../scriptdesign/SKILL.md)
