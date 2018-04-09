using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.ResourceMgr;
using UnityEditor;
/// <summary>
/// 增量的版本更新下载
/// </summary>
public class Editor_GenLocalDataPacket 
{

    public static void GenTableCofig()
    {
        var path = FrameDataHelper.FrameWorkSetting.EditorTablePath;
        path = Path.Combine(Application.dataPath, path);

       //  JDeBug.Inst.Log(path);

        var dir = Path.Combine(Application.streamingAssetsPath, "Table");
        if(Directory.Exists(dir) == false)
        {
            Directory.CreateDirectory(dir);
        }
        var jsonList = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
        //创建config
        var config = new VersionData_Table();
        float count = 0 ;
        foreach(var f in jsonList)
        {
            var fn = Path.GetFileName(f);
            config.FileInfoMap[fn] = HashHelper.CreateMD5ByFile(f);
            var _p = Path.Combine(Application.streamingAssetsPath, "Table/" + fn);
            File.Copy(f, _p,true);
            count++;
            EditorUtility.DisplayProgressBar("正在处理表格..", "处理：" + fn, count / jsonList.Length);
        }
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("提示", "表格导出成功", "OK");
        //
        var str = LitJson.JsonMapper.ToJson(config);
        //存储到stremming
        var p = Path.Combine( Application.streamingAssetsPath, FrameDataHelper.VersionDataSetting.TableConfigName);
        File.WriteAllText(p, str);

        //存储全局config
        str = LitJson.JsonMapper.ToJson(FrameDataHelper.VersionDataSetting);
        p = Path.Combine(Application.streamingAssetsPath, FrameDataHelper.FrameWorkSetting.VersionSettingPath);
        File.WriteAllText(p, str);
    }

    private static void SaveDataToDB()
    {
       
    }
}
