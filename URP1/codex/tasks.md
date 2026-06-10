# 当前任务（精简）

## 当前目标
- 主线场景：`Assets/LXII game 整合/game.unity`
- 当前目标：把 LXII 收敛成可演示的整合场景，不使用 L11 / StarRail，玩家固定为 L10.9 妮露 `Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx`；Humanoid + LXI 动作基础链已打通，当前继续把第三人称控制、草地、雪地、体积云串成同一条演示路径。

## 当前已确认
- `Assets/L15 Water/L15.unity` 已生成纯水体现代二次元水体 Demo：不放水上装饰物，只保留水面、水底凹陷地形、灯光、后处理、相机和 HUD。
- L15 当前水体链路包含：高细分水面网格、4 层 Gerstner 顶点波、深浅水吸收/分层染色、透明混合、Fresnel/高光、岸线泡沫、水底三平面动态焦散；水面网格已改为 UInt32 索引，避免 65535 顶点以上的大三角伪影。
- L15 构建菜单：`Tools/Water/Build L15 Modern Anime Water Demo`；预览截图：`Assets/Screenshots/L15_ModernAnimeWater_caustic_voronoi_current_20260522.png`。
- L15 当前验证已通过：`dotnet build Assembly-CSharp.csproj --no-restore`、`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 均 0 error；`shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- `Assets/L16 Rain/L16.unity` 当前已收敛为只验证 GPU 雨幕的极简 Demo：Compute Shader + `DrawMeshInstancedIndirect` GPU 雨线、Low/Medium/High 三档和 HUD 参数面板；屏幕滑动雨水/镜头雨痕、湿润积水、雨滴涟漪、多灯牌、小柱子等额外展示物已移除。
- L16 构建菜单：`Tools/Rain/Build L16 Advanced Rain Demo`；资源集中在 `Assets/L16 Rain/{Scripts,Shaders,Materials,Editor,Docs}`；预览截图：`Assets/Screenshots/L16_AdvancedRain_current_20260527.png`。
- L16 当前验证已通过：`dotnet build Assembly-CSharp.csproj --no-restore`、`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 均 0 error；L16 雨线 Shader `shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- `Assets/L17 Volumetric Lighting/L17.unity` 已重做为现代 URP RendererFeature 体积光 Demo：不再手摆 cube 光束，改为 fullscreen froxel/integrated buffer 管线，当前回到流畅基线：半分辨率体积 buffer、96 depth steps、密度噪声关闭、blue-noise jitter + complementary 双相采样、5x5 cross-bilateral 低分辨率降噪，并在 Bloom / ACES Tonemapping 前合成；GameView 和 SceneView 都由 `temporalAccumulation` 总开关统一控制 temporal accumulation；temporal resolve 已挪到低分辨率降噪之后，并用 3x3 neighborhood clamp 限制 history；history 重投影按 `UNITY_UV_STARTS_AT_TOP` 修正 Y 方向，避免静止镜头混入上下颠倒的历史帧；参与介质由场景对象 `L17 Local Volume Bounds` 的 Transform 驱动，移动/缩放该 Cube 即可调整体积光范围，避免室外全局雾被太阳方向照成整屏亮团；几何模型已统一收纳到 `L17 Room Geometry` 下，功能对象保留根级，Builder 不再保留整理已有层级栏的一次性维护菜单；`L17VolumetricLightingController` 已改为启用、Inspector 修改或局部体积盒移动/缩放时才同步参数，不再每帧无条件写 RendererFeature；multi scatter 已改为受 shadow map 遮挡约束，`shadowFloor` 降到 0.015；Henyey-Greenstein 前向散射已加生产级峰值约束；GameView Play Mode 下摄像机为手动漫游：`WASD` 移动、按住右键旋转视角、`Shift` 加速、`Q/E` 垂直移动；场景已简化为房间整体、窗户结构和 2 个室内接光物，主要通过窗框和墙体自然切出窗光束。
- L17 构建菜单：`Tools/Volumetric Lighting/Build L17 Modern Window Shafts Demo`；资源集中在 `Assets/L17 Volumetric Lighting/{Shaders,Materials,Scripts,Editor,Docs}`。
- L17 当前验证：`dotnet build Assembly-CSharp.csproj --no-restore`、`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 均 0 warning / 0 error；UnitySkills `debug_check_compilation` 未处于编译/刷新状态，Console Error 0，`validate_scene` 0 issues，`validate_missing_references` 0 issues。`Hidden/L17/Froxel Volumetric Composite` 的 `shader_check_errors` 仍返回 `messageCount=1`，但 Unity Console Warning/Error 均为 0，当前按 ShaderUtil 内部 message 残留记录。
- `Assets/LXII game 整合/Docs/LXII_ClaudeCode_DuoAgent_Workflow.md` 已重写，当前约束和推进顺序已同步。
- LXII 当前采用 Unity 正式验证口径：
  - 以 Unity Editor 自动编译结果为准
  - `debug_check_compilation` 需返回 `isCompiling=false`
  - Unity Console Error 需为 0
  - 场景/Importer 改动后以 `game.unity` 打开状态、Scene 保存和必要 Play Mode 短测为准
  - 不再把 `dotnet build Assembly-CSharp.csproj --no-restore` 作为 LXII 硬性验证项
- `Tools/LXII/Setup Nilou Humanoid In Game Scene` 已存在并可把妮露写入 `game.unity`。
- 妮露材质修正规则已补全，`EffectMesh / EyeStar` 发粉问题已处理。
- 妮露 FBX 已改为显式 Humanoid 主干映射，不再只依赖 Unity 自动猜测。
- `Tools/LXII/Setup LXI Animation Test In Game Scene` 已存在并会生成：
  - `Assets/LXII game 整合/Settings/LXII_Nilou_LXI_Test.controller`
  - `Assets/LXII game 整合/Scripts/Player/LXIIPlayerInputReader.cs`
  - `Assets/LXII game 整合/Scripts/Player/LXIIPlayerMotor.cs`
  - `Assets/LXII game 整合/Scripts/Player/LXIIPlayerAnimationDriver.cs`
  - `Assets/LXII game 整合/Scripts/Player/LXIIPlayerController.cs`
  - `Assets/LXII game 整合/Scripts/Camera/LXIIThirdPersonCameraFollow.cs`
- `game.unity` 当前已包含：
  - `LXII Nilou Player`
  - `LXII L12 Grass Root`
  - `LXII Grass Ground`
  - `LXII Grass Field`
- LXII 当前已经接入可交互草地并写入场景：
  - L12 草地区域已写入 `game.unity`
  - 妮露已挂 `CharacterController`
  - 妮露已挂 `L12GrassInteractor`
  - 角色移动时可对草地产生交互
- L12 草地当前已补充近期构建历史文档：2-card x 5 段三角草尖近景拓扑、缩放保持密度、`targetBladeSpacing` 滑条、`maxBladesPerAxis` 上限 1024、`Tip Brightness`、高低/叶形随机、中文 Inspector，以及 `chunksPerSide` 只做性能分块预筛而非视觉块状裁剪的说明。
- LXII 当前已经具备拆分职责的第三人称角色控制基础，不再依赖单个临时测试驱动：
  - 输入、移动、动画切换职责已拆开
  - `LXIIPlayerInputReader` 负责输入读取
  - `LXIIPlayerMotor` 负责 `CharacterController` 移动与朝向
  - `LXIIPlayerAnimationDriver` 负责 Animator 参数
  - `LXIIPlayerController` 负责总控编排
  - Inspector 主入口已收敛到 `LXIIPlayerController`，内部辅助组件默认隐藏
  - `WASD` 移动
  - `Left Shift` 加速
  - 自动切 `Idle / Run`
  - `3` 触发 `Action`
- LXII 当前第三人称摄像机链路已接入：
  - `Main Camera` 已切到 `LXIIThirdPersonCameraFollow`
  - 可跟随角色观察
  - 可绕角色查看动作表现
- 当前 Avatar / 动作主干观察结论：
  - 没有明显扭胯、塌肩、脚尖异常、头发链违和
  - 仍可能存在局部穿模
  - 衣物和头发物理模拟尚未接入
- L17 已重做为现代 URP RendererFeature 体积光 Demo：
  - 不再手摆 cube 光束
  - fullscreen froxel/integrated buffer 管线已接入 RendererFeature，当前回到半分辨率体积 buffer + 96 depth steps 的流畅基线
  - 使用 scene depth、主光 shadowmap、sun direction、`L17 Local Volume Bounds` 场景 Cube、blue-noise jitter + complementary 双相采样、5x5 cross-bilateral 低分辨率降噪和 Bloom/ACES 前合成；GameView/SceneView temporal accumulation 默认保留，resolve 位于降噪之后，并修正 history 重投影 Y 翻转；multi scatter 已受 shadow map 约束，HG 前向散射加生产级峰值约束以降低墙后漏光团和圆形太阳热点
  - 场景改为窗框和墙体自然切出光束
  - 当前验证：C# 双程序集 0 warning / 0 error，Unity Console Error 0，`scene_health_check` 0 findings；合成 shader 的 `shader_check_errors` 仍有 1 条 ShaderUtil 内部 message，但 Console Warning/Error 为 0

## 当前进度判断
- LXII 已经从“空场景 + 单角色写入”推进到“妮露 + Humanoid + LXI 动作测试 + Inspector 收敛后的拆职责第三人称角色控制 + 第三人称摄像机 + L12 可交互草地已接入”的阶段。
- 当前主线不再是单纯搭测试壳，也不再回退到 `LXIIAnimationTestDriver` 单脚本方案；后续继续控制局部穿模，并把 L14 雪、L13 云按同一路径逐步串入。

## 当前改动落点
- `Assets/LXII game 整合/game.unity`
- `Assets/LXII game 整合/Docs/LXII_ClaudeCode_DuoAgent_Workflow.md`
- `Assets/LXII game 整合/Editor/LXIINilouHumanoidSetup.cs`
- `Assets/LXII game 整合/Editor/LXIIAnimationTestSetup.cs`
- `Assets/LXII game 整合/Editor/LXIIL12GrassSetup.cs`
- `Assets/LXII game 整合/Scripts/Animation/LXIIAnimationTestDriver.cs`（历史测试脚本，当前玩家对象已改用 `Scripts/Player/*` 控制链）
- `Assets/LXII game 整合/Scripts/Camera/LXIIThirdPersonCameraFollow.cs`
- `Assets/LXII game 整合/Scripts/Player/LXIIPlayerInputReader.cs`
- `Assets/LXII game 整合/Scripts/Player/LXIIPlayerMotor.cs`
- `Assets/LXII game 整合/Scripts/Player/LXIIPlayerAnimationDriver.cs`
- `Assets/LXII game 整合/Scripts/Player/LXIIPlayerController.cs`
- `Assets/LXII game 整合/Settings/LXII_Nilou_LXI_Test.controller`
- `Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx.meta`
- `Assets/L12 grass/Docs/L12_InteractiveGrass_Workflow.md`
- `Assets/L17 Volumetric Lighting/L17.unity`
- `Assets/L17 Volumetric Lighting/Shaders/L17TwoSidedInteriorLit.shader`
- `Assets/L17 Volumetric Lighting/Scripts/L17VolumetricLightingController.cs`
- `Assets/L17 Volumetric Lighting/Scripts/L17RuntimeCameraMotion.cs`
- `Assets/L17 Volumetric Lighting/Scripts/L17FrustumVolumetricRendererFeature.cs`
- `Assets/L17 Volumetric Lighting/Shaders/L17FrustumVolumetricLighting.shader`
- `Assets/L17 Volumetric Lighting/Textures/L17_BlueNoise64.asset`
- `Assets/L17 Volumetric Lighting/Materials/L17_PostProcessProfile.asset`
- `Assets/L17 Volumetric Lighting/Materials/{L17_RoomWall,L17_DustyFloor,L17_WindowFrame}.mat`
- `Assets/L17 Volumetric Lighting/Editor/L17VolumetricLightingDemoBuilder.cs`
- `Assets/L17 Volumetric Lighting/Docs/L17_ModernVolumetricLighting_Workflow.md`

## 当前待办
- 后续每轮 LXII 代码或场景改动后，按 Unity 编译状态 + Console Error + 必要 Play Mode 验证记录结果。
- 继续处理最明显、最常出现的局部穿模。
- 把 L10.9 近距离屏幕抖动溶解 shader 接入 LXII 当前玩家链路。
- 继续把 L14 雪先接入 LXII 路径，再接 L13 云，不额外扩散范围。
- 在当前基础控制链上继续补正式输入与交互，不回退到单脚本临时测试驱动。
- 衣物和头发物理模拟不作为当前内建开发项；后续如要推进，默认按 Unity 插件或成熟现成方案接入。
- 补一组 LXII 当前演示截图 / 录像。

## 维护规则
- 只保留当前仍有效的结论，不保留长历史。
- 已完成且不再影响当前决策的旧事项直接删除。
- 每次更新优先覆盖旧描述，避免同一事项多版本并存。
