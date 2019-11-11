using System.Collections.Generic;
using BDFramework.UFlux;
using BDFramework.UFlux.View.Props;
using UnityEngine.UI;

namespace BDFramework.UI.Demo_ScreenRect
{
    public class ScrollRectAdaptor
    {
        private ScrollRect sr;
        public ScrollRectAdaptor(ScrollRect sr)
        {
            this.sr = sr;
        }
        
        public List<IComponent> Contents = new List<IComponent>();

        /// <summary>
        /// 添加item
        /// </summary>
        public void AddItem(IComponent component)
        {
            component.Transform.SetParent(this.sr.content, false);
            this.Contents.Add(component);
        }

        /// <summary>
        /// 获取item
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IComponent GetItem(int index)
        {
            if (index < Contents.Count)
            {
                return Contents[index];
            }

            return null;
        }

        /// <summary>
        /// 移除无效component
        /// </summary>
        public void RemoveUnInvalidComponent()
        {

            for (int i = Contents.Count-1; i >=0; i--)
            {
                var com = Contents[i];
                if (com.IsDestroy)
                {
                    Contents.RemoveAt(i);
                }
            }
        }
    }
}