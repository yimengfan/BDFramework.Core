using dnlib.DotNet;
using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Meta;
using HybridCLR.Editor.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using TypeInfo = HybridCLR.Editor.ABI.TypeInfo;

namespace HybridCLR.Editor.MethodBridge
{
    public class Generator
    {
        public class Options
        {
            public PlatformABI PlatformABI { get; set; }

            public string TemplateCode { get; set; }

            public string OutputFile { get; set; }

            public IReadOnlyList<MethodDef> NotGenericMethods { get; set; }

            public IReadOnlyCollection<GenericMethod> GenericMethods { get; set; }
        }

        private PlatformABI _platformABI;

        private readonly IReadOnlyList<MethodDef> _notGenericMethods;

        private readonly IReadOnlyCollection<GenericMethod> _genericMethods;

        private readonly string _templateCode;

        private readonly string _outputFile;

        private readonly PlatformGeneratorBase _platformAdaptor;

        private readonly TypeCreatorBase _typeCreator;

        private readonly HashSet<MethodDesc> _managed2nativeMethodSet = new HashSet<MethodDesc>();

        private List<MethodDesc> _managed2nativeMethodList;

        private readonly HashSet<MethodDesc> _native2managedMethodSet = new HashSet<MethodDesc>();

        private List<MethodDesc> _native2managedMethodList;

        private readonly HashSet<MethodDesc> _adjustThunkMethodSet = new HashSet<MethodDesc>();

        private List<MethodDesc> _adjustThunkMethodList;

        public Generator(Options options)
        {
            _platformABI = options.PlatformABI;
            _notGenericMethods = options.NotGenericMethods;
            _genericMethods = options.GenericMethods;
            _templateCode = options.TemplateCode;
            _outputFile = options.OutputFile;
            _platformAdaptor = CreatePlatformAdaptor(options.PlatformABI);
            _typeCreator = TypeCreatorFactory.CreateTypeCreator(options.PlatformABI);
        }

        private static PlatformGeneratorBase CreatePlatformAdaptor(PlatformABI type)
        {
            switch (type)
            {
                case PlatformABI.Universal32: return new PlatformGeneratorUniversal32();
                case PlatformABI.Universal64: return new PlatformGeneratorUniversal64();
                case PlatformABI.Arm64: return new PlatformGeneratorArm64();
                case PlatformABI.WebGL32: return new PlatformGeneratorWebGL32();
                default: throw new NotSupportedException();
            }
        }

        private MethodDesc CreateMethodDesc(MethodDef methodDef, bool forceRemoveThis, TypeSig returnType, List<TypeSig> parameters)
        {
            var paramInfos = new List<ParamInfo>();
            if (forceRemoveThis && !methodDef.IsStatic)
            {
                parameters.RemoveAt(0);
            }
            foreach (var paramInfo in parameters)
            {
                paramInfos.Add(new ParamInfo() { Type = _typeCreator.CreateTypeInfo(paramInfo) });
            }
            var mbs = new MethodDesc()
            {
                MethodDef = methodDef,
                ReturnInfo = new ReturnInfo() { Type = returnType != null ? _typeCreator.CreateTypeInfo(returnType) : TypeInfo.s_void },
                ParamInfos = paramInfos,
            };
            _typeCreator.OptimizeMethod(mbs);
            return mbs;
        }

        private void AddManaged2NativeMethod(MethodDesc method)
        {
            method.Init();
            _managed2nativeMethodSet.Add(method);
        }

        private void AddNative2ManagedMethod(MethodDesc method)
        {
            method.Init();
            _native2managedMethodSet.Add(method);
        }

        private void AddAdjustThunkMethod(MethodDesc method)
        {
            method.Init();
            _adjustThunkMethodSet.Add(method);
        }

        private void ProcessMethod(MethodDef method, List<TypeSig> klassInst, List<TypeSig> methodInst)
        {
            if (method.IsPrivate || (method.IsAssembly && !method.IsPublic && !method.IsFamily))
            {
                return;
            }

            TypeSig returnType;
            List<TypeSig> parameters;
            if (klassInst == null && methodInst == null)
            {
                returnType = method.ReturnType;
                parameters = method.Parameters.Select(p => p.Type).ToList();
            }
            else
            {
                var gc = new GenericArgumentContext(klassInst, methodInst);
                returnType = MetaUtil.Inflate(method.ReturnType, gc);
                parameters = method.Parameters.Select(p => MetaUtil.Inflate(p.Type, gc)).ToList();
            }

            var m2nMethod = CreateMethodDesc(method, false, returnType, parameters);
            AddManaged2NativeMethod(m2nMethod);

            if (method.IsVirtual)
            {
                if (method.DeclaringType.IsInterface)
                {
                    AddAdjustThunkMethod(m2nMethod);
                }
                //var adjustThunkMethod = CreateMethodDesc(method, true, returnType, parameters);
                AddNative2ManagedMethod(m2nMethod);
            }
            if (method.Name == "Invoke" && method.DeclaringType.IsDelegate)
            {
                var openMethod = CreateMethodDesc(method, true, returnType, parameters);
                AddNative2ManagedMethod(openMethod);
            }
        }

        public void PrepareMethods()
        {
            foreach(var method in _notGenericMethods)
            {
                ProcessMethod(method, null, null);
            }

            foreach(var method in _genericMethods)
            {
                ProcessMethod(method.Method, method.KlassInst, method.MethodInst);
            }
            
            {
                var sortedMethods = new SortedDictionary<string, MethodDesc>();
                foreach (var method in _managed2nativeMethodSet)
                {
                    sortedMethods.Add(method.CreateCallSigName(), method);
                }
                _managed2nativeMethodList = sortedMethods.Values.ToList();
            }
            {
                var sortedMethods = new SortedDictionary<string, MethodDesc>();
                foreach (var method in _native2managedMethodSet)
                {
                    sortedMethods.Add(method.CreateCallSigName(), method);
                }
                _native2managedMethodList = sortedMethods.Values.ToList();
            }
            {
                var sortedMethods = new SortedDictionary<string, MethodDesc>();
                foreach (var method in _adjustThunkMethodSet)
                {
                    sortedMethods.Add(method.CreateCallSigName(), method);
                }
                _adjustThunkMethodList = sortedMethods.Values.ToList();
            }
        }

        public void Generate()
        {
            var frr = new FileRegionReplace(_templateCode.Replace("{PLATFORM_ABI}", ABIUtil.GetHybridCLRPlatformMacro(_platformABI)));

            List<string> lines = new List<string>(20_0000);

            Debug.LogFormat("== managed2native:{0} native2managed:{1} adjustThunk:{2}",
                _managed2nativeMethodList.Count, _native2managedMethodList.Count, _adjustThunkMethodList.Count);

            foreach(var method in _managed2nativeMethodList)
            {
                _platformAdaptor.GenerateManaged2NativeMethod(method, lines);
            }

            _platformAdaptor.GenerateManaged2NativeStub(_managed2nativeMethodList, lines);

            foreach (var method in _native2managedMethodList)
            {
                _platformAdaptor.GenerateNative2ManagedMethod(method, lines);
            }

            _platformAdaptor.GenerateNative2ManagedStub(_native2managedMethodList, lines);

            foreach (var method in _adjustThunkMethodList)
            {
                _platformAdaptor.GenerateAdjustThunkMethod(method, lines);
            }

            _platformAdaptor.GenerateAdjustThunkStub(_adjustThunkMethodList, lines);

            frr.Replace("CODE", string.Join("\n", lines));

            Directory.CreateDirectory(Path.GetDirectoryName(_outputFile));

            frr.Commit(_outputFile);
        }

    }
}
