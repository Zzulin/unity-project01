# AGENTS.md

## 协作约定
- 默认中文输出；用户明确要求英文时再切换。
- 重要进展只同步到本文件和 `codex/tasks.md`（保持简洁，不写长历史）。
- 引用本仓库源码行号时，以 Rider 显示为准；命令行读取行号需按原始 LF 拆行或使用 `rg -n`，避免 PowerShell `Get-Content` 行号偏移。

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
- `Assets/L14 Snow/L14.unity` 已生成可交互雪地 Demo：Compute Shader 写入 1024² ARGBHalf 雪面高度/堆雪状态图，520x520 高细分网格做真实顶点位移；雪材质已改为 BaseColor/Normal/Height/Roughness/SparkleMask 贴图管线，多尺度高度/法线混合，动态白色流动点和方块雪晶已移除。
- L14 雪面光照已加入包裹漫反射、浅层透光、掠射边缘光和静态视角相关雪晶闪点；脚印区域同步改变高度、法线响应、压实色与粗糙度。
- L14 场景已收敛为纯技术 Demo：玩家和两个自动移动体均改为可见小球；每个小球只保留 1 个 `L14SnowInteractor` 压痕源，玩家不再生成左右脚 stamp，所以小球轨迹是单条圆形压痕；构建菜单为 `Tools/Snow/Build L14 Interactive Snow Demo`。
- L14 资源集中在 `Assets/L14 Snow/{Scripts,Shaders,Materials,Textures,Editor}`；最终预览截图为 `Assets/Screenshots/L14_InteractiveSnow_material_pipeline_v4_final_20260504.png`，后续截图不要复用旧文件名。
- L14 校验已通过：`dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj` 均 0 error；`shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- `Assets/L13 VolumeCloud/L13.unity` 已生成光线步进体积云示例：体积盒 Ray March、周期无缝 3D Shape/Detail 噪声贴图、周期 WeatherMap、低成本光照步进阴影、银边/粉末感、风场动画、XZ 边界淡出、HUD 参数预设。
- L13 资源集中在 `Assets/L13 VolumeCloud/{Scripts,Shaders,Materials,Editor}`；构建菜单为 `Tools/Volume Cloud/Build L13 Raymarched Volume Cloud Demo`。
- L13 噪声资源集中在 `Assets/L13 VolumeCloud/Textures`，可通过 `Tools/Volume Cloud/Regenerate L13 Noise Textures` 重新生成并绑定。
- L13 程序化噪声参数集中在 `Assets/L13 VolumeCloud/Settings/L13CloudNoiseSettings.asset`，Inspector 支持手动生成和可选延迟自动生成。
- L13 已降到默认性能档：48 view steps / 4 light steps；HUD 的高质量预设不再超过 64/5，避免编辑器打开即卡顿。
- L13 校验已通过：`dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj` 均 0 error；Unity Console Error 为 0。
- `Assets/L12 grass/L12.unity` 已升级为 GPU-driven 草地示例：`DrawMeshInstancedIndirect`、chunk 分块、Compute Shader 视锥/距离/密度剔除、近中远 LOD、密度图控制、交互压草纹理。
- L12 运行时相机已支持基础操作：右键拖拽旋转视角、滚轮缩放、中键拖拽平移观察中心、`R` 复位。
- L12 草地脚本与资源集中在 `Assets/L12 grass/{Scripts,Shaders,Materials,Textures,Editor}`；构建菜单为 `Tools/Grass/Build L12 Interactive Grass Demo`。
- L12 校验已通过：`dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj` 均 0 error；`shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- L10.9 运行时相机漫游与最早版近距离屏幕抖动溶解保留；后续“整体同步/模型范围”重做方案效果不理想，已 discard 回退，不作为当前基线。
- Graphics 默认 RP 指向 `NPR Render Pipeline.asset`，当前质量档 `High` 指向 `UniversalRP-HighQuality.asset`。
- `0_mesh_mesh` 材质槽位以场景内嵌 `(Instance)` 为主，`hair` 是 `CharHair`，多数身体/面部槽位仍是 URP Lit。
- `CharBody.shader` 还未稳定进入当前主体材质链路，改 Shader 前要先确认材质治理策略。
- 学习与实现基线收敛为 2 个仓库：`UnityURPToonLitShaderExample` + `StarRailNPRShader`；执行节奏为 1 周冲刺（Demo 优先，不追全特性完美），以 `codex/tasks.md` 为准。
- 计划已细化为“面试向一周 Demo”，每天包含：实现目标、验收产出、可讲述技术点。
- `L11NprContextReporter` 已增强：新增活跃 URP Renderer/默认 RendererFeature 列表与 StarRailFeature 命中提示，并输出角色材质槽位统计（CharBody/Face/Hair、URP Lit、Embedded）。
- 本地编译校验已通过：`dotnet build Assembly-CSharp-Editor.csproj`（0 error）。
