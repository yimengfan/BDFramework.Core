using BDFramework.ResourceMgr;
using UnityEngine;

namespace BDFramework.ResourceMgrV2
{
    /// <summary>
    /// 更安全的类型包装
    /// 防止接口滥用,更好的资源管理
    /// </summary>
    public class GameObjectWrapper
    {
        /// <summary>
        /// 实例id
        /// </summary>
        public int InstId { get; private set; }

        /// <summary>
        /// 源 gameobject的id
        /// 从 哪个Gameobj派生
        /// -1 表示自己就是源
        /// </summary>
        public int SourceGameObjectId { get; private set; } = -1;

        /// <summary>
        /// gameoject
        /// </summary>
        private GameObject gameObject { get; set; }

        /// <summary>
        /// Gameobject包装类型
        /// </summary>
        /// <param name="gameObject"></param>
        public GameObjectWrapper(int instid, int sourceid, GameObject gameObject)
        {
            this.InstId = instid;
            this.SourceGameObjectId = sourceid;
            this.gameObject = gameObject;
        }


        /// <summary>
        /// 实例化
        /// </summary>
        /// <returns></returns>
        public GameObjectWrapper Clone()
        {
            var instid = BResourcesV2.GetGlobalInstId();
            var clonego = GameObject.Instantiate(this.gameObject);
            //返回gow
            return new GameObjectWrapper(instid, this.SourceGameObjectId, clonego);
        }
    }
}
