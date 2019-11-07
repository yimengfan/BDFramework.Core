<img src="./BDTemp/Img/logo.png" width = "280" height = "100" div align=right />

# 简介(Introduction)
Simple! Easy! Beautiful!  This‘s a powerful Unity3d game workflow! Not a collection of libraries  

### 热更项目的开发,只需要轻轻一点，一键帮你完成~  
### Version:Unity2018.4.10f1  
## 文档(Document)  
 [中文 Wiki](https://www.yuque.com/naipaopao/eg6gik)  
 [English Wiki](http://www.nekosang.com)
## 博客（Blog）
[ Blog ](https://zhuanlan.zhihu.com/c_177032018)
## 8月新开技术交流、艺术鉴赏群:763141410
QQ Group:763141410
<br>If you find a bug or have some suggestions,please make issue! I'll get back to you!
<br>任何问题直接提issue,24小时内必解决  
 码云地址: https://gitee.com/yimengfan/BDFramework.Core  ,速度慢下这个(顺便讨个赞)
# 依赖的插件(Dpendent plugins)
使用了以下收费插件,请自行购买下载：  
The following charging plug-ins are used. Please purchase and download them yourself.  
<br>[Odin] (https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041)  
# 更新日志（Update Note）
Master version: 1.0.1  
Debug  version: 1.0.1(already merged)  
注:所有bug修复和新特性加入会先提交到Debug分支。待审核期一个月，稳定则会跟主分支进行合并。    
Note: All bug fixes and new features will be submitted to the Debug branch first. The period to be audited is one month, and stability will be merged with the main branch.  
[  更新日志 ](https://github.com/yimengfan/BDFramework.Core/wiki/V0.01-%E6%9B%B4%E6%96%B0%E6%97%A5%E5%BF%97)  
### Lastest:  
2019.10.8 20.00  
email: 755737878@qq.com  
[master]  
1.Assetbundle管理，修改为全hash管理  
[debuger]  
1.增加新UI系统UFlux  

## v1.0版本计划(v1.0 Plan)
<br> 2019.12 加入后端框架基于Web架构, Asp .Net Core
<br> 其实Web服务器能解决90%的游戏业务(基于分布式架构，部分业务如战斗可以开单独的服务器作计算节点),
<br> 更主要是Web服务器工作流更完善、解决方案更多、更省服务器资源（钱),
<br> ~~2019.10 完成UI工作流的升级和重构(兼容老版本)~~
<br> ~~2019.6.30之前 完成新文档的编写(已完成)
<br> 2018.7 热更新工具整合：代码更新及工具(已完成)
<br> 2018.8 热更新工具整合：资源更新（已完成）~~
## 框架特点(Feature)
   <br>**1.发现式业务注册:**
   <br>BDFrame底层提供了一套发现式的业务注册.无需以前的各种Register,只要定制好自己的标签、管理器就能被自动注册.
   <br>在此之上BD,实现了ScreenviewManger,UIManager,EventManager...等一些列管理器。
   <br>这套机制高度可扩展、可定制,使用者根据自己的需求可以实现其他的管理器
   <br>并且这个在编辑器环境下也生效的哦~ 写工具时候会很有帮助的哦~
   
   <br>**2.模块管理、调度**
   <br>BD给大家带来了一种开发思路，用户使用流程的Timeline（不是unity的那个timeline）,
   <br>根据用户流程进行切分模块、调度,这里的模块并不是狭义的一个窗口哦~
   
   <br>**3.完善的UI工作流:**
   <br>BD中有一整套完善的UI工作流(这里我们只对UI逻辑进行管理，不考虑ui制作)，无论你是UGUI NGUI还是其他。
   <br>我们提供了一套UI管理、值绑定、数据监听、数据流等一系列机制.
   <br>预计2018年Q4我们完成UI系统的进一步升级，希望打造成一套比较先进、科学的工作流。
   
   <br>**4.资源管理：**
   <br>BD抛弃了Resources目录，并且保留的用户Resources的开发习惯.
   <br> 一套API自动切换，兼容AB和Editor模式.
   <br>而且有一套比较完善的AssetBundle管理机制：图集管理、自动搜集Shader、0冗余打包
   <br>并且bd做了一套精简版可寻址，无论你的Asset再Streaming或者persistent下，都能自动寻找并且加载
   
   <br>**5.C#热更代码开发:**
   <br>BD中对ILRuntime进行了二次改造，不用分工程、并且写了一套完善的脚本编译机制,打包工具自动搜集热更代码进行打包。
   <br>并且对常用库进行了适配.
   
   <br>**6.表格管理:** 
   <br>BD中用Sqlite进行管理表格,并且提供了excel2code，excel2json，excel2sqlite等工具
   
   <br>**6.版本发布:**
   <br>代码、资源、表格一键打包,版本管理自动下载

   <br>还有好多我就觉得都是常用的东西:比如事件系统啊、什么http库啊、什么对象池啊就懒得列举了

