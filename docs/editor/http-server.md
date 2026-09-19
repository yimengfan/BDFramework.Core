# Editor Http 服务

框架在编辑器内启动一个 `HttpListener`，提供两个内置 Web API，并支持自定义协议处理器。

## 端口

| 优先级 | 端口 |
|--------|------|
| 1 | **9999** |
| 2 | 9998 |
| 3 | 9997 |
| 4 | 9996 |

从 9999 开始尝试，被占用则顺延。

## 内置处理器

### `WP_EditorInvoke` —— 执行任意 Editor 函数

通过 HTTP 请求调用编辑器内的任意公开静态方法。用于：
- 外部工具/脚本触发编辑器操作
- CI 在不启动 Unity 的情况下驱动编辑器
- 调试时远程触发功能

!!! danger "`WP_EditorInvoke` 能执行任意 Editor 函数"
    这是**开发期工具**，不要在生产环境或不可信网络中暴露该端口。端口只绑定本机时风险可控。

### `WP_LocalABFileServer` —— 本机 AB 文件服务

把本地的 `DevOps/PublishAssets` 目录当作文件服务器提供出来，**让真机能直接下载资源做验证**，不需要部署远端服务器。

典型用法：

```text
① 编辑器内构建资源（DevOps/PublishAssets 就绪）
② 启动本机文件服务器（WP_LocalABFileServer）
③ 真机把 serverUrl 指向 <本机IP>:<port>
④ 真机走完整的热更下载流程
```

配合 `EditorWindow_PublishAssets.OnGUI_PublishEditorService()` 使用。

## 自定义协议处理器

```csharp
public interface IEditorWebApiProcessor
{
    // 自定义请求处理
}
```

实现该接口并注册，即可扩展自己的 HTTP 协议。

## 相关菜单

| 菜单 | 作用 |
|------|------|
| `BDFrameWork工具箱/PublishPipeline/1.发布资源` | 发布资源窗口，含「发布到编辑器服务」 |

## 常见故障

| 现象 | 根因 |
|------|------|
| 端口全部被占用 | 9996–9999 都被其他进程占用；关闭冲突进程 |
| 真机下载失败 | 防火墙拦截；或真机与编辑器不在同一网段 |
| 请求返回 404 | 处理器未注册；或路径不匹配 |

## 相关页面

- [资源发布](../pipeline/publish-assets.md)
- [Editor 核心类](core-classes.md)
- [DevOps 与 CI](../pipeline/devops-ci.md)
