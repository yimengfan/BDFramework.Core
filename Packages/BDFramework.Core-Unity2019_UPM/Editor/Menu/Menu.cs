using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    static public class Menu
    {
        [MenuItem("BDFrameWork工具箱/帮助文档", false, (int) BDEditorMenuEnum.BDHelperURL)]
        public static void Open()
        {
            Application.OpenURL("https://www.yuque.com/naipaopao/eg6gik");
        }

    }
}