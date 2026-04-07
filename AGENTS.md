# Repository Rules

- Do not modify third-party packages or vendored plugin code, especially `Packages/com.code-philosophy.*`.
- Package-scoped code changes are allowed only under `Packages/com.popo.bdframework`.
- If third-party behavior must change, solve it from `Packages/com.popo.bdframework` or project-level files such as `ProjectSettings/`, not by patching the upstream package.

