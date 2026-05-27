# L16 Rain Only Workflow

## Build

Run `Tools/Rain/Build L16 Advanced Rain Demo`.

The builder creates `Assets/L16 Rain/L16.unity`, enables URP depth/opaque textures, and generates only the minimal materials needed to inspect GPU rain streaks.

## Runtime Systems

- `L16RainManager`: compute-populated rain streak buffer rendered with `DrawMeshInstancedIndirect`.
- Scene contents are intentionally minimal: a plain ground plane, one plain backdrop, one directional light, camera, rain volume, and HUD.

## Quality

- Low: 7k rain streaks.
- Medium: 16k rain streaks, default editor preview.
- High: 32k rain streaks.

HUD controls rain intensity, wind, and quality preset.

## Validation

Use Unity automatic compile, console error check, scene health check, and a short Play Mode run. Capture the current preview to `Assets/Screenshots/L16_AdvancedRain_current_YYYYMMDD.png`.
