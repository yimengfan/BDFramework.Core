using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDFramework.Reflection;

namespace BDFramework.UFlux
{
    /// <summary>
    /// State属性
    /// 每个Component会被外部影响修改的
    /// 也就是需要传递的数据流
    /// </summary>
    abstract public class AStateBase : IState, IPropertyChange
    {
        /// <summary>
        /// 属性缓存
        /// </summary>
        public Dictionary<string, MemberInfo> MemberinfoMap { get; set; }
        public AStateBase( )
        {
        }

        /// <summary>
        /// 是否手动mark模式
        /// 只要设置过一次 就是永久手动模式，不会再自动了
        /// </summary>
        public bool IsMunalMarkMode { get; private set; } = false;

        /// <summary>
        /// 数据源
        /// </summary>
        public int Source { get; set; } = -1;

        /// <summary>
        /// 获取 值
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public object GetValue(string fieldName)
        {
            MemberInfo mi = null;
            if (this.MemberinfoMap.TryGetValue(fieldName, out mi))
            {
                if (mi is FieldInfo)
                {
                    return (mi as FieldInfo).GetValue(this);
                }
                else if (mi is PropertyInfo)
                {
                    return (mi as PropertyInfo).GetValue(this);
                }
            }

            return null;
        }

        /// <summary>
        /// 设置属性
        /// </summary>
        public void SetValue(string fieldName, object o)
        {
            MemberInfo mi = null;
            if (this.MemberinfoMap.TryGetValue(fieldName, out mi))
            {
                if (mi is FieldInfo)
                {
                    (mi as FieldInfo).SetValue(this, o);
                    this.SetPropertyChange(fieldName);
                }
                else if (mi is PropertyInfo)
                {
                    (mi as PropertyInfo).SetValue(this, o);
                    this.SetPropertyChange(fieldName);
                }
            }
        }


        #region 值监听

        
        /// <summary>
        /// 是否发生改变
        /// </summary>
        /// <returns></returns>
        public bool IsChanged
        {
            get
            {
                return this.changeProptyList.Count > 0;
            }
          
        }
        /// <summary>
        /// 属性改变的回调
        /// </summary>
        private List<string> changeProptyList = new List<string>();

        /// <summary>
        /// 设置属性改变
        /// </summary>
        /// <param name="name"></param>
        public void SetPropertyChange(string name)
        {
            IsMunalMarkMode = true;
            changeProptyList.Add(name);
        }

        /// <summary>
        /// 设置所有属性改变
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void SetAllPropertyChanged()
        {
            IsMunalMarkMode = true;
            var t = this.GetType();
            var map = StateFactory.GetMemberinfoCache(t);
            var list = map.Keys.ToArray();
            this.changeProptyList.Clear();
            this.changeProptyList.AddRange(list);
        }


        /// <summary>
        /// 获取变更的属性
        /// </summary>
        /// <returns></returns>
        public string[] GetChangedPropertise()
        {
            var ret = changeProptyList.ToArray();
            changeProptyList.Clear();
            return ret;
        }


        /// <summary>
        /// 获取所有的属性
        /// </summary>
        /// <returns></returns>
        public string[] GetAllPropertise()
        {
            return MemberinfoMap.Keys.ToArray();
        }
        
        #endregion

        /// <summary>
        /// 这里是浅复制，后面会改成深复制
        /// </summary>
        /// <returns></returns>
        public AStateBase Clone()
        {
            return MemberwiseClone() as AStateBase;
        }
    }
}