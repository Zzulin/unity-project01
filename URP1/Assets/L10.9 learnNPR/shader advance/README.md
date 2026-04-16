# 源码.canvas 转写说明

此目录从 `D:\GitHub\obsidian-note\untiy render\源码.canvas` 中的图片节点转写得到。

- `ToonShading.shader`：按截图还原的 ShaderLab/HLSL 主体。
- `Poisson.hlsl`：截图里只看到 include 和 `get_main_light_poisson` 调用，未看到完整实现；这里补了一个保守的 URP `GetMainLight` 包装，方便文件自包含。

注意：

- Canvas 截图覆盖了大部分主 pass、描边 pass 和若干替代函数，但缺少一段完整的 `Outline`/`ShadowCaster` 中间源码，所以这两处按截图能确认的内容和 Unity URP 常见写法补齐。
- 截图第 366 行有调试返回 `return float4(rampColor, 1);`，转写文件中保留为注释，避免直接截断后续高光、边缘光和自发光计算。
