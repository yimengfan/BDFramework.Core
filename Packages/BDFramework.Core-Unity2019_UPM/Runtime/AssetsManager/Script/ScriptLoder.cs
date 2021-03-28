using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BDFramework.Core.Tools;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sirenix.Utilities;
using UnityEngine;

namespace BDFramework
{
    static public class ScriptLoder
    {
        static readonly public string DLLPATH = "Hotfix/hotfix.dll";
        /// <summary>
        /// 加载的Assembly
        /// </summary>
        static public Assembly Assembly { get; private set; }
        /// <summary>
        /// 反射注册
        /// </summary>
        private static Action<bool> GamelogicILRBindAction { get; set; }

        /// <summary>
        /// 脚本加载入口
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="runMode"></param>
        /// <param name="editorModelGamelogicTypes">编辑器模式下 UPM隔离了dll,需要手动传入</param>
        static public void Load(AssetLoadPath loadPath,
            HotfixCodeRunMode runMode,
            Type[] editorModelGamelogicTypes,
            Action<bool> gamelogicILRBindAction)
        {
            ScriptLoder.GamelogicILRBindAction = gamelogicILRBindAction;
            
            if (loadPath == AssetLoadPath.Editor)
            {
                BDebug.Log("内置code模式!");
                //反射调用，防止编译报错
                var assembly = Assembly.GetExecutingAssembly();
                var type = assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                //添加框架部分的type，热更下不需要，打包会把框架的部分打进去
                var list = new List<Type>();
                list.AddRange(editorModelGamelogicTypes);
                list.AddRange(typeof(BDLauncher).Assembly.GetTypes());
                method.Invoke(null, new object[] {list.ToArray()});
            }
            else
            {
                var path = "";
                if (Application.isEditor)
                {
                    if (loadPath == AssetLoadPath.Persistent)
                    {
                        path = Path.Combine(Application.persistentDataPath,
                            BDApplication.GetPlatformPath(Application.platform));
                    }
                    else if (loadPath == AssetLoadPath.StreamingAsset)
                    {
                        path = Path.Combine(Application.streamingAssetsPath,
                            BDApplication.GetPlatformPath(Application.platform));
                    }
                }
                else
                {
                    //真机环境，代码在persistent下，因为需要io
                    path = Path.Combine(Application.persistentDataPath,
                        BDApplication.GetPlatformPath(Application.platform));
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
                BDebug.Log("反射Dll路径:" + dllPath, "red");
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

                BDebug.Log("反射加载成功,开始执行Start");
                var type = typeof(ScriptLoder).Assembly.GetType("BDLauncherBridge");
                var method = type.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);

                method.Invoke(null, new object[] {Assembly.GetTypes()});
            }
            //解释执行
            else if (mode == HotfixCodeRunMode.ByILRuntime)
            {
                BDebug.Log("热更Dll路径:" + dllPath, "red");
                //解释执行模式
                ILRuntimeHelper.LoadHotfix(dllPath,GamelogicILRBindAction);
                var gamelogicTypes = ILRuntimeHelper.GetHotfixTypes();
                ILRuntimeHelper.AppDomain.Invoke("BDLauncherBridge", "Start", null, new object[] {gamelogicTypes});
            }
            else
            {
                BDebug.Log("Dll路径:内置", "red");
            }
        }
    }
}