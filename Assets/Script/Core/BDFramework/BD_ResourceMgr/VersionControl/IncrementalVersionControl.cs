using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using BDFramework.ResourceMgr;
using System.Threading;
using BDFramework.net;
/// <summary>
/// 增量的版本更新下载
/// </summary>
public class IncrementalVersionControl :IVersionControl
{
    //
    public bool isTest {get;set;}
    enum HttpLayer :uint
    {
        HotUpdate  = 99,
    }

    public IncrementalVersionControl()
    {
        //注册一个热更专用的通道
        HttpMgr.Instance.GetLayer((uint)HttpLayer.HotUpdate);
        mLocalHotUpdateResPath = Application.persistentDataPath + "/hotupdate/";

    }
    public string mResServerAddress
    {
        get;
        set;
    }

    public string mLocalHotUpdateResPath
    {
        get;
        set;
    }

    
    public void Start(string gameid, string localversion,string sine ,Action<float,string,bool> processCallback)
    {
       if(mResServerAddress == null)
       {
           Debug.Log("请设置服务器地址");
           return;
       }
       if (mLocalHotUpdateResPath == null)
       {
           Debug.Log("请设置本地存储地址");
           return;
       }
        //请求资源服务器
       var md5 = HashHelper.CreateMD5Hash(string.Format("gameid={0}&version_res={1}{2}", gameid, localversion, sine));
       Debug.Log(md5);
       var path = string.Format("{0}?gameid={1}&version_res={2}&sign={3}", mResServerAddress, gameid,localversion, md5);

        //创建请求接入服务器
        var t =  HttpMgr.Task.Create(path,null, (HttpMgr.Task task, WWW w) =>
            {
                Debug.Log("下载：" + task.url);
                if ((w.error) == null)
                {
                    var strw = System.Text.Encoding.UTF8.GetString(w.bytes);
                    if (ParseJson(strw))
                    {
                        //创建版本任务队列
                        CreateVersionTaskQueue(processCallback);
                        //开始网络任务
                        DoNextVersionTask(processCallback);
                    }
                    else
                    {
                        processCallback(-1, "服务器信息解析失败", false);
                    }
                }
                else
                {
                    Debug.LogError(w.error);
                }

            });
        //
        HttpMgr.Instance.GetLayer((uint)HttpLayer.HotUpdate).QueueTask(t);

        return;
        if (ParseJson(File.ReadAllText("C:/test.txt")))
        {
            //创建版本任务队列
            CreateVersionTaskQueue(processCallback);
            //开始网络任务
            DoNextVersionTask(processCallback);
        }
        else
        {
            processCallback(-1, "服务器信息解析失败", false);
        }
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
    /// 版本下载队列
    /// </summary>
    Queue<HttpMgr.Task> versionDownloadTaskQue = new Queue<HttpMgr.Task>();
    /// <summary>
    /// 资源任务队列，每个版本维护一个
    /// </summary>
    Queue<HttpMgr.Task> resDownloadTaskQue = new Queue<HttpMgr.Task>();
    /// <summary>
    /// 开始每个版本的下载
    /// </summary>
    /// <param name="processCallback"></param>
    public void DoNextVersionTask(Action<float, string, bool> processCallback)
    {

        if (versionDownloadTaskQue.Count > 0)
        {
            var curtask = versionDownloadTaskQue.Dequeue();

            Debug.Log("下载版本：" + curtask.url);
            try
            {
                HttpMgr.Instance.GetLayer((uint)HttpLayer.HotUpdate).QueueTask(curtask);
            }
            catch
            {
                processCallback(-1, "请求索引文件错误："+ curtask.url, false);
                return;
            }
        }
        else
        {
            processCallback(1, "全部下载成功...", false);
        }
    }


    /// <summary>
    /// 创建版本下载队列
    /// </summary>
    /// <param name="processCallback"></param>
    private void CreateVersionTaskQueue(Action<float, string, bool> processCallback)
    {

        foreach (var res in srdata.resource)
        {
            string curDownloadPath = "http://" +  srdata.server_resource_online+"/"+res.path_resource;
            var versiontask = HttpMgr.Task.Create(curDownloadPath +"/"+GetPlatformName()+"/" + "VersionUpdateList.bytes", null, (HttpMgr.Task task, WWW w) =>
            {
                Debug.Log("下载资源：" + task.url);
                if (w.error != null)
                {
                    var strw = System.Text.Encoding.UTF8.GetString(w.bytes);
                    //根据服务端列表创建资源下载队列
                    CreateCurVersionResTaskQue(curDownloadPath, w.bytes, processCallback);
                    //开始下载当前版本
                    if (resDownloadTaskQue.Count > 0)
                    {
                        //下载每一个资源
                        var _task = resDownloadTaskQue.Dequeue();
                        try
                        {
                            HttpMgr.Instance.GetLayer((uint)HttpLayer.HotUpdate).QueueTask(_task);
                        }
                        catch
                        {
                            processCallback(-1, "下载资源出错" + _task.url, false);
                            return;
                        }
                    }
                }
                else
                {
                    processCallback(-1, "网络出错:" + w.error, false);
                    Debug.LogError(w.error);
                    return;
                }
            });

            versionDownloadTaskQue.Enqueue(versiontask);
        }

    }

    /// <summary>
    /// 根据服务端列表创建资源下载队列
    /// </summary>
    /// <param name="httppath"></param>
    /// <param name="indexdata"></param>
    /// <param name="processCallback"></param>
    private void CreateCurVersionResTaskQue(string httppath,byte[] indexdata,Action<float, string, bool> processCallback)
    {

        var data = IndexFileData.Create(indexdata);

        foreach (var d in data.dataMap.Values)
        {
            resDownloadTaskQue.Enqueue(HttpMgr.Task.Create(httppath + "/"+GetPlatformName()+"/"+ d.path, null, (HttpMgr.Task task, WWW w) =>
            {
                    Debug.Log("下载资源：" + task.url);
                    if (w.error !=null) //判断是不是发生异常
                    {
                        //写入
                        if (Directory.Exists(mLocalHotUpdateResPath) == false)
                        {
                            Directory.CreateDirectory(mLocalHotUpdateResPath);
                        }
                        //
                        string path = Path.Combine(mLocalHotUpdateResPath, Path.GetFileName(task.url));
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }

                        File.WriteAllBytes(path,w.bytes);

                        //继续当前版本下载任务
                        if (resDownloadTaskQue.Count > 0)
                        {
                            var _task = resDownloadTaskQue.Dequeue();
                            HttpMgr.Instance.GetLayer((uint)HttpLayer.HotUpdate).QueueTask(_task);
                        }

                        else //当前任务全部下载完
                        {
                            //开始下一个版本
                            DoNextVersionTask(processCallback);
                        }
                    }
                    else
                    {
                        processCallback(-1, "网络出错:" + w.error, false);
                        Debug.LogError(w.error);
                        return;
                    }
                }));
        }

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="appVersion"></param>
    /// <param name="callback"></param>
    public void CopyStreamingAsset(float appVersion,Action callback)
    {
        var versionFile = Path.Combine(mLocalHotUpdateResPath, "appversion.txt");
        Debug.Log(versionFile);
        if (File.Exists(versionFile))
        {
            var content = File.ReadAllText(versionFile);
            var temp = content.Split('\r', '\n');
            if (temp.Length > 0)
            {
                var _v = temp[0].Split(':');
                if (_v.Length > 0)
                {
                    float curversion;
                    if (float.TryParse(_v[1], out curversion))
                    {
                        if (curversion >= appVersion)
                        {
                            Debug.Log("不需要拷贝 streaming Asset!");
                            callback();
                            return;
                        }
                    }
                }
            }
        }

        var streaming = Application.streamingAssetsPath;
        if (Directory.Exists(streaming) ==false)
        {
            Directory.CreateDirectory(streaming);
        }
       new Thread(new ThreadStart(() =>
            {
                var files = Directory.GetFiles(streaming,"*.*", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    File.Copy(f, Path.Combine(mLocalHotUpdateResPath, Path.GetFileName(f)));
                }
                File.WriteAllText(versionFile,"version:"+appVersion.ToString());
                callback();
            })).Start();
    }
}
