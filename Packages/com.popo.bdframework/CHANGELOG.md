A Simple、Eesy、Profassional Game workflow!   - BDFramework.
---更新记录---
##V2.0.8 beta3.5
 CN:
【Editor】
- BuildPipeline: 优化打包流程
- Runtime:优化启动代码流程，修复部分beta3 bug.

【Editor】
- BuildPipeline: Optimize build  pipeline.
- Runtime:Optimize luanch pipeline,fixed some bug on mobile from beta3.

 ##V2.0.8 beta3
 CN:
【Editor】
- BuildPipeline: 增加一些AssetGraph节点 
- BuildPipeline: 增加assetbundle检测逻辑
- BuildPipeline: 增加一个新的收集keyword的方案
- PublishPipeline: 增加 “PublishPipeline”.
- PublishPipeline: 增加Publish目录
- PublishPipeline: 增加CI、CD相关支持
【Editor】
- BuildPipeline: Add new node for build assetbundle.
- BuildPipeline: Add AssetBundle Check logic.
- BuildPipeline: Add new collect shader keywords.
- PublishPipeline: Add “Publish Pipeline”.
- PublishPipeline: Add “publish” folder.
- PublishPipeline: Add CI、CD support.

## V2.0.8 beta2
CN:
【Editor】
-Nuget: 修改nugetdll目录.
-Nuget：修改Nuget MenuItem.

EN:
【Editor】
-Nuget: Move nugetdll folder.
-Nuget：Change Nuget menuitem name.

## V2.0.8 beta1
CN:
【Editor】
-AssetBundleV2: 增加assetgraph打包assetbundle支持

EN:
【Editor】
-AssetBundleV2: Add assergraph node build assetbundle.

## V2.0.7
CN:
【Editor】
-重构部分类命名

EN:
【Editor】
-Reconstruct class name

## V2.0.7 beta2
CN:
【Nuget】
-增加Nuget支持
【Runtime】
-修复bug: ios mac版本过高WWW不生效bug
【Editor】
-修复bug：mac版本下mono file.exsit() 操作失败.

EN:
【Nuget】
-Add Nuget support.
【Runtime】
-Fixed bug: some on new ios 14.5 or m1 mac  bug.
【Editor】
-Fixed bug：Mono file.exsit() api excute fail.

## V2.0.7 beta1
【Excel2Sqlite】
- 增加excel空数据的检测
- 增加excel2call导出到策划目录

【UniTest】
- 修复UPM版本的测试用例功能

## V2.0.6
【Editor】
- 增加Excel导表回调，用以做表格检测

V2.0.5
【Editor】
- Auto build dll 修改编译时机，解决play模式退出无法自动编译的bug

【UIManager】
- 制定UIMsgData规范
- 修改UIMsg为UIMsgListener  

## V2.0.4
【Editor】
 - 增加一个编辑器启动引导界面
 - 增加odin加入时相关异常处理
 - 修复编辑器无Odin时语法报错问题

## V2.0.3
【UIManager】
- 增加DI支持（依赖注入）

## V2.0.2
【Sqlite】
- 增加自定义字段导出
- 增加自定义记录导出
- 增加本地服务器双db导出

## V2.0.1
【BDLauncher】
 - 启动速度优化

【Script】
  - Editor：增加自动编译热更DLL功能
  - Runtime：增加反射获取Attribute接口

【Excel2Sqlite】
 - 增加若干功能：自动导表等

【UFlux】
 - 增加AutoInitComponentAttribute，且可自定义
 - 增加ButtonOnclick
 - 修改TransformPath实现

【AssetBundleV2】
 - 修复部分情况下异步加载顺序出错

## V2.0.0
 - 全面流程优化全新升级，部分业务重构.