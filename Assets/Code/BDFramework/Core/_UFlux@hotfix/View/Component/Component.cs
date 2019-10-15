using System;
using BDFramework.UFlux.Reducer;
using BDFramework.UFlux.Store;
using BDFramework.UFlux.View.Props;
using UnityEngine;

namespace BDFramework.UFlux
{
    public class Component<T> : IUFluxComponent where T : PropsBase, new()
    {
        
        /// <summary>
        /// 当前组件所有状态集合
        /// </summary>
        protected T Props { get; private set; }
        
        /// <summary>
        /// 资源节点
        /// </summary>
        public Transform Transform { get; private set; }
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
        public Component()
        {
            var t = this.GetType();
            var attrs = t.GetCustomAttributes(typeof(ComponentAttribute), false);
            if (attrs.Length == 0) return;
            var attr = attrs[0] as ComponentAttribute;
            this.resPath = attr.Path;
            //创建State
            this.Props = new T();
            //自动加载
            if (!attr.IsAsyncLoad)
            {
                this.Load();
            }
            else
            {
                this.AsyncLoad();
            }
            
        }
        
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="trans"></param>
        public Component(Transform trans)
        {
            this.Transform = trans;
            //创建State
            this.Props = new T();
        }
        /// <summary>
        /// 这里重载一个构造函数
        /// </summary>
        /// <param name="resPath"></param>
        public Component(string resPath)
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

            var o = UFlux.Load<GameObject>(resPath);
            this.Transform = GameObject.Instantiate(o).transform;
            this.IsLoad = true;
            UFlux.SetTransformPath(this);
            //初始化
            this.Init();
        }


        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="callback"></param>
        public void AsyncLoad(Action callback=null)
        {
            if (resPath == null) return;
            UFlux.AsyncLoad<GameObject>(resPath, obj =>
            {
                this.Transform = GameObject.Instantiate(obj).transform;
                this.IsLoad = true;
                UFlux.SetTransformPath(this);
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
            if (props.IsChanged())
            {
                this.Props = props;
                UFlux.SetComponentValue(this.Transform, props);
            }
          
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="propsBase"></param>
        public void SetProps(PropsBase propsBase)
        {
            var t = propsBase as T;
            if (t == null)
            {
                BDebug.LogError("类型转换失败:" +propsBase.GetType().Name);
            }
            else
            {
                this.SetProps(t);
            }
        }

        /// <summary>
        /// 提交状态
        /// </summary>
        protected void SetProps()
        {
            if (this.Props.IsChanged())
            {
                UFlux.SetComponentValue(this.Transform, this.Props);
            }
        }
        #endregion
        
        #region 生命周期
        /// <summary>
        /// 打开
        /// </summary>
        virtual public void Open()
        {
            this.IsOpen = true;
            this.Transform.gameObject.SetActive(true);
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
        /// 初始化
        /// </summary>
        virtual public void Init()
        {
            //初始化所有成员变量
           
        }

        /// <summary>
        /// 删除
        /// </summary>
        virtual public void Destroy()
        {
            UFlux.Destroy(this.Transform.gameObject);
            this.Transform = null;
            UFlux.Unload(this.resPath);
            IsDestroy = true;
        }

        #endregion
    }
}