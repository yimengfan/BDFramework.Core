using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using com.putao.hotpudate;

public class CreateVersionUpdateList
{
    public static void Execute(UnityEditor.BuildTarget target)
	{
        string platform = AssetBundleController.GetPlatformName(target);
        string newVersionHash = System.IO.Path.Combine(Application.dataPath, "AssetBundle/" + platform + "/VersionNum/VersionHash.xml");
        string oldVersionHash = System.IO.Path.Combine(Application.dataPath, "AssetBundle/" + platform + "/VersionNum/VersionHash-old.xml");


        SortedList<string, AssetInfo> dicNewHashInfo = CreateHashList.ReadHashFile(newVersionHash);
        SortedList<string, AssetInfo> dicOldHashInfo = new SortedList<string, AssetInfo>();
        if (File.Exists(oldVersionHash))
        {
            dicOldHashInfo = CreateHashList.ReadHashFile(oldVersionHash);
        }


		string versionUpdateFile = System.IO.Path.Combine(Application.dataPath, "AssetBundle/" + platform + "/VersionNum/VersionUpdateList.bytes");

        IndexFileData data = new IndexFileData();
        data.mVersion = "0.01";
        foreach (KeyValuePair<string, AssetInfo> newPair in dicNewHashInfo)
		{

            if (string.Compare(newPair.Key, "AllResources") == 0)
                continue;

            if (dicOldHashInfo.ContainsKey(newPair.Key))
			{
                if (newPair.Value.hash128 != dicOldHashInfo[newPair.Key].hash128)
				{
                    data.dataMap[newPair.Value.fileName] = new IndexData() { path = newPair.Value.fileName, hash = newPair.Value.hash128 };
				}
			}
			else
			{
                data.dataMap[newPair.Value.fileName] = new IndexData() { path = newPair.Value.fileName, hash = newPair.Value.hash128 };
			}
		}

        //增加AssetbundleManifest
        data.dataMap["AllResources"] = new IndexData() { path = "AllResources", hash = "nohash" };
        File.WriteAllText(versionUpdateFile, data.ToString());

        AssetDatabase.Refresh();
	}


    public static void ExportFile(UnityEditor.BuildTarget target,string copyto)
    {
        string platform = AssetBundleController.GetPlatformName(target);
        string file = System.IO.Path.Combine(Application.dataPath, "AssetBundle/" + platform );
        var updatalist = file + "/VersionNum/VersionUpdateList.bytes";
        if (File.Exists(updatalist) == false)
        {
            EditorUtility.DisplayDialog("", updatalist + " 不存在，\n请生成 增量包文件!(第二个按钮)", "OK");
            return;
        }

        var data = IndexFileData.Create(File.ReadAllBytes(updatalist));
        copyto +=("/"+platform);
         
        if(Directory.Exists(copyto))
        {
            Directory.Delete(copyto,true);
        }
        Directory.CreateDirectory(copyto);

        //拷贝版本描述文件
        File.Copy(Path.Combine(file, "VersionNum/VersionHash.xml"), Path.Combine(copyto, "VersionHash.xml"), true);
        File.Copy(Path.Combine(file, "VersionNum/VersionUpdateList.bytes"), Path.Combine(copyto, "VersionUpdateList.bytes"), true);

        foreach (var d in data.dataMap)
        {
            File.Copy(Path.Combine(file,d.Value.path),Path.Combine(copyto,d.Value.path) ,true);
        }
    }
}
