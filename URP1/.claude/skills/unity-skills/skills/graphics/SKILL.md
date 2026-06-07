---
name: unity-graphics
description: "Project-wide GraphicsSettings / QualitySettings / Always Included Shaders / shader stripping for SRP-era Unity projects. Use when users want to inspect graphics & quality overview, switch quality level, assign or clear the default / per-quality SRP asset, manage Always Included Shaders, or configure lightmap / fog / instancing shader stripping. Triggers (EN): Unity graphics settings, GraphicsSettings, QualitySettings, quality level, default render pipeline, SRP asset, Always Included Shaders, shader stripping, lightmap stripping, fog stripping, instancing stripping. Triggers (ZH): 图形设置, 渲染管线设置, 质量设置, 质量等级, 默认渲染管线, SRP 资源, 始终包含的着色器, Shader 剥离, 总在包含的着色器."
---

# Graphics Skills

Project-wide graphics and quality settings (GraphicsSettings + QualitySettings) for Unity 2022.3+. Works in Built-in, URP and HDRP — does not depend on any SRP package being installed.

## Operating Mode

- Query skills (`graphics_get_overview`, `graphics_get_quality_settings`, `graphics_get_render_pipeline_assets`, `graphics_list_always_included_shaders`, `graphics_get_shader_stripping`) are `SkillMode.SemiAuto` — they run in all three modes without grant.
- Mutating skills (`graphics_set_quality_level`, `graphics_set_default_render_pipeline`, `graphics_set_quality_render_pipeline`, `graphics_add_always_included_shader`, `graphics_remove_always_included_shader`, `graphics_set_shader_stripping`) are `SkillMode.FullAuto` — under **Approval** they need user grant (grant triggers one server-side execute returning the result); under **Auto** / **Bypass** they execute directly.
- This module contains **no** Delete / PlayMode / Reload / high-risk skills (no NeverInSemi). `graphics_remove_always_included_shader` is a list mutation, not a `SkillOperation.Delete`.

## Guardrails

**Routing**:
- For current render pipeline detection only: `project_get_render_pipeline`
- For SRP/quality configuration: use this module, not `project_*`

## Skills

### `graphics_get_overview`
Get a graphics/quality/render-pipeline summary.

### `graphics_get_quality_settings`
List quality levels and their render pipeline overrides.

### `graphics_set_quality_level`
Switch the active quality level.

### `graphics_get_render_pipeline_assets`
List default/current/per-quality render pipeline assets.

### `graphics_set_default_render_pipeline`
Set or clear the default SRP asset.

### `graphics_set_quality_render_pipeline`
Assign or clear the SRP asset for a specific quality level.

### `graphics_list_always_included_shaders`
List shaders in Always Included Shaders.

### `graphics_add_always_included_shader`
Add a shader to Always Included Shaders.

### `graphics_remove_always_included_shader`
Remove a shader from Always Included Shaders.

### `graphics_get_shader_stripping`
Inspect shader stripping configuration in GraphicsSettings.

### `graphics_set_shader_stripping`
Modify shader stripping configuration in GraphicsSettings.

---
## Exact Signatures

Exact names, parameters, defaults, and returns are defined by `GET /skills/schema` or `unity_skills.get_skill_schema()`, not by this file.
