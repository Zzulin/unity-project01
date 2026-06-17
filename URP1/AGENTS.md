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
- LXII 已升级为大世界预览观感：`Tools/LXII/Setup L12 Grass In Game Scene` 会写入 960m 起伏地表 mesh 和 420m 贴坡草海，L12 草 shader / culling compute 已加可选地形高度参数且默认不影响原 L12 Demo；`targetBladeSpacing` 之前在 LXII 不明显是被 `maxBladesPerAxis=1024` 截断，当前 LXII 默认上限改为 2048；`Tools/LXII/Setup L13 VolumeCloud In Game Scene` 只保留一个 `LXII Sky Volume Cloud` 体积云对象，云盒为 1600m 级，默认 `10 view steps / 0 light steps`，场景雾距为 `150-900`，相机 far clip 至少 `1200`。
- LXII 本轮大世界预览验证为 Unity 编译空闲、Console Error 0、`validate_scene` 0 issues、`validate_missing_references` 0 issues，L12 草 shader 0 error；Profiler 静止快照约 `49 batches / 49 draw calls / 246k triangles`。当前仍是静态大世界预览，不是真正 streaming 大世界。
- LXII 当前 Avatar / 动作主干没有明显扭胯、塌肩、脚尖异常或头发链违和；剩余主要是局部穿模，衣物和头发物理默认按 Unity 插件或成熟现成方案处理，不作为当前内建开发项。
- `Assets/L15 Water/L15.unity` 已生成纯水体现代二次元水体 Demo：高细分水面网格 + 4 层 Gerstner 顶点波、深浅水吸收/分层染色、透明混合、Fresnel 高光、交界泡沫、水底凹陷地形和三平面投影动态焦散；不放水上装饰物，场景只保留水面、水底、灯光、后处理、相机和 HUD；水面网格已改为 UInt32 索引，避免 65535 顶点以上的大三角伪影。
- L15 资源集中在 `Assets/L15 Water/{Scripts,Shaders,Materials,Textures,Editor}`；构建菜单为 `Tools/Water/Build L15 Modern Anime Water Demo`；预览截图为 `Assets/Screenshots/L15_ModernAnimeWater_caustic_voronoi_current_20260522.png`。
- L15 当前校验已通过：`dotnet build Assembly-CSharp.csproj --no-restore`、`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 均 0 error；`shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- `Assets/L16 Rain/L16.unity` 当前已收敛为只验证 GPU 雨幕的极简 Demo：Compute Shader 填充雨滴位置 + `DrawMeshInstancedIndirect` 绘制 GPU 雨线；屏幕滑动雨水/镜头雨痕、湿润积水、雨滴涟漪、多灯牌、小柱子等额外展示物已移除，场景只保留普通地面、普通背景墙、一个主光、相机、Rain Volume 和 HUD。
- L16 资源集中在 `Assets/L16 Rain/{Scripts,Shaders,Materials,Editor,Docs}`；构建菜单为 `Tools/Rain/Build L16 Advanced Rain Demo`；预览截图为 `Assets/Screenshots/L16_AdvancedRain_current_20260527.png`。
- L16 当前验证已通过：`dotnet build Assembly-CSharp.csproj --no-restore`、`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 均 0 error；L16 雨线 Shader `shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- `Assets/L17 Volumetric Lighting/L17.unity` 已重做为现代 URP RendererFeature 体积光 Demo：不再手摆 cube 光束，改为 fullscreen froxel/integrated buffer 管线，当前回到流畅实时基线：半分辨率体积 buffer、96 depth steps、密度噪声关闭、blue-noise jitter + complementary 双相采样、5x5 cross-bilateral 低分辨率降噪，并在 Bloom / ACES Tonemapping 前合成；GameView 和 SceneView 都由 `temporalAccumulation` 总开关统一控制 temporal accumulation；history 重投影按 `UNITY_UV_STARTS_AT_TOP` 修正 Y 方向；参与介质由场景对象 `L17 Local Volume Bounds` 的 Transform 驱动；RendererFeature 默认要求当前场景存在启用的 `L17VolumetricLightingController` 才渲染，并通过 `Scene -> Controller` 注册表读取当前场景 Controller 参数；几何模型统一收纳到 `L17 Room Geometry` 下，功能对象保留根级；五个房间大面已改为朝室内的 `L17_LightmapReadyPanel` 单面接光 mesh，避免厚 cube 背面参与烘焙导致黑图；`L17TwoSidedInteriorLit` 已在原 shader 名称下升级为双面 URP PBR 表面 Shader，暴露 Base/Metallic/Roughness/Smoothness/Normal/AO/Specular/Environment/Baked GI Strength 参数，并补齐 ShadowCaster、DepthOnly、DepthNormals、Meta pass，可参与 Lighting 面板烘焙间接光；`L17VolumetricLightingController` 只在启用、Inspector 修改或局部体积盒移动/缩放时同步环境/灯光和体积盒缓存；multi scatter 受 shadow map 遮挡约束，`shadowFloor` 为 0.015；GameView Play Mode 下摄像机为手动漫游：`WASD` 移动、右键旋转视角、`Shift` 加速、`Q/E` 垂直移动。
- L17 表面 PBR 的环境镜面反射已改为 URP `GlossyEnvironmentReflection`，会采样 reflection probe，并支持 probe blending / box projection 关键字。
- L17 烘焙/反射探针条件已修正：`Low Angle Sun` 为 Mixed，`bounceIntensity=2.0`，房间大面标记为 LightmapStatic + ReflectionProbeStatic，`L17TwoSidedInteriorLit` 使用自定义 Meta pass 直接输出材质 Albedo，并通过 `GlossyEnvironmentReflection` 采样 reflection probe。
- L17 baked indirect 当前已验证：`L17_LightingSettings` 使用 Baked Indirect、4 bounces、IndirectOutputScale 1.8、关闭 baked AO；`Lightmap-0_comp_light.exr` 于 2026-06-17 13:06 重新生成且非黑，`Room Ceiling` 等大面 `lightmapIndex=0`、`receiveGI=Lightmaps`；UnitySkills 相机截图 `Assets/Screenshots/L17_baked_indirect_fixed_camera.png` 中暗面能看到太阳反弹间接光。
- L17 资源集中在 `Assets/L17 Volumetric Lighting/{Shaders,Materials,Scripts,Editor,Docs}`；构建菜单为 `Tools/Volumetric Lighting/Build L17 Modern Window Shafts Demo`。
- L17 当前验证：`dotnet build Assembly-CSharp.csproj --no-restore`、`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 均 0 warning / 0 error；UnitySkills `debug_check_compilation` 未处于编译/刷新状态，Console Error 0，`validate_scene` 0 issues，`validate_missing_references` 0 issues。`Hidden/L17/Froxel Volumetric Composite` 的 `shader_check_errors` 仍返回 `messageCount=1`，但 Unity Console Warning/Error 均为 0，当前按 ShaderUtil 内部 message 残留记录。
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
