# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个 **Unity URP (Universal Render Pipeline) 着色器学习项目**，包含多个课程场景 (L1-L11)，演示各种渲染技术。项目使用 Unity 2022.3.62f3c1 和 URP 14.0.12。

## 项目开发

### 打开项目
- 使用 Unity Editor 2022.3.62f3c1 或更高版本打开
- 当前激活场景是 `L10`（位于 `Assets/L10 sss/L10.unity`）

### 主要 Unity 包

| 包名 | 版本 | 用途 |
|------|------|------|
| `com.unity.render-pipelines.universal` | 14.0.12 | URP 渲染管线核心 |
| `com.unity.burst` | 1.8.28 | Burst 编译器（性能优化） |
| `com.unity.cinemachine` | 2.10.5 | Cinemachine 相机系统 |
| `com.unity.probuilder` | 5.2.4 | 编辑器内程序化建模 |
| `com.unity.ai.navigation` | 1.1.7 | 导航系统 |
| `com.unity.cloud.gltfast` | 6.14.1 | GLTF 模型加载 |
| `com.unity.textmeshpro` | 3.0.9 | 文本渲染 |
| `com.unity.timeline` | 1.7.7 | 时间线动画 |

### 本地包（项目特有）

| 包名 | 路径 | 用途 |
|------|------|------|
| `com.besty.unity-skills` | `C:/Users/fukeh/Downloads/Unity-Skills-main/...` | Unity 自动化技能 |
| `com.stalomeow.star-rail-npr-shader` | `C:/Users/fukeh/Downloads/StarRailNPRShader-main/...` | 星空铁轨 NPR 着色器参考 |
| `com.merry-yellow.code-assist` | `com.merry-yellow.code-assist` | 代码辅助 |

### Unity Skills 自动化

本项目配置了 `unity-skills` 技能，可自动化以下 Unity Editor 操作：
- 场景管理（创建、打开、保存场景）
- 游戏对象操作
- 组件管理
- 材质和着色器操作
- 资源导入/导出
- 项目设置

## 场景架构

每个课程场景在 `Assets/` 下都有独立文件夹：

| 场景 | 着色器技术 |
|------|-----------|
| `L1消散/` | Bloom/消散着色器效果 |
| `L2视差云/` | 视差云渲染 |
| `L3屏幕空间溶解/` | 屏幕空间溶解效果 |
| `L5/IgniteCoders/Simple Water Shader/` | 水面着色器（带反射） |
| `L6 CG/` | CG 数学（向量/矩阵运算）和着色器示例 |
| `L7 pbr/` | 物理渲染（PBR）材质 |
| `L8 plane reflect box projection/` | 平面反射（盒投影） |
| `L9 lightmap/` | 光照贴图演示 |
| `L10 sss/` | 次表面散射着色器（当前活跃场景） |
| `L11 NPR/` | NPR 着色器 |

## 着色器开发

自定义着色器使用 URP HLSL 语法：

### 必需的 Include
```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
```

### 核心结构和函数
- `Attributes` 和 `Varyings` 结构体（顶点/片元着色器通信）
- `GetVertexPositionInputs()` - 获取顶点位置
- `GetMainLight()` - 获取主光源
- `TransformObjectToWorldNormal()` - 世界空间法线变换

## 项目结构

```
Assets/
├── L1消散/             # 消散/Bloom
├── L2视差云/           # 视差云
├── L3屏幕空间溶解/     # 屏幕溶解
├── L5/IgniteCoders/   # 水面着色器
├── L6 CG/             # CG 数学和着色器
├── L7 pbr/            # PBR 材质
├── L8 plane reflect/  # 平面反射
├── L9 lightmap/       # 光照贴图
├── L10 sss/           # SSS 着色器（当前场景）
├── L11 NPR/           # NPR 着色器
├── Settings/          # URP 质量设置
│   ├── UniversalRP-HighQuality.asset
│   ├── UniversalRP-MediumQuality.asset
│   ├── UniversalRP-LowQuality.asset
│   └── ForwardRenderer.asset
├── Scripts/           # 通用脚本
│   └── SimpleCameraController.cs
├── Skybox/            # 天空盒/场景资源
├── Screenshots/       # 截图
└── URPSimpleGenshinShaders-master/  # 简化版原神风格着色器参考
```

## URP 配置

- URP 配置文件位于 `Assets/Settings/`
- 支持三种质量等级：High、Medium、Low
- ForwardRenderer 配置管理前向渲染通道

## 重要说明

- 场景名称包含中文，表示演示的技术类型
- 每个场景文件夹都是自包含的，拥有自己的着色器和材质资源
- URP 设置在 `Assets/Settings/` 目录下有质量变体
- 项目使用 .NET 脚本后端
- 场景 L4 未使用（编号跳过）

## currentDate
Today's date is 2026-03-29.

## TA 开发标准（着色器 & C#）

### 着色器编码标准 (URP)

- **命名规范**：使用 `_BaseMap` 而非 `_MainTex`，使用 `_BaseColor` 而非 `_Color`（对齐 URP 标准）
- **Include**：始终包含 `"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"` 用于基础变换
- **性能优化**：
  - 颜色和简单插值器使用 `half` 精度
  - 世界位置和 UV 计算使用 `full` 精度 (float)
  - 内部循环中避免 `pow()`、`exp()` 和 `sin()`，尽量使用线性近似
- **SRP Batcher**：将所有材质属性包装在 `CBUFFER_START(UnityPerMaterial)` 块中以兼容 SRP Batcher

### C# & 渲染脚本

- **优化**：使用 `Shader.PropertyToID()` 进行所有着色器属性访问，而非字符串名称
- **生命周期**：避免在 `Update()` 中使用 `GameObject.Find` 或 `GetComponent`，在 `Awake()` 或 `Start()` 中缓存引用
- **命名**：公共方法使用 `PascalCase`，私有字段使用 `_camelCase`

### 数学与物理

- 通过脚本操作变换时，始终使用 `quaternion` 数学而非欧拉角
- 性能关键处统一使用 `Unity.Mathematics`（float3、float4）
