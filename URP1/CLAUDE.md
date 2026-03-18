# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Unity URP (Universal Render Pipeline) shader learning project** containing multiple scenes (L1-L10) that demonstrate various rendering techniques. The project uses Unity 2022.3.62f3c1 with URP 14.0.12.

## Unity Development

### Opening the Project
- Open in Unity Editor 2022.3.62f3c1 or later
- The active scene is `L10` (in `Assets/L10 sss/L10.unity`)

### Key Unity Packages
- `com.unity.render-pipelines.universal` (14.0.12) - URP rendering
- `com.unity.cinemachine` (2.10.5) - Cinemachine camera system
- `com.unity.probuilder` (5.2.4) - In-editor procedural modeling
- `com.unity.ai.navigation` (1.1.7) - Navigation system
- `com.unity.cloud.gltfast` (6.14.1) - GLTF model loading
- `com.besty.unity-skills` (local path) - Custom Unity automation skills

### Unity Skills
This project has Unity Editor automation via the `unity-skills` skill. When Claude Code has this skill loaded, it can automate Unity Editor operations including:
- Scene management (create, open, save scenes)
- Game object manipulation
- Component management
- Material and shader operations
- Asset import/export
- Project settings

## Scene Architecture

Each lesson scene is in its own folder under `Assets/`:
- `L1消散/` - Bloom/dissipation shader effects
- `L2视差云/` - Parallax cloud rendering
- `L3屏幕空间溶解/` - Screen-space dissolve effects
- `L5/IgniteCoders/Simple Water Shader/` - Water shader with reflection
- `L6 CG/` - CG math (vector/matrix operations) and shader examples
- `L7 pbr/` - Physically Based Rendering materials
- `L8 plane reflect box projection/` - Planar reflections with box projection
- `L9 lightmap/` - Lightmapping demonstration
- `L10 sss/` - Subsurface scattering shader (current active scene)

## Shader Development

Custom shaders use URP's HLSL syntax with:
- `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"`
- `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"`
- `Attributes` and `Varyings` structs for vertex/fragment shader communication
- `GetVertexPositionInputs()`, `GetMainLight()`, `TransformObjectToWorldNormal()` helper functions

## Project Structure

```
Assets/
├── L10 sss/           # Current active scene (SSS shader)
├── L1消散/             # Bloom/dissipation
├── L2视差云/           # Parallax clouds
├── L3屏幕空间溶解/     # Screen-space dissolve
├── L5/                # Water shader
├── L6 CG/             # CG math & shaders
├── L7 pbr/            # PBR materials
├── L8 plane reflect/  # Planar reflections
├── L9 lightmap/       # Lightmapping
├── Settings/          # URP quality settings (UniversalRP-HighQuality.asset, etc.)
├── Scripts/           # General scripts (SimpleCameraController.cs)
├── animator/          # Animation resources
├── Skybox/            # Skybox/scene resources
└── TutorialInfo/      # Unity tutorial info scripts
```

## Important Notes

- Scene names contain Chinese characters indicating the technique being demonstrated
- Each scene folder is self-contained with its own shader and material assets
- URP settings are in `Assets/Settings/` with quality variants (High, Medium, Low)
- The project uses the .NET scripting backend (check Assembly-CSharp.csproj)
## TA Development Standards (Shader & C#)

### Shader Coding Standards (URP)
- **Naming Conventions**: Use `_BaseMap` instead of `_MainTex`, and `_BaseColor` instead of `_Color` to align with URP standards.
- **Includes**: Always include `"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"` for basic transforms.
- **Performance**: 
  - Prefer `half` precision for colors and simple interpolators.
  - Use `full` precision (float) only for world positions and UV calculations.
  - Avoid `pow()`, `exp()`, and `sin()` in inner loops if a linear approximation suffices.
- **SRP Batcher**: Ensure all material properties are wrapped in a `CBUFFER_START(UnityPerMaterial)` block for SRP Batcher compatibility.

### C# & Rendering Scripts
- **Optimization**: Use `Shader.PropertyToID()` for all shader property access instead of string names.
- **Lifecycle**: Avoid `GameObject.Find` or `GetComponent` in `Update()`. Cache references in `Awake()` or `Start()`.
- **Naming**: Use `PascalCase` for public methods and `_camelCase` for private fields.

### Math & Physics
- Always use `quaternion` math over `Euler angles` when manipulating transforms via script.
- Standardize on `Unity.Mathematics` (float3, float4) where performance is critical.