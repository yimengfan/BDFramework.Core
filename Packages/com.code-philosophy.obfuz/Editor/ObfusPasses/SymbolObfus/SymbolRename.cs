// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using dnlib.DotNet;
using Obfuz.ObfusPasses.SymbolObfus.NameMakers;
using Obfuz.ObfusPasses.SymbolObfus.Policies;
using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public class SymbolRename
    {
        private readonly bool _useConsistentNamespaceObfuscation;
        private readonly List<string> _obfuscationRuleFiles;
        private readonly string _mappingXmlPath;

        private AssemblyCache _assemblyCache;

        private List<ModuleDef> _toObfuscatedModules;
        private List<ModuleDef> _obfuscatedAndNotObfuscatedModules;
        private HashSet<ModuleDef> _toObfuscatedModuleSet;
        private HashSet<ModuleDef> _nonObfuscatedButReferencingObfuscatedModuleSet;
        private IObfuscationPolicy _renamePolicy;
        private INameMaker _nameMaker;
        private readonly Dictionary<ModuleDef, List<CustomAttributeInfo>> _customAttributeArgumentsWithTypeByMods = new Dictionary<ModuleDef, List<CustomAttributeInfo>>();
        private readonly RenameRecordMap _renameRecordMap;
        private readonly VirtualMethodGroupCalculator _virtualMethodGroupCalculator;
        private readonly List<Type> _customPolicyTypes;

        class CustomAttributeInfo
        {
            public CustomAttributeCollection customAttributes;
            public int index;
            public List<CAArgument> arguments;
            public List<CANamedArgument> namedArguments;
        }

        public SymbolRename(SymbolObfuscationSettingsFacade settings)
        {
            _useConsistentNamespaceObfuscation = settings.useConsistentNamespaceObfuscation;
            _mappingXmlPath = settings.symbolMappingFile;
            _obfuscationRuleFiles = settings.ruleFiles.ToList();
            _renameRecordMap = new RenameRecordMap(settings.symbolMappingFile, settings.debug, settings.keepUnknownSymbolInSymbolMappingFile);
            _virtualMethodGroupCalculator = new VirtualMethodGroupCalculator();
            _nameMaker = settings.debug ? NameMakerFactory.CreateDebugNameMaker() : NameMakerFactory.CreateNameMakerBaseASCIICharSet(settings.obfuscatedNamePrefix);
            _customPolicyTypes = settings.customRenamePolicyTypes;
        }

        public void Init()
        {
            var ctx = ObfuscationPassContext.Current;
            _assemblyCache = ctx.assemblyCache;
            _toObfuscatedModules = ctx.modulesToObfuscate;
            _obfuscatedAndNotObfuscatedModules = ctx.allObfuscationRelativeModules;
            _toObfuscatedModuleSet = new HashSet<ModuleDef>(ctx.modulesToObfuscate);
            _nonObfuscatedButReferencingObfuscatedModuleSet = new HashSet<ModuleDef>(ctx.allObfuscationRelativeModules.Where(m => !_toObfuscatedModuleSet.Contains(m)));

            _renamePolicy = CreateDefaultRenamePolicy(_obfuscationRuleFiles, _customPolicyTypes);
            BuildCustomAttributeArguments();
        }

        public static IObfuscationPolicy CreateDefaultRenamePolicy(List<string> obfuscationRuleFiles, List<Type> customPolicyTypes)
        {
            var ctx = ObfuscationPassContext.Current;
            var obfuscateRuleConfig = new ConfigurableRenamePolicy(ctx.coreSettings.assembliesToObfuscate, ctx.modulesToObfuscate, obfuscationRuleFiles);
            var totalRenamePolicies = new List<IObfuscationPolicy>
            {
                new SupportPassPolicy(ctx.passPolicy),
                new SystemRenamePolicy(ctx.obfuzIgnoreScopeComputeCache),
                new UnityRenamePolicy(),
                obfuscateRuleConfig,
            };

            foreach (var customPolicyType in customPolicyTypes)
            {
                if (Activator.CreateInstance(customPolicyType, new object[] { null }) is IObfuscationPolicy customPolicy)
                {
                    totalRenamePolicies.Add(customPolicy);
                }
                else
                {
                    Debug.LogWarning($"Custom rename policy type {customPolicyType} is not a valid IObfuscationPolicy");
                }
            }

            IObfuscationPolicy renamePolicy = new CacheRenamePolicy(new CombineRenamePolicy(totalRenamePolicies.ToArray()));
            PrecomputeNeedRename(ctx.modulesToObfuscate, renamePolicy);
            return renamePolicy;
        }

        private static void PrecomputeNeedRename(List<ModuleDef> toObfuscatedModules, IObfuscationPolicy renamePolicy)
        {
            foreach (ModuleDef mod in toObfuscatedModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    renamePolicy.NeedRename(type);
                    foreach (var field in type.Fields)
                    {
                        renamePolicy.NeedRename(field);
                    }
                    foreach (var method in type.Methods)
                    {
                        renamePolicy.NeedRename(method);
                    }
                    foreach (var property in type.Properties)
                    {
                        renamePolicy.NeedRename(property);
                    }
                    foreach (var eventDef in type.Events)
                    {
                        renamePolicy.NeedRename(eventDef);
                    }
                }
            }
        }

        private void CollectCArgumentWithTypeOf(IHasCustomAttribute meta, List<CustomAttributeInfo> customAttributes)
        {
            int index = 0;
            foreach (CustomAttribute ca in meta.CustomAttributes)
            {
                List<CAArgument> arguments = null;
                if (ca.ConstructorArguments.Any(a => MetaUtil.MayRenameCustomDataType(a.Type.ElementType)))
                {
                    arguments = ca.ConstructorArguments.ToList();
                }
                List<CANamedArgument> namedArguments = ca.NamedArguments.Count > 0 ? ca.NamedArguments.ToList() : null;
                if (arguments != null || namedArguments != null)
                {
                    customAttributes.Add(new CustomAttributeInfo
                    {
                        customAttributes = meta.CustomAttributes,
                        index = index,
                        arguments = arguments,
                        namedArguments = namedArguments
                    });
                }
                ++index;
            }
        }

        private void BuildCustomAttributeArguments()
        {
            foreach (ModuleDef mod in _obfuscatedAndNotObfuscatedModules)
            {
                var customAttributes = new List<CustomAttributeInfo>();
                CollectCArgumentWithTypeOf(mod, customAttributes);
                foreach (TypeDef type in mod.GetTypes())
                {
                    CollectCArgumentWithTypeOf(type, customAttributes);
                    foreach (FieldDef field in type.Fields)
                    {
                        CollectCArgumentWithTypeOf(field, customAttributes);
                    }
                    foreach (MethodDef method in type.Methods)
                    {
                        CollectCArgumentWithTypeOf(method, customAttributes);
                        foreach (Parameter param in method.Parameters)
                        {
                            if (param.ParamDef != null)
                            {
                                CollectCArgumentWithTypeOf(param.ParamDef, customAttributes);
                            }
                        }
                    }
                    foreach (PropertyDef property in type.Properties)
                    {
                        CollectCArgumentWithTypeOf(property, customAttributes);
                    }
                    foreach (EventDef eventDef in type.Events)
                    {
                        CollectCArgumentWithTypeOf(eventDef, customAttributes);
                    }
                }

                _customAttributeArgumentsWithTypeByMods.Add(mod, customAttributes);
            }
        }

        public void Process()
        {
            _renameRecordMap.Init(_toObfuscatedModules, _nameMaker);
            RenameTypes();
            RenameFields();
            RenameMethods();
            RenameProperties();
            RenameEvents();
        }

        class RefTypeDefMetas
        {
            public readonly List<TypeRef> typeRefs = new List<TypeRef>();

            public readonly List<CustomAttribute> customAttributes = new List<CustomAttribute>();
        }

        private void BuildRefTypeDefMetasMap(Dictionary<TypeDef, RefTypeDefMetas> refTypeDefMetasMap)
        {
            foreach (ModuleDef mod in _obfuscatedAndNotObfuscatedModules)
            {
                foreach (TypeRef typeRef in mod.GetTypeRefs())
                {
                    if (typeRef.DefinitionAssembly.IsCorLib())
                    {
                        continue;
                    }
                    TypeDef typeDef = typeRef.ResolveThrow();
                    if (!refTypeDefMetasMap.TryGetValue(typeDef, out var typeDefMetas))
                    {
                        typeDefMetas = new RefTypeDefMetas();
                        refTypeDefMetasMap.Add(typeDef, typeDefMetas);
                    }
                    typeDefMetas.typeRefs.Add(typeRef);
                }
            }

            foreach (CustomAttributeInfo cai in _customAttributeArgumentsWithTypeByMods.Values.SelectMany(cas => cas))
            {
                CustomAttribute ca = cai.customAttributes[cai.index];
                TypeDef typeDef = MetaUtil.GetTypeDefOrGenericTypeBaseThrowException(ca.Constructor.DeclaringType);
                if (!refTypeDefMetasMap.TryGetValue(typeDef, out var typeDefMetas))
                {
                    typeDefMetas = new RefTypeDefMetas();
                    refTypeDefMetasMap.Add(typeDef, typeDefMetas);
                }
                typeDefMetas.customAttributes.Add(ca);
            }
        }

        private void RetargetTypeRefInCustomAttributes()
        {
            foreach (CustomAttributeInfo cai in _customAttributeArgumentsWithTypeByMods.Values.SelectMany(cas => cas))
            {
                CustomAttribute ca = cai.customAttributes[cai.index];
                bool anyChange = false;
                if (cai.arguments != null)
                {
                    for (int i = 0; i < cai.arguments.Count; i++)
                    {
                        CAArgument oldArg = cai.arguments[i];
                        if (MetaUtil.TryRetargetTypeRefInArgument(oldArg, out CAArgument newArg))
                        {
                            anyChange = true;
                            cai.arguments[i] = newArg;
                        }
                    }
                }
                if (cai.namedArguments != null)
                {
                    for (int i = 0; i < cai.namedArguments.Count; i++)
                    {
                        if (MetaUtil.TryRetargetTypeRefInNamedArgument(cai.namedArguments[i]))
                        {
                            anyChange = true;
                        }
                    }
                }
                if (anyChange)
                {
                    cai.customAttributes[cai.index] = new CustomAttribute(ca.Constructor,
                        cai.arguments != null ? cai.arguments : ca.ConstructorArguments,
                        cai.namedArguments != null ? cai.namedArguments : ca.NamedArguments);
                }
            }
        }

        private readonly Dictionary<TypeDef, RefTypeDefMetas> _refTypeRefMetasMap = new Dictionary<TypeDef, RefTypeDefMetas>();

        private void RenameTypes()
        {
            //Debug.Log("RenameTypes begin");

            RetargetTypeRefInCustomAttributes();

            BuildRefTypeDefMetasMap(_refTypeRefMetasMap);
            _assemblyCache.EnableTypeDefCache = false;

            foreach (ModuleDef mod in _toObfuscatedModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    if (_renamePolicy.NeedRename(type))
                    {
                        Rename(type, _refTypeRefMetasMap.GetValueOrDefault(type));
                    }
                }
            }

            // clean cache
            _assemblyCache.EnableTypeDefCache = true;
            //Debug.Log("Rename Types end");
        }


        class RefFieldMetas
        {
            public readonly List<MemberRef> fieldRefs = new List<MemberRef>();
            public readonly List<CustomAttribute> customAttributes = new List<CustomAttribute>();
        }


        private void BuildHierarchyFields(TypeDef type, List<FieldDef> fields)
        {
            while (type != null)
            {
                fields.AddRange(type.Fields);
                type = MetaUtil.GetBaseTypeDef(type);
            }
        }

        private IEnumerable<T> WalkAllMethodInstructionOperand<T>(ModuleDef mod)
        {
            foreach (TypeDef type in mod.GetTypes())
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody)
                    {
                        continue;
                    }
                    foreach (var instr in method.Body.Instructions)
                    {
                        if (instr.Operand is T memberRef)
                        {
                            yield return memberRef;
                        }
                    }
                }
            }
        }

        private void BuildRefFieldMetasMap(Dictionary<FieldDef, RefFieldMetas> refFieldMetasMap)
        {
            foreach (ModuleDef mod in _obfuscatedAndNotObfuscatedModules)
            {
                foreach (MemberRef memberRef in WalkAllMethodInstructionOperand<MemberRef>(mod))
                {
                    IMemberRefParent parent = memberRef.Class;
                    TypeDef parentTypeDef = MetaUtil.GetMemberRefTypeDefParentOrNull(parent);
                    if (parentTypeDef == null)
                    {
                        continue;
                    }
                    foreach (FieldDef field in parentTypeDef.Fields)
                    {
                        if (field.Name == memberRef.Name && TypeEqualityComparer.Instance.Equals(field.FieldSig.Type, memberRef.FieldSig.Type))
                        {
                            if (!refFieldMetasMap.TryGetValue(field, out var fieldMetas))
                            {
                                fieldMetas = new RefFieldMetas();
                                refFieldMetasMap.Add(field, fieldMetas);
                            }
                            fieldMetas.fieldRefs.Add(memberRef);
                            break;
                        }
                    }
                }
            }
            foreach (var e in _refTypeRefMetasMap)
            {
                TypeDef typeDef = e.Key;
                var hierarchyFields = new List<FieldDef>();
                BuildHierarchyFields(typeDef, hierarchyFields);
                RefTypeDefMetas typeDefMetas = e.Value;
                foreach (CustomAttribute ca in typeDefMetas.customAttributes)
                {
                    foreach (var arg in ca.NamedArguments)
                    {
                        if (arg.IsProperty)
                        {
                            continue;
                        }
                        foreach (FieldDef field in hierarchyFields)
                        {
                            // FIXME. field of Generic Base Type may not be same
                            if (field.Name == arg.Name && TypeEqualityComparer.Instance.Equals(field.FieldType, arg.Type))
                            {
                                if (!refFieldMetasMap.TryGetValue(field, out var fieldMetas))
                                {
                                    fieldMetas = new RefFieldMetas();
                                    refFieldMetasMap.Add(field, fieldMetas);
                                }
                                fieldMetas.customAttributes.Add(ca);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void RenameFields()
        {
            //Debug.Log("Rename fields begin");
            var refFieldMetasMap = new Dictionary<FieldDef, RefFieldMetas>();
            BuildRefFieldMetasMap(refFieldMetasMap);

            foreach (ModuleDef mod in _toObfuscatedModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (FieldDef field in type.Fields)
                    {
                        if (_renamePolicy.NeedRename(field))
                        {
                            Rename(field, refFieldMetasMap.GetValueOrDefault(field));
                        }
                    }
                }
            }
            //Debug.Log("Rename fields end");
        }

        class RefMethodMetas
        {
            public readonly List<MemberRef> memberRefs = new List<MemberRef>();
        }

        private void RenameMethodRef(MemberRef memberRef, Dictionary<MethodDef, RefMethodMetas> refMethodMetasMap)
        {
            if (!memberRef.IsMethodRef)
            {
                return;
            }

            IMemberRefParent parent = memberRef.Class;
            TypeDef parentTypeDef = MetaUtil.GetMemberRefTypeDefParentOrNull(parent);
            if (parentTypeDef == null)
            {
                return;
            }
            foreach (MethodDef methodDef in parentTypeDef.Methods)
            {
                if (methodDef.Name == memberRef.Name && new SigComparer(default).Equals(methodDef.MethodSig, memberRef.MethodSig))
                {
                    if (!refMethodMetasMap.TryGetValue(methodDef, out var refMethodMetas))
                    {
                        refMethodMetas = new RefMethodMetas();
                        refMethodMetasMap.Add(methodDef, refMethodMetas);
                    }
                    refMethodMetas.memberRefs.Add(memberRef);
                    break;
                }
            }
        }


        private void RenameMethodRefOrMethodSpec(IMethod method, Dictionary<MethodDef, RefMethodMetas> refMethodMetasMap)
        {
            if (method is MemberRef memberRef)
            {
                RenameMethodRef(memberRef, refMethodMetasMap);
            }
            else if (method is MethodSpec methodSpec)
            {
                if (methodSpec.Method is MemberRef memberRef2)
                {
                    RenameMethodRef(memberRef2, refMethodMetasMap);
                }
            }
        }

        private void BuildRefMethodMetasMap(Dictionary<MethodDef, RefMethodMetas> refMethodMetasMap)
        {
            foreach (ModuleDef mod in _obfuscatedAndNotObfuscatedModules)
            {
                foreach (IMethod method in WalkAllMethodInstructionOperand<IMethod>(mod))
                {
                    RenameMethodRefOrMethodSpec(method, refMethodMetasMap);
                }

                foreach (var type in mod.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.HasOverrides)
                        {
                            foreach (MethodOverride methodOverride in method.Overrides)
                            {
                                RenameMethodRefOrMethodSpec(methodOverride.MethodDeclaration, refMethodMetasMap);
                                RenameMethodRefOrMethodSpec(methodOverride.MethodBody, refMethodMetasMap);
                            }
                        }
                    }
                }
                foreach (var e in _refTypeRefMetasMap)
                {
                    TypeDef typeDef = e.Key;
                    var hierarchyFields = new List<FieldDef>();
                    BuildHierarchyFields(typeDef, hierarchyFields);
                    RefTypeDefMetas typeDefMetas = e.Value;
                    foreach (CustomAttribute ca in typeDefMetas.customAttributes)
                    {
                        if (ca.Constructor is IMethod method)
                        {
                            RenameMethodRefOrMethodSpec(method, refMethodMetasMap);
                        }
                    }
                }
            }
        }

        private void RenameMethods()
        {
            //Debug.Log("Rename methods begin");
            //Debug.Log("Rename not virtual methods begin");
            var virtualMethods = new List<MethodDef>();
            var refMethodMetasMap = new Dictionary<MethodDef, RefMethodMetas>();
            BuildRefMethodMetasMap(refMethodMetasMap);
            foreach (ModuleDef mod in _toObfuscatedModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.IsVirtual)
                        {
                            continue;
                        }
                        if (_renamePolicy.NeedRename(method))
                        {
                            Rename(method, refMethodMetasMap.GetValueOrDefault(method));
                        }
                    }
                }
            }

            foreach (ModuleDef mod in _obfuscatedAndNotObfuscatedModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    _virtualMethodGroupCalculator.CalculateType(type);
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.IsVirtual)
                        {
                            virtualMethods.Add(method);
                        }
                    }
                }
            }

            //Debug.Log("Rename not virtual methods end");


            //Debug.Log("Rename virtual methods begin");
            var visitedVirtualMethods = new HashSet<MethodDef>();
            var groupNeedRenames = new Dictionary<VirtualMethodGroup, bool>();
            foreach (var method in virtualMethods)
            {
                if (!visitedVirtualMethods.Add(method))
                {
                    continue;
                }
                VirtualMethodGroup group = _virtualMethodGroupCalculator.GetMethodGroup(method);
                if (!groupNeedRenames.TryGetValue(group, out var needRename))
                {
                    var rootBeInheritedTypes = group.GetRootBeInheritedTypes();
                    // - if the group contains no obfuscated methods
                    // - if the group contains method defined in non-obfuscated module but referencing obfuscated module and virtual method in obfuscated type overrides virtual method from non-obfuscated type
                    if (!group.methods.Any(m => _toObfuscatedModuleSet.Contains(m.DeclaringType.Module)) || group.methods.Any(m => _nonObfuscatedButReferencingObfuscatedModuleSet.Contains(m.Module) && rootBeInheritedTypes.Contains(m.DeclaringType)))
                    {
                        needRename = false;
                    }
                    else
                    {
                        needRename = group.methods.All(m => _obfuscatedAndNotObfuscatedModules.Contains(m.Module) && _renamePolicy.NeedRename(m));
                    }
                    groupNeedRenames.Add(group, needRename);
                    if (needRename)
                    {
                        bool conflict = false;
                        string newVirtualMethodName = null;
                        foreach (MethodDef m in group.methods)
                        {
                            if (_renameRecordMap.TryGetExistRenameMapping(m, out var existVirtualMethodName))
                            {
                                if (newVirtualMethodName == null)
                                {
                                    newVirtualMethodName = existVirtualMethodName;
                                }
                                else if (newVirtualMethodName != existVirtualMethodName)
                                {
                                    Debug.LogWarning($"Virtual method rename conflict. {m} => {existVirtualMethodName} != {newVirtualMethodName}");
                                    conflict = true;
                                    break;
                                }
                            }
                        }
                        if (newVirtualMethodName == null || conflict /*|| _nameMaker.IsNamePreserved(group, newVirtualMethodName)*/)
                        {
                            newVirtualMethodName = _nameMaker.GetNewName(group, method.Name);
                        }
                        _renameRecordMap.InitAndAddRename(group, newVirtualMethodName);
                    }
                }
                if (!needRename)
                {
                    continue;
                }
                if (_renameRecordMap.TryGetRename(group, out var newName))
                {
                    Rename(method, refMethodMetasMap.GetValueOrDefault(method), newName);
                }
                else
                {
                    throw new Exception($"group:{group} method:{method} not found in rename record map");
                }
            }
            //Debug.Log("Rename virtual methods end");
            //Debug.Log("Rename methods end");
        }

        class RefPropertyMetas
        {
            public List<CustomAttribute> customAttributes = new List<CustomAttribute>();
        }

        private void BuildHierarchyProperties(TypeDef type, List<PropertyDef> properties)
        {
            while (type != null)
            {
                properties.AddRange(type.Properties);
                type = MetaUtil.GetBaseTypeDef(type);
            }
        }

        private void BuildRefPropertyMetasMap(Dictionary<PropertyDef, RefPropertyMetas> refPropertyMetasMap)
        {
            foreach (var e in _refTypeRefMetasMap)
            {
                TypeDef typeDef = e.Key;
                var hierarchyProperties = new List<PropertyDef>();
                BuildHierarchyProperties(typeDef, hierarchyProperties);
                RefTypeDefMetas typeDefMetas = e.Value;
                foreach (CustomAttribute ca in typeDefMetas.customAttributes)
                {
                    foreach (var arg in ca.NamedArguments)
                    {
                        if (arg.IsField)
                        {
                            continue;
                        }
                        foreach (PropertyDef field in hierarchyProperties)
                        {
                            // FIXME. field of Generic Base Type may not be same
                            if (field.Name == arg.Name && TypeEqualityComparer.Instance.Equals(arg.Type, field.PropertySig.RetType))
                            {
                                if (!refPropertyMetasMap.TryGetValue(field, out var fieldMetas))
                                {
                                    fieldMetas = new RefPropertyMetas();
                                    refPropertyMetasMap.Add(field, fieldMetas);
                                }
                                fieldMetas.customAttributes.Add(ca);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void RenameProperties()
        {
            //Debug.Log("Rename properties begin");
            var refPropertyMetasMap = new Dictionary<PropertyDef, RefPropertyMetas>();
            BuildRefPropertyMetasMap(refPropertyMetasMap);
            foreach (ModuleDef mod in _toObfuscatedModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (PropertyDef property in type.Properties)
                    {
                        if (_renamePolicy.NeedRename(property))
                        {
                            Rename(property, refPropertyMetasMap.GetValueOrDefault(property));
                        }
                    }
                }
            }
            //Debug.Log("Rename properties end");
        }

        private void RenameEvents()
        {
            //Debug.Log("Rename events begin");
            foreach (ModuleDef mod in _toObfuscatedModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (EventDef eventDef in type.Events)
                    {
                        if (_renamePolicy.NeedRename(eventDef))
                        {
                            Rename(eventDef);
                        }
                    }
                }
            }
            //Debug.Log("Rename events begin");
        }

        private void Rename(TypeDef type, RefTypeDefMetas refTypeDefMeta)
        {
            string moduleName = MetaUtil.GetModuleNameWithoutExt(type.Module.Name);
            string oldFullName = type.FullName;
            string oldNamespace = type.Namespace;

            string oldName = type.Name;

            string newNamespace;
            string newName;
            if (_renameRecordMap.TryGetExistRenameMapping(type, out var nns, out var nn))
            {
                newNamespace = nns;
                newName = nn;
            }
            else
            {
                newNamespace = _nameMaker.GetNewNamespace(type, oldNamespace, _useConsistentNamespaceObfuscation);
                newName = _nameMaker.GetNewName(type, oldName);
            }

            if (refTypeDefMeta != null)
            {
                foreach (TypeRef typeRef in refTypeDefMeta.typeRefs)
                {
                    Assert.AreEqual(typeRef.FullName, oldFullName);
                    Assert.IsTrue(typeRef.DefinitionAssembly.Name == moduleName);
                    if (!string.IsNullOrEmpty(oldNamespace))
                    {
                        typeRef.Namespace = newNamespace;
                    }
                    typeRef.Name = newName;
                    //Debug.Log($"rename assembly:{typeRef.Module.Name} reference {oldFullName} => {typeRef.FullName}");
                }
            }
            type.Name = newName;
            type.Namespace = newNamespace;
            string newFullName = type.FullName;
            _renameRecordMap.AddRename(type, newFullName);
            //Debug.Log($"rename typedef. assembly:{type.Module.Name} oldName:{oldFullName} => newName:{newFullName}");
        }

        private void Rename(FieldDef field, RefFieldMetas fieldMetas)
        {
            string oldName = field.Name;
            string newName = _renameRecordMap.TryGetExistRenameMapping(field, out var nn) ? nn : _nameMaker.GetNewName(field, oldName);
            if (fieldMetas != null)
            {
                foreach (var memberRef in fieldMetas.fieldRefs)
                {
                    memberRef.Name = newName;
                    //Debug.Log($"rename assembly:{memberRef.Module.Name} reference {field.FullName} => {memberRef.FullName}");
                }
                foreach (var ca in fieldMetas.customAttributes)
                {
                    foreach (var arg in ca.NamedArguments)
                    {
                        if (arg.Name == oldName)
                        {
                            arg.Name = newName;
                        }
                    }
                }
            }
            //Debug.Log($"rename field. {field} => {newName}");
            _renameRecordMap.AddRename(field, newName);
            field.Name = newName;

        }

        private void Rename(MethodDef method, RefMethodMetas refMethodMetas)
        {
            string oldName = method.Name;
            string newName = _renameRecordMap.TryGetExistRenameMapping(method, out var nn) ? nn : _nameMaker.GetNewName(method, oldName);
            Rename(method, refMethodMetas, newName);
        }

        private void Rename(MethodDef method, RefMethodMetas refMethodMetas, string newName)
        {
            ModuleDefMD mod = (ModuleDefMD)method.DeclaringType.Module;
            RenameMethodParams(method);
            RenameMethodBody(method);
            if (refMethodMetas != null)
            {
                foreach (MemberRef memberRef in refMethodMetas.memberRefs)
                {
                    string oldMethodFullName = memberRef.ToString();
                    memberRef.Name = newName;
                    //Debug.Log($"rename assembly:{memberRef.Module.Name} method:{oldMethodFullName} => {memberRef}");
                }
            }
            _renameRecordMap.AddRename(method, newName);
            method.Name = newName;
        }

        private void RenameMethodBody(MethodDef method)
        {
            if (method.Body == null)
            {
                return;
            }
        }

        private void RenameMethodParams(MethodDef method)
        {
            foreach (Parameter param in method.Parameters)
            {
                if (param.ParamDef != null)
                {
                    Rename(param.ParamDef);
                }
            }
        }

        private void Rename(ParamDef param)
        {
            string newName = _nameMaker.GetNewName(param, param.Name);
            param.Name = newName;
        }

        private void Rename(EventDef eventDef)
        {
            string oldName = eventDef.Name;
            string newName = _renameRecordMap.TryGetExistRenameMapping(eventDef, out var nn) ? nn : _nameMaker.GetNewName(eventDef, eventDef.Name);
            _renameRecordMap.AddRename(eventDef, newName);
            eventDef.Name = newName;
        }

        private void Rename(PropertyDef property, RefPropertyMetas refPropertyMetas)
        {
            string oldName = property.Name;
            string newName = _renameRecordMap.TryGetExistRenameMapping(property, out var nn) ? nn : _nameMaker.GetNewName(property, oldName);

            if (refPropertyMetas != null)
            {
                foreach (var ca in refPropertyMetas.customAttributes)
                {
                    foreach (var arg in ca.NamedArguments)
                    {
                        if (arg.Name == oldName)
                        {
                            arg.Name = newName;
                        }
                    }
                }
            }
            _renameRecordMap.AddRename(property, newName);
            property.Name = newName;
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_mappingXmlPath));
            _renameRecordMap.WriteXmlMappingFile();
        }
    }
}
