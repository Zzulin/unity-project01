# L14 可交互雪地制作流程

## 目标

在 `Assets/L14 Snow` 中制作一个 Unity URP 可交互雪地技术 Demo。核心目标是展示 GPU 高度场、真实网格位移、压痕材质响应和可重建的工程结构，而不是做完整生产级雪地系统。

Demo 通过 Editor 菜单一键生成，便于演示、回滚和继续迭代。

## 当前状态

- 场景：`Assets/L14 Snow/L14.unity`
- 构建入口：`Tools/Snow/Build L14 Interactive Snow Demo`
- 场景内容：雪面、玩家小球、两个自动移动小球、相机、HUD、方向光
- 雪面几何：`520x520` 高细分网格
- 雪面状态图：`1024x1024 ARGBHalf` RenderTexture
- 交互逻辑：`L14SnowInteractor` 写入压痕源，Compute Shader 更新雪面状态
- 材质管线：BaseColor、Normal、Height、Roughness、SparkleMask
- 当前效果：压痕中心更暗、更粗糙、少闪点；边缘堆雪更克制

## 主要资源

| 类型 | 路径 |
| --- | --- |
| 场景 | `Assets/L14 Snow/L14.unity` |
| 构建器 | `Assets/L14 Snow/Editor/L14SnowDemoBuilder.cs` |
| 雪面运行时 | `Assets/L14 Snow/Scripts/L14SnowField.cs` |
| 压痕源 | `Assets/L14 Snow/Scripts/L14SnowInteractor.cs` |
| 玩家移动 | `Assets/L14 Snow/Scripts/L14SnowWalker.cs` |
| 自动移动 | `Assets/L14 Snow/Scripts/L14SnowAutoInteractor.cs` |
| 相机控制 | `Assets/L14 Snow/Scripts/L14SnowCameraRig.cs` |
| HUD | `Assets/L14 Snow/Scripts/L14SnowDemoHud.cs` |
| 雪面 Shader | `Assets/L14 Snow/Shaders/L14SnowSurface.shader` |
| 雪面 Compute | `Assets/L14 Snow/Shaders/L14SnowSim.compute` |
| 材质 | `Assets/L14 Snow/Materials/*` |
| 雪贴图 | `Assets/L14 Snow/Textures/*` |

## 大事记

### 1. 独立 Demo 结构落地

L14 参考 L12 的组织方式，建立独立的 `Scripts / Shaders / Materials / Textures / Editor` 结构。核心能力集中在 `L14SnowDemoBuilder`，通过菜单一键重建场景、雪面、交互体、相机和 HUD。

这一阶段确立了 L14 的基本工程边界：不依赖其他课程场景状态，所有运行时代码和资源都集中在 `Assets/L14 Snow`。

### 2. GPU 高度场交互方案成型

雪面交互采用 GPU 状态图方案：Compute Shader 将交互体轨迹写入 RenderTexture，记录压痕、堆雪和恢复状态；雪面 Shader 在顶点阶段采样高度数据，让网格真实发生位移。

这个方案把 L14 从“贴图脚印”推进到“高度场驱动的真实几何凹陷”。

### 3. 场景收敛为纯技术 Demo

初版包含树、石头、栅栏、飘雪等场景装饰。后续为了突出雪地交互本身，场景收敛为纯技术 Demo，只保留雪面、三个交互小球、相机、HUD 和方向光。

角色和载具也最终统一改为小球，降低视觉干扰，方便观察压痕和材质变化。

### 4. 拓扑级凹陷升级

为了让脚印和轨迹不只是法线效果，雪面升级为：

- `1024x1024 ARGBHalf` GPU 状态图
- `520x520` 高细分雪面网格
- 顶点阶段真实 displacement
- 片元阶段基于高度梯度重建法线
- Compute 局部 dispatch，减少全图扫描成本

这一阶段是 L14 的核心技术升级。

### 5. 雪材质从程序噪声转向贴图管线

早期 Shader 内部 Hash 噪声带来明显方块感，不适合现代雪地观感。随后改为独立雪材质贴图管线：

- BaseColor
- Normal
- Height
- Roughness
- SparkleMask

Shader 使用多尺度高度、法线和粗糙度混合。压痕区域不再只改变高度，也会影响颜色、法线、粗糙度和雪晶高光。

### 6. 雪面光照与闪点升级

雪面光照加入：

- 包裹漫反射
- 浅层透光
- 掠射边缘光
- 静态视角相关雪晶闪点

动态白色流动点和方块雪晶已移除。当前闪点来自 SparkleMask 与视角高光，不再依赖时间流动。

### 7. 压痕材质响应优化

后续重点调整了压痕观感。目标是避免轨迹像发亮的白色软带，让压过区域更像被压实的雪。

当前策略：

- 中心压实区域更暗
- 中心粗糙度更高
- 压过区域减少雪晶闪点
- 边缘堆雪高度和亮度降低
- Compute stamp 形状更集中

### 8. 交互源规则明确

脚印数量由启用的 `L14SnowInteractor` 数量决定。小球版本已改成每个小球只保留 1 个 `L14SnowInteractor`，所以每个小球只产生一条圆形/连续轨迹。

当前有效可见交互体：

- 玩家小球
- 自动滑行小球
- 自动压雪小球

## 当前验证记录

历史验证中多次通过：

- `dotnet build Assembly-CSharp.csproj`：0 error
- `dotnet build Assembly-CSharp-Editor.csproj`：0 error
- `shader_check_errors`：0 error
- `scene_health_check`：0 findings
- Play Mode 短跑后 Console Error：0

备注：2026-05-07 后 `dotnet build` 偶尔会被 Unity 或项目文件锁拖住。遇到这种情况，优先使用 UnitySkills 做编译状态、Shader 和场景健康检查。

## 设计取舍

- 当前是技术 Demo，不是完整 AAA 雪地系统。
- 没有实现地形虚拟纹理、局部 tile cache、时间重投影、真实物理接触解算。
- 雪材质贴图为程序化生成，后续可替换为高质量扫描贴图。
- 当前优先展示 GPU 高度场、真实顶点位移、材质响应和工程可重建性。

## 后续方向

### 小球与雪面一体感

当前最直接的待办是增强小球体积和雪面接触感：

- 放大小球可见体积
- 降低小球中心高度，让球体半嵌入雪面
- 同步放大 `L14SnowInteractor.radius`
- 增强接触区域压痕深度和范围
- 可选增加接触暗部/AO

### 效果继续优化

- 雪粉飞溅
- 接触阴影/AO
- 远近 LOD
- 更精细的方向性雪晶高光
- 压痕恢复时高度、颜色、粗糙度分离衰减
- 替换外部高质量雪材质贴图

## 视频简介可用文案

L14 是一个基于 Unity URP 的可交互雪地技术 Demo。雪面使用 Compute Shader 写入 1024 分辨率的 GPU 高度/压实状态图，再由高细分网格在顶点阶段采样实现真实几何凹陷。材质侧使用 BaseColor、Normal、Height、Roughness、SparkleMask 多贴图混合，压痕区域会同步改变高度、法线、颜色、粗糙度和雪晶高光。整个场景可以通过 Editor 菜单一键重建，适合展示实时交互雪地、GPU 高度场和程序化材质管线。
