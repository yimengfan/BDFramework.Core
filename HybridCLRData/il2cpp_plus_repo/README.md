# il2cpp_plus

原始il2cpp是AOT运行时，不支持动态注册dll元数据。我们轻微改造了metadata管理模块，插入了一些hook代码，支持动态加载dll元数据。

注意，此项目代码不能单独工作，甚至无法成功编译。必须配合 [HybridCLR](https://github.com/focus-creative-games/hybridclr) 解释器才能正常工作。

main分支不包含任何代码，请切到正确的版本。
