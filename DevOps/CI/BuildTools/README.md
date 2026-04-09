# BuildTools

`DevOps/CI/BuildTools/` 是 CI Python 脚本、公共模块和对应测试的收敛目录。

公共规范、阅读顺序和跨模块验证要求统一维护在 [../README.md](../README.md)。本文只保留 `BuildTools/` 自身的目录索引，避免和 CI 根文档重复。

## 目录索引

| 目录 | 职责 | 模块文档 |
| --- | --- | --- |
| `BuildClientPackage/` | Unity 母包构建入口 | `BuildClientPackage/README.md` |
| `BuildClientResCode/` | 三端热更代码构建入口 | `BuildClientResCode/README.md` |
| `BuildClientResAssetbundle/` | 三端热更 Assetbundle 构建入口 | `BuildClientResAssetbundle/README.md` |
| `BuildClientResTable/` | 统一表格构建入口 | `BuildClientResTable/README.md` |
| `Common/` | 公共上传模块与配置解析 | `Common/README.md` |
| `tests/` | BuildTools 对应 pytest 测试集合 | 按模块 README 中的测试命令执行 |

## 使用方式

1. 先读 [../README.md](../README.md)。
2. 再进入具体模块 README。
3. 改动完成后，按模块 README 执行 pytest、smoke test 或 TeamCity 验证。
4. 新增构建类型时，先在 `BuildTools/` 下建独立目录，再决定是否把共享逻辑下沉到 `Common/`；不要把不同业务类型继续堆进已有目录。