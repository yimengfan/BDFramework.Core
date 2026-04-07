#if UNITY_EDITOR
using System;
using System.Linq;
using BDFramework.Core.Tools;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// odin的设置
/// </summary>
public static class OdinSetting
{
    [InitializeOnLoadMethod]
    public static void SetEditorOnlyMode()
    {
        if (EditorApplication.isCompiling)
        {
            return;
        }

        if (HasOdinAssemblies())
        {
            return;
        }

        if (!RemoveOdinSymbols())
        {
            return;
        }

        Debug.Log("[OdinSetting] 未检测到 Odin 相关程序集，已移除 ProjectSettings 中的 Odin 宏并重新触发编译。");
        CompilationPipeline.RequestScriptCompilation();
    }

    private static bool HasOdinAssemblies()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(assembly =>
            {
                if (assembly == null || assembly.IsDynamic)
                {
                    return false;
                }

                var assemblyName = assembly.GetName().Name;
                if (string.IsNullOrEmpty(assemblyName))
                {
                    return false;
                }

                return assemblyName.IndexOf("Sirenix", StringComparison.OrdinalIgnoreCase) >= 0
                       || assemblyName.IndexOf("Odin", StringComparison.OrdinalIgnoreCase) >= 0;
            });
    }

    private static bool RemoveOdinSymbols()
    {
        var isChanged = false;
        foreach (var buildTargetGroup in BApplication.SupportBuildTargetGroups)
        {
            var rawSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            if (string.IsNullOrWhiteSpace(rawSymbols))
            {
                continue;
            }

            var newSymbols = string.Join(";",
                rawSymbols
                    .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(symbol => symbol.Trim())
                    .Where(symbol => !string.IsNullOrEmpty(symbol))
                    .Where(symbol => !IsOdinSymbol(symbol))
                    .Distinct());

            if (string.Equals(rawSymbols, newSymbols, StringComparison.Ordinal))
            {
                continue;
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
            isChanged = true;
        }

        return isChanged;
    }

    private static bool IsOdinSymbol(string symbol)
    {
        return symbol.IndexOf("ODIN", StringComparison.OrdinalIgnoreCase) >= 0
               || symbol.IndexOf("SIRENIX", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}


#endif

#if !ODIN_INSPECTOR
//这里各种Odin包装 用来在无odin环境下不报错
namespace Sirenix.OdinInspector
{
    using System;

    public class InlinePropertyAttribute : Attribute
    {
    }

    public class TitleAttribute : Attribute
    {
        public bool Bold = false;
        public TitleAttribute(string str, TitleAlignments titleAlignment = TitleAlignments.Left)
        {
        }
    }

    public class LabelTextAttribute : Attribute
    {
        public LabelTextAttribute(string str, bool xx = true)
        {
        }
    }

    public class PropertyOrderAttribute : Attribute
    {
        public PropertyOrderAttribute(int order)
        {
        }
    }

    public class ShowInInspectorAttribute : Attribute
    {
    }

    public class ReadOnlyAttribute : Attribute
    {
    }

    public class MultiLinePropertyAttribute : Attribute
    {
        public MultiLinePropertyAttribute(int lines = 3)
        {
        }
    }

    public class OnInspectorGUIAttribute : Attribute
    {
        public OnInspectorGUIAttribute(string str)
        {
        }
    }

    public class LabelWidthAttribute : Attribute
    {
        public LabelWidthAttribute(int i)
        {
        }
    }


    public enum ButtonSizes
    {
        Small,
        Medium,
        Large
    }

    public enum ButtonStyle
    {
        CompactBox,
    }

    public class ButtonAttribute : Attribute
    {
        public string Name;

        public ButtonAttribute(string str ="")
        {
        }

        public ButtonAttribute(ButtonSizes size)
        {
        }

        public ButtonAttribute(string str, ButtonSizes size= ButtonSizes.Large, ButtonStyle buttonStyle = ButtonStyle.CompactBox)
        {
        }
    }

    public class ButtonGroup : Attribute
    {
        public ButtonGroup(string str)
        {
        }
    }

    public class EnumToggleButtons : Attribute
    {
    }

    public class BoxGroupAttribute : Attribute
    {
        public BoxGroupAttribute(string str, bool paramsBool = false)
        {
        }
    }

    public enum TitleAlignments
    {
        Centered,
        Left
    }

    public class TitleGroupAttribute : Attribute
    {
        public TitleGroupAttribute(string str, TitleAlignments alignment = TitleAlignments.Centered)
        {
        }
    }
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class HorizontalGroupAttribute : Attribute
    {
        public int LabelWidth = 0;
        public HorizontalGroupAttribute(string str, int width = 0)
        {

        }
    }
    
    public class VerticalGroupAttribute : Attribute
    {
        public VerticalGroupAttribute(string str, int width = 10)
        {

        }
    }
    

    public class GUIColorAttribute: Attribute
    {
        public GUIColorAttribute(float x, float y, float z)
        {
            
        }
    }

    public class HideLabelAttribute : Attribute
    {
    }

    public class DisableInEditorModeAttribute : Attribute
    {
    }

    public class FilePathAttribute : Attribute
    {
        public string Extensions;
        public string ParentFolder;
    }

    public enum InfoMessageType
    {
        Info
    }

    public class InfoBoxAttribute : Attribute
    {
        public InfoBoxAttribute(string str,InfoMessageType type = InfoMessageType.Info)
        {
        }
    }

    public class ShowIfAttribute : Attribute
    {
        public ShowIfAttribute(string name, object value)
        {
        }
    }

    public class PropertySpaceAttribute : Attribute
    {
        public PropertySpaceAttribute(int value = 0)
        {
        }
    }
    
    public class DisableIfAttribute : Attribute
    {
        public DisableIfAttribute(string  str = "")
        {
            
        }
    }
    public class EnableIfAttribute : Attribute
    {
        public EnableIfAttribute(string  str = "")
        {
            
        }
    }
}


#if UNITY_EDITOR
//Odin适配
namespace Sirenix.OdinInspector.Editor
{
    using UnityEditor;
    public class OdinEditorWindow : EditorWindow
    {
        virtual public void OnDestroy()
        {
        }
    }

    public class OdinMenuTree
    {
        public OdinMenuTree(bool b)
        {
            
        }
    }

    public abstract class OdinMenuEditorWindow : OdinEditorWindow
    {
        protected virtual OdinMenuTree BuildMenuTree()
        {
            return null;
        }
        
        virtual protected void OnDestroy()
        {
        }
    }
}

namespace Sirenix.Utilities.Editor
{
    public class NoError
    {
    }
}

#endif


#endif
