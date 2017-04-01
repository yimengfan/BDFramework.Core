using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.ResourceMgr;
using BDFramework.net;

/// <summary>
/// 增量的版本更新下载
/// </summary>
public class VersionCtrl_Editor 
{

    enum HttpLayer :uint
    {
        HotUpdate  = 99,
    }

    public VersionCtrl_Editor()
    {
        //注册一个热更专用的通道
        HttpMgr.Instance.GetLayer((uint)HttpLayer.HotUpdate);

    }
    public string mResServerAddress
    {
        get;
        set;
    }
    
    public void Start(string gameid, string localversion,string sine ,string writeTo,Action<bool> processCallback)
    {
       if(mResServerAddress == null)
       {
           Debug.Log("请设置服务器地址");
           return;
       }

        //请求资源服务器
       var md5 = HashHelper.CreateMD5Hash(string.Format("gameid={0}&version_res={1}{2}", gameid, localversion, sine));
       Debug.Log(md5);
       var path = string.Format("{0}?gameid={1}&version_res={2}&sign={3}", mResServerAddress, gameid,localversion, md5);

        //创建请求接入服务器
        var t =  HttpMgr.Task.Create(path, null, (HttpMgr.Task task, WWW w) =>
        {
                Debug.Log("下载：" + task.url);
                if (w.error !=null)
                {
                    ResData data = null;
                    if (ParseJson(System.Text.Encoding.UTF8.GetString(w.bytes)))
                    {
                        foreach (var m in srdata.resource)
                        {
                            if (m.version_res_current == m.version_res_next)
                            {
                                data = m;
                                break;
                            }
                        }

                        //
                        if (data != null)
                        {
                            CreateVersionTaskQueue(data, writeTo, processCallback);
                        }
                    }
                    else
                    {
                        processCallback(false);
                    }
                }
                else
                {
                    processCallback(true);
                    Debug.LogError(w.error);
                }

            });
        //
        HttpMgr.Instance.GetLayer((uint)HttpLayer.HotUpdate).QueueTask(t);

        return;
    }

    ServerResponsData srdata = new ServerResponsData();
    /// <summary>
    /// 解析服务端返回数据
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private  bool ParseJson(string s)
    {
        try
        {
            var json = MyJson.Parse(s);
            srdata.http_code = json.asDict()["http_code"].AsString();
            srdata.name = json.asDict()["data"].asDict()["name"].AsString();
            srdata.version_latest = json.asDict()["data"].asDict()["version_latest"].AsString();
            srdata.ver_res_latest = json.asDict()["data"].asDict()["ver_res_latest"].AsString();
            srdata.server_resource_online = json.asDict()["data"].asDict()["server_resource_online"].AsString();
            srdata.server_resource_online_ip = json.asDict()["data"].asDict()["server_resource_online_ip"].AsString();
            srdata.server_resource_local_ip = json.asDict()["data"].asDict()["server_resource_local_ip"].AsString();

            foreach (var _j in json.asDict()["data"].asDict()["resource"].AsList())
            {
                ResData res = new ResData();
                res.version_res_current = _j.asDict()["version_res_current"].AsString();
                res.version_res_next = _j.asDict()["version_res_next"].AsString();
                res.path_resource = _j.asDict()["path_resource"].AsString();
                res.force_update = _j.asDict()["force_update"].AsString();
                res.status = _j.asDict()["status"].AsString();

                srdata.resource.Add(res);

            }
            return true;
        }
        catch
        {
            return false;
        }
                                                                                                                                  

    }

    /// <summary>
    /// 创建版本下载队列
    /// </summary>
    /// <param name="processCallback"></param>
    private void CreateVersionTaskQueue(ResData res,string writeTo,Action<bool> processCallback)
    {
        string curDownloadPath = "http://" +  srdata.server_resource_online+"/"+res.path_resource;
        var versiontask = HttpMgr.Task.Create(curDownloadPath + "/" + GetPlatformName() + "/" + "VersionHash.xml", null, (HttpMgr.Task task, WWW w) =>
        {
            Debug.Log("下载资源：" + task.url);
            if (w.error!=null)
            {
                //写入
                if (File.Exists(writeTo))
                {
                    File.Delete(writeTo);
                }
                File.WriteAllBytes(writeTo,w.bytes);
                processCallback(true);
            }
            else
            {
                Debug.Log("服务端没有资源，请检查是否是第一次生成，或者忘了提交");
                processCallback(true);
                return;
            }
        });
        HttpMgr.Instance.GetLayer((uint)HttpLayer.HotUpdate).QueueTask(versiontask);

    }


    public  string GetPlatformName()
    {

        #if UNITY_IPHONE                
                return "IOS/AllResources";        
        #elif UNITY_ANDROID        
                return "Android/AllResources";
        #elif UNITY_EDITOR
                return "Windows32/AllResources";
        #endif
    }
}
