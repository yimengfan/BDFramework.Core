# 子窗口SubWindow

## 目录

- [demo: 链接](#demo-链接)
- [前言：](#前言)
- [创建：](#创建)
- [获取、显示隐藏：](#获取显示隐藏)
- [消息发送：](#消息发送)
- [建议：](#建议)

#### demo: [链接](https://github.com/yimengfan/BDFramework.Core/blob/master/Assets/Code/Game@hotfix/demo6_UFlux/04.SimpleWindow/Window_SimpleDemo004.cs "链接")

# 前言：

实际上子窗口SubWindow和普通Window结构一致，只是多了个Parent属性，用以访问父窗口.

作用上，只是用于**拆分窗口逻辑**，**管理部分Transform**，避免主窗口过于臃肿\~

# 创建：

```c# 
  public override void Init()
  {
      base.Init();
      //注册子窗口
      RegisterSubWindow(new SubWindow_Demo004(this.Transform.Find("SubWindow")));
      RegisterSubWindow(new SubWindow_Demo005("Windows/localSubWin"));
  }

```


在父窗口初始化事，可以直接new出子窗口对象并注册.

**子窗口有2种构造：**

> 1.传递Transform直接赋值，让其接管该Transform业务
> 2.传递Path，让其加载构造，自行管理

# 获取、显示隐藏：

```c# 
var win = this.GetSubWindow<SubWindow_Demo004>(); 
 win.Open(); 
 win.Close(); 

```


# 消息发送：

如果需要对子窗口发消息，则直接对父窗口发消息即可.

子窗口用 标签 **\[UIMessageListener]** 进行监听，参数匹配则能收到

[消息派发、监听](https://www.wolai.com/jqCgtY76UDEoKtUD2gPPMf "消息派发、监听")

# 建议：

当有子窗口时，父窗口则最好不要进行 页面逻辑的编写，最好作为容器 调度各个子窗口\~

不然你的代码将难看的一批\~
