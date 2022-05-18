using System;
using BDFramework.ResourceMgr;


namespace BDFramework.ResourceMgrV2
{
    /// <summary>
    /// 这里实验性功能扩展
    /// </summary>
    public partial class BResources
    {
        /// <summary>
        /// 同步加载
        /// 返回更安全的包装类型,防止接口滥用.
        /// 也更好进行资源管理
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="pathType">加载类型：路径名还是GUID</param>
        /// <param name="groupName">加载组,用以对资源加载分组</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static GameObjectWrapper Load(string assetPath, LoadPathType pathType = LoadPathType.RuntimePath, string groupName = null)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new Exception("路径错误!");
            }

            
            // //添加到资源组
            // AddAssetsPathToGroup(groupName, assetPath);
            // //加载
            // return ResLoader.Load<GameObject>(assetPath, pathType);
            return new GameObjectWrapper(null);
        }
    }
}
