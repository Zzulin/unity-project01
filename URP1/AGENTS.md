# AGENTS.md

## 协作约定
- 默认中文输出；用户明确要求英文时再切换。
- 重要进展只同步到本文件和 `codex/tasks.md`（保持简洁，不写长历史）。

## 可用技能
- `unity-skills`：通过 REST API 自动化控制 Unity Editor。

## 项目任务概览
- 目标场景：`Assets/L11 NPR/L11.unity`
- 当前目标：打通 StarRail NPR 角色渲染联调链路（管线、RendererFeature、材质、Shader）
- 关键路径：
  - 管线资源：`Assets/Settings/NPR Render Pipeline.asset`
  - Renderer：`Assets/Settings/NPR Render Pipeline Asset_Renderer.asset`
  - Feature：`Plugins/StarRailNPRShader-main/Runtime/StarRailRendererFeature.cs`
  - 角色 Shader：`Plugins/StarRailNPRShader-main/Shaders/Character/*`
  - 快速核对工具：`Assets/L11 NPR/Editor/L11NprContextReporter.cs`（菜单 `Tools/NPR/输出 L11 上下文报告`）

## 当前结论（短版）
- Graphics 默认 RP 指向 `NPR Render Pipeline.asset`，当前质量档 `High` 指向 `UniversalRP-HighQuality.asset`。
- `0_mesh_mesh` 材质槽位以场景内嵌 `(Instance)` 为主，`hair` 是 `CharHair`，多数身体/面部槽位仍是 URP Lit。
- `CharBody.shader` 还未稳定进入当前主体材质链路，改 Shader 前要先确认材质治理策略。
