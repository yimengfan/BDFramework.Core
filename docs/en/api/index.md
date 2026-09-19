# Runtime API

Public API reference for the `BDFramework.Core` assembly.

| Page | Module | Key types |
|------|--------|-----------|
| [Managers (ManagerBase)](manager-base.md) | `Utils/ManagerBase/` | `ManagerBase<T,V>`, `ManagerInstHelper` |
| [Assets (BResources)](resources.md) | `AssetsManager/` | `BResources`, `IResMgr`, `AssetsVersionController` |
| [Tables (SQLite)](sqlite.md) | `HotfixData/Sql/` | `SqliteLoder`, `SqliteHelper`, `TableQueryForILRuntime` |
| [Event Bus](event-bus.md) | `EventBus/` | `AStatusListener`, `ADataListenerT<T>`, `StatusListenerServer` |
| [Configuration (GameConfig)](game-config.md) | `GameConfig/` | `GameConfigLoder`, `GameConfigManager`, `IConfigProcessor` |
| [Screen Navigation](screen-navigation.md) | `Navigation/` | `ScreenViewManager`, `ScreenViewLayer`, `IScreenView` |
| [Service Container & Logging](utils.md) | `Service/`, `Utils/` | `ServiceContainer`, `GameServiceStore`, `BDebug`, `ObjectPool<T>` |

!!! tip "Namespace quick reference (the easiest places to trip up)"
    | Type | Namespace | Pitfall |
    |------|-----------|---------|
    | `ManagerBase<T,V>` / `ManagerAttribute` / `ManagerInstHelper` | `BDFramework.Mgr` | Not `BDFramework` |
    | `GameConfigLoder` | `BDFramework.Configure` | Not `BDFramework` |
    | `GameBaseConfigProcessor` | `BDFramework.Configure` | Same as above |
    | `Store<S>` / `StoreFactory` | `BDFramework.UFlux.Contains` | Not `BDFramework.UFlux` |
    | `Singleton<T>` | `BDFramework.ResourceMgr` | Not `BDFramework.Utils` |
    | `IPath` / `FileHelper` | `System.IO` | **A deliberate namespace hijack** |
    | `BDebug` / `IEnumeratorTool` | global (no namespace) | Cannot be narrowed with `using` |
    | `SqliteHelper` | `BDFramework.Sql` | |
    | `TableQueryForILRuntime` | `SQLite4Unity3d` | The name is a legacy leftover |
    | `AReducers<T>` / `UFluxAction` | `BDFramework.UFlux.Reducer` | |
    | `BResources` | `BDFramework.ResourceMgr` | |
    | `StatusListenerServer` / `AStatusListener` | `BDFramework.DataListener` | |
    | `ServiceContainer` / `GameServiceStore` | `BDFramework.GameServiceStore` | |
