using dnlib.DotNet;
using HybridCLR.Editor.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Editor.MethodBridge
{

    public class Analyzer
    {
        public class Options
        {
            public AssemblyReferenceDeepCollector Collector { get; set; }

            public int MaxIterationCount { get; set; }
        }

        private readonly int _maxInterationCount;
        private readonly AssemblyReferenceDeepCollector _assemblyCollector;

        private readonly object _lock = new object();

        private readonly List<TypeDef> _typeDefs = new List<TypeDef>();
        private readonly List<MethodDef> _notGenericMethods = new List<MethodDef>();

        private readonly HashSet<GenericClass> _genericTypes = new HashSet<GenericClass>();
        private readonly HashSet<GenericMethod> _genericMethods = new HashSet<GenericMethod>();

        private List<GenericMethod> _processingMethods = new List<GenericMethod>();
        private List<GenericMethod> _newMethods = new List<GenericMethod>();

        public IReadOnlyList<TypeDef> TypeDefs => _typeDefs;

        public IReadOnlyList<MethodDef> NotGenericMethods => _notGenericMethods;

        public IReadOnlyCollection<GenericClass> GenericTypes => _genericTypes;

        public IReadOnlyCollection<GenericMethod> GenericMethods => _genericMethods;


        private readonly MethodReferenceAnalyzer _methodReferenceAnalyzer;

        public Analyzer(Options options)
        {
            _maxInterationCount = options.MaxIterationCount;
            _assemblyCollector = options.Collector;
            _methodReferenceAnalyzer = new MethodReferenceAnalyzer(this.OnNewMethod);
        }

        private void TryAddAndWalkGenericType(GenericClass gc)
        {
            if (gc == null)
            {
                return;
            }
            lock(_lock)
            {
                gc = gc.ToGenericShare();
                if (_genericTypes.Add(gc))
                {
                    WalkType(gc);
                }
            }
        }

        private void OnNewMethod(GenericMethod method)
        {
            lock(_lock)
            {
                if (_genericMethods.Add(method))
                {
                    _newMethods.Add(method);
                }
                if (method.KlassInst != null)
                {
                    TryAddAndWalkGenericType(new GenericClass(method.Method.DeclaringType, method.KlassInst));
                }
            }
        }

        private void WalkType(GenericClass gc)
        {
            //Debug.Log($"typespec:{sig} {sig.GenericType} {sig.GenericType.TypeDefOrRef.ResolveTypeDef()}");
            //Debug.Log($"== walk generic type:{new GenericInstSig(gc.Type.ToTypeSig().ToClassOrValueTypeSig(), gc.KlassInst)}");
            ITypeDefOrRef baseType = gc.Type.BaseType;
            if (baseType != null && baseType.TryGetGenericInstSig() != null)
            {
                GenericClass parentType = GenericClass.ResolveClass((TypeSpec)baseType, new GenericArgumentContext(gc.KlassInst, null));
                TryAddAndWalkGenericType(parentType);
            }
            foreach (var method in gc.Type.Methods)
            {
                if (method.HasGenericParameters)
                {
                    continue;
                }
                var gm = new GenericMethod(method, gc.KlassInst, null).ToGenericShare();
                //Debug.Log($"add method:{gm.Method} {gm.KlassInst}");
                
                if (_genericMethods.Add(gm))
                {
                    if (method.HasBody && method.Body.Instructions != null)
                    {
                        _newMethods.Add(gm);
                    }
                }
            }
        }

        private void WalkType(TypeDef typeDef)
        {
            _typeDefs.Add(typeDef);
            ITypeDefOrRef baseType = typeDef.BaseType;
            if (baseType != null && baseType.TryGetGenericInstSig() != null)
            {
                GenericClass gc = GenericClass.ResolveClass((TypeSpec)baseType, null);
                TryAddAndWalkGenericType(gc);
            }
            foreach (var method in typeDef.Methods)
            {
                // 对于带泛型的参数，统一泛型共享为object
                _notGenericMethods.Add(method);
            }
        }

        private void Prepare()
        {
            // 将所有非泛型函数全部加入函数列表，同时立马walk这些method。
            // 后续迭代中将只遍历MethodSpec
            foreach (var ass in _assemblyCollector.GetLoadedModulesExcludeRootAssemblies())
            {
                foreach (TypeDef typeDef in ass.GetTypes())
                {
                    WalkType(typeDef);
                }

                for (uint rid = 1, n = ass.Metadata.TablesStream.TypeSpecTable.Rows; rid <= n; rid++)
                {
                    var ts = ass.ResolveTypeSpec(rid);
                    if (!ts.ContainsGenericParameter)
                    {
                        var cs = GenericClass.ResolveClass(ts, null)?.ToGenericShare();
                        if (cs != null)
                        {
                            TryAddAndWalkGenericType(cs);
                        }
                    }
                }

                for (uint rid = 1, n = ass.Metadata.TablesStream.MethodSpecTable.Rows; rid <= n; rid++)
                {
                    var ms = ass.ResolveMethodSpec(rid);
                    if(ms.DeclaringType.ContainsGenericParameter || ms.GenericInstMethodSig.ContainsGenericParameter)
                    {
                        continue;
                    }
                    var gm = GenericMethod.ResolveMethod(ms, null)?.ToGenericShare();
                    if (gm == null)
                    {
                        continue;
                    }

                    if (_genericMethods.Add(gm))
                    {
                        _newMethods.Add(gm);
                    }
                    //if (gm.KlassInst != null)
                    //{
                    //    TryAddAndWalkGenericType(new GenericClass(gm.Method.DeclaringType, gm.KlassInst));
                    //}
                }
            }
            Debug.Log($"PostPrepare allMethods:{_notGenericMethods.Count} newMethods:{_newMethods.Count}");
        }

        private void RecursiveCollect()
        {
            for (int i = 0; i < _maxInterationCount && _newMethods.Count > 0; i++)
            {
                var temp = _processingMethods;
                _processingMethods = _newMethods;
                _newMethods = temp;
                _newMethods.Clear();

                Task.WaitAll(_processingMethods.Select(method => Task.Run(() =>
                {
                    _methodReferenceAnalyzer.WalkMethod(method.Method, method.KlassInst, method.MethodInst);
                })).ToArray());
                Debug.Log($"iteration:[{i}] allMethods:{_notGenericMethods.Count} genericClass:{_genericTypes.Count} genericMethods:{_genericMethods.Count} newMethods:{_newMethods.Count}");
            }
        }

        public void Run()
        {
            Prepare();
            RecursiveCollect();
        }
    }
}
