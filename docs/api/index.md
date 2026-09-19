# Runtime API

`BDFramework.Core` 程序集的公开 API 参考。

| 页面 | 模块 | 关键类型 |
|------|------|---------|
| [管理器体系 ManagerBase](manager-base.md) | `Utils/ManagerBase/` | `ManagerBase<T,V>`、`ManagerInstHelper` |
| [资源加载 BResources](resources.md) | `AssetsManager/` | `BResources`、`IResMgr`、`AssetsVersionController` |
| [表格 SQLite](sqlite.md) | `HotfixData/Sql/` | `SqliteLoder`、`SqliteHelper`、`TableQueryForILRuntime` |
| [事件总线 EventBus](event-bus.md) | `EventBus/` | `AStatusListener`、`ADataListenerT<T>`、`StatusListenerServer` |
| [配置中心 GameConfig](game-config.md) | `GameConfig/` | `GameConfigLoder`、`GameConfigManager`、`IConfigProcessor` |
| [屏幕导航 ScreenView](screen-navigation.md) | `Navigation/` | `ScreenViewManager`、`ScreenViewLayer`、`IScreenView` |
| [服务容器与日志](utils.md) | `Service/`、`Utils/` | `ServiceContainer`、`GameServiceStore`、`BDebug`、`ObjectPool<T>` |

!!! tip "命名空间速查（最容易踩的几处）"
    | 类型 | 命名空间 | 坑 |
    |------|---------|-----|
    | `ManagerBase<T,V>` / `ManagerAttribute` / `ManagerInstHelper` | `BDFramework.Mgr` | 不是 `BDFramework` |
    | `GameConfigLoder` | `BDFramework.Configure` | 不是 `BDFramework` |
    | `GameBaseConfigProcessor` | `BDFramework.Configure` | 同上 |
    | `Store<S>` / `StoreFactory` | `BDFramework.UFlux.Contains` | 不是 `BDFramework.UFlux` |
    | `Singleton<T>` | `BDFramework.ResourceMgr` | 不是 `BDFramework.Utils` |
    | `IPath` / `FileHelper` | `System.IO` | **刻意的命名空间劫持** |
    | `BDebug` / `IEnumeratorTool` | 全局（无 namespace） | 无法用 `using` 收窄 |
    | `SqliteHelper` | `BDFramework.Sql` | |
    | `TableQueryForILRuntime` | `SQLite4Unity3d` | 名字是历史遗留 |
    | `AReducers<T>` / `UFluxAction` | `BDFramework.UFlux.Reducer` | |
    | `BResources` | `BDFramework.ResourceMgr` | |
    | `StatusListenerServer` / `AStatusListener` | `BDFramework.DataListener` | |
    | `ServiceContainer` / `GameServiceStore` | `BDFramework.GameServiceStore` | |
