using System;
using System.Collections.Generic;
using BDFramework.Editor.AssetGraph.Node;
using BDFramework.ResourceMgr;
using LitJson;

namespace BDFramework.Editor.AssetBundle
{
    /// <summary>
    /// build资产信息
    /// </summary>
    public class BuildAssetsInfo
    {
        public class BuildAssetData
        {
            /// <summary>
            /// Id
            /// </summary>
            public int Id { get; set; } = -1;

            /// <summary>
            /// 在artConfig中的idx,用以辅助其他模块逻辑
            /// </summary>
            public int ArtConfigIdx { get; set; } = -1;

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
            /// 被依赖次数
            /// </summary>
            public int ReferenceCount { get; set; } = 0;

            /// <summary>
            /// hash
            /// </summary>
            public string Hash { get; set; } = "";

            /// <summary>
            /// 依赖列表
            /// </summary>
            public List<string> DependAssetList { get; set; } = new List<string>();

            /// <summary>
            /// 是否被多次引用
            /// </summary>
            public bool IsRefrenceByOtherAsset()
            {
                return this.ReferenceCount > 1;
            }
        }

        /// <summary>
        /// time
        /// </summary>
        public string Time;

        /// <summary>
        /// 资源列表
        /// </summary>
        public Dictionary<string, BuildAssetData> AssetDataMaps = new Dictionary<string, BuildAssetData>(StringComparer.OrdinalIgnoreCase);

        public enum SetABNameMode
        {
            Simple, //如果AB名被修改，则不会再次修改.用以不覆盖先执行的AB颗粒度规则
            Force,  //强制修改该AB名,即使有其他规则已经修改过该AB颗粒度
            ForceAndFixAllRef,// ForceAndFixAllRef:强制修改，并且也修改引用该资源的AB名
            Lock //锁住，修改完不允许其他规则再次修改
        }

        /// <summary>
        /// 设置AB名(颗粒度)
        /// </summary>
        public bool SetABName(string assetName, string newABName, SetABNameMode setNameMode = SetABNameMode.Simple ,string changelog ="")
        {
            //1.如果ab名被修改过,说明有其他规则影响，需要理清打包规则。（比如散图打成图集名）
            //2.如果资源被其他资源引用，修改ab名，需要修改所有引用该ab的名字

            bool isCanSetABName = false;
            bool isSetAllDependAB = false;

            this.AssetDataMaps.TryGetValue(assetName, out var assetData);
            //
            if (assetData != null)
            {
                switch (setNameMode)
                {
                    //未被其他规则设置过abname,可以直接修改
                    case SetABNameMode.Simple:
                    {
                        //AB名和资源名相等说明没有被修改过
                        if (assetData.ABName.Equals(assetName, StringComparison.OrdinalIgnoreCase) || assetData.ABName == newABName)
                        {
                            isCanSetABName = true;
                        }
                    }
                        break;

                    //强行修改
                    case SetABNameMode.Force:
                    {
                        isCanSetABName = true;
                    }
                        break;
                    //强行修改 并且修改所有AB引用
                    case SetABNameMode.ForceAndFixAllRef:
                    {
                        isCanSetABName = true;
                        isSetAllDependAB = true;
                    }
                        break;
                }
            }


            if (isCanSetABName)
            {
                assetData.ABName = newABName;
            }

            //设置所有依赖的AB name
            if (isSetAllDependAB)
            {
                //刷新整个列表替换
                foreach (var assetItem in this.AssetDataMaps)
                {
                    //依赖替换
                    for (int i = 0; i < assetItem.Value.DependAssetList.Count; i++)
                    {
                        if (assetItem.Value.DependAssetList[i] == assetName)
                        {
                            assetItem.Value.DependAssetList[i] = newABName;
                        }
                    }
                }
            }


            return isCanSetABName;
        }

        /// <summary>
        /// 预览 assetbundle颗粒度
        /// </summary>
        /// <returns>ab - 所有</returns>
        public Dictionary<string, List<string>> PreviewAssetbundleUnit()
        {
            var retMap = new Dictionary<string, List<string>>();

            foreach (var item in AssetDataMaps)
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
        public BuildAssetsInfo Clone()
        {
            //手动new防止内部map，防止构造传参失效
            BuildAssetsInfo tempBuildAssetsInfo = new BuildAssetsInfo();
            //
            var json = JsonMapper.ToJson(this);
            var temp = JsonMapper.ToObject<BuildAssetsInfo>(json);
            foreach (var item in temp.AssetDataMaps)
            {
                tempBuildAssetsInfo.AssetDataMaps[item.Key] = item.Value;
            }

            return tempBuildAssetsInfo;
        }
    }
}
