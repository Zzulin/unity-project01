# LXII Claude Code Duo-Agent 工作流

## 当前硬约束

LXII 整合 Demo 从现在开始不使用 L11 的任何内容。

禁止使用：

- `Assets/L11 NPR/**`
- `Plugins/StarRailNPRShader-main/**`
- StarRail RendererFeature / CharBody / CharFace / CharHair 链路
- `Tools/NPR/输出 L11 上下文报告`
- 任何 L11 材质治理、Frame Debugger 验证或 HSR pass 检查

固定使用：

- 玩家模型：`Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx`
- 玩家材质：`Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 1.mat`
- 玩家材质：`Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 2.mat`
- 玩家材质：`Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Hair 1.mat`
- 玩家材质：`Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Face and face_eye.mat`
- 动作来源：`Assets/LXI 动作测试/**`
- 场景系统：L12 草地、L13 体积云、L14 雪地

当前已知状态：

- 妮露 FBX 存在，但 `NPC_Avatar_Girl_Sword_Nilou.fbx.meta` 当前是 Generic rig：`animationType: 2`、`avatarSetup: 0`。
- 因此下一步不是继续游戏化收束，而是先给妮露模型补 Humanoid Avatar / 骨骼映射，让它能接收 LXI Humanoid 动作。

## 目标

在 `Assets/LXII game 整合/game.unity` 中制作一个可玩的整合型 Demo：

- 玩家角色必须是 L10.9 的妮露模型。
- 妮露保留 L10.9 已有 Toon/NPR 材质。
- 妮露通过新增或修正 Avatar 绑定，重定向播放 LXI 动作测试中的 Humanoid 动作。
- 场景继续整合 L12 GPU 草地、L13 体积云、L14 交互雪地。

优先级是“妮露能动、材质正确、场景可玩、验证清楚”。不要再把时间花在 L11 或 StarRail 链路上。

## 项目级定位

- 项目层级：小型游戏原型 / 面试向技术整合 Demo。
- 核心循环：玩家控制妮露在草地、雪地、云影环境中移动，移动触发草和雪的交互反馈，并能展示 LXI 动作。
- 第一版胜利条件：`game.unity` 可进入 Play Mode，妮露可移动，可播放 Idle/Move/Action，草地和雪地至少各有一个可见交互区，体积云正常显示。
- 不做的事：完整战斗系统、复杂任务系统、背包、联网、全角色换装、L11/StarRail 渲染链路。

## Duo-Agent 模式

这个方案可以在一个 Claude Code CLI 对话框里执行，使用“虚拟双 Agent”轮班。如果之后能开两个终端，再升级成两个真实 Claude Code 会话或 git worktree。

### Agent A：Director / Integrator

职责：

- 保持 LXII 总目标、阶段计划和任务边界。
- 先做侦察和风险判断，再给 Agent B 下发小任务。
- 拥有 `Assets/LXII game 整合/Editor/*Builder.cs`、`Assets/LXII game 整合/Docs/*`、场景装配、最终验证。
- 负责更新 `codex/tasks.md` 中的最新结论和未完成事项。
- 每轮结束必须总结：改了什么、验证了什么、下一步做什么。

禁忌：

- 不允许使用 L11 或 StarRail 内容。
- 不允许同时大改 L12/L13/L14 原始 Demo。
- 不允许在没有验证妮露 Avatar 前继续做玩法收束。
- 不允许为了“架构好看”先搭大框架。

### Agent B：Builder / Specialist

职责：

- 执行 Agent A 拆出来的具体任务。
- 一次只负责一个明确切片：妮露 Avatar、LXI 动作重定向、玩家控制、草雪交互、体积云、相机、验证工具、材质绑定其中之一。
- 改动要局限在任务指定目录，优先新增 LXII 适配层，而不是破坏原有课程 Demo。
- 每次交付必须写清：文件、入口、运行方式、残留风险。

禁忌：

- 不抢 Agent A 的总设计权。
- 不跨目录顺手重构。
- 不用“看起来应该能用”的资源路径，必须用搜索或 Unity 检查确认。

## 推荐目录

```text
Assets/LXII game 整合/
  game.unity
  Docs/
    LXII_ClaudeCode_DuoAgent_Workflow.md
    LXII_Integration_Log.md
  Editor/
    LXIIGameBuilder.cs
  Scripts/
    Core/
    Player/
    Animation/
    Interaction/
    Camera/
    UI/
  Materials/
  Prefabs/
  Settings/
```

规则：

- `Editor/*Builder.cs` 是 LXII 的可重建入口，参考 L12/L13/L14 Builder 风格。
- `Scripts/*` 只放 LXII 游戏整合层。L12/L13/L14 已稳定脚本先直接引用，不复制。
- 如果必须改 L12/L13/L14 公共脚本，先确认不会破坏原 Demo，再单独验证原场景。
- 如果必须改 L10.9 妮露 FBX importer，先记录原始 rig 状态，再改为 Humanoid 并 reimport。

## 首条启动 Prompt

把下面这段直接贴给 Claude Code，作为一个 CLI 对话框里的“全天工作协议”：

```text
你现在在 Unity 项目 D:\GitHub\unity-project01\URP1 中工作。默认中文输出。

请按 Duo-Agent 协议推进 `Assets/LXII game 整合/game.unity` 的游戏整合 Demo。

硬约束：
- LXII 不使用 L11 的任何内容。
- 禁止读取或接入 `Assets/L11 NPR/**`、`Plugins/StarRailNPRShader-main/**`、StarRail RendererFeature、CharBody/CharFace/CharHair、`Tools/NPR/输出 L11 上下文报告`。
- 玩家模型固定为 `Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx`。
- 玩家材质固定复用：
  `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 1.mat`
  `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 2.mat`
  `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Hair 1.mat`
  `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Face and face_eye.mat`
- 动作必须来自 `Assets/LXI 动作测试/**`。

当前最高优先级：
先修正妮露模型的骨骼/Avatar 绑定，使它能作为 Humanoid 接收 LXI 动作测试中的动作。不要先继续 Phase 6。

Duo-Agent 规则：
- Agent A = Director / Integrator：负责总目标、阶段拆解、场景整合、验证、更新文档。
- Agent B = Builder / Specialist：负责执行 Agent A 下发的单个实现切片。
- 你只有一个 CLI 对话框，所以请用轮班方式模拟两个 Agent：每轮先由 Agent A 制定 1 个最小任务，再由 Agent B 执行，最后 Agent A 验证并决定下一轮。
- 每轮输出必须包含：本轮目标、触碰文件、验证命令或 Unity 菜单、风险、下一步。

执行约束：
- 先读 `CLAUDE.md`、`AGENTS.md`、`codex/tasks.md` 和 `Assets/LXII game 整合/Docs/LXII_ClaudeCode_DuoAgent_Workflow.md`。
- 不要大范围重构；优先新增 LXII 适配层。
- 重要进度只简洁同步到 `codex/tasks.md`。
- 每次代码改动后至少运行：
  `dotnet build Assembly-CSharp.csproj`
  `dotnet build Assembly-CSharp-Editor.csproj`
- Unity 场景/Importer 改动后必须验证 Console Error。

现在先做 Agent A 的修正计划：检查妮露 FBX rig/importer、LXI 可用动作、当前 LXII Builder 对玩家的生成逻辑，然后提出“妮露 Humanoid Avatar + LXI 动作重定向”的最小实施步骤。
```

## 全天循环

每轮控制在 30-90 分钟内。Claude 如果跑很久，也必须周期性落检查点。

1. Agent A 侦察

输出本轮只要解决的一个问题，例如“先把妮露 FBX 改成 Humanoid 并 reimport”或“先用一个 LXI Idle/Run clip 验证 retarget”。

2. Agent B 实作

只改指定文件。若发现任务前提不成立，停止当前实现，回报 Agent A，而不是临时扩大范围。

3. Agent A 验证

至少做一类验证：

- C# 编译：`dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj`
- Unity 菜单：`Tools/LXII/...`
- Importer/Avatar：妮露 FBX 为 Humanoid，Avatar 有效，Animator 可播放 LXI clip
- 场景检查：Console Error 为 0、关键对象存在、材质/Shader 命中
- 视觉检查：截图或 Play Mode 短跑

4. Agent A 记录

只把最新结论写入 `codex/tasks.md`。长过程写在 LXII Docs，不污染主任务记录。

5. Agent A 派发下一轮

下一轮必须从当前验证结果出发，避免跳任务。

## 阶段路线

### Phase 0：资源盘点，不写功能

产出：

- LXII 当前资源清单。
- 妮露模型 rig 状态：当前 Generic / Humanoid、Avatar 是否有效、骨骼映射缺失项。
- 妮露材质槽位清单：确认 Body 1、Body 2、Hair 1、Face and face_eye 是否正确绑定。
- LXI 可用动作清单：至少找出 Idle、Run/Walk、Action 三类 Humanoid 动作。
- 场景系统清单：L12/L13/L14 哪些脚本可以直接引用，哪些需要适配层。

已知线索：

- 妮露模型路径：`Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx`。
- 当前妮露 importer 是 Generic：`animationType: 2`、`avatarSetup: 0`，需要改成 Humanoid。
- LXI 存在 Humanoid 资源，例如 `erji1112.fbx`、`举手.fbx`、`举手3.fbx`、Kevin Iglesias HumanM/HumanF 模型和大量动作。
- L12 交互入口是 `L12GrassInteractor`，渲染主体是 `L12GrassRenderer`。
- L13 主体是 `L13VolumeCloudController`。
- L14 交互入口是 `L14SnowInteractor`，雪面主体是 `L14SnowField`。

### Phase 1：LXII 可重建场景骨架

目标：

- `Tools/LXII/Build Integrated Game Demo` 能稳定生成整合场景。
- Builder 能创建灯光、相机、地形基准、草地区、雪地区、云体积盒。
- 玩家生成逻辑必须指向妮露模型，不再使用 Capsule 或 LXI HumanM/F dummy 作为最终玩家。

验收：

- 打开菜单后 `Assets/LXII game 整合/game.unity` 可生成基础场景。
- 两个 dotnet build 0 error。
- Play Mode Console Error 为 0。

### Phase 2：妮露 Humanoid Avatar + LXI 动作重定向

目标：

- 将 `NPC_Avatar_Girl_Sword_Nilou.fbx` 设置为 Humanoid。
- 创建或修正妮露 Avatar，使 Unity 判定 Avatar 有效。
- 从 LXI 动作测试中选择 Idle、Move、Action 三个 Humanoid clip。
- 创建 `LXII_PlayerAnimator.controller`，让妮露能播放这些动作。

策略：

- 先只验证一个 Idle 动作能播放，再接 Move 和 Action。
- 如果自动 Humanoid 映射失败，先输出缺失骨骼列表，不要猜骨骼名。
- 如果妮露骨骼无法直接 Humanoid，建立最小 Avatar 映射方案或临时记录阻塞点，但不要回退到 L11。
- Root Motion 默认关闭，先让 `LXIIPlayerController` 控制移动，动画只负责表现。

验收：

- 妮露模型在 Play Mode 中不再是 Capsule 占位体。
- Idle/Move/Action 至少能播放一个完整循环或触发片段。
- 动作不会导致角色飘移、缩放异常或骨骼爆炸。
- Console Error 为 0。

### Phase 3：接入草地和雪地互动

目标：

- 妮露玩家身上挂 `L12GrassInteractor` 和 `L14SnowInteractor` 或 LXII 统一交互适配器。
- 地图上有草地区和雪地区，妮露走过能看到压草和压雪。

策略：

- 直接引用 L12/L14 已验证运行时脚本。
- 如半径、高度、步幅需要调参，在 LXII Builder 中设置，不改原 Demo 默认值。
- 先做单点交互，再考虑双脚 stamp。

验收：

- 草地区可见弯折/压草反馈。
- 雪地区可见压痕反馈。
- 原 L12/L14 场景不被破坏。

### Phase 4：接入体积云和整体画面

目标：

- 使用 L13 体积云作为场景天空/远景氛围。
- 保持默认性能档，避免编辑器打开即卡顿。
- 补充主相机、后处理、Directional Light 和基础构图。

验收：

- 云体积显示稳定，不遮挡主玩法。
- Play Mode 帧率可接受。
- Console Error 为 0。

### Phase 5：妮露材质和近距离消隐

目标：

- 妮露保留 L10.9 已有 Toon/NPR 材质。
- 近距离镜头消隐沿用 L10.9 已跑通的屏幕抖动溶解方案。

策略：

- 只绑定 L10.9 妮露材质，不迁移 L11/StarRail 材质。
- 材质槽位缺失时先输出 SkinnedMeshRenderer 和材质槽清单。
- 如果需要微调 `_NearDissolveStart`、`_NearDissolveEnd`、`_NearDissolvePatternScale`，只改 L10.9 已有材质实例或 LXII 专用副本。

验收：

- 妮露移动时材质不丢、不粉、不黑。
- 近距离镜头消隐可见。
- 不出现 L11/StarRail 相关 RendererFeature 或 Shader 依赖。

### Phase 6：游戏化收束

目标：

- 添加一个简单目标：妮露穿过草地区、雪地区，到达云下终点，按键播放动作。
- 添加最小 HUD：当前区域、动作提示、性能/调试开关。
- 录制或截图一组可展示视角。

验收：

- 从进入 Play Mode 到完成小目标不需要手动摆对象。
- 场景能讲清楚：L10.9 妮露角色与材质、LXI 动作、L12 草、L13 云、L14 雪分别贡献什么。
- `codex/tasks.md` 记录最终状态和残留风险。

## 模块边界

推荐模块：

- `Core`：Bootstrap、游戏状态、全局配置。
- `Player`：输入、移动、角色朝向、相机跟随目标。
- `Animation`：妮露 Avatar 说明、Animator 参数桥接、LXI 动作触发。
- `Interaction`：把妮露位置传给草地和雪地交互器。
- `World`：草地、雪地、云、灯光、后处理的场景装配。
- `Presentation`：L10.9 Toon 材质、近距离消隐、展示 UI。

通信规则：

- LXII 内部可用显式引用，不需要事件总线。
- Builder 负责把引用接好，运行时脚本不做 `GameObject.Find`。
- 共享参数用 Inspector 字段或 `ScriptableObject`，不要塞进静态全局状态。
- `Update` / `LateUpdate` 必须有空引用和初始化保护。

## 文件权限边界

Agent B 默认可改：

- `Assets/LXII game 整合/**`

Agent B 需要 Agent A 复核后才能改：

- `Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx.meta`
- `Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/*.mat`
- `Assets/LXI 动作测试/**`
- `Assets/L12 grass/**`
- `Assets/L13 VolumeCloud/**`
- `Assets/L14 Snow/**`

Agent B 不应改：

- `Assets/L11 NPR/**`
- `Plugins/StarRailNPRShader-main/**`
- `ProjectSettings/**`，除非本轮目标明确需要渲染管线设置。
- `Packages/manifest.json`，除非先确认必须新增包。

## 单轮任务模板

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
风险/下一步：

### Agent A 复核
通过/不通过：
需要补测：
下一轮任务：
```

## 每日节奏

上午：

- 只做侦察、Builder、妮露 Avatar 状态检查。
- 不碰 L11，不开 StarRail 分支。

下午：

- 接入 LXI 动作、玩家控制、草雪交互、体积云。
- 每完成一个系统就做编译验证。

晚上：

- 做画面、材质参数、截图、文档。
- 不再开启大重构。

无人值守时：

- 每轮只取一个小任务。
- 任何编译失败必须先修复再继续。
- Unity 资源路径不确定时先生成清单，不猜。
- 如果 Play Mode 或场景健康检查不可用，至少完成 dotnet build 并记录“未跑 Unity 验证”。

## 风险清单

- 妮露当前不是 Humanoid：这是 LXII 当前最高优先级风险，必须先处理。
- 妮露骨骼可能无法自动映射：需要输出缺失骨骼列表，再决定手动映射或替代动作策略。
- LXI 动作可能有 root motion / 缩放 / 朝向差异：第一版关闭 Root Motion，用代码移动。
- L12/L14 都有交互器静态列表：场景切换和对象销毁时要确认 OnEnable/OnDisable 配对，避免残留引用。
- L13 体积云性能：默认低步数，不要一开始就开高质量。
- 单 CLI 虚拟双 Agent 不是真并行：优势是省钱和减少冲突，代价是速度比两个真实会话慢。

## 升级成真实双终端

如果之后愿意开两个 Claude Code 终端，建议：

- 终端 A：主分支或 `codex/lxii-integrator`，只做 Builder、场景、验证、任务记录。
- 终端 B：`codex/lxii-nilou-avatar`，只做妮露 Avatar、LXI 动作重定向和 Animator。
- 每次 B 完成后由 A 合并或手动拷贝改动。
- 两边不要同时改同一个 `.unity`、`.prefab`、`.mat`、`.fbx.meta` 文件。

真实双终端比单 CLI 更快，但 Unity 场景、prefab、importer 冲突风险明显更高；当前更推荐单 CLI 虚拟 duo-agent。
