# Editor下Http支持

## 目录

- [介绍：](#介绍)
- [默认情况下:](#默认情况下)
- [已有功能：](#已有功能)
- [自定义服务：](#自定义服务)
  - [1.继承IEditorWebApiProcessor](#1继承IEditorWebApiProcessor)
  - [2.实现WebApiProcessor函数](#2实现WebApiProcessor函数)

# 介绍：

**BDFramework**在Unity Editor下实现了一个简单的**Http Server**（使用C# HttpListener）.

用来做一些很酷的功能，比如：

![](https://cdn.nlark.com/yuque/0/2022/png/338267/1658906864815-55c668f0-2060-4835-b9e0-f6fc3ea29823.png)

![](https://cdn.nlark.com/yuque/0/2022/png/338267/1658906913216-72738b1e-8659-4ce7-a2de-da4c16fcd6fa.png)

# **默认情况下:**

我们监听当前机器: 9999端口，预备端口：9998、 9997 、9996

访问:

[**http://127.0.0.1:9999/{自定义协议}/{自定义参数}**](http://127.0.0.1:9999/{自定义协议}/{自定义参数} "http://127.0.0.1:9999/{自定义协议}/{自定义参数}")

![](https://cdn.nlark.com/yuque/0/2022/png/338267/1658906563197-5a173c13-b92c-40d1-b6c6-44ed18f9336a.png)

# 已有功能：

|                       |                        |                                                                                                                                                 |
| --------------------- | ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| class                 | 功能                     | 访问                                                                                                                                              |
| **WP\_EditorInvoke**  | 执行Editor下任意函数          | [**http://127.0.0.1:9999/EditorInvoke/{函数完全限定名}**](http://127.0.0.1:9999/EditorInvoke/{函数完全限定名} "http://127.0.0.1:9999/EditorInvoke/{函数完全限定名}") |
| WP\_LocalABFileServer | Editor下AssetBundle文件服务 | [**http://127.0.0.1:9999/AssetBundle/{文件名}**](http://127.0.0.1:9999/AssetBundle/{文件名} "http://127.0.0.1:9999/AssetBundle/{文件名}")                |

# 自定义服务：

#### 1.继承IEditorWebApiProcessor

```c# 
/// <summary>
/// Editor下webapi接口
/// </summary>
public interface IEditorWebApiProcessor
{

  /// <summary>

  /// 协议名，

  /// 如设置为 test ，则通过127.0.0.1:9999/test访问

  /// </summary>

  string WebApiName { get; }

  /// <summary>

  /// 协议执行

  /// </summary>

  /// <param name="apiParams">Get参数返回</param>

  /// <param name="ctx"></param>

  /// <returns></returns>

  Task<EditorHttpResonseData> WebAPIProcessor(string apiParams, HttpListenerContext ctx);

}

```


#### 2.实现WebApiProcessor函数

![](https://cdn.nlark.com/yuque/0/2022/png/338267/1658907635047-6ef47035-39f8-4751-a18c-380faa2190cc.png)

![](https://cdn.nlark.com/yuque/0/2022/png/338267/1658910691231-b82b894b-3bae-4b2e-a9e5-32763d1234af.png)

这里是一个 实现AssetBundle服务：**通过判断本地是否有文件，有则直接加载返回流.**

**注意事项：**

- \*\* 错误\*\* 可以直接

![](https://cdn.nlark.com/yuque/0/2022/png/338267/1658908145108-cae21eff-598b-440b-b600-44a24932b68f.png)

- 一般来说，我们只需要判断传进来的ApiParams进行相应的逻辑，不需要操作**ctx.response**，

  统一构建**ResponseData**对象 返回

  ![](https://cdn.nlark.com/yuque/0/2022/png/338267/1658910976628-6cea9499-104c-49f7-b439-fdae46ccad99.png)
- 如果直接操作ctx.Response，并且直接返回关闭流，需要**return null**，表示不需要后续处理

抛出异常
