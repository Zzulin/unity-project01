# 当前任务

## 主任务
整理并同步 `L11 NPR` 场景的当前开发上下文，确保 Windows / Mac 两端的 Codex 能无缝续接角色 NPR 渲染开发。

## 子任务
- [x] 确认当前活动场景为 `Assets/L11 NPR/L11.unity`
- [x] 确认当前选中资源为 `Packages/com.stalomeow.star-rail-npr-shader/Shaders/Character/CharBody.shader`
- [x] 确认项目使用 URP 14 与本地 `StarRail NPR Shader` 包
- [x] 确认 `NPR Render Pipeline Asset_Renderer.asset` 已挂载 `StarRailRendererFeature`
- [x] 梳理 `L11` 场景根对象、角色结构与关键组件
- [ ] 进一步确认当前预览链路是否真实跑在 Forward+，而不是仅资产层开启预过滤
- [ ] 补充身体材质、头发材质、面部材质与对应 Shader 的映射关系
- [ ] 开始针对 `CharBody.shader` / `CharBodyCore.hlsl` 做定向修改或验证

## 当前问题
- `project_get_info` 显示当前激活管线名是 `UniversalRP-HighQuality`，但项目内同时存在专用 `NPR Render Pipeline.asset`，实际生效链路还需要再核实。
- `scene_get_info` 显示 `L11` 场景当前是一个最小化角色观察场景，适合材质与光照调试，但场景信息主要集中在角色本体和光照设置。
- 当前已知场景核心对象为 `Main Camera`、`Directional Light`、`Avatar_Ruanmei_Body`；角色上挂有 `StarRailCharacterRenderingController`。
- 当前 `SkinnedMeshRenderer` 已确认使用 StarRail NPR 材质实例，至少读到 `hair.mat`、`face.mat` 和 `CharBody.shader` 这条工作线索。

## 下一步
- 在 Mac 上启动 Codex 后先读取 `AGENTS.md` 与本文件，再打开 `Assets/L11 NPR/L11.unity` 对齐现场。
- 用 UnitySkills 或直接检查资源，确认 `NPR Render Pipeline.asset` 与质量设置、Graphics 设置之间的关系。
- 继续检查 `StarRailRendererFeature`、`CharBody.shader`、`CharBodyCore.hlsl`、角色材质球，明确下一次真正要改的代码点。
