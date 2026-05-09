# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

Unity URP 着色器学习项目，涵盖多个渲染技术 Demo 场景。项目使用 Unity 2022.3.62f3c1 和 URP 14.0.12。

## 编译验证

```bash
dotnet build Assembly-CSharp.csproj
dotnet build Assembly-CSharp-Editor.csproj
```
两个项目均需 0 error 才能算通过。

## 场景架构

每个课程场景在 `Assets/` 下都有独立文件夹，自包含着色器、材质和脚本。L12/L13/L14 等复杂场景还包含 `Editor/`（构建器菜单）和 `Docs/`（制作流程文档）：

每个复杂场景遵循统一结构：
- `Editor/*Builder.cs` — 提供 `Tools/` 菜单一键重建场景、材质、灯光和默认参数
- `Docs/*_Workflow.md` — 记录制作流程、当前状态、设计取舍和待办

| 场景 | 技术 | 备注 |
|------|------|------|
| `L1消散/` | Bloom/消散 | |
| `L2视差云/` | 视差云 | |
| `L3屏幕空间溶解/` | 屏幕空间溶解 | |
| `L5/IgniteCoders/` | 水面着色器（反射） | |
| `L6 CG/` | CG 数学，向量/矩阵运算 | |
| `L7 pbr/` | PBR 材质 | |
| `L8 plane reflect/` | 平面反射（盒投影） | |
| `L9 lightmap/` | 光照贴图 | |
| `L10 sss/` | 次表面散射 | |
| `L10.9 learnNPR/` | NPR 角色渲染 + 屏幕溶解 | 含多个角色模型（妮露/阮梅/刻莱诺/荧），shader/shader advance 两个着色器变体 |
| `L11 NPR/` | StarRail NPR 完整链路 | 当前主线目标场景 |
| `L12 grass/` | GPU Instancing 草地 | 含风场/交互弯折，构建菜单 `Tools/Grass/` |
| `L13 VolumeCloud/` | 光线步进体积云 | 含 3D 噪声纹理/相位函数/银边，构建菜单 `Tools/Volume Cloud/` |
| `L14 Snow/` | 交互雪地 | Compute Shader 写入高度/堆雪状态图，顶点位移，构建菜单 `Tools/Snow/` |
| `LX learn computeShader/` | Compute Shader 基础 | ComputeBuffer 示例 |

场景 L4 未使用（编号跳过）。

## URP 配置

- 配置文件位于 `Assets/Settings/`
- 默认 Graphics RP：`NPR Render Pipeline.asset`
- 质量档 High → `UniversalRP-HighQuality.asset`，另有 Medium/Low 变体
- `ForwardRenderer.asset` 管理前向渲染通道
- `NPR Render Pipeline Asset_Renderer.asset` 供 NPR 管线使用

### 主要 Unity 包

| 包名 | 版本 |
|------|------|
| `com.unity.render-pipelines.universal` | 14.0.12 |
| `com.unity.burst` | 1.8.28 |
| `com.unity.cinemachine` | 2.10.5 |
| `com.unity.probuilder` | 5.2.4 |
| `com.unity.cloud.gltfast` | 6.14.1 |
| `com.unity.textmeshpro` | 3.0.9 |

### 本地包

| 路径 | 用途 |
|------|------|
| `Plugins/StarRailNPRShader-main/` | StarRail NPR 着色器参考（GPL-3.0） |
| `Assets/URPSimpleGenshinShaders-master/` | 原神风格着色器参考 |
| `Assets/UnityURPToonLitShaderExample-master/` | URP Toon Lit 示例（MIT） |
| `Plugins/LWGUI-main/` | Shader GUI 插件（材质面板 Attribute 驱动） |

## 着色器开发

自定义着色器使用 URP HLSL 语法：

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
```

核心函数：`GetVertexPositionInputs()`、`GetMainLight()`、`TransformObjectToWorldNormal()`

## TA 开发标准（着色器 & C#）

### 着色器编码标准
- 命名对齐 URP：`_BaseMap` 而非 `_MainTex`，`_BaseColor` 而非 `_Color`
- 所有材质属性放在 `CBUFFER_START(UnityPerMaterial)` 块中以兼容 SRP Batcher
- 精度：颜色和插值器用 `half`，世界位置和 UV 用 `float`
- 循环内避免 `pow()`、`exp()`、`sin()`，尽量用线性近似

### C# & 渲染脚本
- 着色器属性访问使用 `Shader.PropertyToID()`，不用字符串
- `Update()` 中不用 `GameObject.Find` 或 `GetComponent`，在 `Awake()`/`Start()` 缓存
- 命名：公共 PascalCase，私有字段 `_camelCase`
- 变换操作用 `quaternion`，避免欧拉角
- 性能关键处用 `Unity.Mathematics`（float3、float4）

## 协作约定

- 默认中文输出
- 任务进度同步到 `codex/tasks.md`，保持简洁
- 引用源码行号以 `rg -n` 或 Rider 显示为准
- 复杂场景的制作流程文档在 `Assets/L*/Docs/*.md`，记录当前状态、设计取舍和待办

## 着色器文件命名

- 场景着色器：`L{N}{DescriptiveName}.shader`（如 `L13RaymarchedVolumeCloud.shader`）
- Compute Shader：`L{N}{DescriptiveName}.compute`（如 `L12GrassCull.compute`、`L14SnowSim.compute`）
- 噪声设置资产：`Assets/L*/Settings/L*{Name}Settings.asset`
