# ReleaseLog

## 8.7.0

Release Date: 2025-11-03.

### Runtime

- [fix] fix a crash issue in IH_object_ctor caused by `ctx.GetCurbb()->insts` may be empty in obfuscated code
- [merge] **Unity 6000***: merge il2cpp changes from 6000.0.44 to 6000.0.60
- [merge] **TuanJie**: merge il2cpp changes from tuanjie 1.6.5 to 1.6.7

### Editor

- [fix] fix raising NullReferenceException in generating MethodBridge for MonoPInvokeCallbackAttribute while constructor arguments is empty.

## 8.6.0

Release Date: 2025-09-27.

### Runtime

- [fix] fix the crash in InterpreterDelegateInvoke when compiled in Release mode on Xcode 26.x. This bug is caused by an optimization issue in the newer Clang version.
- [fix] [tuanjie] fix a bug on tuanjie that calling Init of Il2CppClass `Nullable<EnumType`> may not init Il2CppClass of EnumType, which causes crash when box `Nullable<EnumType>`
- [fix] [tuanjie] fix bug that computation of method index in Class::GetGenericInstanceMethodFromDefintion. tuanjie 1.6.4 only fixes it when IL2CPP_ENABLE_LAZY_INIT.
- [merge] merge il2cpp of tuanjie changes from 1.6.0-1.6.4

### Editor

- [fix] fix the bug that BashUtil.CopyDir calls UnityEditor.FileUtil.CopyFileOrDirectory failed when parent directory of dst does not exist.

## 8.5.1

Release Date: 2025-08-25.

### Runtime

- [fix] **CRITICAL!!!** fixed stack calculation bug in instinct transform for `System.Activator.CreateInstance<T>()` when T is value type.

### Editor

- [fix] fixed PInvokeAnalyzer bug in computing PInvoke function calling conventions.

## 8.5.0

Release Date: 2025-08-20.

### Runtime

- [new] AOTHomologousImage supports custom image format
- [fix] Throw an exception when the number of function parameters exceeds 255, as the parameter count type in il2cpp is uint8_t.
- [fix] fix  incorrect type conversions for MethodInfo.parameter_count and Il2CppMethodDefinition.parameterCount.

### Editor

- [change] BashUtil::CopyDir replaces CopyWithCheckLongFile with CopyUnityEditor.FileUtil.CopyFileOrDirectory

## 8.4.0

Release Date: 2025-07-26.

### Runtime

- **[new] IMPORTANT! support custom image format**
- [change] the type of field `offset` of ldsfld, stfld, ldthreadlocalfld、stthreadlocalfld changed from uint16_t to uint32_t so that supports class with huge static fields.
- [opt] optimize to use NewValueTypeVar_Ctor_0 for new zero-argument value type and System.Activator.CreateInstance&lt;T&gt;()
- [opt] optimize new ValueType with zero arguments.

### Editor

- [fix] fix the issue that `Texture Compression` option in Build Settings was changed after running `HybridCLR/Generate/All` on Android platform

## 8.3.0

Release Date: 2025-07-04.

### Runtime

- [fix] fix the bug where RuntimeInitClassCCtor was executed during InterpreterModule::GetInterpMethodInfo. This caused the type static constructor to be incorrectly executed prematurely during PrejitMethod.
- [fix] fix bug that JitMethod jit method of generic class incorrectly.
- [merge] merge il2cpp of tuanjie changes from 1.5.0-1.6.0

### Editor

- [fix] fix the bug that not collect struct in calli and extern method signature in generating MethodBridge.

## 8.2.0

Release Date: 2025-06-12.

### Runtime

- [fix] fix line number mistake in stacktrace
- [fix] Fixed bug that PDBImage::SetupStackFrameInfo didn't set ilOffset and sourceCodeLineNumber of stackFrame when SequencePoint not found
- [merge] merge il2cpp changes from 2022.3.54-2022.3.63

### Editor

- [change] changed from throw exception to logError when not supported pinvoke or reverse pinvoke method parameter type was found

## 8.1.0

Release Date: 2025-05-29.

### Runtime

- [opt] **important**! use std::unordered_set for s_GenericInst to reduce the time cost of Assembly.Load to 33% of the original.

### Editor

- [fix] fix bug of GenericArgumentContext that inflate ByRef and SZArray to Ptr.

## 8.0.0

Release Date: 2025-05-02.

### Runtime

- [new] support define PInvoke method in interpreter assembly
- [new] InterpreterImage initialize ImplMap for PInvoke methods.
- [new] RawImageBase support ModuleRef and ImplMap table.
- [fix] fixed a compilation error on PS4 platform for the code `TokenGenericContextType key = { token, genericContext };` — the C++ compiler version on PS4 is too old to support this initialization syntax for std::tuple.

### Editor

- [fix] fix error of computing CallingConvention in MethodBridge/Generator::BuildCalliMethods
- [new] generate Managed2NativeFunction for PInvoke method
- [change] AssemblyResolver also resolves `*.dll.bytes` files besides `*.dll`.
- [change] change type of the first argument `methodPointer` of Managed2NativeFunctionPointer from `const void*` to `Il2CppMethodPointer`
- [change] the shared type of ElementType.FnPtr is changed from IntPtr to UIntPtr
- [change] validate unsupported parameter type(.e.g string) in MonoPInvokeCallback signature when generate MethodBridge file
- [opt] optimization unnecessary initialization of typeArgsStack and methodArgsStack of GenericArgumentContext
- [refactor] refactor code of settings.
- [refactor] move ReversePInvokeWrap/Analyzer.cs to MethodBridge/MonoPInvokeCallbackAnalyzer.cs

## 7.10.0

Release Date: 2025-04-22.

### Runtime

- [fix] fix the bug that doesn't lock g_MetadataLock in PDBImage::SetupStackFrameInfo.
- [change] remove Il2CppTypeHash and Il2CppTypeEqualTo, replace with il2cpp::metadata::Il2CppTypeHash and il2cpp::metadata::Il2CppTypeEqualityComparer.
- [merge] merge il2cpp changes from tuanjie 1.3.4 to 1.5.0, base unity from 2022.3.48 to 2022.3.55 .

### Editor

- [fix] fix bug of `CompileDll(BuildTarget target)` that use EditorUserBuildSettings.activeBuildTarget instead of target to call CompileDll.
- [opt] AOTAssemblyMetadataStripper strips AOT assembly resources. (#54)

## 7.9.0

Release Date: 2025-03-31.

### Runtime

- [merge] merge il2cpp changes from 6000.0.30f1 to 6000.0.44f1

## 7.8.1

Release Date: 2025-03-24.

### Runtime

- [fix] fix bug of CreateInitLocals when size <= 16
- [change] remove unnecessary `frame->ip = (byte*)ip;` assignment in LOAD_PREV_FRAME()

## 7.8.0

Release Date: 2025-03-24.

### Runtime

- [opt] fixed a **critical** bug where taking the address of the ip variable severely impacted compiler optimizations, leading to significant performance degradation.
- [opt] add HiOpCodeEnum::None case to interpreter loop. avoid decrement *ip when compute jump table,  boosts about 5% performance.
- [opt] opt InitLocals and InitInlineLocals in small size cases
- [opt] reorder MethodInfo fields to reduce memory size

### Editor

- [fix] fixed the bug where BashUtil.RemoveDir failed to run under certain circumstances on macOS systems.

## 7.7.0

Release Date: 2025-03-12.

### Runtime

- [change] fixed the issue that HYBRIDCLR_ENABLE_PROFILER was disabled in release build
- [fix] fix a crash in PDBImage::SetMethodDebugInfo when GetMethodDataFromCache returns nullptr
- [fix] fix assert bug of InterpreterDelegateInvoke when method->parameters_count - curMethod->parameters_count == 1
- [fix] fix compiler error of initialize constructor code `{a, b}` for `std::tuple<void*,void*>` in PS5
- [opt] removed unnecessary pdb lock in PDBImage
- [change] fix some compiler warnings
- [change] HYBRIDCLR_ENABLE_STRACKTRACE was enabled in both DEBUG and RELEASE build without considering HYBRIDCLR_ENABLE_STRACE_TRACE_IN_WEBGL_RELEASE_BUILD flag.

### Editor

- [fix] fixed hook failed in version below MacOS 11
- [change] CompileDllActiveBuildTarget and GenerateAll use EditorUserBuildSettings.development to compile hot update dll.
- [remove] remove option HybridCLRSettings.enableProfilerInReleaseBuild
- [remove] remove option HybridCLRSettings.enableStraceTraceInWebGLReleaseBuild

## 7.6.0

Release Date: 2025-03-01.

### Runtime

- [fix] fixed the bug in ClassFieldLayoutCalculator where it incorrectly handles [StructLayout] and blittable attribute when calculating the layout for structs.
- [fix] fix bug of computing explicit struct layout caused by commit "199b1b1a789d760828bd33e7e1438261cd1f8d15"
- [fix] fix the code `TokenGenericContextType key = { token, genericContext }` has compiler error in PS5
- [merge] merge il2cpp changes from 2021.3.44f1 to 2021.3.49f1

### Editor

- [fix] fixed the bug in the MethodBridge generator where it incorrectly handles [StructLayout] and blittable attribute when generating code for struct classes.
- [new] add AssemblySorter to sort assemblies by reference order

## 7.5.0

Release Date: 2025-02-05.

### Editor

- [revert] Revert 'support preserve UnityEngine core types when GenerateLinkXml'.

## 7.4.1

Release Date: 2025-01-19.

### Editor

[fix] fixe the bug that preserving UnityEngine.PerformanceReportingModule when generating link.xml would cause the Android app built with Unity 2019 to crash on startup.

## 7.4.0

Release Date: 2025-01-17.

### Runtime

- [new] calli supports call both native function pointer and managed method

### Editor

- [new] add Managed2NativeFunctionPointer MethodBridge functions
- [new] support preserve UnityEngine core types when GenerateLinkXml
- [fix] fixed the bug in AOTAssemblyMetadataStripper::Strip where ModuleWriterOptions MetadataFlags.PreserveRids was not used.
- [fix] fixed the bug where StripAOTDllCommand did not set BuildPlayerOptions.subtarget in Unity 2021+ versions, causing failure when publishing dedicated buildTarget.
- [change] add UnityVersion.h.tpl and AssemblyManifest.cpp.tpl, Il2CppDefGenerator doesn't generates and override code file from same one
- [change] add MethodBridge.cpp.tpl. MethodBridgeGeneratorCommand doesn't generate and override from same file

## 7.3.0

Release Date: 2024-12-31.

### Runtime

- [fix] fix bug that Image::ReadRuntimeHandleFromMemberRef didn't inflate parent type when read field of GenericType
- [fix] fix an issue occurred in InterpreterImage::GenerateCustomAttributesCacheInternal where HYBRIDCLR_METADATA_MALLOC was incorrectly used to allocate the cache. When occasional contention occurs, releasing memory using HYBRIDCLR_FREE causes a crash.
- [fix] fixed a potential deadlock issue in Unity 2019 and 2020 versions within InterpreterImage::GenerateCustomAttributesCacheInternal, where il2cpp::vm::g_MetadataLock was held before running ConstructCustomAttribute.
- [fix] fixed a bug in Unity 2019 and 2020 within InterpreterImage::GenerateCustomAttributesCacheInternal, where cache memory leaks occurred under multithreading contention.
- [fix] fix the bug that InterpreterImage::ConstructCustomAttribute doesn't set write barrier for field
- [fix] fix the bug that `InterpreterImage::InitTypeDefs_2` runs after `InitClassLayouts`, causing the `packingSize` field to be incorrectly initialized.
- [fix] fix the bug in ClassFieldLayoutCalculator::LayoutFields where the alignment calculation incorrectly considers naturalAlignment, resulting in field offsets that are inconsistent with the actual field offsets in AOT. This bug originates from IL2CPP itself and only occurs in Unity 2021 and earlier versions.

### Editor

- [fix] fix the issue in Unity 6000 where the modification of the trimmed AOT DLL output directory for the visionOS build target caused CopyStrippedAOTAssemblies::GetStripAssembliesDir2021 to fail in copying AOT DLLs.
- [fix] fix the bug where MissingMetadataChecker can't detect references to newly added AOT assemblies.

## 7.2.0

Release Date: 2024-12-9.

### Runtime

- [fix] fix a critical bug in Image::ReadArrayType, where it incorrectly uses alloca to allocate Il2CppArray's sizes and lobounds data.
- [merge] merge il2cpp changes from 2022.3.51f1 to 2022.3.54f1
- [merge] merge il2cpp changes from 6000.0.21 to 6000.0.30

## 7.1.0

Release Date: 2024-12-4.

### Runtime

- [new] support prejit interpreter class and method
- [new] add RuntimeOptionId::MaxInlineableMethodBodySize
- [merge] merge il2cpp changes from tuanjie v1.3.1 to v1.3.4
- [fix] fix memory leak of TransformContext::irbbs and ir2offsetMap
- [opt] does not insert CheckThrowIfNull check when inlining constructors
- [opt] remove unnecessary typeSize calculation from the NewValueTypeInterpVar instruction.
- [change] change default maxInlineableMethodBodySize from 16 to 32
- [change] remove the unnecessary Inflate operation on arg.type when initializing ArgVarInfo in TransformContext::TransformBodyImpl.
- [change] remove unnecessary fields genericContext, klassContainer, methodContainer from the TransformContext

### Editor

- [new] support prejit interpreter class and method
- [new] add RuntimeOptionId::MaxInlineableMethodBodySize
- [fix] fix the bug that CopyStrippedAOTAssemblies didn't work on UWP platform of 6000.0.x
- [fix] fix the issue that CopyStrippedAOTAssemblies didn't support HMIAndroid in tuanjie engine
- [change] change the attributes on fields of HybridCLRSettings from `[Header]` to `[ToolTip]`
- [refactor] refactor code comments and translate them to English

## 7.0.0

Release Date: 2024-11-15.

### Runtime

- [new] support method inlining
- [refactor] refactor Transform codes

### Editor

- [new] add option RuntimeOptionId::MaxMethodBodyCacheSize and RuntimeOptionId::MaxMethodInlineDepth
- [fix] fix the bug in GenericReferenceWriter where _systemTypePattern did not properly escape the '.' in type names. This caused issues when compiler-generated anonymous types and functions contained string sequences like 'System-Int', incorrectly matching them to 'System.Int', resulting in runtime exceptions.
- [fix] fix the bug in `MissingMetadataChecker` where it did not check for missing fields.

## 6.11.0

Release Date: 2024-10-31.

### Runtime

- [merge] Merges changes from Tuanjie versions 1.3.0 to 1.3.1
- [merge] Merges il2cpp code changes from version 2022.3.48f1 to 2022.3.51f1

## 6.10.1

Release Date: 2024-10-24.

### Editor

- [fix] Fixs HookUtils compile errors in Unity 2019 and 2020
- [change] remove README_zh.md.meta, add README_EN.md.meta

## 6.10.0

Release Date: 2024-10-23.

### Runtime

- [new] Officially supports 6000.0.23f LTS version
- [merge] Merges changes from Tuanjie versions 1.2.6 to 1.3.0
- [fix] Fixed an issue in MonoHook where processorType was not handled correctly on some CPUs when the processorType was returned in all uppercase (e.g., some machines return 'INTEL' instead of 'Intel').

## 6.9.0

Release Date: 2024-9-30.

### Runtime

- [fix] Fixes the bug where thrown exceptions did not call `il2cpp::vm::Exception::PrepareExceptionForThrow`, resulting in an empty stack trace.
- [merge] Merges changes from versions 2021.3.42f1 to 2021.3.44f1, fixing compilation errors introduced by il2cpp changes in version 2021.3.44.
- [merge] Merges changes from versions 2022.3.41f1 to 2022.3.48f1, fixing compilation errors introduced by il2cpp changes in version 2022.3.48.
- [merge] Merges code from 6000.0.19f1 to 6000.0.21f1, fixing compilation errors introduced by il2cpp changes in version 6000.0.20.
- [merge] Merges changes from Tuanjie versions 1.1.0 to 1.2.6

## 6.8.0

Release Date: 2024-9-14.

### Runtime

- [fix] Fixes the bug where exception stacks did not include line numbers.
- [fix] Fixes the bug where `Il2CppGenericContextCompare` simply compared class_inst and method_inst pointers for equality, which is not the case for all GenericInst (e.g., `s_Il2CppMetadataRegistration->genericInsts`) that do not come from GenericInstPool, hence identical GenericInst are not pointer equal.
- [merge] Merges il2cpp code changes from version 6000.0.10 to 6000.0.19

## 6.7.1

Release Date: 2024-8-26.

### Runtime

- [fix] Fixes the bug where there were compilation errors when publishing to the iOS platform with Unity 2019.

## 6.7.0

Release Date: 2024-8-26.

### Runtime

- [opt] No longer enables PROFILER in Release compilation mode, this optimization reduces the overhead of function calls by 10-15%, and overall performance is improved by approximately 2-4%.
- [opt] When publishing for WebGL targets, StackTrace is no longer maintained in Release compilation mode, which improves performance by about 1-2%.
- [fix] Fixes the bug where `Transform Enum::GetHashCode` did not change the variable type from `uintptr_t` to `int32_t` on the stack, leading to incorrect calculations when participating in subsequent numerical computations due to the parameter type being extended to 64 bits.
- [fix] Fixes the bug where a stack overflow was triggered by calling delegates extensively within interpreter functions.

### Editor

- [new] HybridCLRSettings adds two new options: `enableProfilerInReleaseBuild` and `enableStackTraceInWebGLReleaseBuild`.
- [change] Fixes the issue where an assertion failure occurred when switching from the WebGL platform to other platforms (no substantial impact).

## 6.6.0

Release Date: 2024-8-12.

### Runtime

- [fix] Fixes the bug where `CustomAttribute` construction or namedArg includes a `typeof(T[])` parameter, causing a crash.
- [fix] Fixes the bug where `T[index].CallMethod()` throws an `ArrayTypeMismatchException` when `CallMethod` is an interface function of generic type T, and the array element is a subclass of T.
- [fix] Fixes the bug where `MethodBase.GetCurrentMethod` does not return the correct result. A new instinct instruction `MethodBaseGetCurrentMethod` is added.
- [fix] Fixes the bug where loading pdb on platforms like WebGL still does not display stack code line numbers.
- [fix] Fixes the bug where, after calling a sub-interpreter function and returning, logging prints the code line number of the called function due to `frame->ip` not being reset to `&ip`.
- [fix] Fixes the bug where calling a sub-interpreter function displays the line number of the next statement in the parent function's code line number due to `frame->ip` pointing to the next instruction.
- [merge] Merges il2cpp code from 2021.3.42f1 and 2022.3.41f1, fixing compilation errors caused by the new `il2cpp_codegen_memcpy_with_write_barrier` function in versions 2021.3.42f1 and 2022.3.40f1.

## 6.5.0

Release Date: 2024-8-5.

### Runtime

- [new] Hot update function stacks for versions 2019-2020 can now correctly display code files and line numbers.
- [merge] Merges il2cpp changes from Unity versions 6000.0.1 to 6000.0.10

## 6.4.0

Release Date: 2024-7-25.

### Runtime

- [new] Supports loading dll and pdb symbol files with `Assembly.Load(byte[] assData, byte[] pdbData)`, displaying the correct code files and line numbers in function stacks when printing on versions 2021+.
- [fix] Fixes the bug where `InterpreterImage::GetEventInfo` and `GetPropertyInfo` might not initialize `method`, resulting in empty getter functions.
- [opt] Optimizes the order of function stacks printed by `StackTrace` and `UnityEngine.Debug`, displaying interpreter functions at the correct stack positions in most cases.
- [opt] Optimizes metadata memory.

### Editor

- [fix] Fixes the bug where `GenerateMethodBridge` did not consider `ClassLayout`, `Layout`, and `FieldOffset` factors when calculating equivalent classes.
- [fix] Fixes the bug where `PatchScriptingAssembliesJsonHook` throws an异常 when the `Library/PlayerDataCache` directory does not exist.

## 6.3.0

Release Date: 2024-7-15.

### Runtime

- [opt] Significantly optimizes metadata memory, reducing memory usage by 15-40% compared to version 6.2.0.
- [fix] Fixes the bug where memory was not released for `insts` in `IRBasicBlock` during transformation, causing a memory leak approximately 0.7-1.6 times the size of the dll.
- [fix] Fixes the bug where `ClassFieldLayoutCalculator` caused a memory leak.
- [fix] Fixes the bug where `MetadataAllocT` incorrectly used `HYBRIDCLR_MALLOC` instead of `HYBRIDCLR_METADATA_MALLOC`.
- [opt] Optimizes the native stack size occupied by `Interpreter::Execute` to avoid stack overflow errors when nesting is too deep.

### Editor

- [fix] Fixes the bug where exporting an xcode project for Unity 2022 includes multiple ShellScript fragments, incorrectly deleting non-repeated fragments.
- [fix] Fixes the bug where the temporary directory name is `WinxinMiniGame{xxx}` when `TextureCompression` is not the default value on the WeChat Mini Games platform, causing the `scriptingassemblies.json` file to not be successfully modified.
- [fix] Fixes the bug where the WeChat Mini Games platform on the Unity Engine, due to the definition of both `UNITY_WEIXINMINIGAME` and `UNITY_WEBGL` macros, fails to find the `scriptingassemblies.json` file from the wrong path, resulting in a script missing bug at runtime.

## 6.2.0

Release Date: 2024-7-1.

### Runtime

- [merge] Merges changes from versions 2021.3.27f1 to 2021.3.40f1.
- [opt] Optimizes metadata memory, reducing memory usage by 20-25%.
- [opt] Optimizes the implementation of `GetHashCode` for enum types, no longer generating GC.

## 6.1.0

Release Date: 2024-6-17.

### Runtime

- [merge] Merges changes from versions 2022.3.23f1 to 2022.3.33f1, fixing incompatibility issues with version 2022.3.33.
- [new] Supports the new function return value Attribute added in version 2022.3.33.
- [fix] Fixes the bug where `FieldInfo` calling `GetFieldMarshaledSizeForField` crashes.

### Editor

- [fix] Upgrades the dnlib version, fixing the serious bug where `ModuleMD` saves dlls without setting the assembly-qualified `mscorlib` assembly types to the current assembly.
- [fix] Fixes the issue where `Generate/LinkXml` generates a `link.xml` that preserves all `UnityEngine.Debug`, causing compilation errors on iOS and visionOS platforms with Unity 2023 and higher versions. This bug is caused by Unity, and we temporarily solve this problem by ignoring the `UnityEngine.Debug` class when generating `link.xml`.

## 6.0.0

Release Date: 2024-6-11.

### Runtime

- [new] Supports Unity 6000.x.y and Unity 2023.2.x versions.
- [refactor] Merges `ReversePInvokeMethodStub` into `MethodBridge`, and moves ReversePInvoke-related code from `MetadataModule` to `InterpreterModule`.
- [new] Supports MonoPInvokeCallback functions with parameters or return types as struct types.

### Editor

- [new] Supports Unity 6000.x.y and Unity 2023.2.x versions.
- [new] Supports MonoPInvokeCallback functions with parameters or return types as struct types.
- [new] Adds `GeneratedAOTGenericReferenceExcludeExistsAOTClassAndMethods`, which calculates hot update references to AOT generic types and functions, excluding those already existing in AOT, ultimately generating a more accurate list of supplementary metadata assembly programs.
- [fix] Fixes the bug where `CopyStrippedAOTAssemblies` class has compilation errors on some Unity versions that do not support visionOS.
- [fix] Fixes the bug where calculating the `CallingConvention` of `MonoPInvokeCallback` is incorrectly treated as Winapi if the delegate is defined in another assembly, resulting in an incorrect wrapper signature calculation.
- [fix] PatchScriptingAssemblyList.cs has compilation errors on Unity 2023+ WebGL platforms.
- [fix] Fixes the bug where calculating Native2Manager bridge functions does not consider MonoPInvokeCallback functions, leading to `UnsupportedNative2ManagedMethod` when calling C# hot update functions from Lua or other languages.
- [refactor] Merges `ReversePInvokeMethodStub` into `MethodBridge`, and moves ReversePInvoke-related code from `MetadataModule` to `InterpreterModule`.
- [opt] Checks if the development option during packaging is consistent with the current development option. Switching the development option after `Generate/All` and then packaging will cause serious crashes.
- [opt] `Generate/All` checks if HybridCLR is installed before generating.

## 5.4.1

Release Date: 2024-5-30.

### Editor

- [new] Supports the visionOS platform.
- [fix]**[Serious]** Fixes the bug where calculating `MonoPInvokeCallback`'s `CallingConvention` incorrectly treats it as Winapi if the delegate is defined in another assembly, resulting in an incorrect wrapper signature calculation.
- [fix] Fixes the bug where the wrong Unity-iPhone.xcodeproj path is used on tvOS platforms, causing the project.pbxproj to not be found.

## 5.4.0

Release Date: 2024-5-20.

### Runtime

- [new] ReversePInvoke supports CallingConvention.
- [fix] Fixes the bug where `calli`'s `argBasePtr=argIdx[0]` when the number of arguments is 0, due to `argIdxs` not being assigned, causing the function stack frame to point to the wrong location.
- [fix] Fixes the bug where `MetadataModule::GetReversePInvokeWrappe`'s `ComputeSignature` might deadlock.
- [fix] Fixes the bug where AOT base class virtual functions implementing hot update interface functions use `CallInterpVirtual`, causing runtime exceptions.
- [fix] Fixes the issue where some sub-instructions of the `PREFIX1` prefix instruction are missing and not sorted by instruction number in the Transform.
- [fix] Fixes the bug where the `no.{x}` prefix instruction is 3 bytes long but incorrectly treated as 2 bytes in the Transform.
- [fix] Fixes the bug where the `unaligned.{x}` prefix instruction is 3 bytes long but incorrectly treated as 2 bytes in the Transform.
- [opt] Removes unnecessary `INIT_CLASS` operations in `Interpreter_Execute`, as `PREPARE_NEW_FRAME_FROM_NATIVE` will always check.
- [opt] No longer caches MethodBody of non-generic functions, optimizing memory.
- [opt] **Optimizes supplementary metadata memory**, saving approximately 2.8 times the size of metadata dll memory.
- [refactor] Changes the type of the `_rawImage` field in `Image` from `RawImage` to `RawImage*`.

### Editor

- [new] ReversePInvoke supports CallingConvention.
- [fix] Fixes the bug where calculating the equivalence of structs by flattening and expanding them does not apply on some platforms. For example, struct A { uint8_t x; A2 y; } struct A2 { uint8_t x; int32_t y;}; and struct B { uint8_t x; uint8_t y; int32_t z; } are not equivalent under the x86_64 ABI.
- [fix] Fixes the bug where appending to an existing xcode project causes the 'Run Script' command to be duplicated the first time and subsequently fails to find --external-lib-il2-cpp, printing an error log.

## 5.3.0

Release Date: 2024-4-22.

### Runtime

- [fix] Fixes the bug where MachineState::CollectFramesWithoutDuplicates incorrectly uses `hybridclr::metadata::IsInterpreterMethod` to remove hot update functions, leading to an increasingly long StackFrames list and an infinite loop when printing the stack. The implementation is adjusted to uniformly use `il2cpp::vm::StackTrace::PushFrame` and `PopFrame` for perfect interpreter stack printing. The downside is the increased overhead of maintaining the stack when calling interpreter functions.
- [fix] Fixes the serious bug where `StringUtils::Utf16ToUtf8` does not correctly handle `maxinumSize==0`, causing a significant overflow when converting strings of length 0 in `InterpreterImage::ConvertConstValue`.
- [fix] Fixes the bug where `_ReversePInvokeMethod_XXX` functions do not set `Il2CppThreadContext`, causing a crash when obtaining thread variables from native threads.
- [merge] Merges il2cpp changes from versions 2021.3.34 to 2021.3.37f1.
- [merge] Merges il2cpp changes from versions 2022.3.19 to 2022.3.23f1.

### Editor

- [fix] Fixes the bug where exporting a tvOS project does not modify xcode project settings, causing packaging to fail.
- [fix] Fixes the bug where building for tvOS targets does not copy the pruned AOT dll, causing bridge function generation to fail.
- [fix] Solves the issue where the locationPathName generated by `StripAOTDllCommand` is not standardized, causing incompatibility with some plugins like the Embedded Browser.
- [fix] Fixes the bug where deleting the `TUANJIE_2022` macro in Unity Engine 1.1.0 does not copy the pruned AOT assembly.
- [fix] Fixes the bug where `_ReversePInvokeMethod_XXX` functions do not set `Il2CppThreadContext`, causing a crash when obtaining thread variables from native threads.
- [fix] Fixes the bug where iOS platform mono-related header files are not found when the development build option is enabled.

## 5.2.1

Release Date: 2024-4-7.

### Runtime

- [fix] Fixes the bug where stack logs are not printed on the WebGL platform.
- [fix] Fixes the bug where `RuntimeConfig::GetRuntimeOption` incorrectly returns `s_threadFrameStackSize` for `InterpreterThreadExceptionFlowSize`.

### Editor

- [opt] Sets `mod.EnableTypeDefFindCache = true` in `LoadModule`, reducing the time to calculate bridge functions to one-third of the original.
- [fix] Fixes the bug where renaming the xcode project file to `Tuanjie-iPhone.xcodeproj` when exporting for the Unity Engine platform causes xcode project construction to fail.

## 5.2.0

Release Date: 2024-3-25.

### Runtime

- [new] Supports the Unity Engine.
- [new] Supports function pointers, supporting IL2CPP_TYPE_FNPTR type.
- [fix] Fixes the bug where the `SetMdArrElementVarVar_ref` instruction does not SetWriteBarrier.
- [fix] Fixes the bug where `InvokeSingleDelegate` crashes when calling a generic function without supplementary metadata.
- [fix] Fixes the bug where `InterpreterDelegateInvoke` crashes when calling a delegate pointing to a generic function without supplementary metadata.
- [fix] Fixes the bug where `RawImage::GetBlobFromRawIndex` fails when the BlobStream is empty.
- [change] Refactorizes the metadata index design, allowing up to 3 64M dlls, 16 16M dlls, 64 4M dlls, and 255 1M dlls to be allocated.

### Editor

- [new] Supports the Unity Engine.
- [fix] Fixes the bug where `GenericArgumentContext` does not support `ElementType.FnPtr`.
- [change] Adds the `[Preserve]` attribute to RuntimeApi to prevent  it from being pruned.

## 5.1.0

Release Date: 2024-2-26.

### Runtime

- [fix] Fixes the runtime error caused by not implementing `System.ByReference`1's .ctor and get_Value functions in 2021, where il2cpp runs normally through special instinct functions.
- [opt] Optimizes metadata loading by delaying the loading of some metadata, reducing the execution time of `Assembly::Load` by approximately 30%.
- [change] Changes `tempRet` from a local variable in `Interpreter::Execute` to a local variable in `CallDelegateInvoke_xxx`, reducing the possibility of stack overflow when nesting is too deep.

## 5.0.0

Release Date: 2024-1-26.

### Runtime

- [new] Restores support for 2019.
- [fix] Fixes the bug where dlls are not loaded in dependency order, and since the assembly list at the time of image creation is cached, if dependent assemblies are loaded after this assembly, delayed access may result in `TypeLoadedException` due to not being in the cached assembly list.

### Editor

- [new] Restores support for 2019.
- [new] Supports building 2019 on the iOS platform in source form.
- [new] Adds AOTAssemblyMetadataStripper to remove non-generic function metadata from AOT dlls.
- [new] Adds MissingMetadataChecker to check for missing types or function metadata.
- [opt] Optimizes AOTReference calculations; if all generic parameters of a generic are class-constrained, they are not added to the set of metadata that needs to be supplemented.
- [change] Makes some adjustments to support the Unity Engine (note that the il2cpp_plus branch supporting the Unity Engine has not been made public).

## 4.0.15

Release Date: 2024-1-2.

### Runtime

- [fix] Fixes the serious bug where the size of the instance of a not fully instantiated generic class is calculated as `sizeof(void*)`, resulting in an invalid and excessively large instance. This causes an error when using the generic base class instance to overwrite the instance type value set during `LayoutFieldsLocked` in `UpdateInstanceSizeForGenericClass`.
- [change] Supports printing hot update stacks, although the order is not quite correct.
- [change] Replaces IL2CPP_MALLOC with HYBRIDCLR_MALLOC and similar allocation functions.
- [refactor] Refactorizes the Config interface to统一ly retrieve and set options through `GetRuntimeOption` and `SetRuntimeOption`.
- [opt] Removes unnecessary memset operations on structures for `NewValueTypeVar` and `NewValueTypeInterpVar` instructions.

### Editor

- [fix] Fixes the bug where entering `-nullable:enable` in Additional Compiler Arguments throws an `InvalidCastException` in the Editor. Reported at https://github.com/focus-creative-games/hybridclr/issues/116
- [fix] Fixes the error: `BuildFailedException: Build path contains a project previously built without the "Create Visual Studio Solution"`
- [opt] Optimizes bridge function generation by mapping isomorphic structs to the same structure, reducing the number of bridge functions by 30-35%.
- [change] `StripAOTDllCommand` no longer sets the `BuildScriptsOnly` option when exporting.
- [change] Adjusts the display content of the Installer window.
- [refactor] Centralizes the functionality of setting hybridclr parameters in RuntimeApi through `GetRuntimeOption` and `SetRuntimeOption` functions.

## 4.0.14

Release Date: 2023-12-11.

### Runtime

- [fix] Fixes the bug where optimizing the `box; brtrue|brfalse` sequence unconditionally converts to an unconditional branch statement when the type is a class or nullable type.
- [fix] Fixes the bug where `ClassFieldLayoutCalculator` does not release value objects in each key-value pair of `_classMap`, causing a memory leak.
- [fix] Fixes the bug where calculating the native_size of a struct with `ExplicitLayout` is incorrect.
- [fix] Fixes the bug where when there are virtual functions with identical signatures and virtual generic functions, the override calculation does not consider the generic signature, incorrectly returning a non-matching function, resulting in an incorrect vtable.
- [fix][2021] Fixes the bug where when the faster (smaller) build option is enabled, some fully generic shared AOT functions do not use supplementary metadata to set function pointers, causing errors when called.

## 4.0.13

Release Date: 2023-11-27.

### Runtime

- [fix] Fixes the bug where `ConvertInvokeArgs` might pass non-aligned args, causing `CopyStackObject` to crash on platforms like armv7 that require memory alignment.
- [fix] Fixes the serious bug where calculating `ClassFieldLayout` when the size is specified by `StructLayout`.
- [fix] Fixes the bug where instructions like `bgt` do not double-negate the judgment, causing incorrect branch execution when comparing floating-point numbers with NaN due to不对称性.
- [fix] Fixes the serious bug where `Class::FromGenericParameter` incorrectly sets `thread_static_fields_size=-1`, causing ThreadStatic memory allocation for it.
- [opt] Allocates `Il2CppGenericInst`统一ly using `MetadataCache::GetGenericInst` to allocate unique pool objects, optimizing memory allocation.
- [opt] Since some Il2CppGenericInst in the Interpreter uses `MetadataCache::GetGenericInst` uniformly, compare `Il2CppGenericContext` by directly comparing class_inst and method_inst pointers.

### Editor

- [fix] Fixes the bug where pruning aot dll results in an exception when generating bridge functions if netstandard is referenced.
- [fix] Fixes the bug where unusual field names result in compilation errors in the generated bridge function code files.
- [change] Removes the unnecessaryDatas~/Templates directory, using the original files as templates directly.
- [refactor] Refactorizes `AssemblyCache` and `AssemblyReferenceDeepCollector` to eliminate redundant code.

## 4.0.12

Release Date: 2023-11-02.

### Editor

- [fix] Fixes the bug in `BashUtil.RemoveDir` causing Installer installation to fail.

## 4.0.11

Release Date: 2023-11-02.

### Runtime

- [fix] Fixes the bug where when full generic sharing is enabled, for some `MethodInfo`, since `methodPointer` and `virtualMethodPointer` use the interpreter function with supplementary metadata, while `invoker_method` remains in the call form supporting full generic sharing, causing `invoker_method` to mismatch with `methodPointer` and `virtualMethodPointer`.
- [fix] Fixes the bug where `Il2CppGenericContextCompare` only compares inst pointers, causing a large number of duplicate generic functions in the hot update module.
- [fix] Fixes the bug where `MethodInfo` is not correctly set when full generic sharing is enabled.

### Editor

- [new] Checks if the currently installed libil2cpp version matches the package version to avoid issues when upgrading the package without reinstalling.
- [new] `Generate` supports netstandard.
- [fix] Fixes the bug where `ReversePInvokeWrap` generates unnecessarily, parsing referenced dlls, causing parsing errors if aot dll references netstandard.
- [fix] Fixes the bug where `BashUtil.RemoveDir` occasionally fails to delete directories. Adds multiple retries.
- [fix] Fixes the bug where bridge function calculation does not reduce function parameter types, resulting in multiple functions with the same signature.

## 4.0.10

Release Date: 2023-10-12.

### Runtime

- [merge][il2cpp] Merges il2cpp changes from versions 2022.3.10 to 2022.3.11f1, fixing incompatibility issues with version 2022.3.11.

## 4.0.9

Release Date: 2023-10-11.

### Runtime

- [merge][il2cpp][fix] Merges il2cpp changes from versions 2021.3.29 to 2021.3.31f1, fixing incompatibility issues with version 2021.3.31.
- [merge][il2cpp] Merges il2cpp changes from versions 2022.3.7 to 2022.3.10f1.

### Editor

- [fix] Fixes the compilation error with `AddLil2cppSourceCodeToXcodeproj2022OrNewer` on the iOS platform for Unity 2022 versions.

## 4.0.8

Release Date: 2023-10-10.

### Runtime

- [fix] Fixes the bug where calculating the bridge function signature for value type generic bridge functions incorrectly replaces the value type generic parameter type with the signature, resulting in an inconsistent signature with the Editor calculation.
- [fix][refactor] Changes RuntimeApi related functions from PInvoke to InternalCall, solving the issue of reloading libil2cpp.a when calling RuntimeApi on Android platforms.

### Editor

- [refactor] Changes RuntimeApi related functions from PInvoke to InternalCall .
- [refactor] Adjusts some non-standard namespace names in the HybridCLR.Editor module.

## 4.0.7

Release Date: 2023-10-09.

### Runtime

- [fix] Fixes the bug where `initobj` calls `CopyN`, but `CopyN` does not consider object memory alignment, which may cause unaligned access exceptions on platforms like 32-bit.
- [fix] Fixes the bug where calculating the bridge function signature for not fully instantiated generic functions crashes.
- [fix] Fixes the bug where `GenericMethod::CreateMethodLocked` has issues when the Il2cpp code generation option is faster (smaller) for versions 2021 and 2022.
- [remove] Removes all array-related instructions with int64_t indices to simplify the code.
- [remove] Removes the `ldfld_xxx_ref` series of instructions.

### Editor

- [fix] Fixes the bug where generating bridge functions does not generate bridge functions for an aot assembly if the hot update assembly does not directly reference any code, resulting in a `NotSupportNative2Managed` exception.
- [fix] Fixes the bug where copying files fails on Mac due to excessively long paths.
- [fix] Fixes the bug where publishing for PS5 targets does not process `ScriptingAssemblies.json`.
- [change] Clears the pruned aot dll directory when packaging.

## 4.0.6

Release Date: 2023-09-26.

### Runtime

- [fix] Fixes the bug with versions 2021 and 2022 when full generic sharing is enabled.
- [fix] Fixes the bug where loading a PlaceHolder Assembly does not increase `assemblyVersion`, causing `Assembly::GetAssemblies()` to incorrectly obtain an old assembly list.

## 4.0.5

Release Date: 2023-09-25.

### Runtime

- [fix] Fixes the bug where `Transform` does not destruct `pendingFlows`, causing a memory leak.
- [fix] Fixes the bug where `SetMdArrElement` does not distinguish between structures with and without ref.
- [fix] Fixes the bug where `CpobjVarVAr_WriteBarrier_n_4` does not set the size.
- [fix] Fixes the bug where calculating interface member function slots does not consider static and similar functions.
- [fix] Fixes the bug where `ExplicitLayout` is not set for layout.alignment in version 2022, resulting in a size of 0.
- [fix] Fixes the bug where `InterpreterInvoke` in full generic sharing may have inconsistent `methodPointer` and `virtualMethodPointer` for class types, causing an error in incrementing the this pointer by 1.
- [fix] Fixes the bug where `ldobj` does not expand data into an int when T is a type like byte with a size less than 4.
- [fix] Fixes the bug where `CopySize` does not consider memory alignment issues.
- [opt] Optimizes `stelem` when the element is a larger struct, unifying it as a structure containing ref.
- [opt] Adjusts the default memory block size of `TemporaryMemoryArena` from 1M to 8K.
- [opt] Changes `Assembly::GetAllAssemblies()` in `Image::Image` to `Assembly::GetAllAssemblies(AssemblyVector&)`, avoiding the creation of an assembly snapshot and preventing unnecessary memory leaks.

### Editor

- [fix] Fixes the bug where `DllImport` for the StandaloneLinux platform has incorrect dllName and pruned dll path errors.
- [change] For Unity versions with minor incompatibility, installation is no longer prohibited, but a warning is displayed instead.
- [fix] Fixes the bug where `MetaUtil.ToShareTypeSig` calculates `Ptr` and `ByRef` as `IntPtr` in bridge function calculations, which should correctly be `UIntPtr`.

## 4.0.4

Release Date: 2023-09-11.

### Runtime

- [new][platform] Fully supports all platforms, including UWP and PS5.
- [fix][serious] Fixes the bug where calculating the bridge function signature for interpreter parts of enum types is incorrect.
- [fix] Fixes compilation errors on some platforms.
- [fix] Fixes the bug where converting STOBJ instructions does not correctly handle incremental GC.
- [fix] Fixes the bug where the `StindVarVar_ref` instruction does not correctly set WriteBarrier.
- [fix] Fixes the thread safety issue where `GenericMethod::CreateMethodLocked` calls `vm::MetadataAllocGenericMethod()` without holding the `s_GenericMethodMutex` lock in version 2020.

### Editor

- [fix] Fixes the bug where `AddLil2cppSourceCodeToXcodeproj2021OrOlder` includes two ThreadPool.cpp files in different directories, causing compilation errors in Unity 2020.
- [fix] Fixes the bug where obtaining `BuildGroupTarget` from `EditorUserBuildSettings.selectedBuildTargetGroup` is incorrect.
- [fix] `StripAOTDllCommand` generates AOT dlls with the current Player settings to avoid serious mismatches between supplementary metadata and bridge function generation when packaging with development enabled.
- [change] To better support all platforms, adjusts the implementation of dllName in RuntimeApi.cs to default to `__Internal`.
- [change] To better support all platforms, all AOT dll pruning since 2021 is done through MonoHook copying.

## 4.0.3

Release Date: 2023-08-31.

### Editor

- [fix] Fixes the bug in bridge function calculation.

## 4.0.2

Release Date: 2023-08-29.

### Runtime

- [fix][serious] Fixes the bug in `LdobjVarVar_ref` instruction. This bug was introduced by incremental GC code.
- [fix] Fixes the bug where `ResolveField` obtaining a Field as nullptr is not handled, causing a crash.
- [fix] Fixes the bug where AOT and interpreter interface explicitly implement parent interface functions are not correctly handled.

## 4.0.1

Release Date: 2023-08-28.

### Runtime

- [fix] Fixes the compilation error when incremental GC is enabled in version 2020.

## 4.0.0

Release Date: 2023-08-28.

### Runtime

- [new] Supports incremental GC.
- [refactor] Refactorizes bridge functions to fully support all platforms supported by il2cpp.
- [opt] Significantly optimizes Native2Managed direction parameter passing.

### Editor

- [change] Removes incremental GC option checks.
- [refactor] Refactorizes bridge function generation.

## 3.4.2

Release Date: 2023-08-14.

### Runtime

- [fix] Fixes the bug in `RawImage::LoadTables` reading `_4byteGUIDIndex`.
- [version] Supports version 2022.3.7.
- [version] Supports version 2021.3.29.

### Editor

- [fix] Fixes the bug where calculating AOTGenericReference does not consider generic calls on generics, resulting in fewer calculated generics and supplementary metadata.

## 3.4.1

Release Date: 2023-07-31.

### Runtime

- [fix] Fixes the memory visibility issue in `InitializeRuntimeMetadata`.
- [fix] Fixes the bug where `CustomAttribute` does not correctly handle parent NamedArg, causing a crash.
- [opt] Optimizes the code for Transform Instinct instructions, quickly looking up in the HashMap instead of matching one by one.

### Editor

- [fix] Fixes the bug where `FilterHotFixAssemblies` only compares the tail of the assembly name, causing an assembly to be unexpectedly filtered if it matches the tail of an AOT assembly.
- [change] Checks that the assembly name in the hot update assembly list configuration in Settings is not empty.

## 3.4.0

Release Date: 2023-07-17.

### Runtime

- [version] Supports versions 2021.3.28 and 2022.3.4.
- [opt] Removes unnecessary memset after allocating `_StackBase` in `MachineState::InitEvalStack`.
- [fix] Fixes the exception mechanism bug.
- [fix] Fixes the bug where `CustomAttribute` does not support Type[] type parameters.
- [fix] Fixes the issue where the new string(xxx) syntax is not supported.
- [refactor] Refactorizes VTableSetup implementation.
- [fix] Fixes the bug where functions explicitly implementing parent interfaces in subinterfaces are not calculated.
- [opt] Lazily initializes `CustomAttributeData` instead of initializing all at load time, significantly reducing `Assembly.Load` time.
- [fix] Fixes the bug where new byte[]{a,b,c...} initialization of longer byte[] data returns incorrect data in version 2022.

### Editor

- [fix] Fixes the bug where calculating bridge functions does not consider Native2Managed calls that may be included in generic class member functions.
- [change] The default output paths for link.xml and AOTGenericReferences.cs are changed to HybridCLRGenerate to avoid confusion  with the top-level HybridCLRData.
- [fix] Fixes the bug where the include path in the lump file generated on Windows uses \ as the directory separator, causing path not found errors when synchronized to Mac.
- [refactor] Refactorizes the Installer.

## 3.3.0 

Release Date: 2023-07-03.

### Runtime

- [fix] Fixes the bug where memory allocated by localloc is not released.
- [change] `MachineState` uses RegisterRoot to register the execution stack, avoiding GC scanning of the entire stack.
- [opt] Optimizes the performance of Managed2NativeCallByReflectionInvoke by calculating the parameter passing method in advance.
- [refactor] Refactorizes ConvertInvokeArgs.

### Editor

- [fix] Fixes the bug where compiling libil2cpp.a for 2020-2021 does not include brotli-related code files, resulting in compilation errors.
- [fix] Fixes the bug where exporting an xcode project includes absolute paths, causing path not found errors when compiled on other machines.
- [fix] Solves the instability issues in generating LinkXml, MethodBridge, AOTGenericReference, and ReversePInvokeWrap.
- [fix] Fixes the exception when opening the Installer with an incompatible version.
- [change] When hybridclr is disabled, packaging iOS no longer modifies the exported xcode project.

## 3.2.1

### Runtime

- [fix] Fixes the bug where il2cpp TypeNameParser does not remove escape characters '\' from type names, causing nested child types to not be found.

### Editor

- [new] The Installer interface adds display of package version.
- [new] CompileDll adds MacOS, Linux, and WebGL targets.
- [fix] Fixes the help documentation link errors after refactoring the documentation site.
- [change] Adds using qualifiers to Analyzer to resolve compilation conflicts with project types that have the same name.

## 3.2.0

### Runtime

- [fix] Fixes the bug where if an Assembly is not in the PlaceHolder, and there is no interpreter stack, `Class::resolve_parse_info_internal` cannot find the type due to not being in the Assembly list.

### Editor

- [new] Supports packaging iOS directly from source code, no longer needing to compile libil2cpp.a separately.
- [opt] Optimizes error prompts for incompatible versions, no longer throwing exceptions, but displaying "incompatible with the current version".

## 3.1.1

### Runtime

- [fix] Fixes the bug where InterpreterModule::Managed2NativeCallByReflectionInvoke calls value type member functions in 2021 and higher versions, with an extra this=this-1 operation.
- [fix] Fixes the bug where parsing CustomAttribute Enum[] type fields.
- [fix] Fixes the bug where invoking the Invoke function of a closed Delegate via reflection in 2021 and higher versions does not repair the target pointer.

### Editor

- [fix] Fixes compilation errors for Win32, Android32, and WebGL platforms.
- [fix] Fixes the bug where calculating bridge functions does not consider supplementary metadata generic instantiation, which may access some non-public functions, resulting in fewer necessary bridge functions being generated.
- [opt] When generating AOTGenericReferences, the supplementary metadata assembly list is changed from comments to List<string> lists for easy direct use in code.
- [change] CheckSettings no longer automatically sets Api Compatible Level.

## 3.1.0

### Runtime

- [rollback] Reverts support for Unity 2020.3.x.
- [fix] Fixes the WebGL platform ABI bug.

### Editor

- [rollback] Reverts support for Unity 2020.3.x.

## 3.0.3

### Runtime

- [fix] Fixes the bug where Enum::GetValues returns incorrect values.

## 3.0.2

### Runtime

- [fix] Fixes the bug where creating a memory snapshot in Memory Profiler crashes.

### Editor

- [remove] Removes the `HybridCLR/CreateAOTDllSnapshot` menu.

## 3.0.1

### Runtime

- [new] Supports version 2022.3.0.

## 3.0.0

### Runtime

- [fix] Fixes the bug where accessing CustomData fields and values is not supported.
- [remove] Removes support for 2019 and 2020 versions.

### Editor

- Changes the package name to com.code-philosophy.hybridclr.
- Removes the UnityFS plugin.
- Removes the Zip plugin.
- Adjusts the HybridCLR menu location.

## 2.4.2

### Runtime

- [version] Supports 2020.3.48, the last 2020 LTS version.
- [version] Supports 2021.3.25.

## 2.4.1

### Runtime

### Editor

- [fix] Fixes the遗漏 RELEASELOG.md.meta file issue.

## 2.4.0

### Runtime

### Editor

- [new] CheckSettings checks ScriptingBackend and ApiCompatibleLevel, switching to the correct values.
- [new] Adds MsvcStdextWorkaround.cs to solve stdext compilation errors in 2020 vs.
- [fix] Fixes the bug where calculating bridge function signatures for structs containing only one float or double field is incorrect on arm64.

## 2.3.1

### Runtime

### Editor

- [fix] Fixes the bug where copying libil2cpp locally still downloads and installs from the repository.

## 2.3.0

### Runtime

### Editor

- [new] The Installer supports copying modified libil2cpp from a local directory.
- [fix] Fixes the bug where the MonoBleedingEdge subdirectory in version 2019 includes files with excessively long paths, causing the Installer to fail when copying files.




