using System;

namespace BDFramework.Adaptor
{
    /// <summary>
    /// action的Base
    /// </summary>
    abstract public class AActionAdaptor
    {
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="o"></param>
        virtual public void Invoke(object o)
        {
            
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        virtual   public  void TriggerEvent()
        {
            
        }
        virtual public bool Equals(object o)
        {
            return false;
        }
    }
    /// <summary>
    /// Action的适配器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionAdaptor<T> :AActionAdaptor 
    {
        //缓存的action
        public Action<T> Action { get; private set; }
        public ActionAdaptor(Action<T> action)
        {
            this.Action = action;
        }
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="o"></param>
        public override void Invoke(object o)
        {
            // if (o == null)
            // {
            //     BDebug.LogError("数据监听返回为null,不执行回调:"+ Action.GetType().FullName);
            //     return;
            // }
            var t = (T)o;
            Action.Invoke(t);
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public override void TriggerEvent()
        {
            var t = default(T);
            Action.Invoke(t);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        public override bool Equals(object o)
        {
            var action = o as Action<T>;

            if (this.Action == action)
            {
                return true;
            }

            return false;
        }
    }
}