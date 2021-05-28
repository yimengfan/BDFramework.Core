#if !ODIN_INSPECTOR
using System;
using UnityEditor;

//这里各种Odin包装 用来在无odin环境下不报错
namespace Sirenix.OdinInspector
{
    public class InlinePropertyAttribute : Attribute
    {
    }

    public class Title : Attribute
    {
        public Title(string str)
        {
        }
    }

    public class LabelText : Attribute
    {
        public LabelText(string str)
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
        Medium
    }
    public class Button : Attribute
    {
        public string Name;
        public Button(ButtonSizes size)
        {
            
        }
        public Button(string str, ButtonSizes size)
        {
            
        }
    }
    public class ButtonGroup : Attribute
    {
        public ButtonGroup(string str)
        {
            
        }
    }

    public class BoxGroup : Attribute
    {
        public BoxGroup(string str)
        {
        }
    }

    public enum TitleAlignments
    {
        Centered
    }

    public class TitleGroup : Attribute
    {
        public TitleGroup(string str,TitleAlignments alignment = TitleAlignments.Centered)
        {
        }
    }
    
    
    public class HideLabel : Attribute
    {
    }

    public class DisableInEditorMode : Attribute
    {
        
    }

    public class FilePath: Attribute
    {
        public string Extensions;

    }

    public class InfoBox: Attribute
    {
        public InfoBox(string str)
        {
            
        }
    }
}

namespace Sirenix.OdinInspector.Editor
{
    public class OdinEditorWindow : EditorWindow
    {
        virtual public void OnDestroy()
        {
        }
    }
}
#endif