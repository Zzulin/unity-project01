# 当前任务（精简）

## 主任务
- 围绕 `Assets/L11 NPR/L11.unity` 完成 StarRail NPR 角色渲染联调。

## 当前状态
- 场景：`L11`（角色观察场景）
- 管线：Graphics 默认 `NPR Render Pipeline.asset`；当前质量档 `High` 为 `UniversalRP-HighQuality.asset`
- 材质：`0_mesh_mesh` 多数槽位为 URP Lit 内嵌实例，`hair` 使用 `CharHair`
- 工具：可用 `Tools/NPR/输出 L11 上下文报告` 快速核对链路

## 待办（只保留未完成）
- [ ] 用 Frame Debugger/RenderDoc 确认当前链路是否真实命中 Forward+
- [ ] 决定材质治理方案：
- [ ] 方案 A：继续使用场景内嵌材质并逐槽位切换到 `CharBody/CharFace/CharHair`
- [ ] 方案 B：抽离为独立 `.mat` 资产后批量回挂
- [ ] 材质治理完成后，再进入 `CharBody.shader` / `CharBodyCore.hlsl` 视觉参数联调

## 维护规则
- 每次只记录“最新结论 + 未完成事项”，不要写长过程。
- 完成一个待办就勾选，并删除过时信息。
