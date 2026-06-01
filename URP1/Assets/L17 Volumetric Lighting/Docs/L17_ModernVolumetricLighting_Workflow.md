# L17 Modern Volumetric Lighting

## 目标
- 做一个室内窗光束 Demo，只保留房间、窗洞、主光、体积介质代理体和一个接光物。
- 视觉重点是“阳光从窗户照进室内”的高级实时体积光，不做廉价径向模糊假 `god rays`。

## 算法
- 代理体积：在窗前到室内中段放一个局部参与介质 Box，而不是全屏后处理。
- 光照求解：在 Box 内做单次散射 ray march。
- 阴影约束：每个步进点都采样 URP 主光阴影图，让墙体 / 窗框直接决定哪些体积区域被照亮。
- 相位函数：使用 Henyey-Greenstein 前向散射，让逆光方向更容易形成可见光束。
- 体积细节：叠加三层程序化 3D value noise，做微弱飘尘和介质不均匀感。
- 稳定性：使用屏幕空间 interleaved gradient noise 做步进起点抖动，降低层带感。

## 场景组成
- `Assets/L17 Volumetric Lighting/L17.unity`
- `Assets/L17 Volumetric Lighting/Shaders/L17WindowVolumetricBeam.shader`
- `Assets/L17 Volumetric Lighting/Materials/L17_WindowBeam.mat`
- `Assets/L17 Volumetric Lighting/Materials/{L17_RoomWall,L17_DustyFloor,L17_WindowFrame}.mat`
- `Assets/L17 Volumetric Lighting/Editor/L17VolumetricLightingDemoBuilder.cs`

## 构建入口
- `Tools/Volumetric Lighting/Build L17 Modern Window Shafts Demo`

## 现阶段取舍
- 当前版本优先做“室内窗光柱”核心视觉，不额外扩展全局雾、体积云或通用 Renderer Feature。
- 体积代理盒只放在窗光有效区域内，控制成本，也更贴合面试 Demo 的镜头组织。
