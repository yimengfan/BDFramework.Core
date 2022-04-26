using System;
using BDFramework.DataListener;
using BDFramework.Hotfix.Reflection;
using BDFramework.UFlux.View.Props;
using UnityEngine;

namespace BDFramework.UFlux
{
    abstract public class ATComponent<T> : IComponent where T : APropsBase, new()
    {
        /// <summary>
        /// 当前组件所有状态集合
        /// </summary>
        public T Props { get; private set; }

        /// <summary>
        /// 资源节点
        /// </summary>
        public Transform Transform { get; private set; }

        /// <summary>
        /// 状态管理
        /// </summary>
        public AStatusListener State { get; }

        /// <summary>
        /// 是否加载
        /// </summary>
        public bool IsLoad { get; private set; } = false;

        /// <summary>
        /// 是否打开
        /// </summary>
        public bool IsOpen { get; private set; } = false;

        /// <summary>
        /// 是否被删除
        /// </summary>
        public bool IsDestroy { get; private set; } = false;

        #region 构造相关

        /// <summary>
        /// 构造函数,n一旦new 会自动创建相关的渲染组件
        /// </summary>
        public ATComponent()
        {
            var t = this.GetType();
            var attr = t.GetAttributeInILRuntime<ComponentAttribute>();
            if (attr == null)
            {
                return;
            }

            this.resPath = attr.Path;
            //创建State
            this.Props = new T();
            //自动加载
            if (!attr.IsAsyncLoad)
            {
                this.Load();
            }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="trans"></param>
        public ATComponent(Transform trans)
        {
            this.Transform = trans;
            //创建State
            this.Props = new T();
            UFluxUtils.InitComponent(this);
        }

        /// <summary>
        /// 这里重载一个构造函数
        /// </summary>
        /// <param name="resPath"></param>
        public ATComponent(string resPath)
        {
            this.resPath = resPath;
            //创建State
            this.Props = new T();
        }


        private string resPath = null;

        /// <summary>
        /// 加载接口
        /// </summary>
        public void Load()
        {
            if (resPath == null) return;

            var o = UFluxUtils.Load<GameObject>(resPath);
            this.Transform = GameObject.Instantiate(o).transform;
            this.IsLoad = true;
            UFluxUtils.InitComponent(this);
            //初始化
            this.Init();
        }


        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="callback"></param>
        public void AsyncLoad(Action callback = null)
        {
            if (resPath == null) return;
            UFluxUtils.AsyncLoad<GameObject>(resPath, obj =>
            {
                this.Transform = GameObject.Instantiate(obj).transform;
                this.IsLoad = true;
                UFluxUtils.InitComponent(this);
                //初始化
                Init();
                if (callback != null)
                {
                    callback();
                }
            });
        }

        #endregion


        #region 状态处理

        /// <summary>
        /// 设置数据，全局只能通过这个接口设置数据
        /// </summary>
        /// <param name="props"></param>
        public void SetProps(T props)
        {
            this.Props = props;
            this.CommitProps();
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="propsBase"></param>
        public void SetProps(APropsBase propsBase)
        {
            var t = propsBase as T;
            if (t == null)
            {
                BDebug.LogError("类型转换失败:" + propsBase.GetType().Name);
            }
            else
            {
                this.SetProps(t);
            }
        }

        /// <summary>
        ///  提交状态 刷新
        /// </summary>
        /// <param name="transform">不为null，则指定一个Transform刷新.不然则刷新当前Window.Transform</param>
        protected void CommitProps(Transform transform = null)
        {
            if (transform)
            {
                UFluxUtils.SetComponentProps(transform, this.Props);
            }
            else
            {
                UFluxUtils.SetComponentProps(this.Transform, this.Props);
            }
        }

        #endregion


        #region 生命周期

        /// <summary>
        /// 初始化
        /// </summary>
        virtual public void Init()
        {
            //初始化所有成员变量
        }

        /// <summary>
        /// 打开
        /// </summary>
        virtual public void Open(UIMsgData uiMsg = null)
        {
            this.IsOpen = true;
            this.Transform.gameObject.SetActive(true);
        }

        /// <summary>
        /// 获得焦点
        /// </summary>
        virtual public void OnFocus()
        {
            this.Open();
        }

        /// <summary>
        ///  关闭
        /// </summary>
        virtual public void Close()
        {
            this.IsOpen = false;
            this.Transform.gameObject.SetActive(false);
        }


        /// <summary>
        /// 删除
        /// </summary>
        virtual public void Destroy()
        {
            UFluxUtils.Destroy(this.Transform.gameObject);
            this.Transform = null;
            UFluxUtils.Unload(this.resPath);
            IsDestroy = true;
        }

        #endregion
    }
}
