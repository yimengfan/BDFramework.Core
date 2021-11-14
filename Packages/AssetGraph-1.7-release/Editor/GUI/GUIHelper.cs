using UnityEditor;
using System;


namespace UnityEngine.AssetGraph
{
    public static class GUIHelper
    {
        public static string RevealInFinderLabel
        {
            get
            {
#if UNITY_EDITOR_OSX
                return "Reveal in Finder";
#else
                return "Show in Explorer";
#endif                
            }
        }
        
        public static string DrawFolderSelector(string label, 
            string dialogTitle, 
            string currentDirPath, 
            string directoryOpenPath, 
            Func<string, string> onValidFolderSelected = null) 
        {
            string newDirPath;
            using(new EditorGUILayout.HorizontalScope()) {
                if (string.IsNullOrEmpty (label)) {
                    newDirPath = EditorGUILayout.TextField(currentDirPath);
                } else {
                    newDirPath = EditorGUILayout.TextField(label, currentDirPath);
                }

                if(GUILayout.Button("Select", GUILayout.Width(50f))) {
                    var folderSelected = 
                        EditorUtility.OpenFolderPanel(dialogTitle, directoryOpenPath, "");
                    if(!string.IsNullOrEmpty(folderSelected)) {
                        if (onValidFolderSelected != null) {
                            newDirPath = onValidFolderSelected (folderSelected);
                        } else {
                            newDirPath = folderSelected;
                        }
                    }
                }
            }
            return newDirPath;
        }        
    }
}