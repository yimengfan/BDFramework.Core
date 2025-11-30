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

using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Obfuz.Settings
{

    public class ObfuzSettings : ScriptableObject
    {
        [Tooltip("build pipeline settings")]
        public BuildPipelineSettings buildPipelineSettings;

        [Tooltip("compatibility settings")]
        public CompatibilitySettings compatibilitySettings;

        [Tooltip("assembly settings")]
        public AssemblySettings assemblySettings;

        [Tooltip("obfuscation pass settings")]
        public ObfuscationPassSettings obfuscationPassSettings;

        [Tooltip("secret settings")]
        public SecretSettings secretSettings;

        [Tooltip("encryption virtual machine settings")]
        public EncryptionVMSettings encryptionVMSettings;

        [Tooltip("symbol obfuscation settings")]
        public SymbolObfuscationSettings symbolObfusSettings;

        [Tooltip("const encryption settings")]
        public ConstEncryptionSettings constEncryptSettings;

        [Tooltip("remove const field settings")]
        public RemoveConstFieldSettings removeConstFieldSettings;

        [Tooltip("eval stack obfuscation settings")]
        public EvalStackObfuscationSettings evalStackObfusSettings;

        [Tooltip("field encryption settings")]
        public FieldEncryptionSettings fieldEncryptSettings;

        [Tooltip("call obfuscation settings")]
        public CallObfuscationSettings callObfusSettings;

        [Tooltip("expression obfuscation settings")]
        public ExprObfuscationSettings exprObfusSettings;

        [Tooltip("control flow obfuscation settings")]
        public ControlFlowObfuscationSettings controlFlowObfusSettings;

        [Tooltip("garbage code generator settings")]
        public GarbageCodeGenerationSettings garbageCodeGenerationSettings;

        [Tooltip("watermark settings")]
        public WatermarkSettings watermarkSettings;

        [Tooltip("polymorphic dll settings")]
        public PolymorphicDllSettings polymorphicDllSettings;

        public string ObfuzRootDir => $"Library/Obfuz";

        public string GetObfuscatedAssemblyOutputPath(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/ObfuscatedAssemblies";
        }

        public string GetOriginalAssemblyBackupDir(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/OriginalAssemblies";
        }

        public string GetObfuscatedAssemblyTempOutputPath(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/TempObfuscatedAssemblies";
        }

        public string GetObfuscatedLinkXmlPath(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/link.xml";
        }

        private static ObfuzSettings s_Instance;

        public static ObfuzSettings Instance
        {
            get
            {
                if (!s_Instance)
                {
                    LoadOrCreate();
                }
                return s_Instance;
            }
        }

        protected static string SettingsPath => "ProjectSettings/Obfuz.asset";

        private static ObfuzSettings LoadOrCreate()
        {
            string filePath = SettingsPath;
            var arr = InternalEditorUtility.LoadSerializedFileAndForget(filePath);
            //Debug.Log($"typeof arr:{arr?.GetType()} arr[0]:{(arr != null && arr.Length > 0 ? arr[0].GetType(): null)}");

            if (arr != null && arr.Length > 0 && arr[0] is ObfuzSettings obfuzSettings)
            {
                s_Instance = obfuzSettings;
            }
            else
            {
                s_Instance = s_Instance ?? CreateInstance<ObfuzSettings>();
            }
            return s_Instance;
        }

        public static void Save()
        {
            if (!s_Instance)
            {
                return;
            }

            string filePath = SettingsPath;
            string directoryName = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directoryName);
            UnityEngine.Object[] obj = new ObfuzSettings[1] { s_Instance };
            InternalEditorUtility.SaveToSerializedFileAndForget(obj, filePath, true);
        }
    }
}
