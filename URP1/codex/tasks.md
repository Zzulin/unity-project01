# 当前任务（精简）

## 主任务
- 围绕 `Assets/L11 NPR/L11.unity` 完成 StarRail NPR 角色渲染联调。

## 当前状态
- 场景：`L11`（角色观察场景）
- 管线：Graphics 默认 `NPR Render Pipeline.asset`；当前质量档 `High` 为 `UniversalRP-HighQuality.asset`
- 材质：`0_mesh_mesh` 多数槽位为 URP Lit 内嵌实例，`hair` 使用 `CharHair`
- 工具：`Tools/NPR/输出 L11 上下文报告` 已增强，新增默认 RendererFeature 列表、StarRailFeature 命中提示、角色材质槽位统计
- 代码状态：`Assets/L11 NPR/Editor/L11NprContextReporter.cs` 编译通过（`dotnet build Assembly-CSharp-Editor.csproj`，0 error）
- 参考项目本地路径：
- `Assets/UnityURPToonLitShaderExample-master`
- `Plugins/StarRailNPRShader-main`

## 待办（只保留未完成）
- 在 Unity 中执行 `Tools/NPR/输出 L11 上下文报告`，产出最新“命中/未命中”清单。
- 打开 Frame Debugger，验证 `HSRForward1/2/3`、`HSRHair*`、`HSROutline` 实际命中情况。
- 进入 Day 3：按方案 A 将 Demo 角色身体/面部关键槽位逐步切换到 `CharBody/CharFace`（保持可回滚）。

## 一周冲刺计划（2026-04-04 版，Demo 优先）

- 参考源收敛：仅使用 `UnityURPToonLitShaderExample` + `StarRailNPRShader` 完成 Demo。

### 面试向细化目标
- 目标 Demo：`L11` 场景中 1 个角色达到“现代 NPR 观感”（分段明暗 + 稳定描边 + 头发/脸部关系稳定 + 轻量后处理）。
- 面试可讲：能清楚解释“从最小 Toon 方程到 URP RendererFeature 多 Pass 落地”的设计取舍。

### Day 1：最小 Toon 光照跑通
- 参考 `UnityURPToonLitShaderExample`，在项目内做可调阈值/软硬边的最小 Toon Shader。
- 产出：1 个学习材质 + 3 张阈值对比截图。
- 必讲点：`NoL -> smoothstep -> 阴影色混合`，以及为什么先做最小闭环。

### Day 2：L11 链路核对
- 读 `StarRailRendererFeature.cs`，只关注实际启用的关键 Pass 与关键词。
- 用 `Tools/NPR/输出 L11 上下文报告` + Frame Debugger 确认 L11 是否命中 StarRail 链路。
- 产出：1 份“当前命中/未命中”清单。
- 必讲点：`HSRForward1/2/3`、`HSRHair*`、`HSROutline` 在渲染顺序中的作用。

### Day 3：材质治理（只做 Demo 角色）
- 选择方案 A（场景内嵌材质逐槽位切换），不做全项目资产重构。
- 将 Demo 角色身体/面部/头发切到 `CharBody/CharFace/CharHair`。
- 产出：1 套可回放的材质切换结果。
- 必讲点：为什么短周期 Demo 选 A（快、风险低、可回滚）。

### Day 4：视觉一轮联调
- 只调最影响观感的参数：明暗阈值、阴影色、描边宽度、头发高光。
- 产出：中景/近景截图对比（调前 vs 调后）。
- 必讲点：优先级排序（先读形体再读材质细节）。

### Day 5：阴影与前发遮挡稳定化
- 优先保证角色脸部与头发关系稳定，不追求复杂极端机位。
- 产出：3 个机位下无遮挡穿帮的可演示结果。
- 必讲点：前发深度遮挡的必要性与常见 artifact 控制。

### Day 6：后处理与整体观感
- 轻量联调 Bloom/Tonemapping，只做到“角色不灰、不爆、风格统一”。
- 产出：一组最终展示参数（可复制）。
- 必讲点：为什么后处理只做轻量，不让风格依赖强后期。

### Day 7：验收与打包
- 回归检查：PlayMode 稳定、主镜头观感稳定、关键截图齐全。
- 产出：Demo 验收包（场景、参数记录、截图）。
- 必讲点：质量门禁和下一步工程化方向（性能/平台/许可边界）。

### 本项目改造落点（首批）
- `Plugins/StarRailNPRShader-main/Runtime/StarRailRendererFeature.cs`
- `Plugins/StarRailNPRShader-main/Shaders/Character/CharBody.shader`
- `Plugins/StarRailNPRShader-main/Shaders/Character/CharBodyCore.hlsl`
- `Plugins/StarRailNPRShader-main/Shaders/Character/CharFace.shader`
- `Plugins/StarRailNPRShader-main/Shaders/Character/CharFaceCore.hlsl`
- `Plugins/StarRailNPRShader-main/Shaders/Character/CharHair.shader`
- `Plugins/StarRailNPRShader-main/Shaders/Character/CharHairCore.hlsl`
- `Assets/L11 NPR/Editor/L11NprContextReporter.cs`

### 完成标准（DoD）
- [ ] L11 场景已有 1 个“现代 NPR 观感”的可演示角色
- [ ] Frame Debugger 可见关键 StarRail Pass 命中
- [ ] 输出一份可复用参数基线（阈值/描边/Bloom/Tonemapping）
- [ ] 明确 GPL-3.0 边界：学习可用，商用代码需重写实现

## 维护规则
- 每次只记录“最新结论 + 未完成事项”，不要写长过程。
- 完成一个待办就勾选，并删除过时信息。

## 最新进展（2026-04-06）
- 已重写 `Assets/L10.9 learnNPR/Script/SetStencilRef.cs`：支持 `stencilRef`、`includeInactive`、`applyOnStart`，并提供 `OnValidate` 实时刷新与 `Apply StencilRef To Children` 右键菜单。
- 实现方式使用 `renderer.sharedMaterials`，仅对含 `_StencilRef` 的材质执行 `SetInt`，其他材质自动跳过。
- 编译校验：`dotnet build Assembly-CSharp-Editor.csproj` 通过（0 error）。
