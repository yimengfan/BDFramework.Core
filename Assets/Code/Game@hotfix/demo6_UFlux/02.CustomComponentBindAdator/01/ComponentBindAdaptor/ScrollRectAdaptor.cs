using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 每一个ScrollRect的 Adaptor
    /// </summary>
    public class ScrollRectAdaptor
    {
        private ScrollRect sr;

        public ScrollRectAdaptor(ScrollRect sr)
        {
            this.sr = sr;
        }

        public Dictionary<ARenderDataBase, IComponent> ContentMap { get;private  set; } = new Dictionary<ARenderDataBase, IComponent>();


        /// <summary>
        /// 添加item
        /// </summary>
        public void AddItem(ARenderDataBase renderDataBase, IComponent component)
        {
            component.Transform.SetParent(this.sr.content, false);
            this.ContentMap[renderDataBase] = component;
        }

        /// <summary>
        /// 获取item
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IComponent GetItem(ARenderDataBase renderData)
        {
            return ContentMap[renderData];
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="renderData"></param>
        public void Destroy(ARenderDataBase renderData)
        {

            var com = ContentMap[renderData];
            com.Destroy();
            ContentMap.Remove(renderData);
        }
        
    }
}