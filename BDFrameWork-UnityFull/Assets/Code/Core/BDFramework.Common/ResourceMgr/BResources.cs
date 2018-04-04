using UnityEngine;
using System.Collections;
using BDFramework.ResourceMgr;
using System;
using System.Collections.Generic;
public class BResources : MonoBehaviour 
{
    static bool _isAssetBundleModel =true;

    public static bool IsAssetBundleModel
    {
        get { return _isAssetBundleModel; }
        set
        {
            _isAssetBundleModel = value;
            if (_isAssetBundleModel)
            {
                mResMgr = mAssetBundleMgr;
             
                 BDeBug.I.Log("切换到:热更新加载模式");
            }
            else
            {
                mResMgr = mRousourcesMgr;
                 BDeBug.I.Log("切换到:正常加载模式");
            }
        }
    }
    static public IResMgr mResMgr {get;set;}


    static private IResMgr mAssetBundleMgr = new AssetBundleMgr();
    static private IResMgr mRousourcesMgr = new ResourcesMgr(); 
    void Awake()
    {
        //
        DontDestroyOnLoad(this.gameObject);

      
        if (IsAssetBundleModel)
        {
            //实例化为ab加载管理
            mResMgr = mRousourcesMgr;
        }
        else
        {
            //实例化为封装系统实例化接口
            mResMgr = mAssetBundleMgr;
        }
       


    }
    static string mLoadPath;
    public static void SetLocalPath(string path)
    {
         BDeBug.I.Log(string.Format("设置加载路径为：{0}", path));
        mAssetBundleMgr.LocalHotUpdateResPath = path;
    }
    /// <summary>
    /// 加载依赖文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="callback"></param>

   public static  void LoadManifestAsync(string path, Action<bool> callback)
    {
        mResMgr.LoadManifestAsync(path, callback);
    }
    /// <summary>
    /// 同步加载
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T Load<T>(string name) where T : UnityEngine.Object
    {
        return mResMgr.Load<T>(name);
    }
    /// <summary>
    /// 异步加载
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="objName">名称</param>
    /// <param name="action">回调函数</param>
    public  static int LoadAsync<T>(string objName, Action<bool, T> action) where T : UnityEngine.Object
    {

       return  mResMgr.LoadAsync<T>(objName, action);
    }

    /// <summary>
    /// 批量加载
    /// </summary>
    /// <param name="objlist"></param>
    /// <param name="action"></param>
    public static int LoadAsync(IList<string> objlist, Action<IDictionary<string, UnityEngine.Object>> action)
    {
        return mResMgr.LoadAsync(objlist,action);
    }

    /// <summary>
    /// 批量加载
    /// </summary>
    /// <param name="objlist"></param>
    /// <param name="action"></param>
    public static int LoadAsync(IList<string> objlist, Action<string, UnityEngine.Object> action)
    {
        return mResMgr.LoadAsync(objlist, action);
    }
    /// <summary>
    /// 卸载某个gameobj
    /// </summary>
    /// <param name="o"></param>
    public static void UnloadAsset(string path ,bool isUnloadIsUsing =false)
    {
        if(string.IsNullOrEmpty(path))
            return;
        mResMgr.UnloadAsset(path, isUnloadIsUsing);
    }
    /// <summary>
    /// 卸载所有的
    /// </summary>
    public static void UnloadAll()
    {
        mResMgr.UnloadAllAsset();
 
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
        mResMgr.LoadCancel(id);
    }
    

    /// <summary>
    /// 取消所有任务
    /// </summary>
    public static void LoadCancel()
    {
        mResMgr.LoadAllCalcel();
    }

    void Update()
    {
        if (mResMgr != null)
        {
            mResMgr.Update();
        }
    }

}
