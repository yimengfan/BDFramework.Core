# Documentation Governance

This repository has two documentation systems, each with clearly defined boundaries and maintenance conventions.

## Layer structure

| Layer | Carrier | Location | How it is loaded | Budget |
|-------|---------|----------|------------------|--------|
| **L0** Global root rules | `copilot-instructions.md` | `.github/` | Always | ≤1000 lines |
| **L1** File-level coding constraints | `*.instructions.md` | `.github/instructions/` | `applyTo` automatic matching | ≤3000 lines |
| **L2** Package architecture entry | `AGENTS.md` | Repository root, package root, business module root | On demand | ≤3000 lines |
| **L2.5** Skill | `SKILL.md` | `.github/skills/<name>/` | On demand (the model decides) | Keep it lean |
| **L3** Scratch memory | `*.md` | `.agent_memory/` | Task-triggered | — |
| **Published documentation** | `*.md` | `docs/` (this site) | The user browses it | — |

## Creation decision tree

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

## Prohibitions

| Prohibited | Reason |
|------------|--------|
| Adding an `AGENTS.md` in a deeper package subdirectory | The level is too deep; there is no loading mechanism |
| Adding a `*.instructions.md` under a business code directory | It belongs in `.github/instructions/` |
| Adding a `.md` at the root of `.github/` (other than `copilot-instructions.md`) | The root directory holds the single entry point only |
| `.agent_memory/` referencing permanent documents | Scratch memory must not become a dependency |
| Module-deep documentation referencing `AGENTS.md` | The direction should be top-down |
| Writing temporary task details into permanent documents | It rots the rules |
| Adding module rules without updating the instructions that reference it | The rule stops taking effect |

## Change synchronisation

When any of the following changes, every affected document **must be updated in the same change**:

| Change | Must be synchronised |
|--------|---------------------|
| Entry points / command parameters | The corresponding `docs/pipeline/**` and the related skill |
| Output layout / upload protocol | `docs/pipeline/publish-assets.md`, `docs/guide/asset-load-path.md` |
| Test commands | `docs/testing/**` |
| CI log format | `docs/testing/**`, `docs/pipeline/devops-ci.md` |
| Module ownership | The related `AGENTS.md` |
| Assembly dependencies | `docs/architecture/assemblies.md`, `Packages/*/AGENTS.md` |
| Directory structure / namespaces | The module's `AGENTS.md` and the related skill |
| A new module going live | The root `AGENTS.md` routing table, `docs/architecture/*-modules.md` |
| A module being removed | Delete its `AGENTS.md` and remove it from the routing table |

## Verification

```bash
# 检查废弃路径/术语
rg "<废弃路径或术语>" docs/ .github/

# 检查空白错误
git diff --check

# 文档站点构建（会把断链升级为错误）
mkdocs build --strict
```

!!! tip "`mkdocs build --strict` is the quality gate for documentation changes"
    It promotes warnings such as **broken links and missing nav targets** to errors. Run it once before committing any documentation change.

## Local development

```bash
# 首次
python -m pip install -r requirements-docs.txt

# 实时预览（http://127.0.0.1:8000）
mkdocs serve

# 构建
mkdocs build --strict
```

## Chinese and English

The site provides both Chinese and English through `mkdocs-static-i18n`, and the two **share a single `nav`** — there is no need to maintain two `mkdocs.yml` files.

| Item | Convention |
|------|------------|
| Default language | Chinese, in `docs/<section>/<page>.md` |
| English translation | `docs/en/<section>/<page>.md` (the path **corresponds level by level** with the Chinese one) |
| Fallback | `fallback_to_default: true` — untranslated pages automatically display Chinese, so **the English site never hits a 404** |
| Title translation | `languages[en].nav_translations` in `mkdocs.yml`, mapped from the Chinese titles |
| Theme and search | `reconfigure_material` / `reconfigure_search` switch automatically per locale |
| `site_name` | Kept language-neutral (`BDFramework`), shared by both languages |

Translation conventions:

- **Translate all prose; keep code blocks verbatim.** Code has to match the repository's actual code, so the comments inside it stay Chinese-first (the repository's source-comment convention); the home page of the English site already states this.
- Relative links can be **kept as they are**: the directory levels of `docs/en/` and `docs/` correspond exactly, so `../api/x.md` resolves in both.
- The **node labels of Mermaid diagrams count as prose** and should be translated.

The English translation currently covers **54 / 54** pages; when adding a page, **write the Chinese first** — the English can follow later, and the fallback mechanism guarantees no broken links.

!!! danger "A `docs/` top-level directory name must not be two lowercase letters"
    `mkdocs-static-i18n` guesses language directories with `RE_LOCALE = ^[a-z]{2}(-[A-Za-z]{4})?(-[A-Z]{2})?$`.
    When it matches, **every file** under that directory is assigned to another language and silently disappears from the build of **all** languages —
    you only get the indirect warning "link target does not exist", never a warning that the directory was dropped.

    That is why the UI section directory is called `uflux/` and not `ui/` (`ui` happens to be the ISO 639-1 code for Uyghur).
    Names carrying the same risk: `en`, `fr`, `de`, `ja`, `ko`, `it`, `id`, `ml`, `ms`, `or`, `as`, `am` and so on.

## Publishing

The documentation is published to GitHub Pages through GitHub Actions; the workflow is `.github/workflows/docs.yml`:

| Trigger | Condition |
|---------|-----------|
| push | The `master` / `v4/v-4.0.0` branches, when the change touches `docs/**`, `mkdocs.yml`, `requirements-docs.txt` or the workflow itself |
| Manual | `workflow_dispatch` (with an optional choice of whether to publish) |

Publication uses the official Pages Actions (`upload-pages-artifact` + `deploy-pages`) and **does not use `mkdocs gh-deploy`**, which avoids writing build artifacts into the `gh-pages` branch.

The workflow's `actions/configure-pages@v5` carries `enablement: true`, so on its first run it automatically switches the repository's Pages source
from "Deploy from a branch" (the `master` root, with Jekyll rendering `README.md`) to "GitHub Actions".

!!! warning "A one-time repository setting: allowing `v4/v-4.0.0` to publish"
    The `github-pages` environment's `deployment_branch_policy` defaults to `null`, and in that state GitHub
    **only allows the default branch (`master`)** to start a Pages deployment. This repository maintains its documentation on `v4/v-4.0.0`,
    so the first publish fails with the following error:

    ```text
    Invalid deployment branch and no branch protection rules set in the environment.
    Deployments are only allowed from master
    ```

    Only a **repository administrator** can change that policy; `GITHUB_TOKEN` does not have the permission. Pick one of the two:

    - **UI**: Settings → Environments → `github-pages` → Deployment branches and tags
      → choose "All branches", or use Add deployment branch rule to add `v4/v-4.0.0`
    - **API**: call `PUT /repos/{owner}/{repo}/environments/github-pages` with
      an admin-scoped PAT (`deployment_branch_policy.custom_branch_policies = true`),
      then `POST .../deployment-branch-policies` to add `v4/v-4.0.0`

    Once the setting is in place, just re-run the workflow. The workflow prints the same guidance in its deploy-failure step.

## Archive

`docs/archive/wolai/**` is the raw Wolai export from before the migration (89 md files + roughly 10 MB of images + 19 MB of attachments), **excluded from the site build through `exclude_docs`**.

The reasons it is kept:
- Part of it (development plans, CI suggestions, historical design discussions) has archival value
- The migration mapping stays traceable (see [Archive](../archive/index.md))

**The archived content is no longer maintained.** When you need to reference information from it, rewrite that information into the formal documentation.

## Related pages

- [Agent](index.md)
- [Skill Index](skills.md)
- [Archive](../archive/index.md)
- [Structure Issues & Refactor Backlog](../architecture/refactor-backlog.md)
