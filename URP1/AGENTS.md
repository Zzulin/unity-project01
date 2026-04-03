# AGENTS.md

本文件用于声明本项目中 AI 代理（如 Codex）的技能、上下文与协作约定。

## 项目协作约定
- 默认使用中文沟通与输出。
- 如用户当次明确要求英文，则按该次要求切换语言。




## 可用技能
- unity-skills：通过 REST API 自动化控制 Unity Editor。

## 当前开发上下文（由 Codex 维护）

- 当前目标：围绕 `Assets/L11 NPR/L11.unity` 场景继续调试和整理 StarRail NPR 角色渲染工作流，并为 Mac 端同步一份可直接接手的上下文。
- 当前进度：
  - 已确认当前活动场景实际为 `L11`，路径为 `Assets/L11 NPR/L11.unity`。
  - 已确认项目运行环境为 Unity `2022.3.62f3c1`，当前工程开启了 UnitySkills 本地服务，可直接读取 Editor 实时状态。
  - 已确认项目使用 `com.unity.render-pipelines.universal@14.0.12`，并接入本地包 `com.stalomeow.star-rail-npr-shader`。
  - 已确认当前角色场景非常精简，根对象只有 `Main Camera`、`Directional Light`、`Avatar_Ruanmei_Body`。
  - 已确认 `Avatar_Ruanmei_Body` 挂有 `StarRailCharacterRenderingController`，其子物体 `0_mesh_mesh` 使用 `SkinnedMeshRenderer` 渲染角色网格。
  - 已确认当前编辑器选中的资源是 `Packages/com.stalomeow.star-rail-npr-shader/Shaders/Character/CharBody.shader`，说明当前工作重心偏向角色身体 NPR Shader 调整。
  - 已确认项目内存在专用 URP 资源 `Assets/Settings/NPR Render Pipeline.asset` 与 `Assets/Settings/NPR Render Pipeline Asset_Renderer.asset`，Renderer 已挂载 `StarRailRendererFeature`。
  - 已确认 `NPR Render Pipeline.asset` 中 `m_PrefilteringModeForwardPlus: 1`，项目已为 Forward+ 相关路径做好资产级配置准备。
- 当前问题：
  - Unity Editor 当前 `project_get_info` 返回的激活管线名是 `UniversalRP-HighQuality`，而项目内同时存在专用的 `NPR Render Pipeline.asset`；需要进一步确认当前场景是否始终使用 NPR 专用管线资源，还是只在部分质量档位/平台下启用。
  - `L11` 场景当前没有复杂环境对象，主要用于单角色观察与材质/光照验证，后续修改容易集中，但也意味着很多表现依赖材质与 RendererFeature 的正确协作。
  - 角色当前只读到了 `hair.mat`、`face.mat` 和 `CharBody.shader` 相关线索，身体材质映射、面部贴图完整性、前发阴影和自阴影联动还需要持续核对。
  - 当前相机实际渲染路径显示为 `Forward`，需要结合 URP 资产与 Renderer 设置继续确认 Forward+ 是否真正启用在当前预览链路中。
- 下一步：
  - 在 Mac 端打开后优先载入 `Assets/L11 NPR/L11.unity`，确认活动场景、选中 Shader 与当前机器一致。
  - 优先检查 `Assets/Settings/NPR Render Pipeline.asset`、`Assets/Settings/NPR Render Pipeline Asset_Renderer.asset`、`Plugins/StarRailNPRShader-main/Runtime/StarRailRendererFeature.cs` 三处，确认实际渲染路径和 Feature 挂载关系。
  - 继续围绕 `Packages/com.stalomeow.star-rail-npr-shader/Shaders/Character/CharBody.shader`、`CharBodyCore.hlsl`、角色材质球做联调。
  - 后续所有关键进展继续同步到本节和 `codex/tasks.md`，保持 Windows 与 Mac 端上下文一致。
