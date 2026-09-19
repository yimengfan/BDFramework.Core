# 基本组件：Component

## 目录

- [前言：](#前言)
- [组件演示](#组件演示)
- [组件创建、加载](#组件创建加载)
- [组件刷新渲染](#组件刷新渲染)

demo: [https://github.com/yimengfan/BDFramework.Core/tree/master/Assets/Code/Game%40hotfix/demo6\_UFlux/01.Component](https://github.com/yimengfan/BDFramework.Core/tree/master/Assets/Code/Game%40hotfix/demo6_UFlux/01.Component "https://github.com/yimengfan/BDFramework.Core/tree/master/Assets/Code/Game%40hotfix/demo6_UFlux/01.Component")

# 前言：

**Component**作为UI元素最小的颗粒，通常用以大能独立的组件与窗口逻辑拆分，方便管理、减少窗口代码.

一般用来实现，如背包道具，商城界面界面 商品，角色血条逻辑 等...

![](image/image_zMS77dBNq8.png)

# 组件演示

![示例样式](https://cdn.nlark.com/yuque/0/2022/png/338267/1650646310583-c18442e9-ecfc-480e-835e-1c84150d9cfe.png#clientId=u36665953-8293-4\&from=paste\&height=113\&id=u0243b3fe\&name=image.png\&originHeight=178\&originWidth=324\&originalType=binary\&ratio=1\&rotation=0\&showTitle=true\&size=23246\&status=done\&style=none\&taskId=u4353da1e-e1c3-4bfe-be8d-49b89556ee2\&title=示例样式\&width=205.71429505640188 "示例样式")

![Prefab节点结构](https://cdn.nlark.com/yuque/0/2021/png/338267/1617702320190-17bb9cd8-c192-4c3e-bee9-c2224ec25084.png#height=118\&id=rMKA7\&name=image.png\&originHeight=118\&originWidth=251\&originalType=binary\&ratio=1\&rotation=0\&showTitle=true\&size=8313\&status=done\&style=none\&title=Prefab节点结构\&width=251 "Prefab节点结构")

![](https://cdn.nlark.com/yuque/0/2021/png/338267/1617702146291-1d36d29f-c72e-48fc-b8cd-23a4e18d54f9.png#height=129\&id=14x2V\&name=image.png\&originHeight=129\&originWidth=713\&originalType=binary\&ratio=1\&rotation=0\&showTitle=false\&size=15664\&status=done\&style=none\&title=\&width=713)

组件由：
**`Component属性`**： path路径，是否异步加载
**`ATComponen<T>`**:组件基类， T当前组件的渲染数据

**`Props绑定（可选）`**： 后面会详细介绍 [https://www.yuque.com/naipaopao/eg6gik/rewt51](https://www.yuque.com/naipaopao/eg6gik/rewt51 "https://www.yuque.com/naipaopao/eg6gik/rewt51")

![](https://cdn.nlark.com/yuque/0/2021/png/338267/1618141970292-f6913cbf-c3c6-49b0-be9c-e4d8db251ddd.png#height=296\&id=RYy8s\&originHeight=296\&originWidth=1187\&originalType=binary\&ratio=1\&rotation=0\&showTitle=false\&status=done\&style=none\&title=\&width=1187)

**`【TransformPath】`属性和具体的节点路径对应
​`【ComponentValueBind】`绑定具体的组件**和**方法**（这里为什么是\*\*`方法`**，其实对应组件（这里为什么是“方法”，其实对应组件**`绑定逻辑（ComponentBindAdaptor）`\*\* 的一个方法，后面会介绍） 的一个方法，后面会介绍）

# 组件创建、加载

**1.同步加载在构造过程中会自动加载2.异步加载需要手动加载**

![](https://cdn.nlark.com/yuque/0/2021/png/338267/1617702783840-bf0633c0-5796-4fbb-bfc6-cd406a2af5a1.png#height=317\&id=aKGyx\&name=image.png\&originHeight=317\&originWidth=665\&originalType=binary\&ratio=1\&rotation=0\&showTitle=false\&size=20949\&status=done\&style=none\&title=\&width=665)

# 组件刷新渲染

![](https://cdn.nlark.com/yuque/0/2021/png/338267/1617703200552-a3d7def2-a072-47aa-865a-9da3ff3d4f90.png#height=194\&id=vESdW\&name=image.png\&originHeight=194\&originWidth=651\&originalType=binary\&ratio=1\&rotation=0\&showTitle=false\&size=22070\&status=done\&style=none\&title=\&width=651)

> **`1.对具体的Prop值进行修改`**（这里的headImage是string类型，后面后ComponentBindAdaptor实现了根据string加载Image的功能）
> **`2.设置修改属性过属性。`**（注：这一步可以不需要，就会自动进行计算，哪些属性修改过，但是有消耗）
> **`3.提交Props修改，即可刷新`**
