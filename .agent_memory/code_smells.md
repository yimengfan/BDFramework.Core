# Code Smell 追踪
// Code Smell Tracking — max 10 entries, FIFO eviction when full.

| # | 文件路径 | 行号 | 违反规则 | 简述 |
|---|---|---|---|---|
| 1 | Runtime/ScreenNavigation/ScreenViewCenter.cs | ~150 | 可信路径边界保护不完整 | BeginNavBack 仅检查 currentViewIndex==0，不检查 -1，导致 BeginNavTo 后调用 Back 会访问 navViews[-1] 抛 ArgumentOutOfRangeException |
| 2 | Runtime/UI/View/ComponentBindAdaptor/Manager/ComponentBindAdaptorManager.cs | ~AnalysisRenderDataChanged | 可信路径静默吞掉 null 变更 | auto 模式下 newFieldValue==null 时 continue，跳过 value→null 的变更通知 |
