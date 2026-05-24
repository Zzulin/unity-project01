# L12 大规模可交互草地制作流程

## 目标

在 `Assets/L12 grass` 中制作一个 Unity URP 大规模可交互草地技术 Demo。效果目标是展示开放世界草地常见的 GPU 驱动渲染链路：`DrawMeshInstancedIndirect`、草地 Chunk 分块、Compute Shader 剔除、密度图控制生成、近中远 LOD、风场动画和运行时交互压草。

本示例采用独立目录和 Editor 菜单一键生成的方式组织，便于重建、迁移、演示和继续迭代。

## 当前状态

- 场景：`Assets/L12 grass/L12.unity`
- 渲染方式：`Graphics.DrawMeshInstancedIndirect`
- 剔除方式：CPU Chunk 粗剔除 + Compute Shader 草实例剔除
- 草数据：CPU 生成基础草实例数据，GPU 端按可见性写入 LOD Append Buffer
- LOD：LOD0 双交叉面片 5 段草簇，LOD1 双交叉面片 3 段草簇，LOD2 单面片草叶
- 草叶 Mesh：条带主体 + 单顶点三角草尖收束，支持根部宽度倍率、草高随机和叶形随机调节
- 密度控制：`L12_GrassDensity.asset` 灰度密度图控制草保留概率和颜色明暗
- 交互：运行时交互压草贴图，支持轨迹压草、恢复速度、压低强度
- 缩放：`Transform Scale(XZ)` 可直接放大或缩小草地区域；开启 `preserveDensityWhenResized` 时会自动补实例，尽量维持原草间距
- 密度调参：`targetBladeSpacing` 现已改为 Inspector 滑条，推荐在 `0.06 ~ 0.18` 间使用；`0.4` 以上视觉上通常会过稀
- 参数面板：`L12GrassRendererEditor` 提供中文 Inspector，弱化误导性参数名；`chunksPerSide` 当前标注为性能分块数，不代表画面会按块裁剪
- 草尖表现：`tipBrightness` 可增强草尖浅色，配合 `tipColor` 做更接近二次元草海的明亮层次
- 光照：草地 Shader 已接入 URP `Main Light Shadows / Soft Shadows`，可接收方向光和场景物体投下来的阴影
- 风：方向风 + 阵风波带 + 局部抖动，形成一阵一阵吹过的整体感
- 交互控制：WASD / 方向键移动交互体，右键旋转视角，滚轮缩放，中键平移，R 复位
- 场景内容：草地平面、主交互体、自动交互体、相机、灯光和 HUD

## 主要资源

| 类型 | 路径 |
| --- | --- |
| 场景 | `Assets/L12 grass/L12.unity` |
| 草地渲染脚本 | `Assets/L12 grass/Scripts/L12GrassRenderer.cs` |
| 草地 Shader | `Assets/L12 grass/Shaders/L12InteractiveGrass.shader` |
| 剔除 Compute Shader | `Assets/L12 grass/Shaders/L12GrassCull.compute` |
| 交互体注册脚本 | `Assets/L12 grass/Scripts/L12GrassInteractor.cs` |
| 玩家移动脚本 | `Assets/L12 grass/Scripts/L12GrassWalker.cs` |
| 自动交互体脚本 | `Assets/L12 grass/Scripts/L12GrassAutoInteractor.cs` |
| 相机控制 | `Assets/L12 grass/Scripts/L12GrassCameraRig.cs` |
| HUD | `Assets/L12 grass/Scripts/L12GrassDemoHud.cs` |
| 构建器 | `Assets/L12 grass/Editor/L12GrassExampleBuilder.cs` |
| 中文 Inspector | `Assets/L12 grass/Editor/L12GrassRendererEditor.cs` |
| 草地材质 | `Assets/L12 grass/Materials/L12_InteractiveGrass.mat` |
| 地面材质 | `Assets/L12 grass/Materials/L12_Ground.mat` |
| 交互体材质 | `Assets/L12 grass/Materials/L12_Interactor.mat` |
| 密度图 | `Assets/L12 grass/Textures/L12_GrassDensity.asset` |

## Unity 菜单

| 菜单 | 用途 |
| --- | --- |
| `Tools/Grass/Build L12 Interactive Grass Demo` | 重建 L12 场景、草地、材质、密度图、交互体、相机、灯光和默认参数 |

## 制作流程

### 1. 搭建独立 L12 结构

在 `Assets/L12 grass` 下建立独立资源结构：

- `Scripts`
- `Shaders`
- `Materials`
- `Textures`
- `Editor`
- `Docs`

核心思路是让 L12 自成一个可重建 Demo，不依赖其他场景状态。`L12GrassExampleBuilder.cs` 负责创建目录、材质、密度图、场景对象和默认参数。

### 2. 第一版 GPU Instancing 草地

第一版目标是先跑通大量草实例的 GPU 渲染：

- CPU 生成规则网格上的草实例位置。
- 每棵草存储为 `float4`：
  - `x`：本地 X 坐标
  - `y`：本地 Z 坐标
  - `z`：随机 yaw
  - `w`：随机高度缩放
- 草叶 Mesh 使用多张交叉面片形成草簇体积感。
- Shader 通过 `SV_InstanceID` 读取每棵草的实例数据。
- 顶点阶段执行随机旋转、缩放、风吹弯折和颜色变化。

早期版本重点是验证草海能稳定绘制，并能承载后续交互和剔除逻辑。

### 3. 改为 DrawMeshInstancedIndirect

为了支持 GPU 端决定可见实例数量，渲染方式改为：

```csharp
Graphics.DrawMeshInstancedIndirect(...)
```

当前渲染链路：

```text
CPU 生成全部草实例数据
        ↓
Compute Shader 按 Chunk 分批剔除
        ↓
可见草写入 LOD AppendStructuredBuffer
        ↓
ComputeBuffer.CopyCount 写入 indirect args
        ↓
DrawMeshInstancedIndirect 绘制可见草实例
```

这样 CPU 不需要逐个提交草实例，实例数量可以由 GPU 剔除结果决定，适合大规模草地。

### 4. 加入 Chunk 分块

草地被划分为 `chunksPerSide x chunksPerSide` 个 Chunk。默认参数：

- `fieldSize = 90`
- `bladesPerSide = 300`
- `chunksPerSide = 12`

每个 Chunk 记录：

- `sourceOffset`
- `sourceCount`
- 本地中心点
- Chunk 尺寸

渲染前先在 CPU 做粗粒度剔除：

- 计算相机视锥体平面。
- 用 `GeometryUtility.TestPlanesAABB` 检查 Chunk Bounds。
- 超出最大绘制距离的 Chunk 直接跳过。

通过 Chunk 粗剔除，可以减少 Compute Shader 每帧需要处理的草实例范围。

注意：`chunksPerSide` 不是视觉边缘控制参数。当前流程是 CPU 先按 Chunk 做视锥和距离的粗预筛，只有可能可见的 Chunk 才会 dispatch 到 Compute Shader；进入 Compute 后，仍然会对 Chunk 内每株草执行逐株距离剔除：

```hlsl
if (distanceToCamera > _MaxDrawDistance)
{
    return;
}
```

因此最终可见边缘通常是围绕相机的圆弧，而不是方块状 Chunk 边界。这个设计的目的，是让 Chunk 负责减少无效计算，让逐株剔除负责平滑观感。

### 5. Compute Shader 剔除和 LOD 分类

`L12GrassCull.compute` 的职责：

- 根据相机距离剔除超远草。
- 根据 6 个视锥平面剔除屏幕外草。
- 采样密度图，按密度阈值和随机概率控制草是否保留。
- 根据距离把草写入 3 个不同 LOD Buffer：
  - `_VisibleBladeData0`
  - `_VisibleBladeData1`
  - `_VisibleBladeData2`

距离分类：

```text
distance < lod0Distance  -> LOD0
distance < lod1Distance  -> LOD1
其他可见距离             -> LOD2
```

剔除结果通过 `AppendStructuredBuffer` 保存，再用 `ComputeBuffer.CopyCount` 把可见实例数写入 `IndirectArguments` Buffer。

### 6. 草叶 LOD Mesh

当前有 3 套 LOD Mesh：

| LOD | Mesh 结构 | 用途 |
| --- | --- | --- |
| LOD0 | 2 张交叉面片，5 段高度分段 | 近景草簇，保留体积感并降低近景三角形数量 |
| LOD1 | 2 张交叉面片，3 段高度分段 | 中景草簇，降低三角形数量 |
| LOD2 | 1 张面片，1 段高度分段 | 远景草叶，最低成本 |

草叶 Mesh 不是几何着色器生成的，而是在 `L12GrassRenderer.CreateBladeMesh` 中由 C# 构建。

近景曾尝试过多段矩形草叶和顶部斜切矩形，但在近距离会出现顶部毛刺、薄片交叠和不稳定的尖边伪影。当前已回到三角草尖方案，把草叶拓扑收敛为：

```text
条带主体 + 单个尖端顶点
```

最后一段用三角形收束到草尖，减少近景平顶和叉口感。

当前 LOD0 为 `2 card x 5 segment`：每棵草保留两张交叉草叶卡片，通过随机高度、宽窄、自旋和倾斜获得变化；相比原先 3 card 方案，近景体积感略低，但实例数较高时性能和交叠观感更稳定。

### 7. 暴露根部宽度参数

新增 `bladeRootWidthScale` 参数，用于控制草叶 Mesh 根部横向展开比例。

用途：

- 数值小：草更细，根部更窄。
- 数值大：草更厚，底部更宽。
- 和 `bladeWidth` 配合使用：
  - `bladeRootWidthScale` 控制 Mesh 形状。
  - `bladeWidth` 控制 Shader 中最终世界宽度。

当 `bladeRootWidthScale` 改变时，会自动重建 LOD Mesh，并刷新 indirect args buffer。

### 8. 形状、高度和草尖颜色随机

为了让草地更接近开放世界二次元草海，而不是等高等宽的重复刀片，当前版本加入了少量真正影响观感的参数：

- `minBladeHeightScale`：矮草高度倍率，默认 `0.45`。
- `maxBladeHeightScale`：高草高度倍率，默认 `1.55`。
- `shapeVariation`：草叶形状随机度，影响宽窄、自旋、轻微倾斜和顶端偏移。
- `tipBrightness`：草尖明亮度，配合 `tipColor` 让草尖更浅、更通透。

高度随机在 CPU 生成草实例时写入 `blade.w`，并使用偏向低草的随机分布，让大部分草保持正常高度，少量高草穿出整体草面。形状随机和草尖亮度在 Shader 顶点/片元阶段执行，避免增加 CPU 实例数据结构复杂度。

当前不再暴露 `Card Count Near`。近景固定使用 `2 card x 5 segment`，这样参数面板不会继续堆难调项；如果后续要做质量档，可以在内部按质量预设切换，而不是让用户逐项调拓扑。

### 9. 可缩放覆盖范围和密度保护

草地区域支持通过 `Transform Scale(XZ)` 直接改变覆盖范围。为了避免“草地变大但实例数量不变，所以越放大越稀”，当前推荐开启：

```text
preserveDensityWhenResized = true
```

开启后，实际单轴草株数主要由 `targetBladeSpacing` 推导：

```text
scaledFieldSize / targetBladeSpacing
```

再由 `maxBladesPerAxis` 做安全上限保护。当前上限已从早期过高的 `4096` 收敛到 `1024`，避免误调后显存和实例数暴涨。

参数含义：

- `bladesPerSide` / 基础草株数：基础采样参考值；开启保持密度后不再是主要密度旋钮。
- `targetBladeSpacing` / 目标草间距：主要密度旋钮，值越小越密；Inspector 使用 `0.01 ~ 0.4` 滑条。
- `maxBladesPerAxis` / 单轴安全上限：防爆保护，不建议当作常规密度参数。
- `chunksPerSide` / 性能分块数：影响 CPU 粗筛和 Compute dispatch 粒度，不改变逐株距离剔除的圆弧边缘。

### 10. 中文 Inspector 参数面板

`Assets/L12 grass/Editor/L12GrassRendererEditor.cs` 为 `L12GrassRenderer` 提供了中文 Inspector。目的不是改变运行时逻辑，而是把参数名字改成更接近调参意图的中文，减少误解。

示例：

- `Target Blade Spacing` -> `目标草间距`
- `Max Blades Per Axis` -> `单轴安全上限`
- `Chunks Per Side` -> `性能分块数`
- `Min / Max Blade Height Scale` -> `高低层次：矮草倍率 / 高草倍率`
- `Shape Variation` -> `叶形随机度`
- `Tip Brightness` -> `草尖发光感`

### 11. 密度图生成和绑定

密度图路径：

```text
Assets/L12 grass/Textures/L12_GrassDensity.asset
```

它由 `L12GrassExampleBuilder.LoadOrCreateDensityMap()` 生成，不是外部导入 PNG。

生成配置：

- 尺寸：`256 x 256`
- 格式：`RGBA32`
- Mipmap：关闭
- 颜色空间：线性
- Wrap：Clamp
- Filter：Bilinear

每个像素写入灰度密度：

- `centerFalloff`：中心更密，边缘更稀。
- `pathA`：横向弯曲小路，降低草密度。
- `pathB`：纵向弯曲小路，降低草密度。
- `PerlinNoise`：增加自然随机变化。

Compute Shader 采样 R 通道控制草保留：

```text
density <= densityThreshold -> 剔除
Hash > survival             -> 按概率剔除
否则                         -> 保留
```

草地 Shader 也会采样同一张密度图做颜色明暗变化，让稀疏和密集区域过渡更自然。

### 12. 交互压草贴图

早期交互方式是把少量球形交互体直接传给 Shader。这个方案数量有限，且难以保留轨迹。

后续升级为运行时交互压草贴图：

- CPU 维护一张 `interactionTexture`。
- R/G 通道存储压草方向。
- B 通道存储压草压力。
- 每帧先按 `interactionFadeSpeed` 恢复旧痕迹。
- 再根据活跃 `L12GrassInteractor` 写入新的压草轨迹。

压草从圆形印记升级为上一帧到当前帧的胶囊轨迹，移动时能留下连续痕迹。

Shader 中根据交互贴图：

- 横向弯折草叶。
- 向下压低草叶。
- 稍微压暗草尖颜色。

主要参数：

- `interactionTextureSize`：压草纹理尺寸，影响边缘精度。
- `interactionStrength`：横向压草力度。
- `interactionFlattenStrength`：向下压低强度。
- `interactionFadeSpeed`：压草痕迹恢复速度。

### 13. 风场升级

第一版风是局部正弦抖动，整体性不足。后续改为：

- `windDirection`：主风方向。
- `gustStrength`：阵风强度。
- `gustFrequency`：阵风波带频率。
- `gustSpeed`：阵风移动速度。
- `gustWidth`：阵风带宽度。
- `gustNoiseScale`：阵风噪声扰动尺度。

Shader 使用世界空间坐标沿风向计算阵风波带，让一片草地出现“一阵风吹过”的整体运动，而不是每棵草各自乱摆。

### 14. 相机和演示控制

运行时相机由 `L12GrassCameraRig` 控制，主交互体由 `L12GrassWalker` 控制。

输入：

- WASD / 方向键：移动交互体。
- Shift：加速。
- 鼠标右键拖拽：旋转视角。
- 鼠标滚轮：缩放。
- 鼠标中键拖拽：平移观察中心。
- R：复位相机。

HUD 由 `L12GrassDemoHud` 显示：

- 草实例数量。
- Chunk 可见数。
- 当前 Draw 技术。
- LOD 距离。
- 密度图名称。
- 当前交互体数量。

## 主要算法名称

- GPU Instancing
- `DrawMeshInstancedIndirect`
- Indirect Arguments Buffer
- Compute Shader Culling
- AppendStructuredBuffer 可见实例收集
- Chunk-based Frustum Culling
- Distance Culling
- Density Map / Terrain Splat Map 控制草生成
- Hash-based Density Survival
- Near / Mid / Far LOD
- Cross-card Grass Billboard Mesh
- Runtime Interaction Texture
- Trail / Capsule Stamping 压草轨迹
- Exponential Fade Recovery
- Vertex Shader Wind Bending
- Directional Gust Wind Field
- Height Variation / Shape Variation
- Tip Brightness 草尖亮度控制
- Density-preserving Resize
- Custom Inspector 中文调参面板
- MaterialPropertyBlock 参数推送

## 当前验证记录

旧线程中多次通过：

- `dotnet build Assembly-CSharp.csproj`：0 error
- `dotnet build Assembly-CSharp-Editor.csproj`：0 error
- Unity Console Error：0

最近一次文档整理前的本地验证：

- `dotnet build Assembly-CSharp.csproj`：0 error
- `dotnet build Assembly-CSharp-Editor.csproj`：0 error

2026-05-24 草叶拓扑和参数面板更新后验证：

- UnitySkills `asset_refresh`：完成
- UnitySkills `debug_check_compilation`：`isCompiling=false`，`isUpdating=false`
- Unity Console Error：0

## 已知设计取舍

- 当前是 Demo 级大规模草地，不是完整开放世界植被系统。
- 草地高度暂时是平面场地，没有绑定 Unity Terrain 高度采样。
- 密度图是 Editor 生成的灰度 Texture2D，不是 TerrainLayer splat map 直接读取。
- 交互压草贴图由 CPU 更新，逻辑清晰，后续可迁移到 Compute Shader 或 RenderTexture stamping。
- `DrawMeshInstancedIndirect` 主链路已经接近现代大规模草地做法，但还没有做 GPU occlusion culling、Hi-Z、cluster streaming 或 terrain tile streaming。
- LOD 是按距离分类的 Mesh LOD，没有做淡入淡出过渡，远近切换在极端观察角度下可能仍可见。
- 草叶使用交叉面片追求近景体积感，成本高于单面片 billboard；当前近景固定为 `2 card x 5 segment`，不再暴露卡片数参数。
- `chunksPerSide` 只负责 CPU 粗剔除和 dispatch 粒度；最终画面边缘仍由 Compute 中逐株 `distanceToCamera > _MaxDrawDistance` 决定，所以可见边界通常是圆弧而不是块状。

## 后续待办

### Terrain 高度和法线贴合

建议把草实例从平面分布升级到 Terrain 采样：

- 读取 Terrain heightmap 得到根部高度。
- 读取 Terrain normal 控制草根朝向。
- 支持坡度过滤，过陡区域减少草。
- 密度图可与 Terrain splat map 或 TerrainLayer 权重关联。

### 交互贴图 GPU 化

当前压草贴图由 CPU 像素数组更新。后续可改为：

- RenderTexture 存储交互图。
- 使用 Compute Shader 或全屏 Pass 进行 stamping。
- 支持更多交互源和更大范围。
- 支持多通道长期痕迹、短期弯折、湿地脚印等扩展。

### 更完整的远景策略

当前 LOD2 仍然绘制单面片草。后续可继续扩展：

- 远处草密度进一步降低。
- 远景过渡到地表 detail texture / normal texture。
- LOD 之间增加 dithering fade。
- 风动画在远景改为更低频的整体色彩/法线扰动。

### 生产级剔除优化

当前已经有 Chunk 和 Compute 剔除。后续可继续加入：

- Hi-Z occlusion culling。
- GPU-driven chunk dispatch。
- 每个 Chunk 独立 bounds 和可见计数。
- Terrain tile streaming。
- 根据相机速度动态调整密度和 LOD。

## 视频简介可用文案

Unity URP 大规模可交互草地 Demo：使用 `DrawMeshInstancedIndirect`、Compute Shader 剔除、Chunk 粗分块、密度图控制生成、近中远 LOD、交互压草贴图、2-card 三角草尖草叶拓扑、缩放保持密度、中文 Inspector 调参面板和方向阵风风场，实现可运行、可调参、可讲解的 GPU Driven Grass 示例。
