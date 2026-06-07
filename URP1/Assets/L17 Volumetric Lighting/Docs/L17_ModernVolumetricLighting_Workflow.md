# L17 Modern Volumetric Lighting

## 目标
- 做一个现代 URP 室内窗光体积光 Demo，只保留房间整体、窗洞/窗框、主光、相机、后处理和少量室内接光物。
- 视觉重点是“阳光从窗户照进室内”的实时体积散射，不再使用手摆 cube 光束或廉价径向模糊 `god rays`。

## 算法
- RendererFeature：`L17FrustumVolumetricRendererFeature` 在 `BeforeRenderingPostProcessing` 执行。
- 低分辨率体积积分：先生成低分辨率 depth，再沿相机 Z 方向积分 transmittance / in-scattering。
- 阴影约束：积分点采样 URP 主光 shadow map，并结合 sun direction，让窗框和墙体自然切出光束。
- 散射模型：使用 Henyey-Greenstein 前向散射、指数高度衰减和程序化 3D value noise。
- 稳定性：使用 blue-noise jitter、temporal reprojection 和 depth rejection 降低步进噪声。
- 合成：低分辨率结果双边上采样到全屏，并在 Bloom / ACES Tonemapping 前合成。

## 场景组成
- `Assets/L17 Volumetric Lighting/L17.unity`
- `Assets/L17 Volumetric Lighting/Scripts/L17FrustumVolumetricRendererFeature.cs`
- `Assets/L17 Volumetric Lighting/Scripts/L17VolumetricLightingController.cs`
- `Assets/L17 Volumetric Lighting/Shaders/L17FrustumVolumetricLighting.shader`
- `Assets/L17 Volumetric Lighting/Shaders/L17TwoSidedInteriorLit.shader`
- `Assets/L17 Volumetric Lighting/Textures/L17_BlueNoise64.asset`
- `Assets/L17 Volumetric Lighting/Materials/{L17_RoomWall,L17_DustyFloor,L17_WindowFrame}.mat`
- `Assets/L17 Volumetric Lighting/Materials/L17_PostProcessProfile.asset`
- `Assets/L17 Volumetric Lighting/Editor/L17VolumetricLightingDemoBuilder.cs`

## 构建入口
- `Tools/Volumetric Lighting/Build L17 Modern Window Shafts Demo`

## 现阶段取舍
- 当前版本优先做“室内窗光柱”核心视觉，不额外扩展全局雾、体积云或多光源体积照明。
- 体积效果按相机屏幕低分辨率积分，性能稳定，重点展示现代 URP 自定义 RendererFeature 管线。
