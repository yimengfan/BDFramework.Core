# Editor核心类总览

![](https://cdn.nlark.com/yuque/0/2022/png/338267/1658908310935-08b19ebd-07d3-4a65-9ee0-76ac2615b506.png)

该类负责整个框架编辑器环境下的各种服务注册：

**1.业务类，编辑器下注册，使其编辑器下可用：**

- 启发式管理器注册
- BResource.Init Editor下BResources初始化
- BDApplication 资源路径类服务
- ManagerInstHelper Editor下管理器初始化

**2.编辑器服务：**

- BDFrameEditorBehaviorHelper BDFrame生命周期监听 [\[链接\]](https://www.yuque.com/naipaopao/eg6gik/foq3gg "\[链接]")
- BDFrameEditorConfigHelper 框架相关配置
- BDFrameworkPipelineHelper Editor下各种Pipeline的初始化
- EditorTask Editor下的后台任务
- EditorHttp Editor下Http服务 ： [\[](https://www.yuque.com/naipaopao/eg6gik/tkxhos "\[")[https://www.wolai.com/2JSReC3cxvGqntXsrsHvtZ](https://www.wolai.com/2JSReC3cxvGqntXsrsHvtZ "https://www.wolai.com/2JSReC3cxvGqntXsrsHvtZ")[\]](https://www.yuque.com/naipaopao/eg6gik/tkxhos "]")
