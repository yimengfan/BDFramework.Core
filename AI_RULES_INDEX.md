# AI Rules Index

## Managed Files

1. `.github/copilot-instructions.md`
   Project-level mandatory workspace instructions for GitHub Copilot.
2. `Packages/com.popo.bdframework/.talos/AGENTS.md`
   Talos-side mirror of the same rule set for the BDFramework package.
3. `.github/instructions/ci.instructions.md`
   Module-scoped Copilot instructions that auto-attach when editing `DevOps/CI/**`.
   Contains scope, reading order, CI-specific rules, and validation entry.
4. `.github/instructions/bdframework.instructions.md`
   Module-scoped Copilot instructions that auto-attach when editing `Packages/com.popo.bdframework/**`.
   Contains scope, reading order, package-specific rules, and validation entry.

## Module Instructions

- `.github/instructions/ci.instructions.md` governs the CI Python and TeamCity working area.
- `.github/instructions/bdframework.instructions.md` governs the BDFramework package working area.
- Module instructions contain their own scope, reading order, rules, and validation entry sections.
- Module-specific conventions live directly in the corresponding `.github/instructions/*.instructions.md` file.
- Module instructions may add narrower requirements, but they must not weaken the root mandatory rules.

## Sync Policy

- These two files must stay semantically aligned: `.github/copilot-instructions.md` and `Packages/com.popo.bdframework/.talos/AGENTS.md`.
- Any rule addition, deletion, rename, or wording change must be applied to both files in the same commit or change set.
- If one file needs tool-specific wording, keep the behavioral requirement identical and note the divergence inline.
- If root rule paths, scope boundaries, or baseline conventions change, update the affected module instruction files in the same change.

## Update Checklist

1. Edit `.github/copilot-instructions.md`.
2. Mirror the same rule change into `Packages/com.popo.bdframework/.talos/AGENTS.md`.
3. Update any affected module instruction files under `.github/instructions/`.
4. Verify the file paths in all rule files still point to the correct locations.
5. If sync policy changes, update this index in the same change.