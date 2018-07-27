using UnityEngine;
using System.Collections;
using BDFramework.ResourceMgr;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class BResources : MonoBehaviour 
{
    static bool isAssetBundleModel =true;

    public static bool IsAssetBundleModel
    {
        get { return isAssetBundleModel; }
        set
        {
            isAssetBundleModel = value;
            if (isAssetBundleModel)
            {
                resLoader = new AssetBundleMgr();
             
                 BDebug.Log("切换到:热更新加载模式");
            }
            else
            {
                resLoader = new ResourcesMgr(); 
                 BDebug.Log("切换到:正常加载模式");
            }
        }
    }
    static public IResMgr resLoader {get;set;}


    /// <summary>
    /// 加载依赖文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="callback"></param>

   public static  void LoadManifestAsync(string path, Action<bool> callback)
    {
        resLoader.AsyncLoadManifest(path, callback);
    }
    /// <summary>
    /// 同步加载
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T Load<T>(string name) where T : UnityEngine.Object
    {
        return resLoader.Load<T>(name);
    }
    
    /// <summary>
    /// 异步加载
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="objName">名称</param>
    /// <param name="action">回调函数</param>
    public  static int AsyncLoadSource<T>(string objName, Action<bool, T> action) where T : UnityEngine.Object
    {
       return  resLoader.AsyncLoadSource<T>(objName, action);
    }

    /// <summary>
    /// 批量加载
    /// </summary>
    /// <param name="objlist"></param>
    /// <param name="onLoadEnd"></param>
    public static int AsyncLoadSources(IList<string> objlist, Action<int,int> onProcess= null, Action<IDictionary<string, UnityEngine.Object>> onLoadEnd = null )
    {
        return resLoader.AsyncLoadSources(objlist,onLoadEnd ,onProcess);
    }

    /// <summary>
    /// 卸载某个gameobj
    /// </summary>
    /// <param name="o"></param>
    public static void UnloadAsset(string path ,bool isUnloadIsUsing =false)
    {
        if(string.IsNullOrEmpty(path))
            return;
        resLoader.UnloadAsset(path, isUnloadIsUsing);
    }
    /// <summary>
    /// 卸载所有的
    /// </summary>
    public static void UnloadAll()
    {
        resLoader.UnloadAllAsset();
 
    }


    public static void Destroy(Transform trans)
    {
        if (trans != null  )
            GameObject.DestroyObject(trans.gameObject);
    }
    /// <summary>
    /// 取消单个任务
    /// </summary>
    public static void LoadCancel(int id )
    {
        resLoader.LoadCancel(id);
    }
    

    /// <summary>
    /// 取消所有任务
    /// </summary>
    public static void LoadCancel()
    {
        resLoader.LoadAllCalcel();
    }

    void Update()
    {
        if (resLoader != null)
        {
            resLoader.Update();
        }
    }

}
