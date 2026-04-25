# BDFramework Copilot Mandatory Rules

This file is the mandatory workspace instruction set for GitHub Copilot in this repository.

## Module Index

- Copilot mandatory rules file: `.github/copilot-instructions.md`
- Sync registry: `AI_RULES_INDEX.md`
- Module instructions: `.github/instructions/ci.instructions.md`, `.github/instructions/bdframework.instructions.md`, `.github/instructions/e2e.instructions.md`

## Baseline Code Standards

- **All comments and docstrings must be written in paired Chinese and English.** This applies to module docstrings, class docstrings, function docstrings, process comments, fixture descriptions, test docstrings, inline comments, and configuration file comments. Put Chinese first and follow with the English version in the same comment block. Chinese-only or English-only comments do not satisfy this requirement.
- Every touched class must have a class-level bilingual comment or docstring. It must explain the design role of the type, why it exists, and include an example or usage note for key business, protocol, pipeline, or orchestration classes.
- Every touched function or method must have a bilingual comment or docstring that explains purpose and behavior. For non-trivial helpers, document inputs, outputs, side effects, fallback rules, or failure contract rather than repeating parameter names.
- Test files (pytest, Unity tests, etc.) are NOT exempt: every test module, test class, test function, fixture, and test helper must have a bilingual docstring explaining its purpose and the scenario it validates.
- Major workflows must stay concentrated around an explicit entry or coordinator method so the end-to-end path can be read in one place. Do not scatter the primary process across distant files or tiny helpers without a clear bridge.
- Major workflows and critical branches must include bilingual process comments. Use phase-oriented comments so readers can follow the flow from top to bottom without reconstructing it from logs alone.
- Critical configuration files must be documented in bilingual code comments or docstrings where they are declared, loaded, generated, or written. The comment must explain the file purpose and who produces and consumes it.
- Every new or changed code path must add or update automated tests. Prefer unit tests first; if a flow cannot be covered purely with unit tests, add the closest automated verification and explain the gap. Relevant tests must pass before the task is considered complete.

## Markdown Documentation Standards

- All `.md` documentation files must be written in English.
- Keep `.md` files concise and high-signal. Avoid bloated structure, repetitive narration, and low-value trivia.
- Do not update `.md` files for minor wording churn or routine noise. Update them only when behavior, entrypoints, ownership, or required policy actually changes.

## Mandatory Conventions

- Important multi-step flows must emit explicit logs at entry, key branch or fallback, and completion or error so runtime debugging does not rely on inference.
- Automated tests, batch verification entries, and CI validation entrypoints must print Chinese start logs with explicit `测试目的=` and `实现手段=` markers, and multi-step or long-running checks must continue emitting key progress logs so the current validation stage is visible in console and TeamCity output.
- Unity3D business-layer code must not use reflection.
- Reflection is allowed only lightly in framework or infrastructure code when needed for compatibility, platform isolation, or controlled extension points, and the reason must be documented in code comments.

## Terminal Async Execution Discipline / 终端异步执行纪律

// 终端异步执行纪律 —— 禁止主动轮询
// Terminal async execution discipline — NO ACTIVE POLLING

### Forbidden Pattern: Repeated get_terminal_output Polling / 禁止模式：重复调用 get_terminal_output 轮询

**CRITICAL**: When `run_in_terminal` is used with `mode=async` to launch a long-running command (e.g., `run-build --wait`), Copilot **MUST NOT** repeatedly call `get_terminal_output` to poll for progress. This exhausts the context window and freezes the session.

// **关键**：当 `run_in_terminal` 使用 `mode=async` 启动长时间运行的命令（如 `run-build --wait`）时，Copilot **不得** 重复调用 `get_terminal_output` 来轮询进度。这会耗尽上下文窗口并导致会话卡住。

**Correct Pattern / 正确做法**:

1. Use `run_in_terminal` with `mode=async` to launch the command
   // 使用 `run_in_terminal` 的 `mode=async` 启动命令

2. **WAIT** for the terminal completion notification — do NOT call `get_terminal_output` during the wait
   // **等待** 终端完成通知 —— 在等待期间不要调用 `get_terminal_output`

3. After receiving the completion notification, call `get_terminal_output` **ONCE** to read the final result
   // 收到完成通知后，调用 `get_terminal_output` **一次** 读取最终结果

**Why This Matters / 为什么这很重要**:

- The terminal system is designed to notify Copilot when async commands complete
- Polling with `get_terminal_output` creates a busy-wait loop that consumes context budget
- A single `run-build --wait` can run for 30+ minutes; polling every few seconds exhausts context in minutes
- This is a session-killing pattern that must be avoided at all costs

// 终端系统设计为在异步命令完成时通知 Copilot
// 用 `get_terminal_output` 轮询会创建忙等待循环，消耗上下文预算
// 单个 `run-build --wait` 可能运行 30+ 分钟；每几秒轮询一次会在几分钟内耗尽上下文
// 这是一个会杀死会话的模式，必须不惜一切代价避免

**Enforcement / 执行**:

- If you find yourself calling `get_terminal_output` more than once for the same terminal ID without receiving a completion notification, **STOP IMMEDIATELY**
- This is a violation of workspace policy and indicates a bug in your execution pattern
// 如果你发现自己在没有收到完成通知的情况下，对同一个终端 ID 调用 `get_terminal_output` 超过一次，**立即停止**
// 这违反了工作区策略，表明你的执行模式有 bug


## Trust Boundary — Fail Fast vs Graceful Degradation

// 信任边界 —— Fail Fast 与优雅降级
// All data flowing through the system is classified as either **Trusted** or **Untrusted**. The error handling strategy is determined by the data's origin, not by the developer's mood.

### Trusted Path (Internal Data) — Fail Fast
// 可信路径（内部数据）—— 快速失败

Data that originates from inside the system boundary. Errors here are rare; if they happen, it is a **bug**, not a user scenario. **Throw immediately. Do not silently swallow or defensively handle.**

| Source | Examples |
|---|---|
| Config loaded from StreamingAssets / persistentDataPath | `BDFrameworkSetting.conf`, `HotfixFile.conf` |
| SQLite table data already validated at import time | Excel-generated game tables |
| Internal serialization / deserialization | AOT metadata, hotfix DLL loading |
| Framework-internal state | Manager registration, ScreenView navigation stack |

```csharp
// BAD — silently swallows a config error
var config = JsonUtility.FromJson<GameConfig>(json) ?? new GameConfig();

// GOOD — config is trusted; if it fails, it is a bug
var config = JsonUtility.FromJson<GameConfig>(json);
if (config == null)
    throw new Exception($"GameConfig decode failed, path={path}");
```

### Untrusted Path (External Input) — Catch and Report
// 不可信路径（外部输入）—— 捕获并报告

Data that originates from outside the system boundary. Errors here are **expected and frequent**. Catch the error and report it back to the caller or user.

| Source | Examples |
|---|---|
| Network / server responses | Version manifests, hotfix resource downloads |
| User input | Player settings, chat messages |
| External file content before validation | Downloaded asset bundles, CDN resources |
| AI-generated content | Any procedurally generated data |

```csharp
// Untrusted: CDN-downloaded manifest may be corrupted or tampered
var manifest = await DownloadManifestAsync(url);
if (manifest == null || !ValidateManifest(manifest))
{
    BDebug.LogError($"版本清单校验失败, url={url}");
    OnUpdateFailed("资源更新失败，请检查网络");
    return;
}
```

### Key Principle — Persisted Data Is Trusted
// 核心原则 —— 持久化后的数据即可信数据

Strict validation at the **write boundary** (Excel→SQLite import, config file generation, resource build pipeline) ensures bad-format data never reaches storage. If malformed data is read back from SQLite or a config file, it is a **human or pipeline bug** — throw, do not defensively handle.

### Forbidden Patterns
// 禁止的模式

```csharp
// BAD — silently converts errors to null/default, hides bugs
try { DoSomethingCritical(); } catch { /* swallowed */ }
if (obj == null) return; // silently returns, no log, no error
```

The only exception: `TryGet` / `TryParse` patterns where the caller explicitly handles the "not found" case and it is a **normal business flow**, not an error.

## Naming vs Comment Language Boundary

- **File names and directory names** must use ASCII English only. No Chinese, Japanese, or other non-Latin characters.
- **C# identifiers** — class names, method names, property names, parameter names, enum values — must use English.
- **Attribute parameter default values** that serve as code-level conventions must use English (e.g. `suite: "default"`, not `"默认"`).
- **Runtime log text** may use Chinese, since it is developer-facing readable output.
- **Code comments and docstrings** must follow the bilingual Chinese-first convention from the Baseline Code Standards.
- Mnemonic: **Names in English, comments in Chinese+English, logs may be Chinese.**

## Package Independence Constraint

- Packages marked as generic (e.g. `com.talosai.e2e`) must not contain any specific business-party test cases, configurations, or hardcoded logic.
- Business-party test code must live in the business party's own package or project directory, referencing the generic package to use its capabilities.
- Test: if removing a piece of code leaves the generic package still usable by other projects, that code does not belong in the package.
- Shared startup bridges may boot generic capabilities, but they must not hardcode host-owned suite selection, scene sequencing, or business execution choreography. For Talos E2E specifically, Playwright owns what to execute; host bridges such as `ScriptLoder` may only start the generic E2E framework.

## Scope Guardrails

- Do not modify third-party packages or vendored plugin code, especially `Packages/com.code-philosophy.*`.
- Package-scoped code changes are allowed only under first-party embedded packages, currently `Packages/com.popo.bdframework` and `Packages/com.talosai.e2e`.
- If third-party behavior must change, solve it from `Packages/com.popo.bdframework`, `Packages/com.talosai.e2e`, or project-level files such as `ProjectSettings/`, not by patching the upstream package.

## Completion Checklist

Every task must pass all items below before being considered complete:

- [ ] Local tests pass (lint / unit test / smoke test)
- [ ] Changes are committed and pushed to remote
- [ ] Remote CI passes — **must actively trigger and wait, not assume auto-run**:
  1. **Judge affected BuildTypes**: based on changed files, determine which TeamCity build configurations are affected (code compilation, E2E, AssetBundle, etc.).
  2. **Trigger builds**: use the TeamCity skill (`run-build` or `run-build-group`) to trigger all affected BuildTypes on the pushed branch, with descriptive comment and tags.
  3. **Wait and verify**: use `--wait` to block until each build finishes; if any build fails, read its log tail and report the failure — do NOT mark the task as complete.
  4. **Report results**: summarize build IDs, statuses, and URLs in the completion message.
- [ ] No Chinese file names or directory names (comments and logs may use Chinese)
- [ ] C# identifiers and Attribute default parameter values use English
- [ ] Generic packages contain no business-party-specific tests or hardcoded logic

## Task Tracking (Todolist) / 任务追踪 (Todolist)

// 任务追踪规范 —— 多步骤任务必须使用 todolist 进行状态追踪
// Task tracking convention — multi-step tasks MUST use todolist for status tracking

### When to Create Todolist / 何时创建 Todolist

Create `.agent_memory/todolist.md` when:
- Task involves multiple sub-tasks that span multiple sessions
- Task requires CI validation with builds that take significant time
- Task has dependencies between steps (e.g., fix → build → test)
- User explicitly requests "until all problems resolved" (直到解决所有问题为止)

### Todolist Structure / Todolist 结构

```markdown
# [Task Name] Task List
# [任务名称] 任务列表

**Created**: YYYY-MM-DD
**Branch**: `branch-name`
**Commit**: `commit-sha`

---

## Current Status Summary / 当前状态总结

### ✅ Completed Tasks / 已完成任务
- [Task description with evidence]

### ❌ Failed Tasks / 失败任务
- [Task description with error details]

### ⏳ In Progress / 进行中
- [Current task being worked on]

---

## Pending Tasks / 待完成任务

### Task N: [Task Name] [STATUS]
**Priority**: HIGH/MEDIUM/LOW
**Dependency**: [Prerequisites if any]

**Sub-tasks**:
- [ ] Sub-task 1
- [ ] Sub-task 2

---

## Next Actions / 下一步行动
1. [Immediate next step]
```

### Todolist Maintenance Rules / Todolist 维护规则

1. **Update on every progress**: Mark tasks as completed/failed immediately after each step, not at the end
   // 每次进展后立即更新：完成或失败后立即标记，不要等到最后

2. **Include evidence**: Link to build URLs, commit SHAs, or error logs for each status change
   // 包含证据：每次状态变化都要链接到构建 URL、commit SHA 或错误日志

3. **Keep it current**: At session end, todolist must reflect actual state, not planned state
   // 保持最新：会话结束时，todolist 必须反映实际状态，而非计划状态

4. **Use bilingual headers**: All section headers must be bilingual (Chinese first, then English)
   // 使用双语标题：所有章节标题必须双语（中文在前，英文在后）

5. **Track root cause analysis**: For failed tasks, document root cause and next investigation steps
   // 追踪根因分析：对于失败任务，记录根本原因和下一步调查步骤

### Progress Update Guidelines / 进度更新规范

// 文档更新时机与清理策略 —— 确保 todolist 始终反映真实进度
// When to update and how to clean up — keep todolist reflecting actual progress

#### When to Update / 何时更新

Update `.agent_memory/todolist.md` immediately after:
// 以下情况发生后立即更新：

1. **Sub-task completion**: Any individual sub-task finishes (with evidence link)
   // 子任务完成：任何单个子任务完成时（附带证据链接）

2. **Status change**: Task status changes (not-started → in-progress → completed/failed)
   // 状态变更：任务状态变化时

3. **Error discovery**: New error or blocker discovered during execution
   // 发现错误：执行过程中发现新错误或阻塞点

4. **Build completion**: CI build finishes (success or failure)
   // 构建完成：CI 构建结束（成功或失败）

5. **Root cause identified**: Analysis reveals the underlying cause of failure
   // 根因确认：分析确定失败的根本原因

6. **Plan change**: Original plan needs adjustment based on new findings
   // 计划变更：基于新发现需要调整原计划

#### When NOT to Update / 何时不更新

Do NOT update todolist for:
// 以下情况不要更新：

- Routine progress without milestone (e.g., "reading file X", "running command Y")
  // 无里程碑的常规进度（如"正在读取文件 X"、"正在运行命令 Y"）

- Temporary failures that are immediately resolved
  // 立即解决的临时失败

- Work in progress that hasn't reached a checkpoint
  // 未到达检查点的进行中工作

#### Task Cleanup Strategy / 任务清理策略

**Major Task Completion / 大任务完成后**:

When a major task (e.g., "Fix ADB offline issue") is fully completed:
// 当大任务（如"修复 ADB offline 问题"）完全完成时：

1. **Move to "Completed Tasks" section** with:
   - Completion timestamp
   - Summary of what was done
   - Key commit SHAs or build IDs
   - Links to evidence

2. **After 2 sessions or 7 days** (whichever comes first):
   - **Remove completely** from todolist
   - Optionally archive to `/memories/repo/` if it contains valuable lessons

// 1. 移动到"已完成任务"章节，附带：完成时间戳、完成内容摘要、关键 commit SHA 或 build ID、证据链接
// 2. 2 个会话或 7 天后（以先到者为准）：从 todolist 完全移除，如有价值经验可归档到 `/memories/repo/`

**Failed Task Handling / 失败任务处理**:

When a task fails and needs further investigation:
// 当任务失败需要进一步调查时：

1. **Keep in "Failed Tasks" section** with:
   - Failure timestamp
   - Error details and root cause (if known)
   - Next investigation steps
   - Links to failed build logs

2. **When task is abandoned** (e.g., not feasible, blocked permanently):
   - Mark as `❌ ABANDONED` with reason
   - Move to bottom of "Failed Tasks" section
   - Optionally document lesson learned

3. **When task is resolved** (fixed and verified):
   - Move to "Completed Tasks" section
   - Include both the failure and the resolution

// 1. 保留在"失败任务"章节，附带：失败时间戳、错误详情和根因（如已知）、下一步调查步骤、失败构建日志链接
// 2. 当任务被放弃时（如不可行、永久阻塞）：标记为 `❌ ABANDONED` 并说明原因，移到"失败任务"章节底部，可选记录经验教训
// 3. 当任务被解决时（修复并验证）：移动到"已完成任务"章节，包含失败和解决方案

**Session End Cleanup / 会话结束清理**:

Before ending a session:
// 会话结束前：

1. **Remove trivial completed sub-tasks**: If a task has 5+ completed sub-tasks and all are trivial, consolidate into single entry
   // 移除琐碎的已完成子任务：如果任务有 5+ 个已完成的琐碎子任务，合并为单个条目

2. **Archive old completed tasks**: If "Completed Tasks" section has >5 entries, archive oldest ones to `/memories/repo/`
   // 归档旧的已完成任务：如果"已完成任务"章节超过 5 条，将最旧的归档到 `/memories/repo/`

3. **Update "Next Actions"**: Must reflect actual next step for next session
   // 更新"下一步行动"：必须反映下一次会话的实际下一步

4. **Verify bilingual headers**: All new sections must have Chinese+English headers
   // 验证双语标题：所有新章节必须有中英文标题

#### Update Granularity / 更新粒度

**Minimum update unit / 最小更新单元**:
- One sub-task (e.g., "Check MuMu emulator Android boot logs")
- One status change (e.g., in-progress → completed)
- One evidence attachment (e.g., build URL, commit SHA)

**Maximum update delay / 最大更新延迟**:
- Immediate for CI build results
- Within 5 minutes for manual task completion
- Before session ends for all pending changes

// 最小更新单元：一个子任务、一个状态变更、一个证据附件
// 最大更新延迟：CI 构建结果立即更新、手动任务完成 5 分钟内更新、会话结束前更新所有待更改

### Session Continuity / 会话连续性

When a new session starts:
1. Check if `.agent_memory/todolist.md` exists
2. Read todolist to understand current state
3. Continue from "Next Actions" section
4. Update todolist as work progresses

// 新会话开始时：
// 1. 检查 `.agent_memory/todolist.md` 是否存在
// 2. 读取 todolist 了解当前状态
// 3. 从 "Next Actions" 章节继续
// 4. 随着工作进展更新 todolist