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
- `Assets/L18 VolumeCloud+VollumetricLighting/L18.unity` 已改为直接整合 L13 与 L17：场景使用 `L13VolumeCloudController` + L13 原体积云 Shader 渲染云层，使用 `L17VolumetricLightingController` + 原 L17 RendererFeature 渲染低空参与介质；L17 沿主光方向采样绑定的 L13 云密度场作为实时遮光，Coverage/Density 为 0 时透射恢复为 1，不会残留固定光柱。L18 不再保留专用 RendererFeature、Shader 或运行时脚本。
- L13 新增默认关闭的 `macroGapStrength`，默认 0 保持 L13 原场景观感；L17 新增可选 `cloudOccluder` 及云影采样参数，没有绑定 L13 时保持 L17 原场景效果。L18 使用独立云材质，避免参数写回 L13 共享材质；Main Camera far clip 为 2500m，远景直线已确认主要来自体积盒/云底边界而非远裁剪面，当前通过扩大 L17 Bounds、90m 边缘软化和 L13 云底渐隐处理。色调按 RDR2 参考收敛为深蓝灰环境与中性偏暖白光幕，预览为 `Assets/Screenshots/L18_L13_L17_RDR2_tone_final_20260618.png`。
- L18 地面场景已精简为单个根对象 `Ground`，树木、岩石和前景剪影全部移除；程序天空太阳盘缩至 `0.006`，云前向散射/银边峰值同步收窄，避免太阳方向形成大面积白斑。Main Camera 已复用 `L17RuntimeCameraMotion`：WASD、Q/E、右键视角、Shift 加速，基础速度 40m/s、加速倍率 4。
- L18 的云参数已从“完全同步 L13”进入独立美术优化：保持 L13 原 Shader/Controller 和独立材质链路不变，L18 使用 6.2/20 的 Shape/Detail 尺度、0.52 宏观云洞和 24/4 云步进，云层由平滑雾块收敛为深蓝灰、有细节且能切出多层透光缝的结构；L17 云影提升到 8 次采样并加入轻量介质密度起伏，仍由同一 L13 密度场实时驱动，不增加假光柱。当前预览为 `Assets/Screenshots/L18_optimization_round2_pass3_20260619.png`。
- L18 当前按 GTA5 暖色落日参考把主光调整为 `(20, 85.3, 180)`，太阳贴近山脊；针对视线与太阳方向接近时的爆白，L13/L17 保留前向散射峰值上限，L13 同时限制最终太阳直射项，并启用 ACES。L17 户外介质盒继续与 L13 云底软衔接，避免云层上方介质长距离累积。
- L17 体积光 Controller 的 Inspector 调参范围已重整：Intensity 上限从 10 提升到 30，Extinction、Shadow Floor、Multi Scatter、Height Origin、稳定性和太阳参数缩至实际有效区间；Density、Max Distance、Height Falloff、Noise Scale、Bounds Softness 改为低值区展开的非线性滑条，使 L17 室内与 L18 室外两个数量级都可精调。L18 当前使用 Density 0.0048 / Extinction 0.34 / Intensity 24 / Anisotropy 0.78 / Forward Ceiling 1.7，亮度预览为 `Assets/Screenshots/L18_volumetric_brightness_round3_final_20260619.png`；上仰压力测试仍无整屏过曝。
- L13/L17 Controller 均已移除跨系统耦合参数：L13 Inspector 只调体积云，L17 Inspector 只调体积光。RendererFeature 自动发现当前场景的 L13 云，先以 3 次采样、强度 1、对比度 2.8 构建 128x128 世界 XZ 云透射缓存，再供 L17 体积积分读取；耦合实现不再暴露到任何 GameObject。
- L18 最新色调改为 GTA5 暖色落日：使用独立 `L18_GTA5SunsetSky` 程序天空，形成紫灰高空、黄橙地平线、橙红云与暗色地景；Bloom 只补太阳高亮溢出，暖色主要集中在太阳、雾层、云受光和体积散射。预览：`Assets/Screenshots/L18_GTA5_sunset_tone_final_20260622.png`。
- L18 落日阴影区已补足天空漫反射与地面反弹基线：提高 TriLight 环境天空/赤道/地面颜色，地表反照率恢复到 0.2/0.22/0.17，主光 Shadow Strength 从 1 降至 0.55，并把后处理 Contrast 收至 2；无直射区域保持深棕可读，不再纯黑，也未使用材质自发光。预览：`Assets/Screenshots/L18_GTA5_sunset_balanced_shadow_final_20260622.png`。
- `L17VolumetricLightingController.Apply()` 已停止强制写入 `sunLight.shadowStrength=1`；L18 主光 Shadow Strength 0.55 现在在组件重启、Inspector 刷新和 Domain Reload 后都能保持。Controller 目前仍会同步主光强度/颜色/Soft Shadows 与全局 TriLight 环境色，后续若继续严格拆分职责可再解除这些覆盖。
- L18 的 L13 体积云参数已与 `Assets/LXII game 整合/game.unity` 中 `LXII Sky Volume Cloud` 同步：颜色、Density/Coverage、噪声尺度、形状细节、吸收、相函数、风、采样质量和透明度均一致；LXII 未序列化的 Macro Gap / Forward Phase Clamp 按默认 0 同步。L18 仍保留自身云盒 Transform、独立材质和太阳引用。
- L17 第一轮规范化修正已完成：场景深度现在重建世界坐标并换算真实射线距离，避免屏幕边缘和山脊处积分长度错误；删除重复宽 Mie 光瓣，改为单一 HG 相函数和连续软峰值保护，避免太阳方向大圆弧；NPR URP 主光阴影距离由 72m 提升至 350m，覆盖 L18 的 320m 体积积分；Temporal 新增每相机历史深度纹理、天空/几何分类拒绝和相对深度拒绝，正常相机移动不再立即废弃历史。验证截图：`Assets/Screenshots/L18_first_four_algorithm_fixes_20260623.png`。
- L13/L17 第二轮耦合修正已完成：L13 视线积分、自阴影和 L17 云遮光均改用世界米制射线，并以每公里换算现有密度/吸收参数，云盒非等比缩放不再改变采样距离单位；Coverage=0 不再粗暴关闭 L17 遮光，而是继续由 WeatherMap 的实际密度决定透射；L13 自阴影与 L17 遮光都采用包含 Detail Erosion 的完整云密度；L17 当前在透明物体/云之后、Bloom/ACES 前合成。验证截图：`Assets/Screenshots/L18_coupling_round2_play_20260623.png`。
- L18 资源清理已完成：删除零引用的 `L18_BlueNoise64.asset` 及其空 `Textures` 目录，当前 RendererFeature 继续使用 L17 蓝噪声；L18 历史诊断截图由 72 张收敛为文档仍引用的 9 张最终/当前截图，共释放约 31.4MB。全项目重复资源扫描另发现约 197MB 外部模型贴图副本，但因 GUID 独立且可能被各 FBX 材质引用，本轮不删除。
- L18 已新增动态昼夜色调总控 `L18AtmosphereDirector`：以 `L18 Low Storm Sun` 的实际 Euler X 轴做对称昼夜插值，X=0/180 为黄昏 Profile、X=90 为正午 Profile；同步驱动主光颜色/强度/阴影、程序天空盒、TriLight 环境光/雾色、L13 体积云参数和 L17 体积光参数。黄昏 Profile 固化为本任务开始前 L18 GTA5 暖色落日参数，正午 Profile 为蓝天白云和少量白色米氏体积散射。L13 新增 `RefreshImmediate` 供 L18 总控实时推送云参数；编辑态通过 `EditorApplication.update` 监听主光旋转并按昼夜混合值变化刷新。验证截图：`Assets/Screenshots/L18_dynamic_tod_final_noon_auto_20260626.png`、`Assets/Screenshots/L18_dynamic_tod_final_sunset_20260626.png`。
- LXII 已通过 `Tools/LXII/Sync L18 Atmosphere To LXII` 同步 L18 体积云/体积光项目效果：`Assets/LXII game 整合/game.unity` 保留 Main Camera、妮露玩家和 L12 草地，替换旧 Directional Light / LXII L13 云对象 / 旧 Global Volume，并复制 L18 的 `L18 Low Storm Sun`、`L18 Atmosphere Director`、`L17 Outdoor Volume Bounds`、`L13 Integrated Cloud Layer`、`L18 Cinematic Post Process`、`Global Volume`、`L17 Integrated Volumetric Lighting`。同步脚本会重绑 L18 材质、主光、RendererData、云/体积光引用，并把 LXII 初始太阳 X 归一到 0 以保持黄昏基线。验证：UnitySkills 编译空闲、Console Error 0，截图 `Assets/Screenshots/LXII_L18_atmosphere_synced_20260628.png`。
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
- `Assets/L17 Volumetric Lighting/L17.unity` 已重做为现代 URP RendererFeature 体积光 Demo：不再手摆 cube 光束，改为 fullscreen froxel/integrated buffer 管线，当前为半分辨率体积 buffer、96 depth steps、密度噪声关闭、blue-noise jitter、固定 temporal accumulation、低分辨率 5 点十字双边降噪、全分辨率 3x3 双边上采样，并在 Bloom / ACES Tonemapping 前合成；history 重投影按 `UNITY_UV_STARTS_AT_TOP` 修正 Y 方向；L18/LXII 有 L13 云时先构建 128x128 世界 XZ 云透射缓存，BuildVolume 每层只读取一次缓存；参与介质由场景对象 `L17 Local Volume Bounds` 的 Transform 驱动；RendererFeature 默认要求当前场景存在启用的 `L17VolumetricLightingController` 才渲染，并通过 `Scene -> Controller` 注册表读取当前场景 Controller 参数；几何模型统一收纳到 `L17 Room Geometry` 下，功能对象保留根级；五个房间大面已改为朝室内的 `L17_LightmapReadyPanel` 单面接光 mesh，避免厚 cube 背面参与烘焙导致黑图；`L17TwoSidedInteriorLit` 已在原 shader 名称下升级为双面 URP PBR 表面 Shader，暴露 Base/Metallic/Roughness/Smoothness/Normal/AO/Specular/Environment/Baked GI Strength 参数，并补齐 ShadowCaster、DepthOnly、DepthNormals、Meta pass，可参与 Lighting 面板烘焙间接光；`L17VolumetricLightingController` 只在启用、Inspector 修改或局部体积盒移动/缩放时同步环境/灯光和体积盒缓存；multi scatter 受 shadow map 遮挡约束，`shadowFloor` 为 0.015；BuildVolume pass 必须保留 `_MAIN_LIGHT_SHADOWS_CASCADE` 和 `_SHADOWS_SOFT`，L17 室内遮挡体积光依赖主光 cascade shadowmap；GameView Play Mode 下摄像机为手动漫游：`WASD` 移动、右键旋转视角、`Shift` 加速、`Q/E` 垂直移动。
- L17 表面 PBR 的环境镜面反射已改为 URP `GlossyEnvironmentReflection`，会采样 reflection probe，并支持 probe blending / box projection 关键字。
- L17 烘焙/反射探针条件已修正：`Low Angle Sun` 为 Mixed，`bounceIntensity=2.0`，房间大面标记为 LightmapStatic + ReflectionProbeStatic，`L17TwoSidedInteriorLit` 使用自定义 Meta pass 直接输出材质 Albedo，并通过 `GlossyEnvironmentReflection` 采样 reflection probe。
- L17 baked indirect 当前已验证：`L17_LightingSettings` 使用 Baked Indirect、4 bounces、IndirectOutputScale 1.8、关闭 baked AO；`Lightmap-0_comp_light.exr` 于 2026-06-17 13:06 重新生成且非黑，`Room Ceiling` 等大面 `lightmapIndex=0`、`receiveGI=Lightmaps`；UnitySkills 相机截图 `Assets/Screenshots/L17_baked_indirect_fixed_camera.png` 中暗面能看到太阳反弹间接光。
- L17 资源集中在 `Assets/L17 Volumetric Lighting/{Shaders,Materials,Scripts,Editor,Docs}`；构建菜单为 `Tools/Volumetric Lighting/Build L17 Modern Window Shafts Demo`。
- L17 当前验证：`dotnet build Assembly-CSharp.csproj --no-restore`、`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 均 0 warning / 0 error；UnitySkills `debug_check_compilation` 未处于编译/刷新状态，Console Error 0。UnitySkills 截图 `Assets/Screenshots/L17_final_after_shadow_variant_restore_20260624.png` 确认窗框/立柱能遮挡体积光；`Hidden/L17/Froxel Volumetric Composite` 的 `ShaderHasError=False`，但 `shader_check_errors` 仍报告 1 条 URP `Shadows.hlsl` Metal cascade warning，不是 L17 shader 编译错误，不能通过删除 cascade/soft shadow variants 来规避。
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
