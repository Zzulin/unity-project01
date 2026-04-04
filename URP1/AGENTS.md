# AGENTS.md

## 协作约定
- 默认中文输出；用户明确要求英文时再切换。
- 重要进展只同步到本文件和 `codex/tasks.md`（保持简洁，不写长历史）。

## 可用技能
- `unity-skills`：通过 REST API 自动化控制 Unity Editor。

## 学习参考仓库（跨端可直接识别）
- `StarRailNPRShader`（GPL-3.0，已归档）  
  https://github.com/stalomeow/StarRailNPRShader
- `UnityURPToonLitShaderExample`（MIT）  
  https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

## 本地目录（统一使用仓库相对路径，便于 Windows/macOS）
- `UnityURPToonLitShaderExample`：`Assets/UnityURPToonLitShaderExample-master`
- `StarRailNPRShader`：`Plugins/StarRailNPRShader-main`

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
- 学习与实现基线收敛为 2 个仓库：`UnityURPToonLitShaderExample` + `StarRailNPRShader`；执行节奏为 1 周冲刺（Demo 优先，不追全特性完美），以 `codex/tasks.md` 为准。
- 计划已细化为“面试向一周 Demo”，每天包含：实现目标、验收产出、可讲述技术点。
- `L11NprContextReporter` 已增强：新增活跃 URP Renderer/默认 RendererFeature 列表与 StarRailFeature 命中提示，并输出角色材质槽位统计（CharBody/Face/Hair、URP Lit、Embedded）。
- 本地编译校验已通过：`dotnet build Assembly-CSharp-Editor.csproj`（0 error）。
