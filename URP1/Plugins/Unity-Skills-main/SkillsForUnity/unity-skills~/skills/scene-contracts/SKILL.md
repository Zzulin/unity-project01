---
name: unity-scene-contracts
description: "Scene composition contract advisor for Unity. Use when users want to define required scene objects, component dependencies, bootstrap objects, reference wiring, and validation rules so scenes stay explicit and maintainable. Triggers: scene contract, bootstrap scene, required references, scene wiring, installer object, 场景契约, 场景装配."
---

# Unity Scene Contracts

Use this skill when scene setup needs to be explicit instead of relying on hidden runtime lookups.

## Define

- Required root objects
- Required components on each root
- Which references are assigned in Inspector
- Which objects act as bootstrap/installers
- Which objects are runtime-spawned
- Which assumptions should be validated early

## Output Format

- Scene object contract
- Bootstrap sequence
- Inspector wiring rules
- Validation rules
- Hidden dependency risks

## Guardrails

- Prefer explicit scene wiring over chains of runtime `Find`.
- Keep bootstrap objects small and focused.
