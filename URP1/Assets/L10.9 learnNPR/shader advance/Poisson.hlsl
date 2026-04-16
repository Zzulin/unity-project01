#ifndef TOON_SHADING_POISSON_INCLUDED
#define TOON_SHADING_POISSON_INCLUDED

// The screenshots include a local Poisson.hlsl and call get_main_light_poisson.
// The body was not visible in the canvas, so this wrapper keeps the shader
// self-contained while preserving the call site from the captured source.
Light get_main_light_poisson(float4 shadowCoord, float3 positionWS)
{
    Light mainLight = GetMainLight(shadowCoord);
    return mainLight;
}

#endif
