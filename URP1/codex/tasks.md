# 当前任务（精简）

## 当前主线
- `Assets/L10.9 learnNPR/L10.9.unity` 已作为当前可演示 Demo 场景推进：角色展示、运行时相机漫游、近距离镜头消隐已跑通。
- `Assets/L11 NPR/L11.unity` 的 StarRail NPR 完整链路仍作为后续任务保留，暂不继续沿旧的一周冲刺计划推进。

## 已完成结论
- `Main Camera` 已挂 `Assets/Scripts/SimpleCameraController.cs`，运行后支持 WASD 移动、右键旋转视角、中键拖拽平移、滚轮前后推拉、Shift 加速、Q/E 升降。
- `Assets/L10.9 learnNPR/shader advance/Toonshader advanced2.shader` 已加入近距离屏幕抖动溶解，用于替代摄像机贴近角色时的 near clip 硬切。
- Nilou 材质 `Body 1.mat`、`Body 2.mat`、`Hair 1.mat`、`Face and face_eye.mat` 已启用近距离溶解并完成一版演示参数。
- 后续尝试的“整体同步/模型范围”重做方案效果不理想，已由用户 discard 回退；当前以最早这版 `Start/End` 屏幕抖动溶解为准。
- 当前验证：Play 模式贴近角色可见柔性颗粒消隐；Console Error 为 0；`L10.9` 场景未变脏。

## 当前改动落点
- `Assets/L10.9 learnNPR/L10.9.unity`
- `Assets/L10.9 learnNPR/shader advance/Toonshader advanced2.shader`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 1.mat`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 2.mat`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Hair 1.mat`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Face and face_eye.mat`

## 待办（只保留未完成）
- 录制/截图一组 L10.9 演示视角：中景、近景、贴近溶解触发前后。
- 视效果微调近距离溶解参数：`_NearDissolveStart`、`_NearDissolveEnd`、`_NearDissolvePatternScale`、边缘颜色/强度。
- 若要重新做“整体同步消散”，不要沿用已回退的 ModelRadius/LocalWeight/Sync 方案，需重新设计更稳定的角色级方案。
- 若继续 L11：先重新执行 `Tools/NPR/输出 L11 上下文报告`，再用 Frame Debugger 验证 `HSRForward1/2/3`、`HSRHair*`、`HSROutline` 是否命中。
- 若继续 StarRail 材质治理：先确认目标角色与材质策略，再决定是否切到 `CharBody/CharFace/CharHair`，不要沿用旧 Day 3 计划直接改。

## 维护规则
- 每次只记录“最新结论 + 未完成事项”，不要写长过程。
- 完成一个待办就勾选或删除过时项。
