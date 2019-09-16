using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BDFramework.Helper;
using UnityEngine;

namespace BDFramework
{
    static public class ScriptLoder
    {

        static public bool IsReflectionRunning { get; private set; } = false;
        static public Assembly Assembly { get;  private set; }
        /// <summary>
        /// 开始热更脚本逻辑
        /// </summary>
        static public void Load(string root, HotfixCodeRunMode mode)
        {
            if (root != "")
            {
                string dllPath = root + "/" + BDUtils.GetPlatformPath(Application.platform) + "/hotfix/hotfix.dll";

                //反射
                if (mode == HotfixCodeRunMode.ByReflection && 
                    (Application.isEditor || Application.platform == RuntimePlatform.Android ||Application.platform == RuntimePlatform.WindowsPlayer))
                {
                    //反射模式只支持Editor PC Android
                    if (File.Exists(dllPath)) //支持File操作 或者存在
                    {
                        var bytes = File.ReadAllBytes(dllPath);
                        var bytes2 = File.ReadAllBytes(dllPath+".mdb");
                        Assembly = Assembly.Load(bytes,bytes2);
                        var type = Assembly.GetType("BDLauncherBridge");
                        var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                        method.Invoke(null, new object[] {false, true});
                    }
                    else  
                    {
                        //不支持file操作 或者不存在,继续尝试
                        IEnumeratorTool.StartCoroutine(IE_LoadDLL_AndroidOrPC(dllPath));
                    }                   

                }
                //ILR
                else
                {
                    //
                    //ILRuntime基于文件流，所以不支持file操作的，得拷贝到支持File操作的目录
                    
                    //这里情况比较复杂,Mobile上基本认为Persistent才支持File操作,
                    //可寻址目录也只有 StreamingAsset
                    var firstPath  = Application.persistentDataPath + "/" + BDUtils.GetPlatformPath(Application.platform) +"/hotfix/hotfix.dll";
                    var secondPath = Application.streamingAssetsPath + "/" +BDUtils.GetPlatformPath(Application.platform) + "/hotfix/hotfix.dll";

                    if (!File.Exists(dllPath)) //仅当指定的路径不存在(或者不支持File操作)时,再进行可寻址
                    {
                        dllPath = firstPath;
                        if (!File.Exists(firstPath))
                        {
                            //验证 可寻址目录2
                            IEnumeratorTool.StartCoroutine (IE_CopyDLL_WhithLaunch(secondPath, firstPath));
                            return;
                        }
                    }
                    
                    //解释执行模式
                    ILRuntimeHelper.LoadHotfix(dllPath);
                    ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start", null, new object[] {true, false});
                }
            }
            else
            {
                //PC模式

                //这里用反射是为了 不访问逻辑模块的具体类，防止编译失败
                Assembly= Assembly.GetExecutingAssembly();
                //
                var type = Assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] {false, false});
            }
        }

        /// <summary>
        /// 加载dll
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static IEnumerator IE_LoadDLL_AndroidOrPC(string path)
        {
            if (Application.isEditor)
            {
                path = "file://" + path;
            }
            var www = new WWW(path);
            yield return www;
            if (www.isDone && www.error == null)
            {
                Assembly = Assembly.Load(www.bytes);

                var type = Assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] {false, true});
            }
            else
            {
                BDebug.LogError("DLL加载失败:" + www.error);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="copyto"></param>
        /// <returns></returns>
        static IEnumerator IE_CopyDLL_WhithLaunch(string source, string copyto)
        {

            BDebug.Log("复制到第一路径:" + source);

            var www = new WWW(source);
            yield return www;
            if (www.isDone && www.error == null)
            {
                FileHelper.WriteAllBytes(copyto,www.bytes);
                            
                //解释执行模式
                ILRuntimeHelper.LoadHotfix(copyto);
                ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start", null, new object[] {true, false});
            }
            else
            {
                Debug.LogError("可寻址目录不包括DLL:" +source);
            }
            
        }
    }
}