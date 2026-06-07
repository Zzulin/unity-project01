---
name: unity-history
description: "Undo/redo history management (Unity Editor native undo stack). Use when users want to undo or redo one or many steps, or get current undo group name. Triggers: history, undo, redo, undo steps, redo steps, undo history, revert, previous state, current undo group, 历史, 撤销, 重做, 多步撤销, 多步重做, 撤销历史, 上一步, 当前撤销组."
---

# History Skills

Manage Unity Editor undo/redo history.

## Operating Mode

本模块全部 3 个 skill (`history_undo` / `history_redo` / `history_get_current`) 均标 `SkillMode.SemiAuto`，Approval / Auto / Bypass 三档下都可直接执行。**不含 NeverInSemi 高危 skill**。

**DO NOT** (common hallucinations):
- `history_list` / `history_get` do not exist → use `history_get_current` for current undo group
- `history_clear` does not exist → Unity undo history cannot be cleared via API
- `history_save` does not exist → undo history is managed by Unity automatically

**Routing**:
- For simple undo/redo → `history_undo` / `history_redo` (this module) or `editor_undo` / `editor_redo`
- For persistent task-level undo → use `workflow` module
- For conversation-level undo → use `workflow` module's `workflow_session_undo`

## Skills

### `history_undo`
Undo the last operation.
**Parameters:**
- `steps` (int, optional, default 1): Number of operations to undo.

### `history_redo`
Redo the last undone operation.
**Parameters:**
- `steps` (int, optional, default 1): Number of operations to redo.

### `history_get_current`
Get current undo history state.
**Parameters:** None.

## Exact Signatures

Exact names, parameters, defaults, and returns are defined by `GET /skills/schema` or `unity_skills.get_skill_schema()`, not by this file.
