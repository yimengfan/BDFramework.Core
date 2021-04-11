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

        public Dictionary<APropsBase, IComponent> ContentMap { get;private  set; } = new Dictionary<APropsBase, IComponent>();


        /// <summary>
        /// 添加item
        /// </summary>
        public void AddItem(APropsBase propsBase, IComponent component)
        {
            component.Transform.SetParent(this.sr.content, false);
            this.ContentMap[propsBase] = component;
        }

        /// <summary>
        /// 获取item
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IComponent GetItem(APropsBase props)
        {
            return ContentMap[props];
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="props"></param>
        public void Destroy(APropsBase props)
        {

            var com = ContentMap[props];
            com.Destroy();
            ContentMap.Remove(props);
        }
        
    }
}