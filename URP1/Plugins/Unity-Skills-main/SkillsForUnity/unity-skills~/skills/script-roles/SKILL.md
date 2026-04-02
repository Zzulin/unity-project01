---
name: unity-script-roles
description: "Script role planner for Unity. Use when users want to decide which planned scripts should be MonoBehaviour bridges, ScriptableObject configs, pure C# services, presenters, states, or installers before batch code generation. Triggers: script roles, class roles, MonoBehaviour or ScriptableObject, service class, presenter, installer, 脚本职责, 类职责."
---

# Unity Script Roles

Use this skill before creating a batch of gameplay scripts.

## Goal

Turn a rough script list into explicit roles so AI does not generate everything as `MonoBehaviour`.

## Output Format

- Script name
- Recommended role
- Main responsibility
- Main dependencies
- Why this role fits better than the alternatives

## Common Roles

- `MonoBehaviour` bridge
- `ScriptableObject` config/data
- pure C# domain/service
- presenter / controller
- state / state machine node
- installer / bootstrap helper

## Guardrails

- Do not make every class a `MonoBehaviour`.
- Do not force `ScriptableObject` onto runtime state that should stay in memory-only objects.
