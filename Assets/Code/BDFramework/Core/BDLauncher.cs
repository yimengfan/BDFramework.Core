using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BDFramework.GameStart;
using SQLite4Unity3d;
using UnityEngine;
using BDFramework.ResourceMgr;
using BDFramework.Sql;
using BDFramework.UFlux;
using Game.ILRuntime;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BDFramework
{
    public class BDLauncher : MonoBehaviour
    {
        /// <summary>
        /// BDLauncher的版本号
        /// </summary>
        static readonly public string Version = "1.0.1";
        
        
        static public Action OnStart { get; set; }
        static public Action OnUpdate { get; set; }
        static public Action OnLateUpdate { get; set; }
        
        //当BDFrame启动完整后执行
        static public Action OnBDFrameLaunch { get; set; }

        static  public BDLauncher Inst { get; private set; }
        //全局Config
        [HideInInspector]
        public Config Config;

        // Use this for initialization
        private void Awake()
        {
            Inst = this;
            this.gameObject.AddComponent<IEnumeratorTool>();
            this.Config = this.gameObject.GetComponent<Config>();
            LaunchLocal();
            //
        }

        #region 启动非热更逻辑

        private IGameStart mainStart;
        /// <summary>
        /// 启动本地代码
        /// </summary>
        public void LaunchLocal()
        {
            //寻找iGamestart
            foreach (var t in  Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.IsClass && t.GetInterface("IGameStart") != null)
                {
                    var attr = (GameStartAtrribute)t.GetCustomAttribute(typeof(GameStartAtrribute), false);
                    if (attr != null && attr.Index==0)
                    {
                        mainStart = Activator.CreateInstance(t) as IGameStart;
                        //注册
                        mainStart.Start();
                        
                        break;
                    }
                }
            }
            
            //类型注册
            List<Type> types =new List<Type>();
            types.AddRange(typeof(Button).Assembly.GetTypes());
            types.AddRange(typeof(IButton).Assembly.GetTypes());
            var uitype = typeof(UIBehaviour);
            foreach (var t in types)
            {
                //注册所有uiComponent
                if (t.IsSubclassOf(uitype))
                {
                    ILTypeHelper.UIComponentTypes[t.FullName] = t;
                }
            }
            if (Config.CodeRoot == AssetLoadPath.Editor)
            {
                foreach (var t in types)
                {
                    if (t.IsClass && t.GetInterface("IGameStart") != null)
                    {
                        var attr = (GameStartAtrribute)t.GetCustomAttribute(typeof(GameStartAtrribute), false);
                        if (attr != null && attr.Index==1)
                        {
                            mainStart = Activator.CreateInstance(t) as IGameStart;
                            //注册
                            mainStart.Start();
                        
                            break;
                        }
                    }
                }
                
            }

        }

        #endregion

        #region 启动热更逻辑

        /// <summary>
        /// 初始化
        /// 修改版本,让这个启动逻辑由使用者自行处理
        /// </summary>
        /// <param name="GameId">单游戏更新启动不需要id，多游戏更新需要id号</param>
        public void Launch(string GameId = "")
        {
            //初始化资源加载
            string coderoot = "";
            string sqlroot = "";
            string artroot = "";

            //各自的路径
            //art
            if (Config.ArtRoot == AssetLoadPath.Editor)
            {
                if (Application.isEditor)
                {
                    //默认不走AssetBundle
                    artroot = "";
                }
                else
                {
                    //手机默认直接读取Assetbundle
                    artroot = Application.persistentDataPath;
                }
            }
            else if (Config.ArtRoot == AssetLoadPath.Persistent)
            {
                artroot = Application.persistentDataPath;
            }

            else if (Config.ArtRoot == AssetLoadPath.StreamingAsset)
            {
                if (string.IsNullOrEmpty(Config.CustomArtRoot) == false)
                {
                    artroot = Config.CustomArtRoot;
                }
                else
                {
                    artroot = Application.streamingAssetsPath;
                }
            }

            //sql
            if (Config.SQLRoot == AssetLoadPath.Editor)
            {
                //sql 默认读streaming
                sqlroot = Application.streamingAssetsPath;
            }

            else if (Config.SQLRoot == AssetLoadPath.Persistent)
            {
                sqlroot = Application.persistentDataPath;
            }
            else if (Config.SQLRoot == AssetLoadPath.StreamingAsset)
            {
                sqlroot = Application.streamingAssetsPath;
            }

            //code
            if (Config.CodeRoot == AssetLoadPath.Editor)
            {
                //sql 默认读streaming
                coderoot = "";
            }
            else if (Config.CodeRoot == AssetLoadPath.Persistent)
            {
                coderoot = Application.persistentDataPath;
            }
            else if (Config.CodeRoot == AssetLoadPath.StreamingAsset)
            {
                coderoot = Application.streamingAssetsPath;
            }

            //多游戏更新逻辑
            if (Application.isEditor == false)
            {
                if (GameId != "")
                {
                    artroot = artroot + "/" + GameId;
                    coderoot = coderoot + "/" + GameId;
                    sqlroot = sqlroot + "/" + GameId;
                }
            }

            //异步
            BResources.Load(artroot, () =>
            {
                //sql
                SqliteLoder.Load(sqlroot);
                //异步 这里如果代码很早的时候就开始走表格逻辑，有可能报错，
                //但是大部分游戏应该不会，三层回调太丑，暂时用这个
                ScriptLoder.Load(coderoot,Config.CodeRunMode);
            });


            if (OnBDFrameLaunch != null)
            {
                OnBDFrameLaunch();
            }
        }
        
        


        #endregion



        //普通帧循环
        private void Update()
        {
            if(mainStart!=null) mainStart.Update();
            //
            if (OnUpdate != null)
            {
                OnUpdate();
            }
         
        }

        //更快的帧循环
        private void LateUpdate()
        {
            if(mainStart!=null) mainStart.LateUpdate();
            //
            if (OnLateUpdate != null)
            {
                OnLateUpdate();
            }
        }

        void OnApplicationQuit()
        {
#if UNITY_EDITOR
            if (SqliteHelper.DB != null)
            {
               SqliteHelper.DB.Close();
            }

            ILRuntimeHelper.Close();
#endif
        }
    }
}