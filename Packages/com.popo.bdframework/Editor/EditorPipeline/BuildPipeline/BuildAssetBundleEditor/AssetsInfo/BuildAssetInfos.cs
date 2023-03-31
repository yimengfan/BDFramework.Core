using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using BDFramework.StringEx;
using LitJson;
using UnityEditor;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

namespace BDFramework.Editor.BuildPipeline.AssetBundle
{
    /// <summary>
    /// build资产信息
    /// </summary>
    public class BuildAssetInfos
    {
        static public string Tag = "BuildPipeline-Assets";

        /// <summary>
        /// 上次修改时间
        /// </summary>
        public long LastChangeTime = 0;

        /// <summary>
        /// Asset type
        /// </summary>
        public List<string> AssetTypeList { get; private set; } = new List<string>();

        /// <summary>
        /// 资产信息
        /// </summary>
        public class AssetInfo
        {
            /// <summary>
            /// Id
            /// </summary>
            public int Id { get; set; } = -1;

            /// <summary>
            /// 在artConfig中的idx,用以辅助其他模块逻辑
            /// 因为转成assetItem后会有数据丢失
            /// </summary>
            public int ArtAssetsInfoIdx { get; set; } = -1;

            /// <summary>
            /// 资源类型
            /// </summary>
            public int Type { get; set; } = -1;

            /// <summary>
            /// AssetBundleName
            /// 默认AB是等于自己文件名
            /// 当自己自己处于某个ab中的时候这个不为null
            /// </summary>
            public string ABName { get; set; } = "";

            /// <summary>
            /// AB文件的hash
            /// </summary>
            public string ABHash { get; set; } = "";

            /// <summary>
            /// GUid
            /// </summary>
            public string GUID { get; set; }

            /// <summary>
            /// AssetBundle Load的type
            /// </summary>
            public AssetLoaderFactory.AssetBunldeLoadType AssetBundleLoadType { get; set; } = AssetLoaderFactory.AssetBunldeLoadType.Base;

            /// <summary>
            /// 被依赖次数
            /// </summary>
            public int ReferenceCount { get; set; } = 0;

            /// <summary>
            /// hash
            /// 使用murmurhash3构建
            /// </summary>
            public string Hash { get; set; } = "";

            
            /// <summary>
            /// 依赖列表
            /// </summary>
            public List<string> DependAssetList { get; set; } = new List<string>();

            /// <summary>
            /// 是否保留GUID加载
            /// </summary>
            public bool IsKeepGUID { get; set; } = false;

            /// <summary>
            /// 是否被多次引用
            /// </summary>
            public bool IsRefrenceByOtherAsset()
            {
                return this.ReferenceCount > 1;
            }
        }

        /// <summary>
        /// 设置颗粒度的log
        /// </summary>
        public class SetLog
        {
            /// <summary>
            /// 上次设置等级
            /// </summary>
            public SetABPackLevel LastSetLevel = SetABPackLevel.None;

            /// <summary>
            /// log
            /// </summary>
            public string Log = "";

            /// <summary>
            /// 依赖设置
            /// </summary>
            public bool IsSetByDepend = false;

            public string LastSetABName = "";

            public string Time = "";
        }

        /// <summary>
        /// time
        /// </summary>
        public string Time;

        /// <summary>
        /// 参与打包的所有资源
        /// </summary>
        public Dictionary<string, AssetInfo> AssetInfoMap { get; private set; } = new Dictionary<string, AssetInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 验证资产数量
        /// </summary>
        public int GetValidAssetsCount()
        {
            return AssetInfoMap.Keys.Where((k) => File.Exists(k)).Count();
        }

        /// <summary>
        /// 设置日志
        /// </summary>
        private Dictionary<string, List<SetLog>> SetAssetLogs = new Dictionary<string, List<SetLog>>();

        public enum SetABPackLevel
        {
            //高级会覆盖低级设置,
            None = 0,
            Simple, //lv1.如果AB名被修改，则不会再次修改.用以不覆盖先执行的AB颗粒度规则
            Force, //lv2.强制修改该AB名,即使有其他规则已经修改过该AB颗粒度
            FrameworkDefault, //lv3. 框架默认
            Lock //lv4. 最高等级,锁住，修改完不允许其他规则再次修改
        }

        /// <summary>
        ///  设置AB包颗粒度
        /// </summary>
        /// <param name="assetName">资产名</param>
        /// <param name="newABName">ab名，最好为一个资产的路径如：文件夹名、prefab名</param>
        /// <param name="setLevel">设置等级，级别高的会覆盖低的</param>
        /// <param name="owerLog">日志，</param>
        /// <param name="isSetAllDependAsset">设置依赖资产</param>
        /// <returns></returns>
        public (bool, string) SetABPack(string assetName, string newABName, SetABPackLevel setLevel, string owerLog,
            bool isSetAllDependAsset)
        {
            //Folder有些时候没被添加
            if (Directory.Exists(newABName))
            {
                if (!this.AssetInfoMap.ContainsKey(newABName))
                {
                    this.AddAsset(newABName);
                }
            }

            // if (newABName.Equals("assets/resource_svn/runtime/battle/char/common/model/skin_default/skillshow/commontimeline_diagonal02hit13_right.prefab", StringComparison.OrdinalIgnoreCase))
            // {
            //     Debug.Log("1111111111");
            // }
            //1.如果ab名被修改过,说明有其他规则影响，需要理清打包规则。（比如散图打成图集名）
            //2.如果资源被其他资源引用，修改ab名，需要修改所有引用该ab的名字
            bool isCanSetABName = false;
            this.AssetInfoMap.TryGetValue(assetName, out var assetInfo);
            if (assetInfo == null)
            {
                return (false, $"！！！！！！！！！！！！！！！！不存在assetName，请检查：{assetName}");
            }

            //设置日志
            this.SetAssetLogs.TryGetValue(assetName, out var list);
            if (list == null)
            {
                list = new List<SetLog>();
                this.SetAssetLogs[assetName] = list;
            }

            var lastSetLog = list.LastOrDefault();
            if (lastSetLog == null)
            {
                isCanSetABName = true;
            }
            else
            {
                //设置级别比较
                if ((int) setLevel > (int) lastSetLog.LastSetLevel || newABName == lastSetLog.LastSetABName)
                {
                    isCanSetABName = true;
                }
            }

            //输出日志
            string retLog = "";
            if (lastSetLog != null)
            {
                var dependStr = lastSetLog.IsSetByDepend ? "【Depend】" : "";
                retLog = $"\n当前:<color=yellow>Level:{setLevel} Log:{owerLog}</color>, " +
                         $"上次:<color=red>{dependStr}</color> <color=yellow>Level:{lastSetLog.LastSetLevel} Log:{lastSetLog.Log}</color> " +
                         $"\n<color=green>LastTime</color> : {lastSetLog.Time} " +
                         $"<color=green>CurTime</color> : {DateTime.Now.ToLongTimeString()} " +
                         $"\n<color=green>LastAB</color> : {lastSetLog.LastSetABName} " +
                         $"<color=green>CurAB</color> : {newABName}";
            }


            if (isCanSetABName)
            {
                assetInfo.ABName = newABName;

                list.Add(new SetLog()
                {
                    LastSetLevel = setLevel,
                    Log = owerLog,
                    LastSetABName = newABName,
                    Time = DateTime.Now.ToLongTimeString()
                });
            }
            else if (lastSetLog.Log != owerLog || lastSetLog.LastSetLevel != setLevel ||
                     lastSetLog.LastSetABName != newABName)
            {
                retLog = ($"【颗粒度】设置失败{assetName} " + retLog);
            }
            else if (lastSetLog.Log == owerLog && lastSetLog.LastSetLevel == setLevel &&
                     lastSetLog.LastSetABName == newABName)
            {
                return (true, retLog);
            }

            //设置所有依赖的AB name
            if (isCanSetABName && isSetAllDependAsset)
            {
                foreach (var depend in assetInfo.DependAssetList)
                {
                    if (depend != assetName)
                    {
                        // SetABPack(depend, newABName, setLevel, "【depend】" + owerLog, false);
                        SetABPack(depend, newABName, setLevel, owerLog, false);
                    }
                }
            }


            return (isCanSetABName, retLog);
        }

        /// <summary>
        /// 设置LoadType
        /// </summary>
        /// <param name="loadType"></param>
        /// <returns></returns>
        public bool SetABLoadType(string assetName, AssetLoaderFactory.AssetBunldeLoadType loadType)
        {
            this.AssetInfoMap.TryGetValue(assetName, out var assetInfo);

            if (assetInfo != null)
            {
                assetInfo.AssetBundleLoadType = loadType;
            }
            return false;

        }

        /// <summary>
        /// 预览 assetbundle颗粒度
        /// </summary>
        /// <returns>ab - 所有</returns>
        public Dictionary<string, List<string>> PreGetAssetbundleUnit()
        {
            var retMap = new Dictionary<string, List<string>>();

            foreach (var item in AssetInfoMap)
            {
                //增加索引
                var key = item.Value.ABName;
                if (!retMap.ContainsKey(key))
                {
                    retMap[key] = new List<string>();
                }

                //添加
                retMap[key].Add(item.Key);
            }

            return retMap;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public BuildAssetInfos Clone()
        {
            //手动new防止内部map，防止构造传参失效
            BuildAssetInfos tempBuildAssetInfos = new BuildAssetInfos();
            //
            var json = JsonMapper.ToJson(this);
            var temp = JsonMapper.ToObject<BuildAssetInfos>(json);
            foreach (var item in temp.AssetInfoMap)
            {
                tempBuildAssetInfos.AssetInfoMap[item.Key] = item.Value;
            }

            return tempBuildAssetInfos;
        }

        /// <summary>
        /// 获取一个新实例的AssetInfo
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public AssetInfo GetNewInstanceAssetInfo(string path)
        {
            var assetInfo = GetAssetInfo(path, false);
            if (assetInfo != null)
            {
                var json = JsonMapper.ToJson(assetInfo);
                //返回一个新实例
                return JsonMapper.ToObject<AssetInfo>(json);
            }

            return null;
        }


        /// <summary>
        /// 创建构建资产信息
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        private AssetInfo CreateAssetInfo(string assetPath)
        {
            //创建
            var assetInfo = new AssetInfo();
            assetInfo.GUID = AssetBundleToolsV2.AssetPathToGUID(assetPath);
            assetInfo.Hash = AssetBundleToolsV2.GetAssetsHash(assetPath);
            //默认颗粒度 给自己
            assetInfo.ABName = assetPath;
            assetInfo.Type = GetAssetTypeIdx(assetPath);
            //依赖列表
            var dependeAssetList = AssetBundleToolsV2.GetDependAssetList(assetPath);
            assetInfo.DependAssetList = new List<string>(dependeAssetList);

            return assetInfo;
        }


        /// <summary>
        /// 获取资源类型
        /// </summary>
        /// <returns></returns>
        private int GetAssetTypeIdx(string assetPath)
        {
            //判断资源类型
            var type = AssetBundleToolsV2.GetMainAssetTypeAtPath(assetPath);
            if (type == null)
            {
                Debug.LogError("获取资源类型失败:" + assetPath);
                return -1;
            }

            //
            var idx = this.AssetTypeList.FindIndex((a) => a == type.FullName);
            if (idx == -1)
            {
                this.AssetTypeList.Add(type.FullName);
                idx = AssetTypeList.Count - 1;
            }

            return idx;
        }

        /// <summary>
        /// 添加AssetInfo
        /// </summary>
        /// <param name="assetInfo">缓存的AssetInfo</param>
        public void AddAsset(string assetPath, AssetInfo assetInfo)
        {
            assetPath = assetPath.ToLower();
            if (!this.AssetInfoMap.ContainsKey(assetPath))
            {
                assetInfo.Id = this.AssetInfoMap.Count + 1;
                assetInfo.Type = GetAssetTypeIdx(assetPath);
                //添加
                this.AssetInfoMap[assetPath] = assetInfo;
            }
        }

        /// <summary>
        ///添加AssetPath
        /// </summary>
        /// <param name="assetPath"></param>
        public bool AddAsset(string assetPath)
        {
            assetPath = assetPath.ToLower();
            if (!this.AssetInfoMap.ContainsKey(assetPath))
            {
                var assetInfo = this.CreateAssetInfo(assetPath);
                //添加
                assetInfo.Id = this.AssetInfoMap.Count + 1;
                this.AssetInfoMap[assetPath] = assetInfo;

                return true;
            }

            return false;
        }


        private bool isReorganized = false;

        /// <summary>
        /// 重组Assetbundle 颗粒度
        /// </summary>
        public void ReorganizeAssetBundleUnit()
        {
            if (isReorganized)
            {
                return;
            }

            isReorganized = true;

            #region 整理AB颗粒度

            //1.把依赖资源替换成AB Name，
            foreach (var item in this.AssetInfoMap)
            {
                var mainAsset = item.Value;
                // if (mainAsset.ABName.Equals("assets/resource/runtime/shader/allshaders.shadervariants", StringComparison.OrdinalIgnoreCase))
                // {
                //     Debug.Log("xx");
                // }
                //
                for (int i = 0; i < mainAsset.DependAssetList.Count; i++)
                {
                    var dependAssetName = mainAsset.DependAssetList[i];
                    if (this.AssetInfoMap.TryGetValue(dependAssetName, out var ai))
                    {
                        //替换成真正AB名
                        if (!string.IsNullOrEmpty(ai.ABName))
                        {
                            mainAsset.DependAssetList[i] = ai.ABName;
                        }
                    }
                    else
                    {
                        if (!File.Exists(dependAssetName))
                        {
                            Debug.LogError($"【AssetbundleV2】资源整理出错,不存在:{dependAssetName} - mainAssets: {item.Key} ");
                        }
                        else
                        {
                            Debug.LogError($"【AssetbundleV2】资源整理出错,未收集:{dependAssetName} - mainAssets: {item.Key} ");
                        }
                    }
                }

                //去重，将本体移到最后
                mainAsset.DependAssetList = mainAsset.DependAssetList.Distinct().ToList();
                mainAsset.DependAssetList.Remove(mainAsset.ABName);
                mainAsset.DependAssetList.Add(mainAsset.ABName);
            }

            //2.按规则纠正ab名
            //使用guid 作为ab名
            foreach (var mainAsset in this.AssetInfoMap)
            {
                var guid = AssetBundleToolsV2.AssetPathToGUID(mainAsset.Value.ABName);
                if (!string.IsNullOrEmpty(guid))
                {
                    mainAsset.Value.ABName = guid;
                }
                else
                {
                    BDebug.LogError(BuildAssetInfos.Tag, "主资源，获取GUID失败：" + mainAsset.Value.ABName);
                }

                for (int i = 0; i < mainAsset.Value.DependAssetList.Count; i++)
                {
                    var dependAssetPath = mainAsset.Value.DependAssetList[i];

                    guid = AssetBundleToolsV2.AssetPathToGUID(dependAssetPath);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        mainAsset.Value.DependAssetList[i] = guid;
                    }
                    else
                    {
                        if (!File.Exists(dependAssetPath))
                        {
                            BDebug.LogError(BuildAssetInfos.Tag, $"文件不存在,获取GUID失败：{dependAssetPath}");
                        }
                        else
                        {
                            BDebug.LogError(BuildAssetInfos.Tag, $"文件存在,获取GUID失败：{dependAssetPath}");
                        }
                    }
                }
            }

            #endregion
        }


        /// <summary>
        ///获取Assetbundle 打包列表
        /// </summary>
        public List<AssetBundleItem> GetAssetBundleItems()
        {
            if (!isReorganized)
            {
                Debug.LogError("AssetMap没有过重新整理过");
                return null;
            }


            var cloneAssetInfos = this.Clone();

            //根据buildinfo 生成加载用的 Config
            //runtime下的全部保存配置，其他的只保留一个ab名即可
            //1.导出配置
            var assetbundleItemList = new List<AssetBundleItem>();
            //占位，让id和idx恒相等
            assetbundleItemList.Add(new AssetBundleItem(0, null, null, 0,null, -1, new int[] { }));

            //I.先搜集所有的AssetBundlePath文件,因为ab只会出现一次
            foreach (var assetInfo in cloneAssetInfos.AssetInfoMap)
            {
                var find = assetbundleItemList.FirstOrDefault((abi) => abi.AssetBundlePath == assetInfo.Value.ABName);
                if (find == null) //不存在
                {
                    var abItem = new AssetBundleItem(assetbundleItemList.Count, "", assetInfo.Value.ABName, (int)assetInfo.Value.AssetBundleLoadType,"", -1, new int[] { });
                    assetbundleItemList.Add(abItem); //.ManifestMap[key] = mi;
                }
                else
                {
                    //存在则根据一些情况更新数据

                    //更新LoadType
                    if (find.AssetBundleLoadType == (int)AssetLoaderFactory.AssetBunldeLoadType.Base)
                    {
                        find.AssetBundleLoadType = (int)assetInfo.Value.AssetBundleLoadType;
                    }
                    else if(assetInfo.Value.AssetBundleLoadType!= (int)AssetLoaderFactory.AssetBunldeLoadType.Base)
                    {
                        Debug.LogError($"AssetLoder冲突! \n {JsonMapper.ToJson(assetInfo,true)}");
                    }
                    
                }
            }

            Debug.Log($"<color=yellow>AssetBundle数量:{assetbundleItemList.Count}</color>");


            //II.搜集runtime的 ,分两个for 让序列化后的数据更好审查
            int counter = 0;
            foreach (var assetInfo in cloneAssetInfos.AssetInfoMap)
            {
                //runtime路径下，写入配置
                //剔除文件夹
                if (AssetBundleToolsV2.IsRuntimePathAssetWithoutFolder(assetInfo.Key))
                {
                    var loadPath = AssetBundleToolsV2.GetAbsPathFormRuntime(assetInfo.Key);
                    //添加
                    var abItem = new AssetBundleItem(assetbundleItemList.Count, loadPath, "",  (int)assetInfo.Value.AssetBundleLoadType,assetInfo.Value.GUID, assetInfo.Value.Type, new int[] { });

                    assetbundleItemList.Add(abItem); 
                    assetInfo.Value.ArtAssetsInfoIdx = abItem.Id;
                    counter++;
                }
            }

            Debug.Log($"<color=yellow>Runtime数量:{counter}</color>");
            Debug.Log($"<color=yellow>ABList数量:{assetbundleItemList.Count}</color>");
            //III. 保留GUID的操作，可能会加载非Runtime下的资产
            foreach (var assetInfo in cloneAssetInfos.AssetInfoMap)
            {
                //保留GUID
                if (assetInfo.Value.IsKeepGUID && assetInfo.Value.ArtAssetsInfoIdx == -1)
                {
                    if (!AssetBundleToolsV2.IsRuntimePath(assetInfo.Key))
                    {
                        var abItem = new AssetBundleItem(assetbundleItemList.Count, "", "",  (int)assetInfo.Value.AssetBundleLoadType,assetInfo.Value.GUID, assetInfo.Value.Type, new int[] { });
                        assetbundleItemList.Add(abItem);
                        assetInfo.Value.ArtAssetsInfoIdx = abItem.Id;
                    }
                    else
                    {
                        Debug.LogError("流程出错，出现runtime资产未在列表中");
                    }
                }
            }


            //2.将depend资源
            //替换成依赖的AB,减少序列化数据量
            for (int idx = 0; idx < assetbundleItemList.Count; idx++)
            {
                var assetbundleItem = assetbundleItemList[idx];

                if (!string.IsNullOrEmpty(assetbundleItem.LoadPath) || !string.IsNullOrEmpty(assetbundleItem.GUID))
                {
                    var assetInfoItem = cloneAssetInfos.AssetInfoMap.FirstOrDefault((ai) => ai.Value.ArtAssetsInfoIdx == idx || ai.Value.GUID == assetbundleItem.GUID);

                    //dependAsset 替换成ID
                    List<int> dependIdList = new List<int>();
                    foreach (var dependGUID in assetInfoItem.Value.DependAssetList)
                    {
                        //寻找保存列表中依赖的id（可以认为是下标）
                        var dependAsset = cloneAssetInfos.AssetInfoMap.Values.FirstOrDefault((ai) => ai.GUID == dependGUID);
                        int dependABIdx = -1;
                        if (dependAsset != null)
                        {
                            dependABIdx = assetbundleItemList.FindIndex((abi) => abi.AssetBundlePath == dependAsset.ABName);
                        }
                        else //可能是文件夹
                        {
                            Debug.LogError($"AssetInfoMap不存在资产:{AssetDatabase.GUIDToAssetPath(dependGUID)}");
                        }


                        //依赖的AB

                        if (dependABIdx > -1)
                        {
                            dependIdList.Add(dependABIdx);
                        }
                        else
                        {
                            Debug.LogError($"AssetbundleItemList中不存在依赖:{assetInfoItem.Key}-{dependGUID} / {AssetDatabase.GUIDToAssetPath(dependGUID)}");
                        }
                    }

                    //赋值dependAssetIdx,去重，排序，保持主资源在最后
                    var tmpList = dependIdList.Distinct().ToList();
                    var mainIdx = tmpList.Last();
                    tmpList.Remove(mainIdx);
                    if (tmpList.Count > 1)
                    {
                        tmpList.Sort();
                    }

                    tmpList.Add(mainIdx);
                    assetbundleItem.DependAssetIds = tmpList.ToArray();
                }
            }

            //4.设置溯源packHash
            foreach (var abi in assetbundleItemList)
            {
                if (abi.IsAssetBundleSourceFile())
                {
                    //获取到pack hash
                    var packHash = cloneAssetInfos.GetAssetsPackSourceHash(abi.AssetBundlePath);
                    //赋值
                    abi.AssetsPackSourceHash = packHash;
                }
            }

            //最后.检查config是否遗漏  
            foreach (var assetInfo in cloneAssetInfos.AssetInfoMap)
            {
                AssetBundleItem checkABItem = null;
                //使用runtime加载
                if (AssetBundleToolsV2.IsRuntimePathAssetWithoutFolder(assetInfo.Key))
                {
                    if (assetInfo.Value.ArtAssetsInfoIdx < 0 || assetInfo.Value.ArtAssetsInfoIdx > assetbundleItemList.Count)
                    {
                        Debug.LogError("AssetBundleList中不存在Runtime资产:" + assetInfo.Key);
                        continue;
                    }

                    checkABItem = assetbundleItemList[assetInfo.Value.ArtAssetsInfoIdx];
                }
                //使用GUID加载
                else if (assetInfo.Value.IsKeepGUID)
                {
                    checkABItem = assetbundleItemList.FirstOrDefault((ai) => ai.GUID == assetInfo.Value.GUID);
                    if (checkABItem == null)
                    {
                        Debug.LogError("GUID加载资产遗漏:" + assetInfo.Key);
                        continue;
                    }
                }

                if (checkABItem != null)
                {
                    //寻找依赖是否存在
                    foreach (var dependGUID in assetInfo.Value.DependAssetList)
                    {
                        var dependAI = cloneAssetInfos.AssetInfoMap.Values.FirstOrDefault((ai) => ai.GUID == dependGUID);
                        if (dependAI == null)
                        {
                            Debug.LogError("Depend资产未找到:" + AssetDatabase.GUIDToAssetPath(dependGUID));
                            continue;
                        }


                        //检查依赖是否存在
                        var ret = checkABItem.DependAssetIds.Where((did) => assetbundleItemList[did].AssetBundlePath == dependAI.ABName);
                        if (ret == null || ret.Count() == 0)
                        {
                            Debug.LogError($"Depend AB未找到:{assetInfo.Key} - ");
                        }
                        else if (ret.Count() > 1)
                        {
                            Debug.LogError($"Depend AB配置冗余:{assetInfo.Key} -  {dependAI.ABName}");
                        }
                    }
                }
            }

            //
            return assetbundleItemList;
        }

        /// <summary>
        /// 获取资产信息
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public AssetInfo GetAssetInfo(string assetPath, bool isLog = true, bool isAddAssetIfMapNotExsit = false)
        {
            var ret = this.AssetInfoMap.TryGetValue(assetPath, out var assetInfo);

            if (ret == null && isAddAssetIfMapNotExsit)
            {
            }

            if (assetInfo == null && isLog)
            {
                Debug.LogError("AssetInfoMap 不存在资产:" + assetPath);
            }

            return assetInfo;
        }


        /// <summary>
        /// 获取一个AB中所有源资产的hash
        /// </summary>
        /// <returns></returns>
        public string GetAssetsPackSourceHash(string assetbundlePath)
        {
            var assetInfos = AssetInfoMap.Values.Where((ai) => ai.ABName.Equals(assetbundlePath)).ToList();
            return GetAssetsPackSourceHash(assetInfos);
        }

        /// <summary>
        /// 获取一个AB中所有源资产的hash
        /// </summary>
        /// <returns></returns>
        static private string GetAssetsPackSourceHash(List<AssetInfo> assetInfos)
        {
            //按guid排序
            assetInfos.Sort((a, b) => { return a.GUID.CompareTo(b.GUID); });

            //
            var totalHashStr = "";
            foreach (var ai in assetInfos)
            {
                totalHashStr += ai.Hash;
            }

            //bytes
            byte[] bytes = System.Text.Encoding.Default.GetBytes(totalHashStr);

            //
            var totalHash = FileHelper.GetMurmurHash3(bytes);
            return totalHash;
        }
    }
}
