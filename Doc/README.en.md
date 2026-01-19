<img src="logo.jpg" alt="logo.png" width="60%"><br />[![](https://img.shields.io/npm/v/com.popo.bdframework?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.popo.bdframework/) [![](https://img.shields.io/badge/license-Anti%20996-blue.svg?style=flat-square)](https://github.com/996icu/996.ICU/blob/master/LICENSE) [![](https://img.shields.io/badge/link-996.icu-%23FF4D5B.svg?style=flat-square)](https://996.icu/#/zh_CN) [![](https://img.shields.io/github/license/yimengfan/BDFramework.Core)](https://github.com/yimengfan/BDFramework.Core/blob/master/LICENSE) 
# Introduction

Simple! Easy! Professional! This‘s a powerful Unity3d game workflow!<br />The design concept of BDFramework is always: **Committed to creating an efficient game industrialization pipeline!!**<br /><br />Most of the functional development of BDFramework revolves around a complete workflow, released in the form of a **Pipeline**.<br />Such as:**BuildPipeline, PublishPipeline, DevOps** etc...<br />The use of third-party libraries is also deeply customized for the Pipeline. In many cases, a large amount of Editor coding will be written for some user experience optimization.<br />This is also one of the design concepts of BDFramework:<br />**What can be solved by the editor should not be solved by the business layer! What can be automated should not be done manually!**<br />BDFramework does not have any cool-looking features. It is an accumulation of little by little, a little bit of automation, and a little bit of experience in business coding. It is precisely because of this persistence that this framework has appeared.<br />It is more about sharing and discussing commercial technical solutions.<br />For some special reasons, only the implementation of some game infrastructure solution Pipelines can be released.<br />There will be no solutions for specific business logic, so the entire workflow is more like a game development scaffolding.<br />Hope for understanding!<br />Finally,<br />Although this framework can be used out of the box, I personally recommend and encourage: **Think for yourself and transform it for your own project!**<br />Any questions are welcome to discuss~


# Document

[**Chinese Wiki**](https://www.wolai.com/ky1FVhe7Mudg6ru277gfNr)

#### [English Wiki](http://www.nekosang.com/)

#### [Video Tutorial](https://www.bilibili.com/video/av78814115/)

#### [Blog](https://zhuanlan.zhihu.com/c_177032018)

# Community

#### Online Discussions: [Click](https://github.com/yimengfan/BDFramework.Core/discussions)  
#### Game Dev Group: [Click to join](https://jq.qq.com/?_wv=1027&k=OSxzhgK4)  

If you find a bug or have some suggestions,please make issue! I'll get back to you

# Start
#### OpenUPM(Highly recommended): [Link](https://www.wolai.com/4CdvGJ93AXPJ2kLMC49F2Z)   
# Publish

#### Stable version hosted on OpenUPM :  [Link](https://www.wolai.com/4CdvGJ93AXPJ2kLMC49F2Z)   

### Support odd-numbered versions: 2019, 2021, Unity6 (2023)

#### Unity2018 - [ObsoleteBranch](https://github.com/yimengfan/BDFramework.Core/tree/2018.4.23LTS)  
#### Unity2019 - [Link](https://www.wolai.com/4CdvGJ93AXPJ2kLMC49F2Z)    (Current main branch)  
#### Unity2021 - [To be tested]
#### Unity6(2023) - [To be tested]  

Version development process:<br />=>Modify, Fix bug, add new features based on **Master (currently Unity2019)**<br />=>Merge to Unity2021 for testing  

#### [Development Plan](https://www.wolai.com/rYPc8FpYj1Lu9EjYoz1Ci9)  

## V2.1 Version:

#### -Add BuildPipeline!
#### -Add PublishPipeline!
#### -Add HotfixPipeline!
#### -Full support for DevOps workflow.
## V2 Version:
#### 1. Fully upgraded to UPM management

#### 2. Fully adapt to the URP pipeline workflow

#### 3. Fully customize the Unity Editor environment and upgrade the editor operations. More convenient and user-friendly development experience

#### 4. Comprehensively optimize the framework startup speed and refactor some ancient codes.

#### 5. UFlux UI workflow is fully upgraded: smarter value binding, simpler workflow, more convenient custom extensions, DI, etc...

#### 6. More comprehensive documentation

#### 7. Commercial-level demos will be added, and free commercial-level project development tutorials will be opened later

## V1 Version:

### C# hotfix:

- Custom compilation service
- Optional project stripping (hot update can not split the project)
- One-click packaging of hot-fix DLLs
- Compatible with DevOps, CI, CD.

### Table Manage:

- Excel one-click generation of Class
- Excel one-click generation of Sqlite, Json, Xml, etc.
- Server and local tables are exported separately.
- Custom configuration of reserved fields, single records, etc.
- SQLite ORM tool (compatible with hot update)
- Custom table logic detection.
- Compatible with DevOps, CI, CD.

### Assets Manage:

- Re-customize the directory management specifications and guide management.
- A set of APIs automatically switches between AB and Editor modes, retaining the Resources.load habit.
- Visual packaging logic configuration, 0 redundant packaging.
- Extensible packaging rules
- Subcontracting mechanism.
- Packaging logic error correction mechanism.
- Built-in incremental packaging mechanism to prevent different machines and projects from packaging different ABs.
- Automatic atlas management.
- Automatically collect Shader Keywords.
- Addressable loading system.
- Assetbundle obfuscation mechanism to prevent cracked resources to a certain extent.
- Assetbundle synchronous and asynchronous loading verification.
- Assetbundle loading performance test.
- Full support under Editor
- Support DevOps access, CI/CD friendly.

### Publish:

- One-click packaging of code, resources, and tables, and automatic download of version management.
- Built-in local file server
- Support DevOps access, CI/CD friendly.

### UFlux:

- Provide a set of Flux ui management mechanism (similar to MVI)
- Perfect UI management, can be used with any NGUI, UGUI, FairyGUI, etc.
- Complete UI abstraction: Windows, Component, State, Props...
- Support UI management, value binding, data monitoring, data flow, state management, etc.
- Support DI dependency injection.

### Logic Manage:

- Automatic registration of managers and managed classes
- On this basis, BD implements ScreenviewManger, UIManager, EventManager... and other managers. Users can implement other managers according to their own needs.
- Full support under Editor.

### Navication:

- Navigation mechanism such as modules and user timelines.
- Convenient for module scheduling, division and other logic...

### Fully customized Editor:

- Provide a complete editor life cycle, which is convenient for customization and expansion.
- **Complete test cases to ensure the stability of the framework.**
- All functions are fully compatible with DevOps, CI, CD and other tools.
- Other large numbers of customized Editors to ensure user experience... (too many to count)

**There are many trivial systems that are not listed...**  

