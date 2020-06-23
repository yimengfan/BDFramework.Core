### Version : Unity2018.4.10f1  
<img src="./BDTemp/Img/logo.png" width = "280" height = "100" div align=right />

# 简介(Introduction)
Simple! Easy! Beautiful!  This‘s a powerful Unity3d game workflow! Not a collection of libraries  

#### 热更项目的开发,只需要轻轻一点，一键帮你完成~  

#### 第九第十艺术交流:763141410 （QQ Group:763141410）  [点击加群](http://shang.qq.com/wpa/qunwpa?idkey=8e33dccb44f8ac09e3d9ef421c8ec66391023ae18987bdfe5071d57e3dc8af3f)
If you find a bug or have some suggestions,please make issue! I'll get back to you!  
任何问题直接提issue,24小时内必解决   
github地址: https://github.com/yimengfan/BDFramework.Core  
gitee地址: https://gitee.com/yimengfan/BDFramework.Core  ,速度慢下这个(顺便讨个赞)

## 文档(Document)  
 ### [中文 Wiki](https://www.yuque.com/naipaopao/eg6gik)  
 #### [English Wiki](http://www.nekosang.com)  
 #### [  视频教程（video）](https://www.bilibili.com/video/av78814115/)
 #### [  博客（Blog）](https://zhuanlan.zhihu.com/c_177032018)
 ### [  更新日志 ](https://github.com/yimengfan/BDFramework.Core/wiki/V0.01-%E6%9B%B4%E6%96%B0%E6%97%A5%E5%BF%97)  
 ## 已经适配LWRP、URP工作流!!!    Supported URP!
注:所有bug修复和新特性加入会先提交到Debug分支。待审核期一个月，稳定则会跟主分支进行合并。  
Note: All bug fixes and new features will be submitted to the Debug branch first. The period to be audited is one month, and stability will be merged with the main branch.


# 依赖的插件(Dependent plugins)
使用了以下收费插件,请自行购买下载（或问好心群友要） ：  
**(否则会报错!)**  
**(否则会报错!)**  
**(否则会报错!)**  
The following charging plug-ins are used. Please purchase and download them yourself.  
<br>[Odin] (https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041)  

## v1.0版本计划(v1.0 Plan)
### [To do List]( https://github.com/yimengfan/BDFramework.Core/projects/1)  
1.DevOps工作流加入，CI、CD更加完整  
2.逐步完成框架层API测试用例 ，ILR下测试框架整合  
3.Serverless 后端工作流引入
<br> ~~2020.2 TDD工作流加入~~
<br> ~~2019.10 完成UI工作流的升级和重构(兼容老版本)~~
<br> ~~2019.6.30之前 完成新文档的编写(已完成)
<br> 2018.7 热更新工具整合：代码更新及工具(已完成)
<br> 2018.8 热更新工具整合：资源更新（已完成）~~  
## 贡献者名单
[@gaojiexx](https://github.com/gaojiexx)  
[@ricashao](https://github.com/ricashao)  
### 【2020.06开启 社区建设计划】  
### [To do List]( https://github.com/yimengfan/BDFramework.Core/projects/1)  
如果你想加入我们，并贡献代码，请联系我们。QQ:755737878  
## 框架特点(Feature)
   **·TDD工作流、完整的测试用例:**  
  完整的测试用例，保证框架的稳定。
   
   **·DevOps工作流:**  
   这个还得等一小会~
  
  **一键C#热更:**  
   BD中对ILRuntime进行了二次改造，不用分工程、并且写了一套完善的脚本编译机制,打包工具自动搜集热更代码进行打包。  
   并且对常用库进行了适配.  
 
   **一键版本发布:**  
   代码、资源、表格一键打包,版本管理自动下载  

   
   **完善的资源管理系统，一套API各平台自动切换：**
   BD抛弃了Resources目录，并且保留的用户Resources的开发习惯.  
   一套API自动切换，兼容AB和Editor模式.  
   而且有一套比较完善的AssetBundle管理机制：图集管理、自动搜集Shader、0冗余打包  
   并且bd做了一套精简版可寻址，无论你的Asset再Streaming或者persistent下，都能自动寻找并且加载  
   
   **完善的UI工作流:**  
   BD中有一整套完善的UI工作流(这里我们只对UI逻辑进行管理，不考虑ui制作)，无论你是UGUI NGUI还是其他。  
   我们提供了一套UI管理、值绑定、数据监听、数据流、状态管理等一系列机制.  
   
   **SQL化表格管理:**   
   BD中用Sqlite进行管理表格,并且提供了excel2code，excel2json，excel2sqlite等工具  
   
   **发现式业务注册:**  
   BDFrame底层提供了一套发现式的业务注册.无需以前的各种Register,只要定制好自己的标签、管理器就能被自动注册.  
   在此之上BD,实现了ScreenviewManger,UIManager,EventManager...等一些列管理器。  
   这套机制高度可扩展、可定制,使用者根据自己的需求可以实现其他的管理器  
   并且这个在编辑器环境下也生效的哦~ 写工具时候会很有帮助的哦~  
   
   **模块管理、调度**  
   BD给大家带来了一种开发思路，用户使用流程的Timeline（不是unity的那个timeline）,  
   根据用户流程进行切分模块、调度,这里的模块并不是狭义的一个窗口哦~  
   

   

   ## Feature  

   **· TDD workflow, complete test cases:**  
 what? Dare you use a framework (library) without test cases?  
 
   **· DevOps workflow:**  
   This has to wait for a while ~  
      
   **One key export C# hotfix code:**  
In BD, ILRuntime was re-transformed without sub-projects, and a complete script compilation mechanism was written. The packaging tool automatically collected hot code for packaging.
And adapted to commonly used libraries.

 **One key publish:**  
One key publish of codes, resources, and forms, and version management is automatically downloaded
There are many other things that I think are commonly used: such as the event system, what http library, what object pool is too lazy to list

**A complete resource management system, a set of APIs automatically switch between platforms:**  
BD abandoned the Resources directory, and retains the development habits of user Resources.
A set of APIs automatically switch, compatible with AB and Editor modes.

**And there is a relatively complete AssetBundle management mechanism:**   
atlas management, automatic collection Shader, 0 redundant packaging
And bd has made a set of streamlined addressable, no matter your Asset under Streaming or persistent, it can automatically find and load

**Perfect UI workflow(Flux like):**  
There is a complete set of UI workflow in BD (here we only manage the UI logic, not considering ui production), whether you are UGUI NGUI or other.
We provide a set of mechanisms for UI management, value binding, data monitoring, data flow, state management, etc.  
   
   **Perfect UI workflow:**  
   There is a complete set of UI workflow in BD (here we only manage the UI logic, not considering ui production), whether you are UGUI NGUI or other.  
   We provide a set of mechanisms for UI management, value binding, data monitoring, and data flow.  
   We expect to complete the further upgrade of the UI system in Q4 2018, hoping to create a more advanced and scientific workflow.    
   
   **SQL table management:**  
Sqlite is used to manage forms in BD, and excel2code, excel2json, excel2sqlite and other tools are provided

   **Discovery business registration:**  
The bottom layer of BDFrame provides a set of discovery-type business registration. Without the previous various Registers, as long as you customize your own labels and managers, you can be automatically registered.
On top of this, BD implements a series of manager such as ScreenviewManger, UIManager, EventManager...etc.
This mechanism is highly extensible and customizable, and users can implement other managers according to their own needs
And this is also effective in the editor environment~ It will be very helpful when writing tools~

   **Module management and scheduling:**  
BD brings you a development idea, the user uses the timeline of the process (not the timeline of unity),
Divide the module and schedule according to the user process.The module here is not a narrow window~
     
   </br>There are many other things that I think are commonly used: such as the event system, what http library, what object pool is too lazy to list  

