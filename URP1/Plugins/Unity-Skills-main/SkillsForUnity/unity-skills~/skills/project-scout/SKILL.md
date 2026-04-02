---
name: unity-project-scout
description: "Unity project reconnaissance advisor. Use when users want architecture or refactoring advice in an existing project and the AI should inspect the current Unity version, packages, asmdefs, folder layout, coding patterns, and constraints before proposing changes. Triggers: inspect project, scout project, project baseline, current architecture, existing structure, 项目画像, 项目侦察, 现有架构."
---

# Unity Project Scout

Use this before recommending architecture changes in an existing project.

## Inspect First

Collect only the information needed to avoid clashing with the current project:

- Unity version and render pipeline
- Installed packages and notable dependencies
- `asmdef` layout, if any
- Folder structure under `Assets/`
- Whether the project already uses:
  - `ScriptableObject` config
  - service/singleton patterns
  - event-driven flows
  - custom inspectors/property drawers
  - tests
- Existing naming and code organization style

## Suggested Tools / Inputs

- Unity project info and project settings
- Script/file search for patterns
- Local inspection of `Packages/manifest.json`, `Assets/`, and `*.asmdef`

## Output Format

- Technical baseline
- Existing architectural signals
- Existing conventions worth preserving
- Existing risks or inconsistencies
- Constraints for future suggestions
- Unknowns that still need confirmation

## Guardrails

- Do not propose a clean-slate architecture if the project already has a consistent pattern.
- Do not recommend new dependencies until the current stack is clear.
