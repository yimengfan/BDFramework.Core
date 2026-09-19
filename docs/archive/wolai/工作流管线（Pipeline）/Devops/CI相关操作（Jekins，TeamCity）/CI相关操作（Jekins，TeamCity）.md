# CI相关操作（Jekins，TeamCity）

## 目录

- [CI相关操作（Jekins，TeamCity）](#CI相关操作JekinsTeamCity)
  - [1.预览框架(项目)中所有CI](#1预览框架项目中所有CI)
  - [2.如何显示在该列表中](#2如何显示在该列表中)
  - [3.注意事项](#3注意事项)

# CI相关操作（Jekins，TeamCity）

#### 1.预览框架(项目)中所有CI

![](image/image_EflfTOczXZ.png)

![](image/image_1_ioURfH_UO6.png)

#### 2.如何显示在该列表中

![](image/image_2_M5SRPl8u45.png)

使用CI Method Attribute 对方法修饰即可

#### 3.注意事项

![](image/image_3_xWq-Gr3iTA.png)

CI的class建议为 Static class，

> 📌**并且静态构造函数中需要初始化框架：`BDFrameworkEditorBehaviour.InitBDFrameworkEditor();`**
