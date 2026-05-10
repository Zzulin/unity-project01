# LXII Claude Code Duo-Agent 工作流

## 目标

在 `Assets/LXII game 整合/game.unity` 中做一个可玩的整合型 Demo，把现有内容收束成一个小型游戏垂直切片：

- L11/L10.9：NPR 角色观感、近距离镜头消隐、角色展示基线。
- LXI：角色 Avatar、AnimatorController、动作素材与动作测试场景。
- L12：GPU-driven 草地、交互压草。
- L13：光线步进体积云与天气氛围。
- L14：交互雪地、压痕与雪面材质。

优先级是“可玩、可讲、可验证”，不是把所有 Demo 的全部功能一口气塞进 LXII。

## 项目级定位

- 项目层级：小型游戏原型 / 面试向技术整合 Demo。
- 核心循环：玩家控制一个 NPR 角色在草地、雪地、云影环境中移动，移动会触发草和雪的交互反馈，并能展示一组动作状态。
- 第一版胜利条件：`game.unity` 可进入 Play Mode，玩家可移动，角色可播放 Idle/Move/Action，草地和雪地至少各有一个可见交互区，天空体积云正常显示。
- 不做的事：完整战斗系统、复杂任务系统、背包、联网、全角色换装、全材质治理。

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

- 不允许同时大改 L12/L13/L14 原始 Demo。
- 不允许在没有验证 Avatar/材质策略前直接重写 L11 角色 Shader 链路。
- 不允许为了“架构好看”先搭大框架。

### Agent B：Builder / Specialist

职责：

- 执行 Agent A 拆出来的具体任务。
- 一次只负责一个明确切片：动画、场景系统接入、角色控制、相机、验证工具、材质适配其中之一。
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
    LXII_Integration_Log.md              # Claude 后续可创建，记录短检查点
  Editor/
    LXIIGameBuilder.cs                   # 后续创建，一键重建整合场景
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

## 首条启动 Prompt

把下面这段直接贴给 Claude Code，作为一个 CLI 对话框里的“全天工作协议”：

```text
你现在在 Unity 项目 D:\GitHub\unity-project01\URP1 中工作。默认中文输出。

请按 Duo-Agent 协议推进 `Assets/LXII game 整合/game.unity` 的游戏整合 Demo：

- Agent A = Director / Integrator：负责总目标、阶段拆解、场景整合、验证、更新文档。
- Agent B = Builder / Specialist：负责执行 Agent A 下发的单个实现切片。
- 你只有一个 CLI 对话框，所以请用轮班方式模拟两个 Agent：每轮先由 Agent A 制定 1 个最小任务，再由 Agent B 执行，最后 Agent A 验证并决定下一轮。
- 每轮输出必须包含：本轮目标、触碰文件、验证命令或 Unity 菜单、风险、下一步。

当前目标：
在 `Assets/LXII game 整合/game.unity` 中制作一个可玩的整合型 Demo，整合：
1. L11/L10.9 的 NPR 角色视觉基线；
2. LXI 的 Avatar/Animator/动作素材；
3. L12 的交互草地；
4. L13 的体积云；
5. L14 的交互雪地。

执行约束：
- 先读 `CLAUDE.md`、`AGENTS.md`、`codex/tasks.md` 和 `Assets/LXII game 整合/Docs/LXII_ClaudeCode_DuoAgent_Workflow.md`。
- 不要大范围重构；优先新增 LXII 适配层。
- 重要进度只简洁同步到 `codex/tasks.md`。
- 每次代码改动后至少运行：
  `dotnet build Assembly-CSharp.csproj`
  `dotnet build Assembly-CSharp-Editor.csproj`
- Unity 场景改动后优先提供可重复的 `Tools/LXII/...` 菜单或清晰手动验证步骤。

现在先做 Agent A 的项目侦察：列出 LXII、LXI、L11/L10.9、L12-L14 可复用资源清单，并提出第一轮最小可玩切片计划。不要先写代码。
```

## 全天循环

每轮控制在 30-90 分钟内。Claude 如果跑很久，也必须周期性落检查点。

1. Agent A 侦察

输出本轮只要解决的一个问题，例如“先创建 LXII Builder 骨架”或“先验证 LXI 哪个 Avatar 可以驱动动作”。

2. Agent B 实作

只改指定文件。若发现任务前提不成立，停止当前实现，回报 Agent A，而不是临时扩大范围。

3. Agent A 验证

至少做一类验证：

- C# 编译：`dotnet build Assembly-CSharp.csproj`、`dotnet build Assembly-CSharp-Editor.csproj`
- Unity 菜单：`Tools/LXII/...`
- 场景检查：Console Error 为 0、关键对象存在、材质/Shader 命中
- 视觉检查：截图或 Play Mode 短跑

4. Agent A 记录

只把最新结论写入 `codex/tasks.md`。长过程写在 LXII Docs，不污染主任务记录。

5. Agent A 派发下一轮

下一轮必须从当前验证结果出发，避免跳任务。

## 阶段路线

### Phase 0：资源盘点，不写功能

产出：

- LXII 资源清单。
- 可用动作清单：LXI 中哪些 FBX 是 Humanoid，哪些 AnimatorController 可直接用。
- 可用角色清单：L11/L10.9 中哪个角色最适合先做玩家。
- 场景系统清单：L12/L13/L14 哪些脚本可以直接引用，哪些需要适配层。

已知线索：

- LXI 存在 Humanoid 资源，例如 `erji1112.fbx`、`举手.fbx`、`举手3.fbx`、Kevin Iglesias HumanM/HumanF 模型和大量动作。
- L12 交互入口是 `L12GrassInteractor`，渲染主体是 `L12GrassRenderer`。
- L13 主体是 `L13VolumeCloudController`，可用 Builder 生成默认体积云。
- L14 交互入口是 `L14SnowInteractor`，雪面主体是 `L14SnowField`。
- L11 的 StarRail 链路必须先跑 `Tools/NPR/输出 L11 上下文报告`，不要盲改材质。

### Phase 1：LXII 可重建场景骨架

目标：

- 创建 `Tools/LXII/Build Integrated Game Demo`。
- Builder 能创建灯光、相机、地形基准、玩家占位体、草地区、雪地区、云体积盒。
- `game.unity` 由 Builder 保存，后续可以反复重建。

验收：

- 打开菜单后 `Assets/LXII game 整合/game.unity` 可生成基础场景。
- 两个 dotnet build 0 error。
- Play Mode Console Error 为 0。

### Phase 2：玩家控制和动画先跑通

目标：

- 先用 LXI 已确认 Avatar/Animator 的角色或简单 Human dummy 作为玩家。
- 做 `LXIIPlayerController`，支持移动、转向、跳过复杂战斗。
- Animator 至少有 Idle/Move/Action 三态。

策略：

- 先用能动的 LXI 角色验证动作链路。
- L11/L10.9 NPR 角色如果没有可用 Avatar，先作为视觉替换风险项，不阻塞第一版可玩。
- 若要绑定 NPR 角色，先确认 Rig/Avatar，再决定 retarget、复制 Avatar、还是做骨骼映射适配。

验收：

- 玩家移动时动画切换正常。
- 动作不会导致角色飘移、缩放异常或骨骼爆炸。
- Console Error 为 0。

### Phase 3：接入草地和雪地互动

目标：

- 玩家身上挂 `L12GrassInteractor` 和 `L14SnowInteractor` 或 LXII 统一交互适配器。
- 地图上有草地区和雪地区，玩家走过能看到压草和压雪。

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

### Phase 5：NPR 角色视觉替换

目标：

- 让玩家或展示角色具备 L11/L10.9 的 NPR 外观。
- 最小可交付是“可动角色 + Toon/NPR 材质 + 镜头近距离不硬切”。

策略：

- 优先复用 L10.9 已跑通的角色材质和近距离屏幕抖动溶解。
- L11 StarRail NPR 作为进阶目标：先跑上下文报告和 Frame Debugger，再做材质治理。
- 不要把 StarRail 参考仓库的 GPL-3.0 内容复制成对外可分发的新代码；项目内学习引用要保留来源边界。

验收：

- 角色移动时材质不丢、不粉、不黑。
- 近距离镜头消隐可见。
- 如果启用 StarRail RendererFeature，确认 `HSRForward*` / `HSRHair*` / `HSROutline` 命中。

### Phase 6：游戏化收束

目标：

- 添加一个简单目标：穿过草地区、雪地区，到达云下终点，按键播放动作。
- 添加最小 HUD：当前区域、动作提示、性能/调试开关。
- 录制或截图一组可展示视角。

验收：

- 从进入 Play Mode 到完成小目标不需要手动摆对象。
- 场景能讲清楚 L11-L14 和 LXI 分别贡献什么。
- `codex/tasks.md` 记录最终状态和残留风险。

## 模块边界

推荐模块：

- `Core`：Bootstrap、游戏状态、全局配置。
- `Player`：输入、移动、角色朝向、相机跟随目标。
- `Animation`：Animator 参数桥接、动作触发、Avatar 资源说明。
- `Interaction`：把玩家位置传给草地和雪地交互器。
- `World`：草地、雪地、云、灯光、后处理的场景装配。
- `Presentation`：NPR 材质、近距离消隐、展示 UI。

通信规则：

- LXII 内部可用显式引用，不需要事件总线。
- Builder 负责把引用接好，运行时脚本不做 `GameObject.Find`。
- 共享参数用 Inspector 字段或 `ScriptableObject`，不要塞进静态全局状态。
- `Update` / `LateUpdate` 必须有空引用和初始化保护。

## 文件权限边界

Agent B 默认可改：

- `Assets/LXII game 整合/**`

Agent B 需要 Agent A 复核后才能改：

- `Assets/L12 grass/**`
- `Assets/L13 VolumeCloud/**`
- `Assets/L14 Snow/**`
- `Assets/L11 NPR/**`
- `Assets/L10.9 learnNPR/**`
- `Assets/LXI 动作测试/**`

Agent B 不应改：

- `ProjectSettings/**`，除非本轮目标明确需要渲染管线设置。
- `Packages/manifest.json`，除非先确认必须新增包。
- 参考仓库插件源码，除非任务明确是修复其兼容性问题。

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

- 只做侦察、Builder、可重建场景。
- 不碰复杂 Shader 和骨骼绑定。

下午：

- 接入玩家动作、草雪交互、体积云。
- 每完成一个系统就做编译验证。

晚上：

- 做画面、参数、截图、文档。
- 不再开启大重构。

无人值守时：

- 每轮只取一个小任务。
- 任何编译失败必须先修复再继续。
- Unity 资源路径不确定时先生成清单，不猜。
- 如果 Play Mode 或场景健康检查不可用，至少完成 dotnet build 并记录“未跑 Unity 验证”。

## 风险清单

- L11 角色没有稳定 Avatar：会阻塞“原角色直接动作化”，但不应阻塞 LXII 第一版。先用 LXI Humanoid 角色跑玩法，再替换外观。
- L12/L14 都有交互器静态列表：场景切换和对象销毁时要确认 OnEnable/OnDisable 配对，避免残留引用。
- L13 体积云性能：默认低步数，不要一开始就开高质量。
- GPL-3.0 参考仓库：展示和学习可以继续，外发作品前要重新审查授权边界。
- 单 CLI 虚拟双 Agent 不是真并行：优势是省钱和减少冲突，代价是速度比两个真实会话慢。

## 升级成真实双终端

如果之后愿意开两个 Claude Code 终端，建议：

- 终端 A：主分支或 `codex/lxii-integrator`，只做 Builder、场景、验证、任务记录。
- 终端 B：`codex/lxii-animation` 或 `codex/lxii-world-adapter`，只做一个专题目录。
- 每次 B 完成后由 A 合并或手动拷贝改动。
- 两边不要同时改同一个 `.unity`、`.prefab`、`.mat` 文件。

真实双终端比单 CLI 更快，但 Unity 场景和 prefab 冲突风险明显更高；LXII 这种整合任务，前两天更推荐单 CLI 虚拟 duo-agent。
