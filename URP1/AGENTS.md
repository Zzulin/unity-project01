# AGENTS.md

## 协作约定
- 默认中文输出；用户明确要求英文时再切换。
- 重要进展只同步到本文件和 `codex/tasks.md`（保持简洁，不写长历史）。
- 引用本仓库源码行号时，以 Rider 显示为准；命令行读取行号需按原始 LF 拆行或使用 `rg -n`，避免 PowerShell `Get-Content` 行号偏移。

## 可用技能

## 学习参考仓库（跨端可直接识别）
- `StarRailNPRShader`（GPL-3.0，已归档）  
  https://github.com/stalomeow/StarRailNPRShader
- `UnityURPToonLitShaderExample`（MIT）  
  https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample

## 本地目录（统一使用仓库相对路径，便于 Windows/macOS）
- `UnityURPToonLitShaderExample`：`Assets/UnityURPToonLitShaderExample-master`
- `StarRailNPRShader`：`Plugins/StarRailNPRShader-main`

## 项目任务概览
- 目标场景：`Assets/LXII game 整合/game.unity`
- 当前目标：用 Claude Code 单 CLI duo-agent 工作流推进 LXII 游戏整合 Demo；LXII 不使用 L11，玩家固定为 L10.9 妮露 FBX，Humanoid + LXI 动作基础链已打通，当前继续把第三人称控制、草地、雪地、体积云串成同一条演示路径。
- 关键路径：
  - Claude 工作流：`Assets/LXII game 整合/Docs/LXII_ClaudeCode_DuoAgent_Workflow.md`
  - LXII 场景：`Assets/LXII game 整合/game.unity`
  - 妮露模型：`Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx`
  - 妮露材质：`Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/{Body 1,Body 2,Hair 1,Face and face_eye}.mat`
  - 动作资源：`Assets/LXI 动作测试/*`
  - 场景系统：`Assets/L12 grass/*`、`Assets/L13 VolumeCloud/*`、`Assets/L14 Snow/*`

## 当前结论（短版）
- LXII 方案已修正：不使用 L11/StarRail；玩家必须是 `NPC_Avatar_Girl_Sword_Nilou.fbx`，保留 L10.9 妮露材质；Humanoid Avatar、LXI Idle/Run/Action、L12 可交互草地、第三人称摄像机和拆职责角色控制链已接入 `Assets/LXII game 整合/game.unity`。
- LXII 当前角色控制链已从单脚本测试驱动拆为 `LXIIPlayerInputReader`、`LXIIPlayerMotor`、`LXIIPlayerAnimationDriver`、`LXIIPlayerController`；Inspector 主入口收敛到 `LXIIPlayerController`，内部辅助组件默认隐藏；保留 `CharacterController` 与第三人称摄像机，当前输入为 `WASD` 移动、`Left Shift` 加速、自动 Idle/Run、`3` 触发 Action。
- LXII 当前 Avatar / 动作主干没有明显扭胯、塌肩、脚尖异常或头发链违和；剩余主要是局部穿模，衣物和头发物理默认按 Unity 插件或成熟现成方案处理，不作为当前内建开发项。
- `Assets/L14 Snow/L14.unity` 已生成可交互雪地 Demo：Compute Shader 写入 1024² ARGBHalf 雪面高度/堆雪状态图，520x520 高细分网格做真实顶点位移；雪材质已改为 BaseColor/Normal/Height/Roughness/SparkleMask 贴图管线，多尺度高度/法线混合，动态白色流动点和方块雪晶已移除。
- L14 雪面光照已加入包裹漫反射、浅层透光、掠射边缘光和静态视角相关雪晶闪点；压痕区域同步改变高度、法线响应、压实色与粗糙度，压过区域会降低雪晶闪点并压低边缘堆雪亮度，避免“白色软管”感。
- L14 场景已收敛为纯技术 Demo：玩家和两个自动移动体均改为可见小球；每个小球只保留 1 个 `L14SnowInteractor` 压痕源，玩家不再生成左右脚 stamp，所以小球轨迹是单条圆形压痕；构建菜单为 `Tools/Snow/Build L14 Interactive Snow Demo`。
- L14 资源集中在 `Assets/L14 Snow/{Scripts,Shaders,Materials,Textures,Editor}`；最终预览截图为 `Assets/Screenshots/L14_InteractiveSnow_material_pipeline_v4_final_20260504.png`，后续截图不要复用旧文件名。
- L14 历史校验已通过：当时 `dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj` 均 0 error；`shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- `Assets/L13 VolumeCloud/L13.unity` 已生成光线步进体积云示例：体积盒 Ray March、周期无缝 3D Shape/Detail 噪声贴图、周期 WeatherMap、低成本光照步进阴影、银边/粉末感、风场动画、XZ 边界淡出、HUD 参数预设。
- L13 云盒缩放已与噪声采样解耦：Transform Scale 只控制 Ray-Box 边界，Shader/Controller 通过独立 `Noise World Size` 保持云纹理世界尺度，非等比缩放云盒不会拉伸云团。
- L13 资源集中在 `Assets/L13 VolumeCloud/{Scripts,Shaders,Materials,Editor}`；构建菜单为 `Tools/Volume Cloud/Build L13 Raymarched Volume Cloud Demo`。
- L13 噪声资源集中在 `Assets/L13 VolumeCloud/Textures`，可通过 `Tools/Volume Cloud/Regenerate L13 Noise Textures` 重新生成并绑定。
- L13 程序化噪声参数集中在 `Assets/L13 VolumeCloud/Settings/L13CloudNoiseSettings.asset`，Inspector 支持手动生成和可选延迟自动生成。
- L13 已降到默认性能档：16 view steps / 0 light steps；HUD 高质量预设为 24/1，避免编辑器打开即卡顿。
- L13 历史校验已通过：当时 `dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj` 均 0 error；Unity Console Error 为 0。
- `Assets/L12 grass/L12.unity` 已升级为 GPU-driven 草地示例：`DrawMeshInstancedIndirect`、chunk 分块、Compute Shader 视锥/距离/密度剔除、近中远 LOD、密度图控制、交互压草纹理。
- L12 运行时相机已支持基础操作：右键拖拽旋转视角、滚轮缩放、中键拖拽平移观察中心、`R` 复位。
- L12 制作流程文档已新增：`Assets/L12 grass/Docs/L12_InteractiveGrass_Workflow.md`。
- L12 草地脚本与资源集中在 `Assets/L12 grass/{Scripts,Shaders,Materials,Textures,Editor}`；构建菜单为 `Tools/Grass/Build L12 Interactive Grass Demo`。
- L12 历史校验已通过：当时 `dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj` 均 0 error；`shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- L10.9 运行时相机漫游与最早版近距离屏幕抖动溶解保留；后续“整体同步/模型范围”重做方案效果不理想，已 discard 回退，不作为当前基线。
- Graphics 默认 RP 指向 `NPR Render Pipeline.asset`，当前质量档 `High` 指向 `UniversalRP-HighQuality.asset`。
- `0_mesh_mesh` 材质槽位以场景内嵌 `(Instance)` 为主，`hair` 是 `CharHair`，多数身体/面部槽位仍是 URP Lit。
- L11/StarRail 相关结论只作为历史记录；LXII 当前不使用 L11、StarRailRendererFeature、CharBody/CharFace/CharHair。
- 学习参考仓库仍可作为历史资料保留，但 LXII 当前实现基线改为 L10.9 妮露 + LXI 动作 + L12/L13/L14 场景系统。
- 计划已细化为“面试向一周 Demo”，每天包含：实现目标、验收产出、可讲述技术点。
- `L11NprContextReporter` 属于 L11 历史工具，LXII 当前不要调用。
- 当前 LXII 正式验证口径以 Unity Editor 自动编译、`debug_check_compilation`、Unity Console Error 和必要 Play Mode 短测为准；`dotnet build Assembly-CSharp.csproj --no-restore` 不再作为 LXII 硬性验证项。


## UnitySkills
- unity-skills: Unity Editor automation via REST API

## Git baseline
- 2026-05-19: 仓库维持单一 `main` 主线，不拆 `windows/mac` 长期分支；已补 `.gitignore` / `.gitattributes` 基线，并将 `.dotnet`、`.dotnet_home`、`.trae` 这类本机环境文件从 Git 跟踪中移除。
