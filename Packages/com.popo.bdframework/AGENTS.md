
# BDFramework 包级规则

作用域：`Packages/com.popo.bdframework/` 整个子树。

阅读顺序：
1. `.github/copilot-instructions.md`
2. `AI_RULES_INDEX.md`
3. `.github/instructions/bdframework.instructions.md`
4. 本文件
5. 当前功能附近的文档、测试与实现文件

说明：
- 仓库级强制规范以 `.github/copilot-instructions.md` 为准。
- 本文件只补充 BDFramework 包内实现、测试与 BatchMode 约束，不再承载仓库级镜像内容，也不再承载 Talos E2E 包规则。

---

## 1. 改动总原则

1. 只做最小必要改动，避免无关重构、无关格式化、无关重命名。
2. 每次改动都同时检查 `Editor`、`Player`、`BatchMode/CI` 三个场景。
3. 遇到平台差异，显式使用：
   - `#if UNITY_EDITOR`
   - `#if !UNITY_EDITOR`
   - `Application.isPlaying`
   - `Application.isEditor`
   - `Application.isBatchMode`
4. 改动完成后，要把经验沉淀到测试、注释或本文件，而不是只留在会话里。

---

## 2. 编码规范

### 2.1 目录与边界

- Runtime 代码放 `Runtime/`、`Runtime.AOT/`
- Editor 专属代码放 `Editor/`
- 测试代码放 `Runtime.Test/`，且与被测模块保持**相对目录一致**
- 不把 Editor API 混入 Runtime 程序集；确需共存时必须用条件编译隔离

### 2.2 代码要求

- 保持现有代码风格与命名，不做大面积重排
- 优先复用已有实现，不重复造轮子
- 修改公共 API 前，先检查调用链、序列化字段、Inspector、反射使用点
- 涉及线程、文件 IO、持久化时，必须考虑并发安全、重入安全、异常策略、资源释放
- 临时调试逻辑不要直接提交成正式行为

### 2.3 注释要求

注释只写必要信息，重点说明“为什么这样做”，不要重复代码字面意思。

以下场景必须有说明性注释：
- 公开类型、关键流程
- `Editor` / `Player` 职责边界
- 条件编译分支
- 反射调用
- 文件格式、序列化协议、加密逻辑
- 必须按固定顺序执行的初始化 / 销毁逻辑

禁止保留失效注释、误导性注释、与实现不一致的注释。

---

## 3. 测试规范

### 3.1 通用要求

- 任何非纯样式改动，原则上都要补测试或更新现有测试
- 优先补最贴近变更点的测试，不只做端到端冒烟
- 测试命名要直接体现行为与预期
- 涉及磁盘读写必须使用临时目录，并在测试结束后清理

### 3.2 单元测试

适用于：纯函数、路径处理、配置归一化、序列化协议、加解密、排序/清理逻辑。

要求：
- 一个测试只验证一个核心行为
- 输入和断言必须具体，不能只验证“不报错”
- 时间、路径、随机数、外部环境尽量做隔离
- 私有逻辑优先重构为可测接口；确需反射测试时，范围要小且意图明确

### 3.3 集成测试

适用于：Unity 生命周期联动、文件落盘、批量导出、Editor Hook / Player Hook 分工、跨模块协作。

要求：
- 覆盖真实交互路径，不重复单元测试
- 测试名和断言要体现 `Editor` / `Player` 运行环境差异
- 至少覆盖成功路径、关键异常路径、边界条件

### 3.4 日志模块测试模板

日志相关改动，优先参考并维护：
- `Runtime.Test/Editor/Utils/Logs/LogCryptoAndReaderTests.cs`
- `Runtime.Test/Editor/Utils/Logs/PersistenceAndBDebugTests.cs`

最少覆盖：
- 明文记录读取
- 加密记录读取
- 错误密码失败路径
- 文本导出成功路径
- 超过上限时删除最旧文件
- 当前活跃文件不被误删

---

## 4. BatchMode 规范

1. 自动化验证优先使用 Unity Test Framework + **Unity BatchMode**，不要用自定义 runner 替代标准流程。
2. 推荐参数：
   - `-batchmode`
   - `-projectPath`
   - `-runTests`
   - `-testPlatform EditMode`
   - `-assemblyNames <AssemblyName>`
   - `-testResults <Path>`
   - `-logFile <Path>`
   - `-quit`
3. 如果项目已在 Unity 中打开，不要直接对同一工程再启动第二个 Unity 实例；必要时使用临时克隆目录。
4. BatchMode 代码必须兼容非交互环境：
   - 不无条件弹窗
   - 不阻塞等待人工点击
   - 初始化逻辑显式处理 `Application.isBatchMode`
   - 不依赖当前打开场景、手工 Inspector 状态、本地临时缓存
5. BatchMode 失败时，优先记录并检查：
   - 编译错误
   - 程序集缺失
   - 测试程序集未发现
   - 项目锁
   - Unity 日志关键片段与退出码

---

## 5. 本地日志模块规范（`Runtime/Utils/Logs/`）

修改以下模块时，必须整体看待：
- `BDebug`
- `Editor_UnityLogHook`
- `Persistence`
- `PersistenceSettings`
- `LogReader`
- `LogCrypto`
- `SerializedLogEntry`

### 5.1 职责边界

- `Editor` 下：`BDebug` 负责常规 Console 输出；日志序列化由 `Editor_UnityLogHook` 负责
- `Player` / 真机下：`BDebug` 负责自身二进制日志持久化；不依赖 `Editor_UnityLogHook`
- 不允许把 Editor 序列化逻辑误带到 Player，也不允许把 Player 持久化副作用带到 Editor

### 5.2 Player 日志规则

- Player 侧日志序列化默认开启
- 是否加密可配置；修改加密开关或密码时，要同时验证写入、读取、错误密码失败路径
- `playerlogs/` 默认最多保留 20 份归档
- 清理策略按时间戳排序，删除最旧文件，同时避免误删当前活跃文件
- 修改文件命名、格式头、记录结构时，必须同步更新读取逻辑、导出逻辑和对应测试

### 5.3 日志模块交付检查

- [ ] Editor Console 行为未被破坏
- [ ] Editor 与 Player 的序列化职责未混淆
- [ ] Player 初始化 / Flush / Shutdown 生命周期正确
- [ ] 加密与非加密路径都至少验证一条
- [ ] 文本导出可用
- [ ] 保留数量与清理顺序正确
- [ ] 对应单元测试 / 集成测试已补齐
- [ ] BatchMode 下可执行，或至少能稳定定位失败原因

---

## 6. 提交前清单

- [ ] 只修改了 `Packages/com.popo.bdframework`
- [ ] 已识别 `Editor` / `Player` / `BatchMode` 差异
- [ ] 注释与实现一致
- [ ] 无无关重构
- [ ] 新增或更新测试放在相对一致目录
- [ ] 临时文件、日志、导出路径可控
- [ ] 相关程序集可编译
- [ ] 相关测试已执行，或至少完成最小可验证检查
- [ ] 模块边界未被破坏

