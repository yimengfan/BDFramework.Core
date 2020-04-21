using System.IO;
using UnityEditor;
using UnityEngine;

public class ILREditorExten 
{

    public static void InsertDelegate(string sb)
    {
        if(!Application.isEditor) return;

         var path = Application.dataPath+ "/Code/Game/ILRuntime/Delegate/ILRuntimeDelegateHelper.cs";
         var content = File.ReadAllText(path);

         if (!content.Contains(sb)) //不存在则添加
         {
             var index = content.IndexOf("//[insert]");
             if (index > -1)
             {
                 content = content.Insert(index, sb + "\n");
                 File.WriteAllText(path, content); 
             }
             else
             {
                 Debug.LogError("请手动添加：" + sb);
             }

             AssetDatabase.Refresh();
         }
    }
    
}
