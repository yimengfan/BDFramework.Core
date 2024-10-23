#if ODIN_INSPECTOR && UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
/// <summary>
/// odin的设置
/// </summary>
public class OdinSetting
{
    
    [InitializeOnLoadMethod]
    public static void SetEditorOnlyMode()
    {
       var ret =  EditorOnlyModeConfig.Instance.IsEditorOnlyModeEnabled();
       if (!ret)
       {
           EditorOnlyModeConfig.Instance.EnableEditorOnlyMode(false);
       }
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

    public class Title : Attribute
    {
        public bool Bold = false;
        public Title(string str, TitleAlignments titleAlignment = TitleAlignments.Left)
        {
        }
    }

    public class LabelText : Attribute
    {
        public LabelText(string str, bool xx = true)
        {
        }
    }

    public class OnInspectorGUI : Attribute
    {
        public OnInspectorGUI(string str)
        {
        }
    }

    public class LabelWidth : Attribute
    {
        public LabelWidth(int i)
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

    public class Button : Attribute
    {
        public string Name;

        public Button(string str ="")
        {
        }

        public Button(ButtonSizes size)
        {
        }

        public Button(string str, ButtonSizes size= ButtonSizes.Large, ButtonStyle buttonStyle = ButtonStyle.CompactBox)
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

    public class BoxGroup : Attribute
    {
        public BoxGroup(string str, bool paramsBool = false)
        {
        }
    }

    public enum TitleAlignments
    {
        Centered,
        Left
    }

    public class TitleGroup : Attribute
    {
        public TitleGroup(string str, TitleAlignments alignment = TitleAlignments.Centered)
        {
        }
    }
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class HorizontalGroup : Attribute
    {
        public int LabelWidth = 0;
        public HorizontalGroup(string str, int width = 0)
        {

        }
    }
    
    public class VerticalGroup : Attribute
    {
        public VerticalGroup(string str, int width = 10)
        {

        }
    }
    

    public class GUIColor: Attribute
    {
        public GUIColor(float x, float y, float z)
        {
            
        }
    }

    public class HideLabel : Attribute
    {
    }

    public class DisableInEditorMode : Attribute
    {
    }

    public class FilePath : Attribute
    {
        public string Extensions;
        public string ParentFolder;
    }

    public enum InfoMessageType
    {
        Info
    }

    public class InfoBox : Attribute
    {
        public InfoBox(string str,InfoMessageType type = InfoMessageType.Info)
        {
        }
    }

    public class ShowIf : Attribute
    {
        public ShowIf(string name, object value)
        {
        }
    }

    public class PropertySpace : Attribute
    {
        public PropertySpace(int value = 0)
        {
        }
    }
    
    public class DisableIf : Attribute
    {
        public DisableIf(string  str = "")
        {
            
        }
    }
    public class EnableIf : Attribute
    {
        public EnableIf(string  str = "")
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
