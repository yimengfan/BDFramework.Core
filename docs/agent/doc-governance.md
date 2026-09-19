# 文档治理

本仓库有两套文档体系，各自有明确的边界与维护约定。

## 分层结构

| 层级 | 载体 | 位置 | 加载方式 | 预算 |
|------|------|------|---------|------|
| **L0** 全局根规范 | `copilot-instructions.md` | `.github/` | 始终 | ≤1000 行 |
| **L1** 文件级编码约束 | `*.instructions.md` | `.github/instructions/` | `applyTo` 自动匹配 | ≤3000 行 |
| **L2** 包架构入口 | `AGENTS.md` | 仓库根、package 根、业务模块根 | 按需 | ≤3000 行 |
| **L2.5** Skill | `SKILL.md` | `.github/skills/<name>/` | 按需（模型判断） | 保持精简 |
| **L3** 临时记忆 | `*.md` | `.agent_memory/` | 任务触发 | — |
| **发布文档** | `*.md` | `docs/`（本站） | 用户浏览 | — |

## 创建决策树

```text
要创建文档 →
├─ 全局工作链路 / 质量门禁 / 跨模块约束？  → .github/copilot-instructions.md
├─ 编辑某类文件的编码规范？                → .github/instructions/<name>.instructions.md
│                                             （必须含 applyTo + description + 实质规则）
├─ 包架构理解 / 用法排障？                 → AGENTS.md（仓库根 / package 根）
├─ 某模块的领域知识 / API 速查 / 工作流？   → .github/skills/<name>/SKILL.md
├─ 深度模块规则 / 行为矩阵？               → 该 skill 的 references/ 或 talos-docs/modules/
├─ 面向人的功能文档 / 教程？               → docs/<分区>/<page>.md
├─ 临时状态 / 代码异味？                   → .agent_memory/
└─ 以上都不是 → 不创建
```

## 禁止事项

| 禁止 | 原因 |
|------|------|
| 在 package 更深子目录新增 `AGENTS.md` | 层级过深，无加载机制 |
| 在业务代码目录新增 `*.instructions.md` | 应放 `.github/instructions/` |
| 在 `.github/` 根目录新增 `.md`（除 `copilot-instructions.md`） | 根目录只放唯一入口 |
| `.agent_memory/` 引用永久文档 | 临时记忆不应成为依赖 |
| 模块深度文档引用 `AGENTS.md` | 方向应是从上到下 |
| 把临时任务细节写进永久文档 | 会腐化规则 |
| 新增模块规则却不更新引用它的 instruction | 规则失效 |

## 变更同步

以下变化时**必须在同一次改动中**更新所有受影响文档：

| 变化 | 需同步 |
|------|--------|
| 入口 / 命令参数 | 对应 `docs/pipeline/**`、相关 skill |
| 输出布局 / 上传协议 | `docs/pipeline/publish-assets.md`、`docs/guide/asset-load-path.md` |
| 测试命令 | `docs/testing/**` |
| CI 日志格式 | `docs/testing/**`、`docs/pipeline/devops-ci.md` |
| 模块归属 | 相关 `AGENTS.md` |
| 程序集依赖 | `docs/architecture/assemblies.md`、`Packages/*/AGENTS.md` |
| 目录结构 / 命名空间 | 模块 `AGENTS.md`、相关 skill |
| 新模块上线 | 根 `AGENTS.md` 路由表、`docs/architecture/*-modules.md` |
| 模块移除 | 删除对应 `AGENTS.md` 并从路由表移除 |

## 验证

```bash
# 检查废弃路径/术语
rg "<废弃路径或术语>" docs/ .github/

# 检查空白错误
git diff --check

# 文档站点构建（会把断链升级为错误）
mkdocs build --strict
```

!!! tip "`mkdocs build --strict` 是文档改动的质量门禁"
    它会把**断链、缺失 nav 目标**等 warning 升级为 error。文档改动提交前必须跑一次。

## 本地开发

```bash
# 首次
python -m pip install -r requirements-docs.txt

# 实时预览（http://127.0.0.1:8000）
mkdocs serve

# 构建
mkdocs build --strict
```

## 中英双语

站点通过 `mkdocs-static-i18n` 提供中英两个语言，两者**共用一份 `nav`**，不需要维护两份 `mkdocs.yml`。

| 项 | 约定 |
|----|------|
| 默认语言 | 中文，文件在 `docs/<分区>/<page>.md` |
| 英文译文 | `docs/en/<分区>/<page>.md`（路径与中文**逐级对应**） |
| 回退 | `fallback_to_default: true`——未翻译的页面自动显示中文，**英文站不会出现 404** |
| 标题翻译 | `mkdocs.yml` 的 `languages[en].nav_translations`，按中文标题映射 |
| 主题与搜索 | `reconfigure_material` / `reconfigure_search` 按 locale 自动切换 |
| `site_name` | 保持语言中性（`BDFramework`），中英共用 |

翻译约定：

- **正文全译，代码块逐字保留**。代码要与仓库实际代码一致，因此代码里的注释仍是中文优先（仓库源码注释规范），英文页首页已就此说明。
- 相对链接**原样保留**即可：`docs/en/` 与 `docs/` 目录层级完全对应，`../api/x.md` 两边都成立。
- Mermaid 图的**节点标签属于正文**，应当翻译。

当前英文翻译覆盖 **54 / 54** 页；新增页面时**先写中文**，英文可后续补，回退机制保证不会断链。

!!! danger "`docs/` 顶层目录名不得是 2 个小写字母"
    `mkdocs-static-i18n` 用 `RE_LOCALE = ^[a-z]{2}(-[A-Za-z]{4})?(-[A-Z]{2})?$` 猜测语言目录。
    命中时该目录下**所有文件**都会被归到另一种语言，从**所有**语言的构建里静默消失——
    只报“链接目标不存在”的间接警告，不报目录被丢弃。

    这就是 UI 分区目录叫 `uflux/` 而不是 `ui/` 的原因（`ui` 恰好是维吾尔语的 ISO 639-1 代码）。
    同类高风险名字：`en`、`fr`、`de`、`ja`、`ko`、`it`、`id`、`ml`、`ms`、`or`、`as`、`am` 等。

## 发布

文档通过 GitHub Actions 发布到 GitHub Pages，工作流见 `.github/workflows/docs.yml`：

| 触发 | 条件 |
|------|------|
| push | `master` / `v4/v-4.0.0` 分支，且改动涉及 `docs/**`、`mkdocs.yml`、`requirements-docs.txt`、workflow 自身 |
| 手动 | `workflow_dispatch`（可选是否发布） |

发布方式使用官方 Pages Actions（`upload-pages-artifact` + `deploy-pages`），**不使用 `mkdocs gh-deploy`**，避免向 `gh-pages` 分支写构建产物。

workflow 里的 `actions/configure-pages@v5` 带 `enablement: true`，会把仓库的 Pages 源
从 “Deploy from a branch”（`master` 根目录，由 Jekyll 渲染 `README.md`）自动改为 “GitHub Actions”
（`build_type: workflow`）。本仓库已完成该切换。

!!! info "一次性仓库设置：允许 `v4/v-4.0.0` 发布（已配置）"
    `github-pages` 环境的 `deployment_branch_policy` 默认为 `null`，此时 GitHub
    **只允许默认分支（`master`）**发起 Pages 部署。本仓库文档在 `v4/v-4.0.0` 上维护，
    未配置时会以如下错误失败：

    ```text
    Invalid deployment branch and no branch protection rules set in the environment.
    Deployments are only allowed from master
    ```

    本仓库已完成该配置：`deployment_branch_policy.custom_branch_policies = true`，
    并添加了分支规则 `v4/v-4.0.0`。**Fork 或重建仓库时需要重新配置一次**，
    该策略只能由**仓库管理员**修改，`GITHUB_TOKEN` 无权限：

    - **UI**：Settings → Environments → `github-pages` → Deployment branches and tags
      → 选 “All branches”，或 Add deployment branch rule 添加 `v4/v-4.0.0`
    - **API**：用带 admin 权限的 PAT 调
      `PUT /repos/{owner}/{repo}/environments/github-pages`
      （`deployment_branch_policy.custom_branch_policies = true`），
      再 `POST .../deployment-branch-policies` 添加 `v4/v-4.0.0`

    设置完成后重新运行 workflow 即可。workflow 在部署失败步骤里会打印同样的指引。

## 归档

`docs/archive/wolai/**` 是迁移前的原始 Wolai 导出（89 篇 md + 约 10 MB 图片 + 19 MB 附件），**通过 `exclude_docs` 排除在站点构建之外**。

它保留的原因是：
- 部分内容（开发计划、CI 建议、历史设计讨论）有史料价值
- 迁移对照可追溯（见[归档说明](../archive/index.md)）

**归档内容不再维护**。需要引用其中的信息时，应改写进正式文档。

## 相关页面

- [Agent 集成](index.md)
- [Skill 索引](skills.md)
- [归档](../archive/index.md)
- [结构问题与重构清单](../architecture/refactor-backlog.md)
