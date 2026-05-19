# 当前任务（精简）

## 当前主线
- `Assets/LXII game 整合/game.unity` 当前方案已修正：不使用 L11/StarRail；玩家固定为 L10.9 妮露 `Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx`，保留其 L10.9 材质，并补 Humanoid Avatar 后使用 LXI 动作。
- `Assets/L14 Snow/L14.unity` 已作为可交互雪地 Demo 场景落地，当前可进入 Play Mode 验证。
- `Assets/L13 VolumeCloud/L13.unity` 已作为光线步进体积云 Demo 场景落地，当前可进入 Play Mode 验证。
- `Assets/L12 grass/L12.unity` 已作为大规模可交互草地 Demo 场景落地，当前可进入 Play Mode 验证。
- `Assets/L10.9 learnNPR/L10.9.unity` 已作为当前可演示 Demo 场景推进：角色展示、运行时相机漫游、近距离镜头消隐已跑通。
- `Assets/L11 NPR/L11.unity` 的 StarRail NPR 完整链路仍作为后续任务保留，暂不继续沿旧的一周冲刺计划推进。

## 已完成结论
- 已修正 Claude Code 单 CLI duo-agent 工作流文档：`Assets/LXII game 整合/Docs/LXII_ClaudeCode_DuoAgent_Workflow.md`，明确 LXII 不用 L11，下一优先级是妮露 Humanoid Avatar/骨骼绑定 + LXI 动作重定向。
- 已重写 LXII 工作流文档结构：补充当前仓库真实快照、阶段完成标准、验证矩阵和启动 Prompt，避免把目标状态误写成已落地状态。
- 已新增 `Tools/LXII/Setup Nilou Humanoid In Game Scene` 编辑器工具：会校验/必要时重导入妮露 FBX 为 Humanoid，验证 Avatar 有效性，并把妮露实例 + Animator + 固定材质写入 `Assets/LXII game 整合/game.unity`。
- 已在 Unity 2022 中执行 `Tools/LXII/Setup Nilou Humanoid In Game Scene` 和 `Tools/LXII/Validate Nilou Humanoid Avatar`：`game.unity` 已写入 `LXII Nilou Player`，Avatar 校验通过。
- 已修正 LXII 妮露初版材质替换漏网项：`EffectMesh / EyeStar` 现在也会落到 `Face and face_eye.mat`，衣服发粉问题已消失。
- 已将妮露 Humanoid 从“自动猜测映射”改成“显式主干映射”：`NPC_Avatar_Girl_Sword_Nilou.fbx.meta` 现在明确写入 Hips / Spine / Chest / UpperChest / Neck / Head / 四肢骨骼名称，避免 Unity 自动映射漂移。
- 已新增 `Tools/LXII/Setup LXI Animation Test In Game Scene`：会把 LXI 的 `Idle / Run / Action` 三个 Female Humanoid clip 接入 `game.unity`，生成 `LXII_Nilou_LXI_Test.controller`，并给 `LXII Nilou Player` 挂上 `LXIIAnimationTestDriver`。
- 已在 Unity 2022 的 `game.unity` 中完成 LXI 动作测试：`1=Idle`、`2=Run`、`3=Action` 实测可切换，当前机位下可直接观察胯部、肩颈和头发链表现。
- 已调整 LXII 测试机位：`Main Camera` 固定为 `Position (0, 1.18, -3.6)`、`Rotation (5, 0, 0)`、`FOV 40`，用于人物正面动作验证。
- L14 雪地示例已完成：Compute Shader 写入 1024² ARGBHalf 雪面高度/堆雪状态图，520x520 高细分网格做真实顶点位移，片元阶段用高度梯度重建法线。
- L14 雪材质已改为 BaseColor/Normal/Height/Roughness/SparkleMask 贴图管线；Shader 使用多尺度高度/法线混合、压实色与粗糙度联动，静态 SparkleMask + 视角高光实现雪晶闪点，动态白色流动点和方块雪晶已移除。
- L14 压痕材质响应已调优：压过区域更暗、更粗糙、少闪点，边缘堆雪高度和亮度降低，轨迹从偏“白色软管”改为更克制的压实凹槽。
- L14 场景已收敛为纯技术 Demo：玩家和两个自动移动体均改为可见小球；每个小球只保留 1 个 `L14SnowInteractor` 压痕源，玩家不再生成左右脚 stamp，所以小球轨迹是单条圆形压痕；构建器菜单为 `Tools/Snow/Build L14 Interactive Snow Demo`。
- L14 资源位于 `Assets/L14 Snow/{Scripts,Shaders,Materials,Textures,Editor}`；最终预览截图位于 `Assets/Screenshots/L14_InteractiveSnow_material_pipeline_v4_final_20260504.png`，后续截图不要复用旧文件名。
- L14 校验已通过：`dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj` 均 0 error；`shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- L13 体积云示例已完成：体积盒 Ray March、周期无缝 3D Shape/Detail 噪声贴图、周期 WeatherMap、低成本光照步进阴影、Henyey-Greenstein 相位、银边/粉末感、风场动画和 XZ 边界淡出。
- L13 云盒缩放已与噪声采样解耦：Transform Scale 只控制 Ray-Box 边界，Shader/Controller 通过独立 `Noise World Size` 保持云纹理世界尺度，非等比缩放云盒不会拉伸云团。
- L13 场景包含云体积盒、低角度太阳、远景地貌、后处理 Volume、相机控制和 HUD 预设；构建器菜单为 `Tools/Volume Cloud/Build L13 Raymarched Volume Cloud Demo`。
- L13 噪声资源位于 `Assets/L13 VolumeCloud/Textures`；可通过 `Tools/Volume Cloud/Regenerate L13 Noise Textures` 重建。
- L13 噪声生成参数已抽成 `Assets/L13 VolumeCloud/Settings/L13CloudNoiseSettings.asset`，可在 Inspector 调 Shape/Detail/Weather 程序化噪声参数；自定义 Inspector 提供手动生成按钮和可选延迟自动生成。
- L13 已优化默认性能档：16 view steps / 0 light steps，`Light Step Count = 0` 时走低成本近似透光，编辑器非播放状态不再每帧写云材质。
- 当前验证：`dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj` 均 0 error；Unity Console Error 为 0。
- L12 草地示例已升级：`DrawMeshInstancedIndirect` 替代 Procedural Draw，Compute Shader 使用 AppendBuffer 输出 3 档 LOD 可见草簇，args buffer 驱动间接绘制。
- L12 现在包含 chunk 分块、CPU chunk 粗剔除、GPU 视锥/距离/密度图剔除、`Assets/L12 grass/Textures/L12_GrassDensity.asset` 密度图、交互压草纹理和 HUD 状态展示。
- L12 运行时相机已支持右键拖拽旋转、滚轮缩放、中键拖拽平移观察中心、`R` 复位。
- L12 制作流程文档已新增：`Assets/L12 grass/Docs/L12_InteractiveGrass_Workflow.md`。
- 当前验证：`dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj` 均 0 error；`shader_check_errors` 0 error；`scene_health_check` 0 findings；Play Mode 短跑后 Console Error 为 0。
- `Main Camera` 已挂 `Assets/Scripts/SimpleCameraController.cs`，运行后支持 WASD 移动、右键旋转视角、中键拖拽平移、滚轮前后推拉、Shift 加速、Q/E 升降。
- `Assets/L10.9 learnNPR/shader advance/Toonshader advanced2.shader` 已加入近距离屏幕抖动溶解，用于替代摄像机贴近角色时的 near clip 硬切。
- Nilou 材质 `Body 1.mat`、`Body 2.mat`、`Hair 1.mat`、`Face and face_eye.mat` 已启用近距离溶解并完成一版演示参数。
- 后续尝试的“整体同步/模型范围”重做方案效果不理想，已由用户 discard 回退；当前以最早这版 `Start/End` 屏幕抖动溶解为准。
- 当前验证：Play 模式贴近角色可见柔性颗粒消隐；Console Error 为 0；`L10.9` 场景未变脏。

## 当前改动落点
- `Assets/LXII game 整合/Docs/LXII_ClaudeCode_DuoAgent_Workflow.md`
- `Assets/LXII game 整合/Editor/LXIINilouHumanoidSetup.cs`
- `Assets/L14 Snow/L14.unity`
- `Assets/L14 Snow/Scripts/*`
- `Assets/L14 Snow/Shaders/L14SnowSurface.shader`
- `Assets/L14 Snow/Shaders/L14SnowSim.compute`
- `Assets/L14 Snow/Materials/*`
- `Assets/L14 Snow/Textures/*`
- `Assets/L14 Snow/Editor/L14SnowDemoBuilder.cs`
- `Assets/Screenshots/L14_InteractiveSnow_material_pipeline_v4_final_20260504.png`
- `Assets/L13 VolumeCloud/L13.unity`
- `Assets/L13 VolumeCloud/Scripts/*`
- `Assets/L13 VolumeCloud/Shaders/L13RaymarchedVolumeCloud.shader`
- `Assets/L13 VolumeCloud/Materials/*`
- `Assets/L13 VolumeCloud/Textures/*`
- `Assets/L13 VolumeCloud/Editor/L13VolumeCloudDemoBuilder.cs`
- `Assets/L12 grass/L12.unity`
- `Assets/L12 grass/Scripts/*`
- `Assets/L12 grass/Shaders/L12InteractiveGrass.shader`
- `Assets/L12 grass/Shaders/L12GrassCull.compute`
- `Assets/L12 grass/Materials/*`
- `Assets/L12 grass/Textures/*`
- `Assets/L12 grass/Editor/L12GrassExampleBuilder.cs`
- `Assets/L12 grass/Docs/L12_InteractiveGrass_Workflow.md`
- `Assets/L10.9 learnNPR/L10.9.unity`
- `Assets/L10.9 learnNPR/shader advance/Toonshader advanced2.shader`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 1.mat`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 2.mat`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Hair 1.mat`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Face and face_eye.mat`

## LXII 进展（按新约束修正后）
- 妮露 FBX Avatar 已修正：`animationType` 2→3（Humanoid），`avatarSetup` 0→1，Unity reimport 无 Avatar 警告。
- Builder 已修正：生成妮露模型（非 Capsule/HumanM），绑定 4 个 L10.9 Toon 材质。
- 动画测试已补全三态：Idle、Run、Action，当前使用 LXI Female Humanoid 动画，场景内通过 `1/2/3` 切换。
- 动作测试相机已固化：正面中景机位，便于观察胯部和头发链。
- L11/StarRail 未使用（符合硬约束）。
- 验证：Unity 2022 中 `game.unity` 已实测切换 Idle / Run / Action；`dotnet build` 本轮未拿到稳定结果，暂以 Unity 场景内验证为准。

## 待办（只保留未完成）
- LXII：继续人工复核 Avatar Configuration 中胯部/头发链观感是否仍异常，必要时再细化到 Pose 或具体骨骼节点。
- LXII：集成 L10.9 近距离屏幕抖动溶解 shader。
- LXII 进阶：终点触发器（穿过草地区→雪地区→云下终点）。
- LXII 进阶：双脚 stamp 交互。
- 录制/截图一组 LXII 演示视角。

## 维护规则
- 每次只记录“最新结论 + 未完成事项”，不要写长过程。
- 完成一个待办就勾选或删除过时项。
