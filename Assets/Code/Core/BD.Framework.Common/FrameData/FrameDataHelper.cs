using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class FrameDataHelper
{
    private static FrameWorkSetting fws;
    /// <summary>
    /// 框架总配置
    /// </summary>
    public static FrameWorkSetting FrameWorkSetting
    {
        get
        {
            if (fws == null)
            {
                var content = Resources.Load<TextAsset>("FrameWork/FrameSetting").text;
                fws = LitJson.JsonMapper.ToObject<FrameWorkSetting>(content);
               
            }
            return fws;
        }
    }


    static private VersionDataSetting vds;
    /// <summary>
    /// 资源版本总配置
    /// </summary>
    static public VersionDataSetting VersionDataSetting
    {
        get
        {
            if (vds == null)
            {
                //编辑器环境下需要用
                if(Application.isEditor)
                {
                    var content = Resources.Load<TextAsset> ("FrameWork/VersionDataSetting").text;
                    vds = LitJson.JsonMapper.ToObject<VersionDataSetting>(content);
                  
                }
                else
                {
                    vds = new VersionDataSetting();
                   // vds.LocalResourcePath = Path.Combine(Application.persistentDataPath, vds.LocalResourcePath);
                }
            }
            //vds.Version = ClientMain.Version;
            return vds;
        }
        set { vds = value; }
    }
}
