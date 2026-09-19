# Editor Http Server

The framework starts an `HttpListener` inside the Editor, exposes two built-in web APIs, and supports custom protocol handlers.

## Ports

| Priority | Port |
|----------|------|
| 1 | **9999** |
| 2 | 9998 |
| 3 | 9997 |
| 4 | 9996 |

It tries 9999 first and falls back to the next port when the current one is taken.

## Built-in handlers

### `WP_EditorInvoke` — execute arbitrary Editor functions

Invokes any public static method inside the Editor through an HTTP request. Used for:
- External tools/scripts triggering Editor operations
- CI driving the Editor without launching Unity
- Triggering functionality remotely while debugging

!!! danger "`WP_EditorInvoke` can execute arbitrary Editor functions"
    This is a **development-time tool** — do not expose this port in a production environment or on an untrusted network. When the port is bound to the local machine only, the risk is contained.

### `WP_LocalABFileServer` — local AB file server

Serves the local `DevOps/PublishAssets` directory as a file server, **so a device can download assets directly for verification** without deploying a remote server.

Typical usage:

```text
① 编辑器内构建资源（DevOps/PublishAssets 就绪）
② 启动本机文件服务器（WP_LocalABFileServer）
③ 真机把 serverUrl 指向 <本机IP>:<port>
④ 真机走完整的热更下载流程
```

Used together with `EditorWindow_PublishAssets.OnGUI_PublishEditorService()`.

## Custom protocol handlers

```csharp
public interface IEditorWebApiProcessor
{
    // 自定义请求处理
}
```

Implement this interface and register it to extend the server with your own HTTP protocol.

## Related menus

| Menu | Purpose |
|------|---------|
| `BDFrameWork工具箱/PublishPipeline/1.发布资源` | Asset publishing window, including publishing to the Editor service |

## Common failures

| Symptom | Root cause |
|---------|------------|
| All ports are taken | 9996–9999 are all held by other processes; close the conflicting processes |
| Device download fails | A firewall is blocking the connection; or the device and the Editor are not on the same subnet |
| The request returns 404 | The handler is not registered; or the path does not match |

## Related pages

- [Asset Publishing](../pipeline/publish-assets.md)
- [Editor Core Classes](core-classes.md)
- [DevOps & CI](../pipeline/devops-ci.md)
