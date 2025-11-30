using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;

namespace HybridCLR.Editor.Settings
{
    public class HybridCLRSettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedObject;
        private SerializedProperty _enable;
        private SerializedProperty _useGlobalIl2cpp;
        private SerializedProperty _hybridclrRepoURL;
        private SerializedProperty _il2cppPlusRepoURL;
        private SerializedProperty _hotUpdateAssemblyDefinitions;
        private SerializedProperty _hotUpdateAssemblies;
        private SerializedProperty _preserveHotUpdateAssemblies;
        private SerializedProperty _hotUpdateDllCompileOutputRootDir;
        private SerializedProperty _externalHotUpdateAssemblyDirs;
        private SerializedProperty _strippedAOTDllOutputRootDir;
        private SerializedProperty _patchAOTAssemblies;
        private SerializedProperty _outputLinkFile;
        private SerializedProperty _outputAOTGenericReferenceFile;
        private SerializedProperty _maxGenericReferenceIteration;
        private SerializedProperty _maxMethodBridgeGenericIteration;

        public HybridCLRSettingsProvider() : base("Project/HybridCLR Settings", SettingsScope.Project) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            InitGUI();
        }

        private void InitGUI()
        {
            var setting = HybridCLRSettings.LoadOrCreate();
            _serializedObject?.Dispose();
            _serializedObject = new SerializedObject(setting);
            _enable = _serializedObject.FindProperty("enable");
            _useGlobalIl2cpp = _serializedObject.FindProperty("useGlobalIl2cpp");
            _hybridclrRepoURL = _serializedObject.FindProperty("hybridclrRepoURL");
            _il2cppPlusRepoURL = _serializedObject.FindProperty("il2cppPlusRepoURL");
            _hotUpdateAssemblyDefinitions = _serializedObject.FindProperty("hotUpdateAssemblyDefinitions");
            _hotUpdateAssemblies = _serializedObject.FindProperty("hotUpdateAssemblies");
            _preserveHotUpdateAssemblies = _serializedObject.FindProperty("preserveHotUpdateAssemblies");
            _hotUpdateDllCompileOutputRootDir = _serializedObject.FindProperty("hotUpdateDllCompileOutputRootDir");
            _externalHotUpdateAssemblyDirs = _serializedObject.FindProperty("externalHotUpdateAssembliyDirs");
            _strippedAOTDllOutputRootDir = _serializedObject.FindProperty("strippedAOTDllOutputRootDir");
            _patchAOTAssemblies = _serializedObject.FindProperty("patchAOTAssemblies");
            _outputLinkFile = _serializedObject.FindProperty("outputLinkFile");
            _outputAOTGenericReferenceFile = _serializedObject.FindProperty("outputAOTGenericReferenceFile");
            _maxGenericReferenceIteration = _serializedObject.FindProperty("maxGenericReferenceIteration");
            _maxMethodBridgeGenericIteration = _serializedObject.FindProperty("maxMethodBridgeGenericIteration");
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedObject == null || !_serializedObject.targetObject)
            {
                InitGUI();
            }
            _serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_enable);
            EditorGUILayout.PropertyField(_hybridclrRepoURL);
            EditorGUILayout.PropertyField(_il2cppPlusRepoURL);
            EditorGUILayout.PropertyField(_useGlobalIl2cpp);
            EditorGUILayout.PropertyField(_hotUpdateAssemblyDefinitions);
            EditorGUILayout.PropertyField(_hotUpdateAssemblies);
            EditorGUILayout.PropertyField(_preserveHotUpdateAssemblies);
            EditorGUILayout.PropertyField(_hotUpdateDllCompileOutputRootDir);
            EditorGUILayout.PropertyField(_externalHotUpdateAssemblyDirs);
            EditorGUILayout.PropertyField(_strippedAOTDllOutputRootDir);
            EditorGUILayout.PropertyField(_patchAOTAssemblies);
            EditorGUILayout.PropertyField(_outputLinkFile);
            EditorGUILayout.PropertyField(_outputAOTGenericReferenceFile);
            EditorGUILayout.PropertyField(_maxGenericReferenceIteration);
            EditorGUILayout.PropertyField(_maxMethodBridgeGenericIteration);
            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
                HybridCLRSettings.Save();
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            HybridCLRSettings.Save();
        }

        static HybridCLRSettingsProvider s_provider;

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (s_provider == null)
            {
                s_provider = new HybridCLRSettingsProvider();
            }
            return s_provider;
        }
    }
}