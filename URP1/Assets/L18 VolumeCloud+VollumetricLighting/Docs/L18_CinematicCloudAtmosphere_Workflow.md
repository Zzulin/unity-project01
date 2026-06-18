# L18 电影化体积云与体积光

## 当前目标

L18 将 L13 的体积云密度思路与 L17 的 URP RendererFeature、半分辨率积分和深度感知合成方式整合为一个室外天气 Demo，画面方向参考《荒野大镖客 2》的厚云、云隙光和山谷空气透视。

## 渲染链

```text
Camera color / depth
        ↓
半分辨率 depth
        ↓
统一积分高空云层与低空参与介质
        ↓
云层自透射 + 云影调制山谷雾
        ↓
深度感知 3x3 降噪与上采样
        ↓
Bloom / ACES 前合成
```

`L18AtmosphereRendererFeature` 仅在当前场景存在启用的 `L18AtmosphereController` 时执行，因此可以与 L17 RendererFeature 同时保留在 Renderer Data 中。

## 云层

- 复用 L13 的 `ShapeNoise3D`、`DetailNoise3D` 和 `WeatherMap`。
- 使用世界空间高空云层，不再绘制透明 Cube。
- Shape Noise 控制主体，Detail Noise 控制边缘侵蚀，WeatherMap 控制覆盖和局部密度。
- 保留短距离太阳方向采样、HG 相位、暗云颜色和云边亮度。
- 云层覆盖率直接乘入云密度；`Cloud Coverage = 0` 或 `Cloud Density = 0` 时云密度严格为 0。

## 体积光

- 低空介质由全局薄雾与高度衰减山谷雾组成。
- 每个采样点沿太阳方向查询云层透射率。
- 场景主光阴影和云层遮光共同调制散射。
- 低空散射只读取当前帧云密度计算得到的太阳透射率，不再包含固定坐标云洞或独立艺术光柱。
- 云层移动、覆盖率和密度变化会实时改变光束；云层消失后局部光柱同步消失。

## Scene View

- RendererFeature 同时支持 Game Camera 和 Unity 默认 SceneView Camera。
- SceneView 使用自身位置、旋转、深度和视锥执行体积积分，可正常自由移动。
- 不再通过 Editor update 强制 SceneView 跟随 Main Camera。

## 主要资源

- 场景：`Assets/L18 VolumeCloud+VollumetricLighting/L18.unity`
- RendererFeature：`Scripts/L18AtmosphereRendererFeature.cs`
- 参数入口：`Scripts/L18AtmosphereController.cs`
- 天气预设：`Scripts/L18WeatherProfile.cs`
- Shader：`Shaders/L18CinematicCloudAtmosphere.shader`
- 构建器：`Editor/L18AtmosphereDemoBuilder.cs`
- 预览：`Assets/Screenshots/L18_RDR2_cloudbreak_current_20260618.png`

## 构建与操作

- 构建菜单：`Tools/L18/Build RDR2 Cloud Break Demo`
- Play Mode：
  - `WASD` 移动
  - 右键旋转
  - `Shift` 加速
  - `Q/E` 垂直移动

## 当前质量档

- 半分辨率体积 Buffer
- 32 次视线采样
- 4 次云层光照采样
- 最大积分距离 1150m
- 3x3 深度感知低分辨率过滤和上采样
- Blue Noise 步进抖动

当前版本尚未加入 L17 式 temporal history；后续如继续提高采样稳定性，应优先增加每 Camera history、重投影和天气变化时的 history reset，而不是继续提高默认步数。
