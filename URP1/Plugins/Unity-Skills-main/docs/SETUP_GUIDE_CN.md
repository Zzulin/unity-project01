# UnitySkills 安装与使用指南

> [English](SETUP_GUIDE.md) | 中文

---

## 环境要求

- **Unity**：`2022.3+`（推荐 LTS；完整支持 Unity 6）
- **网络**：本地回环地址 `localhost` / `127.0.0.1`
- **Python**（可选）：3.7+，需安装 `requests` 包，用于 Python 客户端

---

## 1. 安装 Unity 包

在 Unity 编辑器中打开：

```
Window → Package Manager → + → Add package from git URL
```

选择以下地址之一：

| 频道 | URL |
|------|-----|
| **稳定版** (main) | `https://github.com/Besty0728/Unity-Skills.git?path=/SkillsForUnity` |
| **开发测试版** (beta) | `https://github.com/Besty0728/Unity-Skills.git?path=/SkillsForUnity#beta` |
| **指定版本** | `https://github.com/Besty0728/Unity-Skills.git?path=/SkillsForUnity#v1.6.8` |

也可以从 [Releases 页面](https://github.com/Besty0728/Unity-Skills/releases) 下载特定版本。

---

## 2. 启动服务

```
Window → UnitySkills → Start Server
```

正常启动后，Console 会输出：

```
[UnitySkills] REST Server started at http://localhost:8090/
```

验证：

```bash
curl http://localhost:8090/health
```

> **注意**：脚本编译、Domain Reload 和部分资源操作会导致服务短暂不可达。这是 Unity 编辑器的正常行为 — 等待几秒后重试即可。

---

## 3. 安装 AI Skills

### 推荐：一键安装

```
Window → UnitySkills → Skill Installer
```

选择目标 AI 工具后点击 **Install**。安装器会将 `unity-skills~/` 模板目录复制到正确位置。安装的文件包括：

```
SKILL.md                    # 主 Skill 定义（AI 读取）
skills/                     # 按模块分类的 Skill 文档（38 功能 + 14 顾问）
scripts/unity_skills.py     # Python 客户端库
scripts/agent_config.json   # Agent 配置
references/                 # Unity 开发参考文档
```

> **Codex 说明**：推荐使用全局安装。项目级安装需要在 `AGENTS.md` 中声明才能识别。

### 手动安装

如果一键安装不支持你的工具，可手动将 `SkillsForUnity/unity-skills~/` 目录内容复制到工具的 skills 目录。

**常见工具路径：**

| 工具 | Skills 目录 |
|------|------------|
| Claude Code | `~/.claude/skills/` |
| Antigravity | `~/.agent/skills/` |
| Gemini CLI | `~/.gemini/skills/` |
| Codex | `~/.codex/skills/`（全局） |
| Cursor | `~/.cursor/skills/` |

### 支持的 AI 工具

以下工具已通过官方测试：

| 工具 | 状态 | 特色 |
|------|:----:|------|
| **Antigravity** | ✅ | 原生 `/unity-skills` 斜杠命令 |
| **Claude Code** | ✅ | 智能 Skill 意图识别 |
| **Gemini CLI** | ✅ | `experimental.skills` 支持 |
| **Codex** | ✅ | `$skill` 显式调用 + 隐式意图识别 |

> ⚠️ **通用兼容性**：UnitySkills 遵循开放的 Skill 标准。**任何能读取 markdown 文件并发送 HTTP 请求的 AI 工具**都可以使用 UnitySkills — 不限于上述列表。只需将 `unity-skills~/` 目录内容复制到你的工具的 skill 或 prompt 位置，确保工具能访问 `http://localhost:8090` 即可。

---

## 4. Python 客户端

### 基本用法

```python
import unity_skills

# 检查服务器状态
unity_skills.health()

# 调用技能
unity_skills.call_skill("gameobject_create",
    name="MyCube", primitiveType="Cube", x=0, y=1, z=0)

# 获取所有可用技能
unity_skills.get_skills()
```

### 过滤查询与推荐

```python
# 按元数据过滤技能
unity_skills.get_skills(category="GameObject", operation="Create")
unity_skills.get_skills(tags="batch")
unity_skills.get_skills(read_only=True)
unity_skills.get_skills(q="screenshot")

# 意图推荐（服务端评分排序）
unity_skills.find_skills("create red cube", top_n=5)

# 查找产出特定字段的技能链
unity_skills.get_skill_chain("instanceId")
```

### 工作流上下文

```python
# 批量操作分组，支持一键撤销/重做
with unity_skills.workflow_context("Build Scene", "Create player and environment"):
    unity_skills.call_skill("gameobject_create", name="Player")
    unity_skills.call_skill("component_add", name="Player", componentType="Rigidbody")
# 所有操作可通过 workflow_undo_task 一次性回滚
```

### CLI 用法

```bash
python unity_skills.py --list
python unity_skills.py gameobject_create name=MyCube primitiveType=Cube
```

---

## 5. REST API

### 直接 HTTP 调用

```bash
# 健康检查
curl http://localhost:8090/health

# 获取所有技能
curl http://localhost:8090/skills

# 过滤技能
curl "http://localhost:8090/skills?category=GameObject&operation=Create"

# 意图推荐
curl "http://localhost:8090/skills/recommend?intent=create+cube&topN=5"

# 执行技能
curl -X POST http://localhost:8090/skill/gameobject_create \
  -H "Content-Type: application/json" \
  -d '{"name":"MyCube","primitiveType":"Cube","x":1,"y":2,"z":3}'
```

### 响应格式

所有技能返回统一格式：

```json
{
  "status": "success",
  "result": {
    "success": true,
    "name": "MyCube",
    "instanceId": 12345,
    "position": {"x": 1, "y": 2, "z": 3}
  }
}
```

---

## 6. 核心概念

### Domain Reload 与短暂不可达

以下操作可能触发 Unity 编译并短暂中断服务：

- `script_create`、`script_append`、`script_replace`
- `debug_force_recompile`、`debug_set_defines`
- 部分 `asset_import` / `asset_reimport` / `asset_move` 操作
- 包安装/移除

**建议处理方式**：等待几秒后，调用 `wait_for_unity()` 或使用 `call_skill_with_retry()`。

### 批量优先原则

操作 2 个及以上对象时，始终优先使用 `*_batch` 技能：

```python
# ✅ 正确 — 单次请求
unity_skills.call_skill("gameobject_create_batch", items=[
    {"name": "A", "primitiveType": "Cube", "x": -1},
    {"name": "B", "primitiveType": "Cube", "x": 1},
])

# ❌ 避免 — 多次请求
for name in ["A", "B"]:
    unity_skills.call_skill("gameobject_create", name=name)
```

### 多实例路由

同时打开多个 Unity 项目时：

```python
unity_skills.set_unity_version("2022.3")   # 按 Unity 版本路由
unity_skills.list_instances()               # 枚举所有实例
```

### 测试模块

`test_run` 和 `test_run_by_name` 是异步的 — 立即返回 `jobId`，使用 `test_get_result(jobId)` 轮询结果。

---

## 7. 常见排障

| 问题 | 现象 | 建议 |
|------|------|------|
| 连接失败 | `Cannot connect to http://localhost:8090` | 检查服务是否已启动；可能正在编译 / Domain Reload |
| 请求超时 | 超过 15 分钟后无响应 | 确认是否为长任务；在 Unity 面板中调高超时设置 |
| 技能列表为空 | `/skills` 返回异常 | 检查 Console 是否有编译错误 |
| 脚本创建后断连 | `script_create` 后服务不可达 | 正常现象 — 等待编译完成后重试 |
| 连接到错误实例 | 请求打到了错误项目 | 使用 `set_unity_version()` 或按项目名连接 |
| 工作流状态异常 | 客户端/服务端状态不一致 | 读取 `workflow_session_status`；客户端已内置恢复逻辑 |

---

## 8. 参考索引

| 资源 | 说明 |
|------|------|
| [README.md](../README.md) | 项目说明（英文） |
| [README_CN.md](../README_CN.md) | 项目说明（中文） |
| [SKILL.md](../SkillsForUnity/unity-skills~/SKILL.md) | 完整 Skill API 参考 |
| [CHANGELOG.md](../CHANGELOG.md) | 版本更新记录 |
| [agent.md](../agent.md) | AI Agent 项目概览 |
