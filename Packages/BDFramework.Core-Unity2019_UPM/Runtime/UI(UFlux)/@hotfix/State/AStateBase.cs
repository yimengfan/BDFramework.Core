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
        Dictionary<string, MemberInfo> propMap = new Dictionary<string, MemberInfo>();

        public AStateBase()
        {
            var t = this.GetType();
            var cache = StateFactory.GetCache(t);
            if (cache == null)
            {
                List<MemberInfo> list = new List<MemberInfo>();
                var flag = BindingFlags.Instance | BindingFlags.Public;
                list.AddRange(t.GetFields(flag));
                list.AddRange(t.GetProperties(flag));
                //缓存所有属性
                propMap = new Dictionary<string, MemberInfo>();
                foreach (var mi in list)
                {
                    var attr = mi.GetAttributeInILRuntime<TransformPath>();
                    if (attr!=null)
                    {
                        propMap[mi.Name] = mi;
                    }
                }

                StateFactory.AddCache(t, propMap);
            }
            else
            {
                propMap = cache;
            }
        }

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
            if (this.propMap.TryGetValue(fieldName, out mi))
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
            if (this.propMap.TryGetValue(fieldName, out mi))
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
        /// 属性改变的回调
        /// </summary>
        private List<string> curProptyChangeList = new List<string>();

        /// <summary>
        /// 设置属性改变
        /// </summary>
        /// <param name="name"></param>
        public void SetPropertyChange(string name)
        {
            curProptyChangeList.Add(name);
        }

        /// <summary>
        /// 设置所有属性改变
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void SetAllPropertyChanged()
        {
            var t = this.GetType();
            var map = StateFactory.GetCache(t);
            var list = map.Keys.ToArray();
            this.curProptyChangeList.Clear();
            this.curProptyChangeList.AddRange(list);
        }
        
        private int curGetIndex = -1;
        /// <summary>
        /// 获取变更的属性
        /// </summary>
        /// <returns></returns>
        public string GetChangedProperty()
        {
            curGetIndex = curGetIndex+1;
            //
            if (curProptyChangeList.Count == 0 || curGetIndex == curProptyChangeList.Count)
            {
                curGetIndex=-1;
                curProptyChangeList.Clear();
                return null;
            }
            //获取
            var name = curProptyChangeList[curGetIndex];
            return name;
        }

        /// <summary>
        /// 是否发生改变
        /// </summary>
        /// <returns></returns>
        public bool IsChanged()
        {
            return this.curProptyChangeList.Count > 0 && curGetIndex < curProptyChangeList.Count;
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