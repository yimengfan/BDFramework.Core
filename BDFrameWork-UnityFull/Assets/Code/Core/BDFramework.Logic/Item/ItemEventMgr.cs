using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BDFramework.Mgr;
using BDFramework.Event;
namespace  BDFramework.Logic.Item
{

    public class ItemEventMgr : MgrBase<ItemEventMgr>
    {       
        /// <summary>
        /// 检测类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        override public void CheckType(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(EventAttribute), false);
            if (attrs.Length >0)
            {
                foreach (var attr in attrs)
                {
                    var _attr = (EventAttribute)attr;
                    if(_attr.Type ==  (int)EventType.Item)
                    {
                        SaveAttribute(_attr.Name, new ClassData() { Attribute = _attr, Type = type });
                    }
                }
            }
        }

        /// <summary>
        ///  执行item的event
        /// </summary>
        /// <param name="eventName"></param>
        public void DoItemEvent(string eventName,IList<object> args = null)
        {
            var inst = this.GetTypeInst<IEvent>(eventName);
            inst.OnTrriger(args);
        }
    }

}