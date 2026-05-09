# L13 光线步进体积云制作流程

## 目标

在 `Assets/L13 VolumeCloud` 中制作一个 Unity URP 光线步进体积云技术 Demo。效果目标参考 UE5 体积云的基础观感：体积盒 Ray March、贴图驱动云形、天气图分布、光照透射、银边、粉末感、风场动画和可调参数。

本示例采用独立目录和 Editor 菜单一键生成的方式组织，便于重建、迁移和演示。

## 当前状态

- 场景：`Assets/L13 VolumeCloud/L13.unity`
- 渲染方式：体积盒 Ray March
- 云形数据：预生成 `Texture3D` 噪声贴图 + `WeatherMap`
- 噪声类型：周期无缝 Shape Noise、Detail Noise、WeatherMap
- 光照：低成本 Light Marching / 近似透光、Henyey-Greenstein 相位函数
- 效果：银边、粉末感、风场动画、XZ 边界淡出
- 盒体缩放：Transform Scale 只控制 Ray-Box 边界，噪声采样由独立 `Noise World Size` 控制，非等比缩放云盒时云纹理不随盒体拉伸。
- 交互：运行时相机控制、HUD 参数预设
- 场景内容：低角度太阳、远景地貌、后处理 Volume、中性蓝天 Procedural Sky

## 主要资源

| 类型 | 路径 |
| --- | --- |
| 场景 | `Assets/L13 VolumeCloud/L13.unity` |
| 体积云 Shader | `Assets/L13 VolumeCloud/Shaders/L13RaymarchedVolumeCloud.shader` |
| 云控制脚本 | `Assets/L13 VolumeCloud/Scripts/L13VolumeCloudController.cs` |
| HUD | `Assets/L13 VolumeCloud/Scripts/L13VolumeCloudDemoHud.cs` |
| 相机控制 | `Assets/L13 VolumeCloud/Scripts/L13VolumeCloudCameraRig.cs` |
| 噪声设置 | `Assets/L13 VolumeCloud/Settings/L13CloudNoiseSettings.asset` |
| 噪声设置脚本 | `Assets/L13 VolumeCloud/Scripts/L13CloudNoiseSettings.cs` |
| 噪声设置面板 | `Assets/L13 VolumeCloud/Editor/L13CloudNoiseSettingsEditor.cs` |
| 构建器 | `Assets/L13 VolumeCloud/Editor/L13VolumeCloudDemoBuilder.cs` |
| 生成贴图 | `Assets/L13 VolumeCloud/Textures/*` |
| 材质 | `Assets/L13 VolumeCloud/Materials/*` |

## Unity 菜单

| 菜单 | 用途 |
| --- | --- |
| `Tools/Volume Cloud/Build L13 Raymarched Volume Cloud Demo` | 重建 L13 场景、云盒、相机、灯光、材质和默认参数 |
| `Tools/Volume Cloud/Regenerate L13 Noise Textures` | 重新生成 ShapeNoise3D、DetailNoise3D、WeatherMap |
| `Tools/Volume Cloud/Select L13 Noise Settings` | 选中噪声参数资产 |
| `Tools/Volume Cloud/Capture L13 Preview` | 使用 Main Camera 输出预览截图 |

## 制作流程

### 1. 搭建独立 L13 结构

在 `Assets/L13 VolumeCloud` 下建立独立资源结构：

- `Scripts`
- `Shaders`
- `Materials`
- `Textures`
- `Settings`
- `Editor`
- `Docs`

核心思路是让 L13 自成一个可重建 Demo，不依赖 L11/L12 的场景状态。

### 2. 第一版 Ray March 原型

第一版实现内容：

- 用 Cube 作为体积云盒体。
- 在 Shader 中做 Ray-Box Intersection。
- 相机射线进入盒体后执行 Ray March。
- 每一步计算云密度。
- 沿太阳方向做短距离 Light Marching。
- 使用 Henyey-Greenstein Phase Function 控制前向散射。
- 加入银边、粉末感、风场动画、HUD 预设和相机控制。

第一版密度来源是 Shader 内部程序 FBM / Value Noise 现场计算。这个方案能快速验证体积云管线，但画面粗糙，性能也偏重。

### 3. 性能初次降档

第一版默认参数过高：

- `96 view steps`
- `8 light steps`
- 每步还现场计算多层程序噪声

Scene View 和 Game View 同时渲染时会明显卡顿。之后做了第一轮降档：

- 默认降为 `48 view steps / 4 light steps`
- 光照阴影采样改成低成本密度路径
- 编辑器非 Play Mode 不再每帧写云材质

这个阶段确认瓶颈不是模型数量，而是全屏体积 Shader 的按像素步进成本。

### 4. 改成贴图驱动体积云

为了让云形更接近真实体积云管线，第二版把 Shader 内部现场噪声改为预生成贴图采样。

新增自动生成资源：

- `Assets/L13 VolumeCloud/Textures/ShapeNoise3D.asset`
- `Assets/L13 VolumeCloud/Textures/DetailNoise3D.asset`
- `Assets/L13 VolumeCloud/Textures/WeatherMap.png`

Shader 密度路径改为：

- `_ShapeNoise.SampleLevel(...)`
- `_DetailNoise.SampleLevel(...)`
- `_WeatherMap.SampleLevel(...)`

贴图职责：

- `ShapeNoise3D`：控制大云团轮廓和主体体积。
- `DetailNoise3D`：控制云边侵蚀和细碎破碎。
- `WeatherMap`：控制平面云区覆盖、云型、局部密度和细节侵蚀强度。

### 5. 修复接缝

问题表现：云层出现明显十字接缝。

根因：

- 生成的 3D / 2D 噪声不是周期无缝。
- Shader 又使用 `frac` 或 Repeat 采样。
- 噪声回卷面在体积内部形成硬缝。

修复方式：

- ShapeNoise3D、DetailNoise3D、WeatherMap 改为周期无缝生成。
- Shader 去掉内部 `frac(p01...)` 回卷采样。
- 纹理 wrap mode 使用 Repeat。
- 增加 XZ 边界淡出，减少体积盒边缘硬切。

### 6. 调整天空和场景观感

问题表现：天空盒偏绿。

根因不是体积云 Shader，而是 Unity Procedural Sky、低角度暖色太阳、雾色和地面色混合后偏绿。

修复方式：

- 天空盒改为更中性的蓝天参数。
- 雾色改为偏蓝灰。
- 修复 `LoadOrCreateSkyboxMaterial()`：材质已存在时也会刷新参数，避免旧参数残留。

### 7. 修复参数保存和污染

问题表现：`Cloud Color` 需要点一下 Inspector 才生效，保存场景后又变回纯蓝。

根因：

- `_SunColor`、`_SunDirectionWS` 等运行时参数不在 Shader Properties 中。
- Unity 不会稳定序列化这些运行时写入值。
- 保存或刷新后光照参数丢失，云色计算退回异常状态。

修复方式：

- `L13VolumeCloudController` 每次渲染前使用 `MaterialPropertyBlock` 推送完整参数。
- 不再依赖材质 asset 保存动态光照值。
- Play Mode 的 HUD 预设只写 `MaterialPropertyBlock`，避免污染 shared material。
- `Regenerate L13 Noise Textures` 只刷新贴图和绑定，不再重置云材质艺术参数。

### 8. 性能二次优化

在高分辨率 Game View 下，`Step Count = 24` 仍然较重。原因是体积云成本近似：

```text
屏幕像素数 * View Steps * Light Steps
```

当 Game View 接近 500 万像素时，即使 24 步也会很重。

优化方式：

- 默认 `View Steps = 16`
- `Light Step Count` 支持 `0`
- `Light Step Count = 0` 时跳过嵌套 Light Marching，使用低成本近似透光
- HUD 预设降档：
  - Soft：`12 / 0`
  - UE-like：`16 / 0`
  - Storm：`24 / 1`
- `Step Count` 最低值放到 `3`，用于极限低成本预览

### 9. 噪声参数面板化

为了让程序化 3D 噪声可调，新增：

- `Assets/L13 VolumeCloud/Settings/L13CloudNoiseSettings.asset`
- `Assets/L13 VolumeCloud/Scripts/L13CloudNoiseSettings.cs`
- `Assets/L13 VolumeCloud/Editor/L13CloudNoiseSettingsEditor.cs`

生成器 `L13VolumeCloudDemoBuilder.cs` 改为读取 settings asset 生成贴图。

面板策略：

- 默认手动点击生成，避免拖动滑条时每帧重算 3D Texture。
- 可选自动延迟生成。
- 因 Unity 2022.3 的 `Texture3D` 没有 `Reinitialize`，尺寸变化时重建对应 Texture3D asset。
- 当前 Inspector 已改成中文精简面板，只显示主要形象参数，隐藏 seed、octaves、A/B/C 权重等研发细节。

当前主要面板参数：

- 主体精度
- 细节精度
- 分布图精度
- 大云团数量
- 块状边缘
- 柔和云量
- 块状云量
- 云体饱满度
- 棉絮起伏
- 细节颗粒大小
- 边缘破碎强度
- 云区大小
- 云区破碎度
- 覆盖范围
- 密度范围
- 细节侵蚀范围

### 10. 盒体缩放与噪声拉伸解耦

之前 Shader 使用盒体局部坐标 `pOS + 0.5` 直接采样 Shape/Detail/Weather 噪声，所以调整 `Raymarched Volume Cloud Box` 的 Transform Scale 时，云形会跟随盒体被压扁或拉长。

当前改为：

- Transform Scale 只决定 Ray-Box Intersection 的体积边界。
- Shader 内新增 `_NoiseWorldSize`，作为独立的噪声采样世界尺寸。
- `L13VolumeCloudController.noiseWorldSize` 每次渲染前通过 `MaterialPropertyBlock` 推送到 Shader。
- 默认 `Noise World Size = (240, 76, 160)`，对应构建器生成的基准云盒尺寸。
- 放大云盒时会显示更大范围的云场；缩小云盒时只裁剪云场范围，云团颗粒尺度保持一致。

## 当前验证记录

旧线程中多次通过：

- `dotnet build Assembly-CSharp.csproj`：0 error
- `dotnet build Assembly-CSharp-Editor.csproj`：0 error
- Unity Console Error：0

最近一次文档整理前的编辑器侧验证：

- `L13CloudNoiseSettingsEditor.cs`：UnitySkills `script_get_compile_feedback` 0 error
- Unity Console Error：0

## 已知设计取舍

- 体积云仍是透明体积盒 Demo，不是完整 URP RendererFeature 体积云 Pass。
- 没有做半分辨率渲染、时间重投影、深度融合和云层阴影投射。
- 当前重点是技术展示和可讲述性，不追求生产级天气系统。
- 噪声贴图在 Editor 生成，运行时只采样贴图，不运行 CPU 噪声生成。
- 高分辨率 Game View 下体积云依然会受像素数显著影响。

## 后续待办

### 效果继续优化方向

- 云层轮廓更自然：优化 WeatherMap 覆盖和低频 Shape Noise。
- 云体透光更舒服：微调 Light Marching、相位函数和吸收参数。
- 银边强度更稳定：根据太阳方向和视角关系重新调范围。
- HUD 预设命名更演示化：例如 Soft Layer、Cumulus、Storm Shelf。
- 截图和录屏角度：固定中景、近景、逆光银边、风场运动四组镜头。

## 视频简介可用文案

基于 Unity URP 实现的光线步进体积云 Demo，使用 Ray Marching、Ray-Box Intersection、3D Texture 体积噪声采样、Tileable Value/Worley Noise、Perlin-Worley 混合噪声、FBM、Weather Map、Height Gradient、Detail Erosion、Beer-Lambert 吸收、Light Marching、Henyey-Greenstein 相位函数、单次散射、银边效果、粉末感透光、风场平流与透明混合，实现可调参数的实时体积云效果。
