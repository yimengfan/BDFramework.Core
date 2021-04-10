using System.Collections.Generic;
using BDFramework.UFlux.View.Props;

namespace BDFramework.UFlux.Collections
{

    interface IComponentList<out T> where T : APropsBase
    {
        /// <summary>
        /// 是否改变
        /// </summary>
        bool IsChanged { get; }
        /// <summary>
        /// 获取新增列表
        /// </summary>
        /// <returns></returns>
        T[] GetNewDatas();

        /// <summary>
        /// 获取移除的元素
        /// </summary>
        /// <returns></returns>
        T[] GetRemovedDatas();


        /// <summary>
        /// 获取改变的元素
        /// </summary>
        /// <returns></returns>
        T[] GetChangedDatas();
    }
    /// <summary>
    /// 组件列表的实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CompnentList<T> : List<T> ,IComponentList<T>  where T : APropsBase
    {

        /// <summary>
        /// 是否被修改
        /// </summary>
        public bool IsChanged { get; set; }

        #region 基本容器操作重写
        /// <summary>
        /// 重写添加
        /// </summary>
        /// <param name="t"></param>
        new public void Add(T t)
        {
            IsChanged = true;
            newDataList.Add(t);
            base.Add(t);
        }


        /// <summary>
        /// 重写移除
        /// </summary>
        /// <param name="t"></param>
        new public void Remove(T t)
        {
            IsChanged = true;
            base.Remove(t);
            this.removeDataList.Add(t);
        }


        /// <summary>
        /// 重写移除
        /// </summary>
        /// <param name="idx"></param>
        public void RemoveAt(int idx)
        {
            IsChanged = true;
            this.removeDataList.Add(this[idx]);
            base.RemoveAt(idx);
        }



        /// <summary>
        /// 清理
        /// </summary>
        new public void Clear()
        {
            IsChanged = true;
            this.removeDataList.AddRange(this);
            base.Clear();
        }
        

        #endregion

        /// <summary>
        /// 设置改变的容器
        /// </summary>
        /// <param name="t"></param>
        public void SetChanedData(T t)
        {
            IsChanged = true;
            ChangedDataList.Add(t);
        }

        private List<T> ChangedDataList = new List<T>();
        private List<T> newDataList = new List<T>();
        private List<T> removeDataList = new List<T>();
        
        /// <summary>
        /// 获取新增列表
        /// </summary>
        /// <returns></returns>
        public T[] GetNewDatas()
        {
            var ret = this.newDataList.ToArray();
            this.newDataList.Clear();

            if (this.newDataList.Count == 0 && removeDataList.Count == 0 && ChangedDataList.Count == 0)
            {
                IsChanged = false;
            }
            
            return ret;
        }

        /// <summary>
        /// 获取移除的元素
        /// </summary>
        /// <returns></returns>
        public T[] GetRemovedDatas()
        {
            var ret = this.removeDataList.ToArray();
            this.removeDataList.Clear();

            if (this.newDataList.Count == 0 && removeDataList.Count == 0 && ChangedDataList.Count == 0)
            {
                IsChanged = false;
            }
            
            return ret;
        }

        /// <summary>
        /// 获取改变的元素
        /// </summary>
        /// <returns></returns>
        public T[] GetChangedDatas()
        {
            var ret = this.ChangedDataList.ToArray();
            this.ChangedDataList.Clear();

            if (this.newDataList.Count == 0 && removeDataList.Count == 0 && ChangedDataList.Count == 0)
            {
                IsChanged = false;
            }
            
            return ret;
        }

    }
}