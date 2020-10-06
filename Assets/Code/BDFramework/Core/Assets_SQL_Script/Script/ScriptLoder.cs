using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Code.BDFramework.Core.Tools;
using UnityEngine;

namespace BDFramework
{
    static public class ScriptLoder
    {
        static readonly public string DLLPATH = "Hotfix/hotfix.dll";
        static public Assembly Assembly { get; private set; }

        /// <summary>
        /// 开始热更脚本逻辑
        /// </summary>
        static public void Load(AssetLoadPath loadPath, HotfixCodeRunMode runMode)
        {
            if (loadPath == AssetLoadPath.Editor)
            {
                BDebug.Log("内置code模式!");
                //反射调用，防止编译报错
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] {false, false});
            }
            else
            {
                var path = "";
                if (Application.isEditor)
                {
                    if (loadPath == AssetLoadPath.Persistent)
                    {
                        path = Path.Combine(Application.persistentDataPath, BDApplication.GetPlatformPath(Application.platform));
                    }
                    else if (loadPath == AssetLoadPath.StreamingAsset)
                    {
                        path = Path.Combine(Application.streamingAssetsPath, BDApplication.GetPlatformPath(Application.platform));
                    }
                }
                else
                {
                    //真机情况下全在persistent下
                    path = Path.Combine(Application.persistentDataPath, BDApplication.GetPlatformPath(Application.platform));
                }

                //加载dll
                var dllPath = Path.Combine(path, DLLPATH);
                LoadDLL(dllPath, runMode);
            }
        }



        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="source"></param>
        /// <param name="copyto"></param>
        /// <returns></returns>
        static void LoadDLL(string dllPath, HotfixCodeRunMode mode)
        {
            //反射执行
            if (mode == HotfixCodeRunMode.ByReflection)
            {
                BDebug.Log("Dll路径:" + dllPath, "red");
                var dllBytes = File.ReadAllBytes(dllPath);
                var pdbPath = dllPath + ".pdb";
                if (File.Exists(pdbPath))
                {
                    var pdbBytes = File.ReadAllBytes(pdbPath);
                    Assembly = Assembly.Load(dllBytes, pdbBytes);
                }
                else
                {
                    Assembly = Assembly.Load(dllBytes);
                }

                BDebug.Log("代码加载成功,开始执行Start");
                var type = Assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] {false, true});
            }
            //解释执行
            else if (mode == HotfixCodeRunMode.ByILRuntime)
            {
                BDebug.Log("Dll路径:" + dllPath, "red");
                //解释执行模式
                ILRuntimeHelper.LoadHotfix(dllPath);
                ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start", null, new object[] {true, false});
            }
            else
            {
                BDebug.Log("Dll路径:内置", "red");
            }
        }
    }
}