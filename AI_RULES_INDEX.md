# AI Rules Index

## Managed Files

1. `.github/copilot-instructions.md`
   Project-level mandatory workspace instructions for GitHub Copilot.
2. `Packages/com.popo.bdframework/.talos/AGENTS.md`
   Talos-side mirror of the same rule set for the BDFramework package.
3. `.github/instructions/ci.instructions.md`
   Module-scoped Copilot instructions that auto-attach when editing `DevOps/CI/**`.
4. `.github/instructions/bdframework.instructions.md`
   Module-scoped Copilot instructions that auto-attach when editing `Packages/com.popo.bdframework/**`.
5. `DevOps/CI/AI_RULES_INDEX.md`
   CI module rules index and reading order for BuildTools, pytest, and TeamCity-related work.
6. `Packages/com.popo.bdframework/AI_RULES_INDEX.md`
   BDFramework package module rules index and reading order for runtime, editor, AOT, and Unity test work.

## Module Indexes

- `DevOps/CI/AI_RULES_INDEX.md` governs the CI Python and TeamCity working area.
- `Packages/com.popo.bdframework/AI_RULES_INDEX.md` governs the BDFramework package working area.
- The root mirror pair only holds public baseline rules and repository-wide guardrails.
- Module-specific conventions must live in `.github/instructions/*.instructions.md` and the corresponding module index files.
- Module indexes may add narrower reading order and validation requirements, but they must not weaken the root mandatory rules.

## Sync Policy

- These two files must stay semantically aligned.
- Any rule addition, deletion, rename, or wording change must be applied to both files in the same commit or change set.
- If one file needs tool-specific wording, keep the behavioral requirement identical and note the divergence inline.
- If root rule paths, scope boundaries, or baseline conventions change, update the affected module instruction files and module indexes in the same change.

## Update Checklist

1. Edit `.github/copilot-instructions.md`.
2. Mirror the same rule change into `Packages/com.popo.bdframework/.talos/AGENTS.md`.
3. Update any affected module instruction files under `.github/instructions/`.
4. Update any affected module indexes under `DevOps/CI/` and `Packages/com.popo.bdframework/`.
5. Verify the file paths in all rule files still point to the correct indexes.
6. If sync policy changes, update this index in the same change.