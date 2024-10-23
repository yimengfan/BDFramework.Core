using System;
using System.Collections.Generic;
using BDFramework.UFlux.View.Props;

namespace BDFramework.UFlux.Collections
{
    public interface IPropsList
    {
        /// <summary>
        /// Count
        /// </summary>
        int Count { get; }
        
        /// <summary>
        /// 遍历
        /// </summary>
        /// <param name="action"></param>
        void Foreach(Action<int,APropsBase> action);
        

        /// <summary>
        /// 是否改变
        /// </summary>
        bool IsChanged { get; }

        /// <summary>
        /// 获取新增列表
        /// </summary>
        /// <returns></returns>
        APropsBase[] GetNewDatas();

        /// <summary>
        /// 获取移除的元素
        /// </summary>
        /// <returns></returns>
        APropsBase[] GetRemovedDatas();
        
        /// <summary>
        /// 获取改变的元素
        /// </summary>
        /// <returns></returns>
        APropsBase[] GetChangedDatas();


        /// <summary>
        /// 清理改变的数据
        /// </summary>
        void ClearChangedData();
    }

    /// <summary>
    /// 组件列表的实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropsList<T> : IPropsList where T : APropsBase
    {
        /// <summary>
        /// 所有组件的列表，
        /// 这里暴露只是为了方便遍历，查询
        /// 不适合直接增删改操作
        /// </summary>
        public List<T> BaseList = new List<T>();

        /// <summary>
        /// 是否被修改
        /// </summary>
        public bool IsChanged { get; set; }

        #region 数组基本操作重写，防止跨域继承

        /// <summary>
        /// Count
        /// </summary>
        public int Count
        {
            get
            {
                return this.BaseList.Count;
            }
        }


        /// <summary>
        /// 遍历
        /// </summary>
        /// <param name="action"></param>
        public void Foreach(Action<int,APropsBase> action)
        {
            for (int i = 0; i < this.BaseList.Count; i++)
            {
                var t = this.BaseList[i];
                action(i,t);
            }
        }


        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="idx"></param>
        public T Get(int idx)
        {
            return this.BaseList[idx];
        }

        #endregion


        #region 基本容器操作重写

        /// <summary>
        /// 重写添加
        /// </summary>
        /// <param name="t"></param>
        new public void Add(T t)
        {
            IsChanged = true;
            newDataList.Add(t);
            this.BaseList.Add(t);
        }


        /// <summary>
        /// 设置改变的容器
        /// </summary>
        /// <param name="t"></param>
        public void SetChangedData(T t)
        {
            IsChanged = true;
            changedDataList.Add(t);
        }


        /// <summary>
        /// 重写移除
        /// </summary>
        /// <param name="t"></param>
        new public void Remove(T t)
        {
            IsChanged = true;
            this.BaseList.Remove(t);
            this.removeDataList.Add(t);
        }


        /// <summary>
        /// 重写移除
        /// </summary>
        /// <param name="idx"></param>
        public void RemoveAt(int idx)
        {
            IsChanged = true;
            this.removeDataList.Add(this.BaseList[idx]);
            this.BaseList.RemoveAt(idx);
        }


        /// <summary>
        /// 清理
        /// </summary>
        new public void Clear()
        {
            IsChanged = true;
            this.removeDataList.AddRange(this.BaseList);
            this.BaseList.Clear();
        }

        #endregion


        #region 差异数据

        private List<T> changedDataList = new List<T>();
        private List<T> newDataList = new List<T>();
        private List<T> removeDataList = new List<T>();

        /// <summary>
        /// 获取新增列表
        /// </summary>
        /// <returns></returns>
        public APropsBase[] GetNewDatas()
        {
            var ret = this.newDataList.ToArray();
            this.newDataList.Clear();

            if (this.newDataList.Count == 0 && removeDataList.Count == 0 && changedDataList.Count == 0)
            {
                IsChanged = false;
            }

            return ret;
        }

        /// <summary>
        /// 获取移除的元素
        /// </summary>
        /// <returns></returns>
        public APropsBase[] GetRemovedDatas()
        {
            var ret = this.removeDataList.ToArray();
            this.removeDataList.Clear();

            if (this.newDataList.Count == 0 && removeDataList.Count == 0 && changedDataList.Count == 0)
            {
                IsChanged = false;
            }

            return ret;
        }

        /// <summary>
        /// 获取改变的元素
        /// </summary>
        /// <returns></returns>
        public APropsBase[] GetChangedDatas()
        {
            var ret = this.changedDataList.ToArray();
            this.changedDataList.Clear();

            if (this.newDataList.Count == 0 && removeDataList.Count == 0 && changedDataList.Count == 0)
            {
                IsChanged = false;
            }

            return ret;
        }

        /// <summary>
        /// 清理改变的数据
        /// </summary>
        public void ClearChangedData()
        {
            this.IsChanged = false;
            this.newDataList.Clear();
            this.removeDataList.Clear();
            this.changedDataList.Clear();
        }

        #endregion
    }
}