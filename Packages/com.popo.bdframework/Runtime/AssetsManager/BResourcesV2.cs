using BDFramework.ResourceMgr;
using UnityEngine;


namespace BDFramework.ResourceMgrV2
{
    /// <summary>
    /// 这里实验性功能扩展
    /// </summary>
    public partial class BResourcesV2
    {
        /// <summary>
        /// 实例计数
        /// </summary>
        static private int instCounter = 0;

        /// <summary>
        /// 获取全局实例id
        /// </summary>
        /// <returns></returns>
        public static int GetGlobalInstId()
        {
            instCounter++;
            return instCounter;
        }

        /// <summary>
        ///  实例化
        /// </summary>
        /// <param name="assetLoadPath">资源路径</param>
        /// <param name="pathType">加载类型：路径名还是GUID</param>
        /// <param name="groupName">加载组,用以对资源加载分组</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static GameObjectWrapper Instantiate<T>(string assetLoadPath, LoadPathType pathType = LoadPathType.RuntimePath, string groupName = null) where T : UnityEngine.Object
        {
            var id = GetGlobalInstId();
            //加载
            var go = ResourceMgr.BResources.Load<T>(assetLoadPath, pathType, groupName);
            //实例化
            if (go)
            {
                var inst = GameObject.Instantiate(go) as GameObject;
                //返回包装类型
                var gw = new GameObjectWrapper(id, -1, inst);
                return gw;
            }
            else
            {
                BDebug.LogError("BResource",$"无该资源:{assetLoadPath}");
                return new GameObjectWrapper(-1,-1,null);
            }

        }
    }
}
