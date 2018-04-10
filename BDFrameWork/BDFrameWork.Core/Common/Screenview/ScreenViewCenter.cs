using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace BDFramework.ScreenView
{
    /// <summary>
    /// 导航管理器
    /// </summary>
    public class ScreenViewCenter
    {
        /// <summary>
        /// 首先把ScreenView分层，各层相互独立，每层玩自己的导航
        /// </summary>
        List<ScreenViewLayer> layers = new List<ScreenViewLayer>();

        /// <summary>
        /// 通过id获取导航层
        /// </summary>
        /// <param name="layerid">导航层id</param>
        /// <returns></returns>
        public ScreenViewLayer GetLayer(int layerid)
        {
            return layers[layerid];
        }

        /// <summary>
        /// 添加导航层
        /// </summary>
        public void AddLayer()
        {

            layers.Add(new ScreenViewLayer(layers.Count));
        }

        /// <summary>
        /// 获取导航层数量
        /// </summary>
        /// <returns></returns>
        public int GetLayerCount()
        {
            return layers.Count;
        }


        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="delta">deltaTime</param>
        public void Update(float delta)
        {
            //遍历导航层,调用每个导航层的Update
            for (int i = 0; i < layers.Count; i++)
            {
                layers[i].Update(delta);
            }
        }

        /// <summary>
        /// 固定更新
        /// </summary>
        /// <param name="delta"></param>
        public void FixedUpdate(float delta)
        {
            //遍历导航层,调用每个导航层的FixedUpdate
            for (int i = 0; i < layers.Count; i++)
            {
                layers[i].FixedUpdate(delta);
            }
        }

    }
    /// <summary>
    /// 导航层，一个导航层管理若干ScreenView,ScreenView之间可以多个同时出现，是一种层叠的关系
    /// </summary>
    public class ScreenViewLayer
    {
        /// <summary>
        /// 导航层构造函数
        /// </summary>
        /// <param name="layerid">导航层id</param>
        public ScreenViewLayer(int layerid)
        {
            this.layerid = layerid;
        }

        /// <summary>
        /// 导航层id
        /// </summary>
        public int layerid
        {
            get;
            private set;
        }


        /// <summary>
        /// 通过名称获取IScreenView
        /// </summary>
        /// <param name="svName">IScreenView名称</param>
        /// <returns></returns>
        public IScreenView GetScreenView(string svName)
        {
            IScreenView sv = null;
            //遍历显示中的IScreenView列表,找到直接跳出
            foreach (var _sv in views)
            {
                if (_sv.Name == svName)
                {
                    sv = _sv;
                    break;
                }
            }
            //如果没有找到
            if (sv == null)
            {
                //遍历未显示的IScreenView列表,找到直接跳出
                foreach (var _sv in unuseViews)
                {
                    if (_sv.Key == svName)
                    {
                        sv = _sv.Value;
                        break;
                    }
                }
            }
            return sv;
        }


        /// <summary>
        /// 增加一个Screen，Screen 虽然立即创建，Screen应设计为不执行BeginLoad不加载任何内容，完成后由回调通知
        /// <para>注册IScreenView,默认添加到未使用的IScreenView列表</para>
        /// </summary>
        /// <param name="creator"></param>
        public void RegScreen(IScreenView view)
        {
            unuseViews.Add(view.Name, view);
        }


        string curName;
        /// <summary>
        /// 灵魂功能，导航到一个指定名称的ScreenView，可能是向前，也可能是向后
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="onLoad"></param>
        public void BeginNavTo(object name, Action<Exception> onLoad = null)
        {
            //获取IScreenView名称
            string _name = name.ToString();
            curName = _name;
            // My JDeBug.Inst.Log("name:" + name);
            IScreenView view = null;
            //如果在未使用的IScreenView列表中找到
            if (unuseViews.TryGetValue(_name, out view))
            {
                //My JDeBug.Inst.Log("unuseViews: " + name);

                BeginNavForward(_name, onLoad);
                return;
            }
            else
            {
                //My JDeBug.Inst.Log("using : " + name);
                //有可能在队列中
                bool bNeedBack = false;
                int navtoindex = -1;
                int navtobeginindex = -1;
                for (int i = views.Count - 1; i >= 0; i--)
                {
                    if (views[i].Name == _name)
                    {
                        if (i == views.Count - 1)
                        {
                            //别闹，就在顶上，Nav个毛线
                            if (onLoad != null)

                                onLoad(new Exception("别闹，就在顶上，Nav个毛线"));
                            return;
                        }
                        else
                        {
                            //在队列中,NavBack直到达成目标
                            //My JDeBug.Inst.Log("在队列中,NavBack直到达成目标");
                            bNeedBack = true;
                            navtobeginindex = navtoindex = i;
                            break;
                        }
                    }
                }
                while (bNeedBack && views[navtobeginindex].IsTransparent && navtobeginindex >= 0)
                {
                    navtobeginindex--;
                }

                //
                if (bNeedBack)
                {
                    List<IScreenView> viewforexit = new List<IScreenView>();
                    List<IScreenView> viewforinit = new List<IScreenView>();
                    int exitcount = 0;
                    int initcount = 0;
                    Action<Exception> onnavinit = (err) =>
                    {
                        //Debug.LogWarning("initone:" + initcount);
                        if (err != null)
                        {
                            if (onLoad != null)

                                onLoad(err);
                            return;
                        }
                        initcount--;
                        if (initcount == 0)
                        {
                            if (onLoad != null)

                                onLoad(err);
                        }
                    };

                    Action<Exception> doinit = (err) =>
                    {
                        if (viewforinit.Count == 0)
                            if (onLoad != null)

                                onLoad(null);
                        foreach (var v in viewforinit)
                        {
                            v.BeginInit(onnavinit, this);
                        }
                    };
                    Action<Exception> onnav = (err) =>
                    {
                        //Debug.LogWarning("exitone:" + exitcount);
                        if (err != null)
                        {
                            if (onLoad != null)
                                onLoad(err);
                            return;
                        }
                        exitcount--;
                        if (exitcount == 0)
                        {
                            doinit(err);
                        }
                    };
                    //Debug.LogWarning("from" + (views.Count - 1).ToString() + "to===" + navtoindex);
                    for (int i = views.Count - 1; i > navtoindex; i--)
                    {
                        //Debug.LogWarning("begin exitone==" + views[i].name);

                        if (views[i].IsLoad)
                        {
                            //Debug.LogWarning("begin exitone:" + views[i].name);
                            viewforexit.Add(views[i]);
                        }
                        this.unuseViews.Add(views[i].Name, views[i]);
                        views.RemoveAt(i);

                    }
                    for (int i = navtobeginindex; i <= navtoindex; i++)
                    {
                        if (!views[i].IsLoad)
                        {
                            viewforinit.Add(views[i]);
                        }
                    }
                    initcount = viewforinit.Count;
                    exitcount = viewforexit.Count;
                    //Debug.LogWarning("exitcount:" + exitcount);

                    foreach (var e in viewforexit)
                    {
                        e.BeginExit(onnav);
                    }
                    //Debug.LogWarning("need navto begin in:" + views[navtobeginindex].name + "-" + views[navtoindex].name);
                    //Action<Exception> onnav = null;
                    //onnav = (err) =>
                    //{
                    //    if (err != null)
                    //    {
                    //        onLoad(err);
                    //    }
                    //    var vlast = views[views.Count - 1];
                    //    My JDeBug.Inst.Log("vlast:" + vlast + ",now:" + name);
                    //    if (vlast.name == name)
                    //    {
                    //        if (onLoad != null)
                    //            onLoad(null);
                    //    }
                    //    else
                    //    {
                    //        BeginNavBack(onnav);
                    //    }
                    //};
                    //BeginNavBack(onnav);

                    return;
                }
                else
                {
                    if (onLoad == null)
                    {
                        //Debug.LogError("onload is null");

                    }
                    else
                        onLoad(new Exception("name: " + _name + "view 不存在."));
                }
                return;
            }

        }

        /// <summary>
        /// ScreenViewLayer 向前导航(从未使用的IScreenView列表中查找,然后重新添加到显示中的IScreenView列表中)
        /// </summary>
        /// <param name="name">IScreenView名称</param>
        /// <param name="onLoad">异常回调</param>
        public void BeginNavForward(object name, Action<Exception> onLoad = null)
        {
            //获取IScreenView名称
            string _name = name.ToString();
            IScreenView view = null;
            //如果在未使用的IScreenView列表中找到这个IScreenView
            if (unuseViews.TryGetValue(_name, out view))
            {
                //如果isLoad = true
                //if (view.isLoad)
                //{
                //    //如果有异常回调函数,抛出异常
                //    if (onLoad != null)
                //        onLoad(new Exception("一个不在使用中的view 却显示并加载，这 很异常"));
                //    return;
                //}
                //从未使用的IScreenView列表中移除这个IScreenView
                unuseViews.Remove(_name);
                //创建一个新的NavTask,入队
                //实际就是重新将这个IScreenView添加到显示的IScreenView列表中
                tasks.Enqueue(new NavTask(NavTaskType.InitAndAdd, view, onLoad, this));
            }
            //如果没有找到
            else
            {
                //如果有异常回调函数,抛出异常
                if (onLoad != null)

                    onLoad(new Exception("找不到这个view"));
            }
        }

        /// <summary>
        /// ScreenViewLayer 向后导航
        /// </summary>
        /// <param name="onLoad">异常回调</param>
        public void BeginNavBack(Action<Exception> onLoad)
        {
            //如果显示中的IScreenView列表数量等于0,抛出异常
            if (views.Count == 0)
            {
                onLoad(new Exception("views没有视图，无法NavBack"));
                return;
            }
            //显示中的IScreenView的最后一个IScreenView索引,暂未使用
            int k = views.Count - 1;
            //tasks.Enqueue(new NavTask(NavTaskType.ExitAndRemove, views[k], onLoad, this));

            //显示中的IScreenView的倒数第二个IScreenView索引
            int navtoindex = views.Count - 2;

            //要退出的IScreenView列表
            List<IScreenView> viewforexit = new List<IScreenView>();
            //要初始化的IScreenView列表
            List<IScreenView> viewforinit = new List<IScreenView>();
            //要退出的IScreenView列表数量
            int exitcount = 0;
            //要初始化的IScreenView列表数量
            int initcount = 0;


            Action<Exception> onnavinit = (err) =>
            {
                //Debug.LogWarning("initone:" + initcount);
                //如果有异常回调函数,引发异常
                if (err != null)
                {
                    if (onLoad != null)

                        onLoad(err);
                    return;
                }
                //IScreenView初始化计数器递减
                initcount--;
                if (initcount == 0)
                {
                    if (onLoad != null)

                        onLoad(err);
                }
            };

            Action<Exception> doinit = (err) =>
            {
                //如果要初始化的IScreenView数量为0
                if (viewforinit.Count == 0)
                    //如果有异常回调函数,引发异常
                    if (onLoad != null)

                        onLoad(null);
                //遍历要初始化的IScreenView
                foreach (var v in viewforinit)
                {
                    //初始化IScreenView
                    v.BeginInit(onnavinit, this);
                }
            };

            //
            Action<Exception> onnav = (err) =>
            {
                //Debug.LogWarning("exitone:" + exitcount);
                //如果回调函数不为空,引发异常,返回
                if (err != null)
                {
                    if (onLoad != null)
                        onLoad(err);
                    return;
                }
                //IScreenView退出计数器递减
                exitcount--;
                //如果IScreenView退出计数器==0
                if (exitcount == 0)
                {
                    doinit(err);
                }
            };
            //Debug.LogWarning("from" + (views.Count - 1).ToString() + "to===" + navtoindex);

            //从显示中的IScreenView列表中的最后一个开始向前遍历
            //虽然是遍历,但范围太小.就是从倒数第一个到倒数第二个,实际只操作倒数第一个
            for (int i = views.Count - 1; i > navtoindex; i--)
            {
                //Debug.LogWarning("begin exitone==" + views[i].name);
                //如果这个IScreenView isLoad
                if (views[i].IsLoad)
                {
                    //Debug.LogWarning("begin exitone:" + views[i].name);
                    //将这个IScreenView添加到要退出的IScreenView列表
                    viewforexit.Add(views[i]);
                }
                //将这个IScreenView添加到未使用的IScreenView列表中
                this.unuseViews.Add(views[i].Name, views[i]);
                //将这个IScreenView从显示中的IScreenView列表中删除
                views.RemoveAt(i);

            }

            //如果navtoindex >= 0 说明显示中的IScreenView列表中有至少两个IScreenView
            if (navtoindex >= 0)

                viewforinit.Add(views[navtoindex]);
            //for (int i = navtobeginindex; i <= navtoindex; i++)
            //{
            //    if (!views[i].isLoad)
            //    {

            //    }
            //}
            initcount = viewforinit.Count;
            exitcount = viewforexit.Count;
            //Debug.LogWarning("exitcount:" + exitcount);

            //遍历要退出的IScreenView列表
            foreach (var e in viewforexit)
            {
                //退出
                e.BeginExit(onnav);
            }
            //Debug.LogWarning("need navto begin in:" + views[navtobeginindex].name + "-" + views[navtoindex].name);


            return;
        }

        /// <summary>
        /// ScreenViewLayer关联的导航任务队列
        /// </summary>
        Queue<NavTask> tasks = new Queue<NavTask>();
        /// <summary>
        /// 当前导航任务
        /// </summary>
        NavTask taskCurrect = null;

        /// <summary>
        /// 导航任务枚举
        /// </summary>
        enum NavTaskType
        {
            /// <summary>
            /// Init 一个ScreenView，并将它添加到View列表中
            /// </summary>
            InitAndAdd,
            /// <summary>
            /// 仅Init
            /// </summary>
            Init,
            /// <summary>
            /// 仅Exit
            /// </summary>        
            Exit,
            /// <summary>
            /// Exit 一个ScreenView，并将它从View列表中移除
            /// </summary>
            ExitAndRemove,
            /// <summary>
            /// 销毁
            /// </summary>
            Destroy,
        }

        /// <summary>
        /// 导航任务
        /// </summary>
        class NavTask
        {
            /// <summary>
            /// 导航任务关联的ScreenViewLayer
            /// </summary>
            ScreenViewLayer layer;

            //委托的变量是函数指针，委托Action<Exception>里面的Exception就是函数的参数

            /// <summary>
            /// 导航任务构造函数
            /// </summary>
            /// <param name="type">导航任务类型枚举</param>
            /// <param name="view">导航任务关联的IScreenView</param>
            /// <param name="_callback">导航任务关联的回调函数</param>
            /// <param name="layer">导航任务关联的ScreenViewLayer</param>
            public NavTask(NavTaskType type, IScreenView view, Action<Exception> _callback, ScreenViewLayer layer)
            {
                this.type = type;
                this.view = view;
                this.callback = _callback;
                this.layer = layer;
            }

            /// <summary>
            /// 回调任务
            /// </summary>
            /// <param name="err">异常</param>
            void CallBackTask(Exception err)
            {
                if (callback != null)
                {
                    callback(err);
                }
                //这个是set
                done = true;
            }

            /// <summary>
            /// 任务开始
            /// </summary>
            public void Begin()
            {
                //任务类型为InitAndAdd
                if (type == NavTaskType.InitAndAdd)
                {
                    //将关联的IScreenView添加到关联的ScreenViewLayer中的显示的IScreenView列表
                    layer.views.Add(view);
                }
                //任务类型为ExitAndRemove
                else if (type == NavTaskType.ExitAndRemove)
                {
                    //从关联的ScreenViewLayer中的显示的IScreenView列表中移除关联的IScreenView
                    layer.views.Remove(view);
                    //在关联的ScreenViewLayer中的未使用的IScreenView列表中添加这个IScreenView
                    layer.unuseViews.Add(view.Name, view);
                }

                //任务计数器
                int taskq = 0;
                //是否等待,默认true
                bool bwait = true;

                Action<Exception> _callback = (_err) =>
                {

                    taskq--;
                    //                        My JDeBug.Inst.Log("taskq=" + taskq);
                    if (taskq == 0 && !bwait)
                    {
                        CallBackTask(_err);
                    }
                };

                //类型为 Init || InitAndAdd
                if (type == NavTaskType.Init || type == NavTaskType.InitAndAdd)
                {
                    taskq++;
                    //                  My JDeBug.Inst.Log("init taskq=" + taskq);
                    //初始化相关联的IScreenView
                    view.BeginInit(_callback, layer);
                    //任务完成
                    this.done = true;
                }
                //类型为 Exit || ExitAndRemove
                else if (type == NavTaskType.Exit || type == NavTaskType.ExitAndRemove)
                {
                    taskq++;
                    //                     Debug.LogWarning("exit taskq=" + taskq);
                    //如果相关联的IScreenView isLoad
                    if (view.IsLoad)
                        //退出相关联的IScreenView
                        view.BeginExit(_callback);
                    //任务完成
                    this.done = true;
                }
                //其余情况,目前只有Destroy
                else
                {
                    //如果IScreenView isLoad
                    if (view.IsLoad)
                    {
                        //调用IScreenView Destroy
                        view.Destory();
                        callback(null);
                        return;
                    }
                }

                //如果类型为 InitAndAdd
                if (type == NavTaskType.InitAndAdd)//继续处理
                {
                    //如果相关联的IScreenView 不是透明的
                    if (view.IsTransparent == false)
                    {
                        //                        My JDeBug.Inst.Log("Trans and Hide");

                        for (int i = layer.views.Count - 2; i >= 0; i--)
                        {
                            var v = layer.views[i];
                            if (v.IsLoad)
                            {

                                taskq++;
                                //                            My JDeBug.Inst.Log("exitaa taskq=" + taskq);
                                layer.tasks.Enqueue(new NavTask(NavTaskType.Exit, v, _callback, layer));
                            }
                        }
                    }

                }
                //如果类型为 ExitAndRemove
                else if (type == NavTaskType.ExitAndRemove)
                {
                    //如果相关联的IScreenView 不是透明的
                    if (view.IsTransparent == false)
                    {
                        // My JDeBug.Inst.Log("Trans and Show");
                        //从相关联的ScreenViewLayer中的最后一个显示中的IScreenView开始倒序遍历
                        for (int i = layer.views.Count - 1; i >= 0; i--)
                        {
                            var v = layer.views[i];
                            Debug.LogWarning("inlist:" + v + "," + v.IsLoad);

                            if (!v.IsLoad)
                            {
                                taskq++;
                                // My JDeBug.Inst.Log("initaa taskq=" + taskq);

                                layer.tasks.Enqueue(new NavTask(NavTaskType.Init, v, _callback, layer));
                            }
                            //如果IScreenView不透明,直接跳出循环
                            if (v.IsTransparent == false)
                            {
                                break;
                            }
                        }
                    }
                }
                //                My JDeBug.Inst.Log("finish taskq=" + taskq);

                if (taskq == 0 && bwait)
                {

                    CallBackTask(null);

                }
                //此任务不用等待
                bwait = false;
            }
            /// <summary>
            /// 导航任务关联的导航任务类型枚举
            /// </summary>
            NavTaskType type;

            /// <summary>
            /// 导航任务关联的IScreenView
            /// </summary>
            public IScreenView view;

            /// <summary>
            /// 任务是否完成
            /// </summary>
            public bool done
            {
                get;
                private set;
            }

            /// <summary>
            /// 异常回调
            /// </summary>
            Action<Exception> callback;
        }

        /// <summary>
        /// 未使用的(IScreenView名称,IScreenView)字典
        /// </summary>
        Dictionary<string, IScreenView> unuseViews = new Dictionary<string, IScreenView>();

        /// <summary>
        /// 显示中的IScreenView列表
        /// </summary>
        List<IScreenView> views = new List<IScreenView>();//显示栈，当前在显示栈中的界面

        /// <summary>
        /// 获取显示的IScreenView列表中的最后一个
        /// </summary>
        /// <returns></returns>
        public IScreenView Peek()
        {
            if (views.Count == 0) return null;
            return views[views.Count - 1];
        }

        /// <summary>
        /// 导航层Update
        /// </summary>
        /// <param name="delta">deltaTime</param>
        public void Update(float delta)
        {
            //倒序遍历
            for (int i = views.Count - 1; i >= 0; i--)
            {
                var v = views[i];


                //当前的帧循环

                //float start = Time.realtimeSinceStartup;
                v.Update(delta);
                //float end = Time.realtimeSinceStartup;

                //My JDeBug.Inst.Log( "[" + v.name + "] - " + "View Update execution time:" + (end - start));

                //如果IScreenView不透明,直接break,不处理后面的IScreenView
                if (!v.IsTransparent)
                {
                    break;
                }
                //如果isLoad = false,直接break,不处理后面的IScreenView
                if (!v.IsLoad)
                {
                    break;
                }
            }

            //遍历导航列表
            foreach (var t in tasks)
            {
                //更新任务
                // float start = Time.time;
                t.view.UpdateTask(delta);
                // float end = Time.time;

                //Debug.LogError("UpdateTask execution time:" + (end - start));
            }
            //如果当前任务为空且任务队列数量大于0
            if (taskCurrect == null && tasks.Count > 0)
            {
                // float start = Time.time;
                //出队
                taskCurrect = tasks.Dequeue();
                //执行任务
                taskCurrect.Begin();
                // float end = Time.time;

                //Debug.LogError("task execution time:" + (end - start));
            }
            //如果当前任务不为空且当前任务已完成,设置当前任务为空
            if (taskCurrect != null && taskCurrect.done)
            {
                //将当前任务置空
                taskCurrect = null;
                //                My JDeBug.Inst.Log("TaskFinish");
            }

        }
        /// <summary>
        /// 更高频的帧循环
        /// <para>固定更新</para>
        /// </summary>
        /// <param name="delta">deltaTime</param>
        public void FixedUpdate(float delta)
        {
            //倒序遍历
            for (int i = views.Count - 1; i >= 0; i--)
            {
                var v = views[i];
                //当前的帧循环
                v.FixedUpdate(delta);
                //如果不透明,直接跳出循环
                if (!v.IsTransparent)
                {
                    break;
                }
                //如果isLoad=false,直接跳出循环
                if (!v.IsLoad)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 向IScreenView发送消息
        /// </summary>
        /// <param name="svName">IScreenView名称</param>
        /// <param name="msg">消息</param>
        /// <param name="_params">参数</param>
        /// <param name="callback">回调函数(暂未使用)</param>
        public void SendMassage(string svName, int msg, object _params = null, Action callback = null)
        {
            //获取IScreenView
            var sv = GetScreenView(svName);
            if (sv == null)
            {
                Debug.LogError("找不到view - name:" + svName);
                return;
            }
            //如果IScreenView实现了IReceiver
            if (sv is IReceiver)
            {
                //调用IScreenView中的ReceiveMassage方法
                (sv as IReceiver).ReceiveMessage(msg, _params, callback);
            }
            else
            {
                Debug.LogError("接收对象并没实现IReceiver接口!");
            }

        }

        public ScreenViewEnum GetCurName()
        {
            return (ScreenViewEnum)Enum.Parse(typeof(ScreenViewEnum), curName);
        }
    }

}


