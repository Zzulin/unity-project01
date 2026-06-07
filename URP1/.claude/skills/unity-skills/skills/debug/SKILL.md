---
name: unity-debug
description: "Debug, diagnostics and compile state. Read console errors, force recompile, fetch stack traces, list assemblies, manage scripting define symbols, sample memory, and triage editor health. Triggers: debug, diagnostics, triage, error, exception, warning, console error, compilation, recompile, force recompile, refresh, stack trace, assembly, define symbols, scripting defines, memory usage, mono heap, system info, unity version, editor health, unity_diagnose, debug_get_errors, debug_get_logs, debug_check_compilation, debug_force_recompile, debug_get_stack_trace, debug_get_defines, debug_set_defines, debug_get_memory_info, 调试, 诊断, 错误, 异常, 警告, 控制台错误, 编译, 重编译, 强制重编, 堆栈, 调用栈, 程序集, 宏定义, 脚本宏, 平台宏, 内存, 内存占用, Mono 堆, 系统信息, Unity 版本, 域重载, 编辑器健康度."
---

# Debug Skills

Debug utilities for error checking and diagnostics.

## Operating Mode

- **Approval**: 只读类 skill（`unity_diagnose` / `debug_get_errors` / `debug_get_logs` / `debug_check_compilation` / `debug_get_system_info` / `debug_get_stack_trace` / `debug_get_assembly_info` / `debug_get_defines` / `debug_get_memory_info`，全部标 `SkillMode.SemiAuto`）直接执行；`debug_force_recompile` / `debug_set_defines` 默认 `SkillMode.FullAuto`，需用户 grant，grant 后一步执行返结果。
- **Auto / Bypass**: 直接执行。
- **本模块含 Reload 类高危 skill**：`debug_force_recompile`（标 `MayTriggerReload=true`）必然触发 Domain Reload；`debug_set_defines` 修改 `PlayerSettings` 的 scripting defines 也会触发重编译 —— 这些 skill 在 Approval / Auto 下会被 `IsForbiddenInSemi` 自动拦截，**仅 Bypass 或 Allowlist 命中可执行**，调用后服务端会短暂不可用。

**DO NOT** (common hallucinations):
- `debug_compile` / `debug_recompile` do not exist → use `debug_force_recompile`
- `debug_run` does not exist → use `editor_play` (editor module)
- `debug_clear` does not exist → use `console_clear` (console module)
- `debug_set_defines` triggers Domain Reload — server will be temporarily unavailable

**Routing**:
- For runtime console logs → use `console` module's `console_get_logs` / `console_start_capture`
- For play mode control → use `editor` module
- For script compile feedback → use `script` module's `script_get_compile_feedback`

## Skills

### `unity_diagnose`
**Aggregated Editor health snapshot — call this FIRST when triaging problems.** Combines console errors, compile state, recent workflow tasks, recent jobs, and server stats in a single response. Avoids chaining 4-5 individual skills.

**Parameters:**
- `errorLimit` (int, optional, default 20, range 1-200): Max console entries to return.
- `includeWarnings` (bool, optional, default true): Include warnings (false = errors only).
- `includeRecentJobs` (bool, optional, default true): Include the 10 most recent async jobs.

**Returns:** `{ summary: { healthy, consoleErrorCount, consoleWarningCount, isCompiling, serverRunning, hint }, compile, console, workflow, server, recentJobs }`

### `debug_get_logs`
Get console logs filtered by type and content.
**Parameters:**
- `type` (string, optional): Filter by type (Error/Warning/Log). Default: Error.
- `filter` (string, optional): Filter by content.
- `limit` (int, optional): Max entries. Default: 50.

### `debug_get_errors`
Get only active errors and exceptions from console.
**Parameters:**
- `limit` (int, optional): Max entries. Default: 50.

### `debug_check_compilation`
Check if there are any compilation errors.
**Parameters:** None.

### `debug_force_recompile`
Force Unity to recompile all scripts.
**Parameters:** None.

### `debug_get_system_info`
Get system and Unity environment information.
**Parameters:** None.

### `debug_get_stack_trace`
Get stack trace for a log entry by index.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| entryIndex | int | Yes | - | Index of the log entry to retrieve stack trace for |

**Returns:** `{ index, message, stackTrace }`

### `debug_get_assembly_info`
Get project assembly information.

**Parameters:** None.

**Returns:** `{ success, count, assemblies }`

### `debug_get_defines`
Get scripting define symbols for current platform.

**Parameters:** None.

**Returns:** `{ success, buildTargetGroup, defines }`

### `debug_set_defines`
Set scripting define symbols for current platform.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| defines | string | Yes | - | Scripting define symbols to set |

**Returns:** `{ success, buildTargetGroup, defines, serverAvailability }`

### `debug_get_memory_info`
Get memory usage information.

**Parameters:** None.

**Returns:** `{ success, totalAllocatedMB, totalReservedMB, totalUnusedReservedMB, monoUsedSizeMB, monoHeapSizeMB }`

---
## Exact Signatures

Exact names, parameters, defaults, and returns are defined by `GET /skills/schema` or `unity_skills.get_skill_schema()`, not by this file.