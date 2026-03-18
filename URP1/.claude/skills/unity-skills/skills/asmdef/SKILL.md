---
name: unity-asmdef
description: "Assembly definition advisor for Unity projects. Use when users want better module boundaries, faster iteration, cleaner dependencies, or editor/runtime/test assembly separation. Triggers: asmdef, assembly definition, module boundary, compile time, dependency graph, 程序集定义, 模块边界."
---

# Unity asmdef Advisor

Use this skill when the project is large enough that compile boundaries and dependency direction matter.

## Recommend Only When Worth It

`asmdef` is usually worth discussing when:

- the project has multiple domains/systems
- editor code and runtime code are mixed
- compile times are becoming noticeable
- tests should be isolated cleanly

## Output Format

- Whether `asmdef` is justified now
- Proposed assemblies
- Allowed dependency direction
- Editor/runtime/test split
- Migration steps
- Risks or churn to avoid

## Default Guidance

- Prefer a few meaningful assemblies over many tiny ones.
- Split editor code from runtime first.
- Keep the dependency graph directional and shallow.

## Guardrails

- Do not introduce `asmdef` fragmentation for a tiny prototype.
- Do not create circular dependencies or force everything through a shared dumping-ground assembly.
