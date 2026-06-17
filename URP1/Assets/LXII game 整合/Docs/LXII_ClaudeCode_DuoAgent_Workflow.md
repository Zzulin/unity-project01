# LXII Claude Code Duo-Agent 工作流

## 1. 文档用途

这份文档只服务一件事：

把 `Assets/LXII game 整合/game.unity` 推成一个可讲、可跑、可验证的整合 Demo。

目标不是写一份“理想方案”，而是让单 CLI 的 Claude Code 工作流在当前仓库状态下可以持续推进，不跑偏、不回到 L11、不过早做大重构。

## 2. 当前仓库快照

以当前仓库内容为准，LXII 目录当前状态如下：

- `Assets/LXII game 整合/` 当前已经有：
  - `game.unity`
  - `Docs/`
  - `Editor/`
  - `Scripts/`
  - `Settings/`
- 当前已存在的 LXII 适配入口包括：
  - `Tools/LXII/Setup Nilou Humanoid In Game Scene`
  - `Tools/LXII/Setup LXI Animation Test In Game Scene`
  - `Tools/LXII/Setup L12 Grass In Game Scene`
  - `Tools/LXII/Setup L13 VolumeCloud In Game Scene`
- `game.unity` 已不再是纯空壳，当前已落地：
  - `LXII Nilou Player`
  - `LXII L12 Grass Root`
  - `LXII Grass Ground`
  - `LXII Grass Field`
  - `LXII L13 VolumeCloud Root`
  - `LXII Sky Volume Cloud`
- 当前角色链路已具备：
  - 妮露 Humanoid Avatar
  - LXI Idle / Run / Action 测试控制器
  - 拆职责第三人称角色控制链
  - 第三人称跟随摄像机
- 当前第三人称控制链已经从单脚本测试驱动拆为：
  - `LXIIPlayerInputReader`
  - `LXIIPlayerMotor`
  - `LXIIPlayerAnimationDriver`
  - `LXIIPlayerController`
- 当前 Inspector 入口已收敛：
  - `LXIIPlayerController` 作为主要可调组件
  - 输入、移动、动画三个内部组件默认隐藏
  - 默认操作为 `WASD` 移动、`Left Shift` 加速、`3` 触发 Action
- 当前 Avatar / 动作主干观察结论：
  - 没有明显扭胯、塌肩、脚尖异常、头发链违和
  - 仍可能存在局部穿模
  - 衣物和头发物理模拟尚未接入
- 当前 L13 体积云接入结论：
  - 使用 LXII 专用材质实例，不直接改 L13 原始 Demo 材质
  - 云盒覆盖天空区域，当前为 1600m 级天空体积盒
  - `Noise World Size` 独立保持云纹理世界尺度，当前为 `{x: 920, y: 260, z: 920}`
  - 默认性能档为 `10 view steps / 0 light steps`
  - 云盒不参与碰撞、阴影或 motion vector
  - 不保留远景云幕或椭球云团占位；天空只保留 `LXII Sky Volume Cloud` 一个体积云对象
- 当前 L12 草地接入结论：
  - `LXII Grass Ground` 已从平面换成 960m 起伏地表 mesh
  - `LXII Grass Field` 已扩展到 420m 贴坡草海
  - `targetBladeSpacing` 会被 `maxBladesPerAxis` 保护上限截断；当前 Inspector 范围为 `0.02-1.0`，LXII 默认 `targetBladeSpacing=0.4`、`maxBladesPerAxis=2048`
  - L12 草 shader / culling compute 通过可选高度参数贴合坡面，默认高度为 0 时不改变原 L12 Demo
  - L12 草 shader `Properties` 默认值已同步到当前 LXII 草地观感参数；`L12_InteractiveGrass.mat` 对应覆盖值也同步
  - 当前方案是静态大世界预览，不是真正 streaming 大世界

这意味着：

- LXII 当前重点已经从“先把整合层真正搭起来”转成“保持现有控制链稳定，并把 L14 雪地串入同一路径，同时确认大世界预览的观感与性能边界”。
- `codex/tasks.md` 中关于 LXII 的一些条目可能代表目标状态或待复核状态；是否已真实落地，必须以仓库文件、Unity Scene、Importer 状态和验证结果为准。

## 3. 硬约束

### 3.1 禁止使用

- `Assets/L11 NPR/**`
- `Plugins/StarRailNPRShader-main/**`
- StarRail RendererFeature / CharBody / CharFace / CharHair 链路
- `Tools/NPR/输出 L11 上下文报告`
- 任何 L11 材质治理、Frame Debugger 验证或 HSR pass 检查

### 3.2 固定使用

- 玩家模型：`Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx`
- 玩家材质：
  - `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 1.mat`
  - `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 2.mat`
  - `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Hair 1.mat`
  - `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Face and face_eye.mat`
- 动作来源：`Assets/LXI 动作测试/**`
- 场景系统来源：
  - L12 草地
  - L13 体积云
  - L14 雪地

### 3.3 总原则

- 优先新增 LXII 适配层，不直接破坏 L12/L13/L14 原始 Demo。
- 除非当前轮明确需要，不改 `ProjectSettings/**`、`Packages/manifest.json`。
- 资源路径不确定时先搜索确认，不猜。
- Unity 资源文件一次只改一类问题，不在同一轮同时推进 Avatar、Scene、材质和玩法。

## 4. 最终要做成什么

LXII 的第一版胜利条件：

- `Assets/LXII game 整合/game.unity` 能进入 Play Mode。
- 玩家是 L10.9 妮露，不是 Capsule、HumanM/F 或其他占位体。
- 妮露保留 L10.9 Toon/NPR 材质。
- 妮露能播放至少 3 类 LXI Humanoid 动作：
  - Idle
  - Move
  - Action
- 场景内至少有：
  - 1 个可见起伏草地区，能响应交互
  - 1 个可见雪地区，能响应交互
  - 1 组正常显示的体积云
- 验证链条完整：
  - Unity Editor 自动编译完成
  - `debug_check_compilation` 返回 `isCompiling=false`、`isUpdating=false`
  - Unity Console Error 为 0
  - 关键场景改动后能打开并保存 `game.unity`
  - 玩法链路改动后做必要 Play Mode 短测

不做的事：

- 完整战斗系统
- 任务系统
- 背包
- 联网
- 全角色换装
- 任何 L11 / StarRail 渲染链路

## 5. 当前最高优先级

当前最高优先级已经不再是“先证明妮露能接收 LXI 动作”，因为这条链已经基本打通。

当前更高优先级的是：

- 保持当前 Avatar / 动作主干结论稳定
- 保持拆职责第三人称角色控制链稳定，不回退到单脚本测试驱动
- 保持 `LXIIPlayerController` 作为主 Inspector 入口，内部组件只做自动绑定和职责拆分
- 处理最明显的局部穿模
- 把 L14 雪地接入 LXII 主路径
- 复核大世界预览在 Play Mode 下的草地延伸、天空覆盖、遮挡关系和性能快照

当前不作为内建开发项的内容：

- 衣物物理模拟
- 头发物理模拟

原因：

- 这两项更适合依赖 Unity 侧现成插件或成熟方案接入
- 当前仓库没有确认好的统一物理插件基线
- 在没有插件基线的前提下，不应在 LXII 整合阶段手写一套临时物理替代品

## 6. 当前目录约定

LXII 当前已经落地的目录按下面这条线维护：

```text
Assets/LXII game 整合/
  game.unity
  Docs/
    LXII_ClaudeCode_DuoAgent_Workflow.md
  Editor/
    LXIINilouHumanoidSetup.cs
    LXIIAnimationTestSetup.cs
    LXIIL12GrassSetup.cs
    LXIIL13VolumeCloudSetup.cs
  Materials/
    LXII_L13_RaymarchedCloud_Performance.mat
  Meshes/
    LXII_OpenWorldRollingGrassGround.asset
  Scripts/
    Player/
      LXIIPlayerInputReader.cs
      LXIIPlayerMotor.cs
      LXIIPlayerAnimationDriver.cs
      LXIIPlayerController.cs
    Camera/
      LXIIThirdPersonCameraFollow.cs
    Animation/
      LXIIAnimationTestDriver.cs
  Settings/
    LXII_Nilou_LXI_Test.controller
```

目录规则：

- `Editor/*Setup.cs` 是当前阶段的可重建入口；后续如要统一 Builder，再单独收敛，不在接 L14 / L13 时顺手重构。
- `Scripts/*` 只放 LXII 整合层，不复制 L12/L13/L14 现有脚本。
- `Scripts/Animation/LXIIAnimationTestDriver.cs` 是历史测试脚本，当前玩家对象应优先使用 `Scripts/Player/*` 控制链。
- 如果必须修改外部系统脚本，先确认不会破坏原 Demo，再单独验证原场景。

## 7. Duo-Agent 模式

当前采用单 CLI 的“虚拟双 Agent”轮班模式。

### Agent A：Director / Integrator

职责：

- 保持总目标、阶段顺序、风险边界。
- 先侦察，再拆任务，再验收。
- 负责：
  - `Assets/LXII game 整合/Docs/*`
  - `Assets/LXII game 整合/Editor/*`
  - 场景装配
  - 最终验证
  - `codex/tasks.md` 最新结论同步

Agent A 禁忌：

- 没有确认 Avatar 前就推进玩法包装。
- 同时大改 L12/L13/L14 原始 Demo。
- 为了“架构整洁”提前引入大框架。

### Agent B：Builder / Specialist

职责：

- 一次只实现一个明确切片。
- 常见切片：
  - 妮露 Avatar / Importer
  - LXI 动作筛选与 Animator
  - 玩家控制
  - 草雪交互适配
  - 体积云接入
  - 材质绑定
  - 调试与验证工具

Agent B 禁忌：

- 不跨目录顺手重构。
- 不抢总设计权。
- 不用“看起来像对的”资源路径。

## 8. 阶段路线

### Phase 0：基线侦察

目标：

- 明确 LXII 当前真实落地状态。
- 输出后续工作依赖清单。

必须产出：

- LXII 目录清单
- `game.unity` 当前根对象与挂载脚本
- 妮露 FBX rig/importer 状态
- 妮露材质槽位清单
- LXI 可用 Humanoid 动作清单
- L12/L13/L14 可直接复用的入口组件清单

完成标准：

- 不写新功能，只做事实确认。
- 文档和 `codex/tasks.md` 中对 LXII 的描述与仓库事实不冲突。

### Phase 1：LXII 场景骨架

目标：

- 建立 `Tools/LXII/Build Integrated Game Demo`。
- 让 `game.unity` 从“空壳”变成“可重建基础场景”。

至少包含：

- Main Camera / 调试相机
- Directional Light
- 地面或分区基准
- 草地区容器
- 雪地区容器
- 云体积盒容器
- 玩家出生点

完成标准：

- 菜单可重建场景。
- `game.unity` 不再依赖手工摆对象才能进入下一阶段。

### Phase 2：妮露 Humanoid Avatar

目标：

- 让 `NPC_Avatar_Girl_Sword_Nilou.fbx` 成为有效 Humanoid。

实施顺序：

1. 记录当前 importer 状态。
2. 切到 Humanoid。
3. 检查 Avatar 是否有效。
4. 如果失败，输出缺失骨骼或映射问题。

完成标准：

- Unity 认可 Avatar 有效。
- 无骨骼爆炸、极端缩放、明显朝向错误。
- 在 LXI 基础动作下没有明显扭胯、塌肩、脚尖异常。

阻塞规则：

- 自动映射失败时，先记录缺失项，不猜骨骼名，不直接跳去做玩法。

### Phase 3：LXI 动作重定向

目标：

- 妮露能播放 LXI 的 Idle / Move / Action 三类动作。

实施顺序：

1. 先挑 1 个 Idle clip 做最小验证。
2. 再接 1 个 Move。
3. 再接 1 个 Action。
4. 最后整理到 `LXII_PlayerAnimator.controller`。

实现约束：

- 默认关闭 Root Motion。
- 移动由 `LXIIPlayerController` 或等价控制脚本驱动。
- 动画先负责表现，不负责位移。

完成标准：

- Play Mode 中能稳定切换 Idle / Move / Action。
- 动作不会把角色拉飞、倒置、缩放异常。
- 允许存在少量局部穿模，但不把衣物/头发物理缺失误判为 Humanoid 主干失败。

补充说明：

- 如果后续需要衣物或头发物理，默认视为插件接入任务，而不是当前 Animator / Avatar 修复任务。

### Phase 4：草地与雪地交互

目标：

- 妮露进入草地区和雪地区时，能触发可见反馈。

建议方式：

- 直接引用：
  - `L12GrassInteractor`
  - `L14SnowInteractor`
- 如参数不适合 LXII，在 Builder 或 LXII 适配层调参，不改原 Demo 默认表现。

完成标准：

- 草地区可见压草或弯折。
- 雪地区可见压痕。
- L12 / L14 原始场景仍可独立运行。

### Phase 5：体积云与画面整合

目标：

- 接入 L13 体积云，完成基础空间氛围。

要求：

- 默认使用低成本档位。
- 不因为一上云就让编辑器明显卡死。
- 统一主光、相机、云体积盒的基本构图。

完成标准：

- 云正常显示。
- 不压住主玩法区域。
- Console Error 为 0。

### Phase 6：材质与近距离消隐

目标：

- 妮露保持 L10.9 材质。
- 接入已经跑通过的近距离屏幕抖动溶解方案。

要求：

- 只绑定 L10.9 材质，不迁移任何 L11 / StarRail 材质。
- 若材质槽位不匹配，先输出 `SkinnedMeshRenderer` 与槽位信息。

完成标准：

- 材质不粉、不黑、不丢失。
- 近距离镜头下可见消隐效果。

### Phase 7：最小玩法收束

目标：

- 做一个可演示的最小闭环：
  - 草地区起步
  - 雪地区穿行
  - 云下终点
  - 按键触发动作

完成标准：

- 从进入 Play Mode 到完成一轮演示，不需要手工摆对象。
- 能清楚讲出：
  - L10.9 负责角色与材质
  - LXI 负责动作
  - L12 负责草
  - L13 负责云
  - L14 负责雪

## 9. 每轮执行规则

每轮只解决一个问题，时长控制在 30 到 90 分钟。

固定流程：

1. Agent A 侦察
2. Agent A 派发一个最小任务
3. Agent B 只改指定切片
4. Agent A 做最小必要验证
5. Agent A 更新 `codex/tasks.md`
6. Agent A 决定下一轮

每轮输出必须包含：

- 本轮目标
- 触碰文件
- 验证方式
- 风险
- 下一步

## 10. 验证矩阵

### 必跑验证

- Unity Editor 自动编译状态检查：
  - `debug_check_compilation`
  - 期望 `isCompiling=false`
  - 期望 `isUpdating=false`
- Unity Console Error 检查：
  - `console_get_logs` 过滤 `Error`
  - 期望 Error 数量为 0

说明：

- Unity 2022 生成的 `Assembly-CSharp.csproj` 是给 IDE / Rider 使用的旧式 `.NET Framework v4.7.1` 项目，不是普通 SDK-style .NET 项目。
- 当前仓库不再把 `dotnet build Assembly-CSharp.csproj --no-restore` 作为 LXII 的硬性验证标准。
- 如需排查特殊 C# 编译问题，优先使用 Unity Editor 编译反馈；外部 `dotnet` / `xbuild` 只能作为辅助诊断，不能替代 Unity Console。

### 条件验证

- 如果改了 FBX importer / Avatar：
  - 检查 Humanoid 状态
  - 检查 Avatar 是否有效
- 如果改了场景装配：
  - 打开 `Assets/LXII game 整合/game.unity`
  - 检查关键对象是否存在
- 如果改了材质或动画：
  - 至少做一次 Play Mode 短跑
- 如果改了 L12 / L13 / L14 公共脚本：
  - 回原场景做回归检查

### 验收底线

- Unity Console Error 为 0
- 编译 0 error
- 关键对象与关键效果都能被观察到

如果某一轮无法跑 Unity 验证，至少要在记录里明确写出：

- 本轮只完成静态修改
- 尚未完成 Unity 侧验证
- 下一轮优先补哪项验证

## 11. 文件改动边界

默认可改：

- `Assets/LXII game 整合/**`

需要先复核再改：

- `Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx.meta`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/*.mat`
- `Assets/LXI 动作测试/**`
- `Assets/L12 grass/**`
- `Assets/L13 VolumeCloud/**`
- `Assets/L14 Snow/**`

不应改：

- `Assets/L11 NPR/**`
- `Plugins/StarRailNPRShader-main/**`
- `ProjectSettings/**`，除非本轮目标明确需要
- `Packages/manifest.json`，除非先确认必须新增包

## 12. 单轮模板

```text
### Agent A 派工
目标：
文件范围：
可复用资源：
验收方式：
禁止事项：

### Agent B 交付
已改文件：
实现说明：
验证结果：
风险 / 下一步：

### Agent A 复核
是否通过：
需要补测：
下一轮任务：
```

## 13. 首条启动 Prompt

把下面这段直接贴给 Claude Code，作为当前仓库下的工作协议：

```text
当前工作目标：
推进 `Assets/LXII game 整合/game.unity` 的整合 Demo。默认中文输出。

硬约束：
- LXII 不使用 L11 的任何内容。
- 禁止接入 `Assets/L11 NPR/**`、`Plugins/StarRailNPRShader-main/**`、StarRail RendererFeature、CharBody/CharFace/CharHair、`Tools/NPR/输出 L11 上下文报告`。
- 玩家模型固定为 `Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx`。
- 玩家材质固定复用：
  - `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 1.mat`
  - `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 2.mat`
  - `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Hair 1.mat`
  - `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Face and face_eye.mat`
- 动作必须来自 `Assets/LXI 动作测试/**`。

当前仓库事实：
- `Assets/LXII game 整合/` 当前已有 `game.unity`、`Docs/`、`Editor/`、`Scripts/`、`Settings/`。
- `game.unity` 当前已包含 `LXII Nilou Player`、L12 草地区域和第三人称摄像机链路。
- 当前角色控制已经从 `LXIIAnimationTestDriver` 单脚本测试方案拆为 `LXIIPlayerInputReader`、`LXIIPlayerMotor`、`LXIIPlayerAnimationDriver`、`LXIIPlayerController`。
- 当前可用操作为 `WASD` 移动、`Left Shift` 加速、自动 `Idle / Run` 切换、`3` 触发 `Action`、第三人称摄像机跟随/观察。
- 当前 Inspector 以 `LXIIPlayerController` 为主入口，内部辅助组件默认隐藏；如需调试，可临时开启主控上的内部组件显示开关。
- 当前 Avatar / 动作主干没有明显扭胯、塌肩、脚尖异常或头发链违和；剩余主要是局部穿模，以及衣物/头发物理尚未接入。

执行方式：
- 按单 CLI 的虚拟 duo-agent 模式工作。
- Agent A 负责侦察、拆任务、验证、更新文档。
- Agent B 负责单个实现切片。
- 每轮只解决一个问题。
- 每轮输出必须包含：本轮目标、触碰文件、验证方式、风险、下一步。

执行约束：
- 先读 `AGENTS.md`、`codex/tasks.md`、`Assets/LXII game 整合/Docs/LXII_ClaudeCode_DuoAgent_Workflow.md`。
- 优先新增 LXII 适配层，不做大范围重构。
- 重要进度只简洁同步到 `codex/tasks.md`。
- 每次代码改动后至少检查 Unity 编译状态：
  - `debug_check_compilation`
  - Unity Console Error 为 0
- Unity 场景或 Importer 改动后必须检查 `game.unity` 状态，并按需要做 Play Mode 短测。

当前最高优先级：
1. 保持当前妮露 Humanoid + LXI 动作链稳定
2. 保持拆职责第三人称角色控制链稳定，不回退到单脚本测试驱动
3. 把 L14 雪地按 LXII 路径接入
4. 再接 L13 云完成终点氛围
5. 把局部穿模和展示问题控制在可接受范围
```

## 14. 风险清单

- 妮露 Humanoid + LXI 动作主干当前已基本打通，但后续接雪地/云/溶解时仍要防止 Animator 参数和移动链路被破坏。
- LXI 动作仍可能在新增动作时出现 root motion、朝向或缩放差异。
- L12 / L14 交互器可能存在静态列表生命周期问题，整合时要注意启停配对。
- L13 体积云对编辑器性能敏感，默认不要开高质量。
- 衣物和头发物理如果要做，当前默认需要插件或成熟现成方案，不作为本轮内建开发项。
- 单 CLI 双 Agent 不是真并行，必须靠严格切片避免混乱。

## 15. 判断标准

后续每次推进 LXII 时，都优先问这三个问题：

1. 这一轮是在补“真实落地”，还是只是在补“文档想象”？
2. 这一轮有没有让妮露 Humanoid + LXI 动画链更接近可验证？
3. 这一轮结束后，是否能用编译、Scene、Console 或 Play Mode 给出明确结果？

如果答案不清楚，这一轮就该继续收缩范围，而不是扩大实现面。
