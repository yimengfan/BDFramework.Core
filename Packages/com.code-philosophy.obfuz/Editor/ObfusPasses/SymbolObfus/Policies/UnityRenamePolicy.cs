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

﻿using dnlib.DotNet;
using Obfuz.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{

    public class UnityRenamePolicy : ObfuscationPolicyBase
    {
        private static HashSet<string> s_monoBehaviourEvents = new HashSet<string> {

            // MonoBehaviour events
    "Awake",
    "FixedUpdate",
    "LateUpdate",
    "OnAnimatorIK",

    "OnAnimatorMove",
    "OnApplicationFocus",
    "OnApplicationPause",
    "OnApplicationQuit",
    "OnAudioFilterRead",

    "OnBecameVisible",
    "OnBecameInvisible",

    "OnCollisionEnter",
    "OnCollisionEnter2D",
    "OnCollisionExit",
    "OnCollisionExit2D",
    "OnCollisionStay",
    "OnCollisionStay2D",
    "OnConnectedToServer",
    "OnControllerColliderHit",

    "OnDrawGizmos",
    "OnDrawGizmosSelected",
    "OnDestroy",
    "OnDisable",
    "OnDisconnectedFromServer",

    "OnEnable",

    "OnFailedToConnect",
    "OnFailedToConnectToMasterServer",

    "OnGUI",

    "OnJointBreak",
    "OnJointBreak2D",

    "OnMasterServerEvent",
    "OnMouseDown",
    "OnMouseDrag",
    "OnMouseEnter",
    "OnMouseExit",
    "OnMouseOver",
    "OnMouseUp",
    "OnMouseUpAsButton",

    "OnNetworkInstantiate",

    "OnParticleSystemStopped",
    "OnParticleTrigger",
    "OnParticleUpdateJobScheduled",
    "OnPlayerConnected",
    "OnPlayerDisconnected",
    "OnPostRender",
    "OnPreCull",
    "OnPreRender",
    "OnRenderImage",
    "OnRenderObject",

    "OnSerializeNetworkView",
    "OnServerInitialized",

    "OnTransformChildrenChanged",
    "OnTransformParentChanged",
    "OnTriggerEnter",
    "OnTriggerEnter2D",
    "OnTriggerExit",
    "OnTriggerExit2D",
    "OnTriggerStay",
    "OnTriggerStay2D",

    "OnValidate",
    "OnWillRenderObject",
    "Reset",
    "Start",
    "Update",

    // Animator/StateMachineBehaviour
    "OnStateEnter",
    "OnStateExit",
    "OnStateMove",
    "OnStateUpdate",
    "OnStateIK",
    "OnStateMachineEnter",
    "OnStateMachineExit",

    // ParticleSystem
    "OnParticleTrigger",
    "OnParticleCollision",
    "OnParticleSystemStopped",

    // UGUI/EventSystems
    "OnPointerClick",
    "OnPointerDown",
    "OnPointerUp",
    "OnPointerEnter",
    "OnPointerExit",
    "OnDrag",
    "OnBeginDrag",
    "OnEndDrag",
    "OnDrop",
    "OnScroll",
    "OnSelect",
    "OnDeselect",
    "OnMove",
    "OnSubmit",
    "OnCancel",
};

        private readonly CachedDictionary<TypeDef, bool> _computeDeclaringTypeDisableAllMemberRenamingCache;
        private readonly CachedDictionary<TypeDef, bool> _isSerializableCache;
        private readonly CachedDictionary<TypeDef, bool> _isInheritFromMonoBehaviourOrScriptableObjectCache;
        private readonly CachedDictionary<TypeDef, bool> _isScriptOrSerializableTypeCache;

        public UnityRenamePolicy()
        {
            _computeDeclaringTypeDisableAllMemberRenamingCache = new CachedDictionary<TypeDef, bool>(ComputeDeclaringTypeDisableAllMemberRenaming);
            _isSerializableCache = new CachedDictionary<TypeDef, bool>(MetaUtil.IsSerializableType);
            _isInheritFromMonoBehaviourOrScriptableObjectCache = new CachedDictionary<TypeDef, bool>(MetaUtil.IsScriptType);
            _isScriptOrSerializableTypeCache = new CachedDictionary<TypeDef, bool>(MetaUtil.IsScriptOrSerializableType);
        }

        private bool IsUnitySourceGeneratedAssemblyType(TypeDef typeDef)
        {
            if (typeDef.Name.StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes_"))
            {
                return true;
            }
            if (typeDef.FullName == "Unity.Entities.CodeGeneratedRegistry.AssemblyTypeRegistry")
            {
                return true;
            }
            if (typeDef.Name.StartsWith("__JobReflectionRegistrationOutput"))
            {
                return true;
            }
            if (MetaUtil.HasDOTSCompilerGeneratedAttribute(typeDef))
            {
                return true;
            }
            if (typeDef.DeclaringType != null)
            {
                return IsUnitySourceGeneratedAssemblyType(typeDef.DeclaringType);
            }
            return false;
        }

        private bool ComputeDeclaringTypeDisableAllMemberRenaming(TypeDef typeDef)
        {
            if (typeDef.IsEnum && MetaUtil.HasBlackboardEnumAttribute(typeDef))
            {
                return true;
            }
            if (IsUnitySourceGeneratedAssemblyType(typeDef))
            {
                return true;
            }
            if (MetaUtil.IsInheritFromDOTSTypes(typeDef))
            {
                return true;
            }
            return false;
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            if (_isScriptOrSerializableTypeCache.GetValue(typeDef))
            {
                return false;
            }
            if (_computeDeclaringTypeDisableAllMemberRenamingCache.GetValue(typeDef))
            {
                return false;
            }
            if (MetaUtil.HasBurstCompileAttribute(typeDef))
            {
                return false;
            }
            if (typeDef.Methods.Any(m => MetaUtil.HasRuntimeInitializeOnLoadMethodAttribute(m)))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            TypeDef typeDef = methodDef.DeclaringType;
            if (s_monoBehaviourEvents.Contains(methodDef.Name) && _isInheritFromMonoBehaviourOrScriptableObjectCache.GetValue(typeDef))
            {
                return false;
            }
            if (_computeDeclaringTypeDisableAllMemberRenamingCache.GetValue(typeDef))
            {
                return false;
            }
            if (MetaUtil.HasRuntimeInitializeOnLoadMethodAttribute(methodDef))
            {
                return false;
            }
            if (MetaUtil.HasBurstCompileAttribute(methodDef) || MetaUtil.HasBurstCompileAttribute(methodDef.DeclaringType) || MetaUtil.HasDOTSCompilerGeneratedAttribute(methodDef))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            TypeDef typeDef = fieldDef.DeclaringType;
            if (_isScriptOrSerializableTypeCache.GetValue(typeDef))
            {
                if (typeDef.IsEnum)
                {
                    return false;
                }
                if (fieldDef.IsPublic && !fieldDef.IsStatic)
                {
                    return false;
                }
                if (!fieldDef.IsStatic && MetaUtil.IsSerializableField(fieldDef))
                {
                    return false;
                }
            }
            if (_computeDeclaringTypeDisableAllMemberRenamingCache.GetValue(typeDef))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            TypeDef typeDef = propertyDef.DeclaringType;
            if (_isSerializableCache.GetValue(typeDef))
            {
                bool isGetterPublic = propertyDef.GetMethod != null && propertyDef.GetMethod.IsPublic && !propertyDef.GetMethod.IsStatic;
                bool isSetterPublic = propertyDef.SetMethod != null && propertyDef.SetMethod.IsPublic && !propertyDef.SetMethod.IsStatic;

                if (isGetterPublic || isSetterPublic)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
