using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace BDFramwork.ScreenView
{

    /// <summary>
    /// 导航管理器
    /// 在App开发中，导航与View的概念逐渐深入人心，于是这里要将这个部分再整理的更清楚，形成一个更易于复用的模式
    /// </summary>
    public class ScreenViewCenter
    {
        /// <summary>
        /// 首先把ScreenView分层，各层相互独立，每层玩自己的导航
        /// </summary>
        List<ScreenViewLayer> layers = new List<ScreenViewLayer>();

        public ScreenViewLayer GetLayer(int layerid)
        {
            return layers[layerid];
        }
        public void AddLayer()
        {
            layers.Add(new ScreenViewLayer(layers.Count));
        }
        public int GetLayerCount()
        {
            return layers.Count;
        }
        /// <summary>
        /// 刷新ScreenView
        /// </summary>
        /// <param name="delta"></param>
        public void Update(float delta)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                layers[i].Update(delta);
            }
        }



    }
    /// <summary>
    /// 导航层，一个导航层管理若干ScreenView,ScreenView之间可以多个同时出现，是一种层叠的关系
    /// </summary>
    public class ScreenViewLayer
    {
        public ScreenViewLayer(int layerid)
        {
            this.layerid = layerid;
        }
        public int layerid
        {
            get;
            private set;
        }
        /// <summary>
        /// 获取一个sv
        /// </summary>
        /// <param name="svName"></param>
        /// <returns></returns>
        public IScreenView GetScreenView(string svName)
        {
            IScreenView sv = null;
            //
            foreach (var _sv in views)
            {
                if(_sv.name == svName)
                {
                    sv = _sv;
                    break;
                }
            }
            //
            if (sv == null)
            {
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
        /// </summary>
        /// <param name="creator"></param>
        public void RegScreen(IScreenView view)
        {
            unuseViews.Add(view.name, view);
        }



        /// <summary>
        /// 灵魂功能，导航到一个指定名称的ScreenView，可能是向前，也可能是向后
        /// </summary>
        /// <param name="name"></param>
        /// <param name="onLoad"></param>
        public void BeginNavTo(string name, Action<Exception> onLoad =null)
        {
//            MyDebug.Log("name:" + name);
               
            IScreenView view = null;
            if (unuseViews.TryGetValue(name, out view))
            {
//                MyDebug.Log("unuseViews: " + name);
                BeginNavForward(name, onLoad);
                return;
            }
            else
            {
                //MyDebug.Log("using : " + name);
                //有可能在队列中
                bool bNeedBack = false;
                int navtoindex = -1;
                int navtobeginindex = -1;
                for (int i = views.Count - 1; i >= 0; i--)
                {
                    if (views[i].name == name)
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
                            //MyDebug.Log("在队列中,NavBack直到达成目标");
                            bNeedBack = true;
                            navtobeginindex = navtoindex = i;
                            break;
                        }
                    }
                }
                while (bNeedBack && views[navtobeginindex].isTransparent && navtobeginindex >= 0)
                {
                    navtobeginindex--;
                }
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

                        if (views[i].isLoad)
                        {
                            //Debug.LogWarning("begin exitone:" + views[i].name);
                            viewforexit.Add(views[i]);
                        }
                        this.unuseViews.Add(views[i].name, views[i]);
                        views.RemoveAt(i);

                    }
                    for (int i = navtobeginindex; i <= navtoindex; i++)
                    {
                        if (!views[i].isLoad)
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
                    //    MyDebug.Log("vlast:" + vlast + ",now:" + name);
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
                        onLoad(new Exception("name: " + name + "view 不存在."));
                }
                return;
            }

        }

        public void BeginNavForward(string name, Action<Exception> onLoad =null)
        {
            IScreenView view = null;
            if (unuseViews.TryGetValue(name, out view))
            {
                if (view.isLoad)
                {
                    if (onLoad != null)

                        onLoad(new Exception("一个不在使用中的view 却显示并加载，这 很异常"));
                    return;
                }
                unuseViews.Remove(name);
                //将对象添加到队列的末尾处
                tasks.Enqueue(new NavTask(NavTaskType.InitAndAdd, view, onLoad, this));
            }
            else
            {
                if (onLoad != null)

                    onLoad(new Exception("找不到这个view"));
            }
        }
        public void BeginNavBack(Action<Exception> onLoad)
        {
            if (views.Count == 0)
            {
                onLoad(new Exception("views没有视图，无法NavBack"));
                return;
            }
            int k = views.Count - 1;
            //tasks.Enqueue(new NavTask(NavTaskType.ExitAndRemove, views[k], onLoad, this));
            int navtoindex = views.Count - 2;

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

                if (views[i].isLoad)
                {
                    //Debug.LogWarning("begin exitone:" + views[i].name);
                    viewforexit.Add(views[i]);
                }
                this.unuseViews.Add(views[i].name, views[i]);
                views.RemoveAt(i);

            }
            if(navtoindex>=0)
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

            foreach (var e in viewforexit)
            {
                e.BeginExit(onnav);
            }
            //Debug.LogWarning("need navto begin in:" + views[navtobeginindex].name + "-" + views[navtoindex].name);


            return;
        }


        Queue<NavTask> tasks = new Queue<NavTask>();
        NavTask taskCurrect = null;
        enum NavTaskType
        {
            InitAndAdd,     //Init 一个ScreenView，并将它添加到View列表中
            Init,           //仅Init
            Exit,           //仅Exit
            ExitAndRemove,  //Exit 一个ScreenView，并将它从View列表中移除
            Destroy,        //销毁
        }
        class NavTask
        {
            ScreenViewLayer layer;
            //委托的变量是函数指针，委托Action<Exception>里面的Exception就是函数的参数
            public NavTask(NavTaskType type, IScreenView view, Action<Exception> _callback, ScreenViewLayer layer)
            {
                this.type = type;
                this.view = view;
                this.callback = _callback;
                this.layer = layer;
            }
            void CallBackTask(Exception err)
            {
                if (callback != null)
                {
                    callback(err);
                }
                //这个是set
                done = true;
            }
            public void Begin()
            {
                if (type == NavTaskType.InitAndAdd)
                {
                    layer.views.Add(view);
                }
                else if (type == NavTaskType.ExitAndRemove)
                {
                    layer.views.Remove(view);
                    layer.unuseViews.Add(view.name, view);
                }
                int taskq = 0;
                bool bwait = true;
                Action<Exception> _callback = (_err) =>
                    {
                        taskq--;
//                        MyDebug.Log("taskq=" + taskq);
                        if (taskq == 0 && !bwait)
                        {
                            CallBackTask(_err);
                        }
                    };

                if (type == NavTaskType.Init || type == NavTaskType.InitAndAdd)
                {
                    taskq++;
  //                  MyDebug.Log("init taskq=" + taskq);
                    view.BeginInit(_callback, layer);
                    this.done = true;
                }
                else if (type == NavTaskType.Exit || type == NavTaskType.ExitAndRemove)
                {
                    taskq++;
//                     Debug.LogWarning("exit taskq=" + taskq);
                   if(view.isLoad)
                    view.BeginExit(_callback);
                    this.done = true;
                }
                else
                {
                    if (view.isLoad)
                    {
                        view.Destory();
                        callback(null);
                        return;
                    }
                }

                if (type == NavTaskType.InitAndAdd)//继续处理
                {
                    if (view.isTransparent == false)
                    {
//                        MyDebug.Log("Trans and Hide");

                        for (int i = layer.views.Count - 2; i >= 0; i--)
                        {
                            var v = layer.views[i];
                            if (v.isLoad)
                            {

                                taskq++;
    //                            MyDebug.Log("exitaa taskq=" + taskq);
                                layer.tasks.Enqueue(new NavTask(NavTaskType.Exit, v, _callback, layer));
                            }
                        }
                    }

                }
                else if (type == NavTaskType.ExitAndRemove)
                {
                    if (view.isTransparent == false)
                    {
                        Debug.Log("Trans and Show");
                        for (int i = layer.views.Count - 1; i >= 0; i--)
                        {
                            var v = layer.views[i];
                            Debug.LogWarning("inlist:" + v + "," + v.isLoad);

                            if (!v.isLoad)
                            {
                                taskq++;
                                Debug.Log("initaa taskq=" + taskq);

                                layer.tasks.Enqueue(new NavTask(NavTaskType.Init, v, _callback, layer));
                            }
                            if (v.isTransparent == false)
                            {
                                break;
                            }
                        }
                    }
                }
//                MyDebug.Log("finish taskq=" + taskq);

                if (taskq == 0 && bwait)
                {

                    CallBackTask(null);

                }
                bwait = false;
            }
            NavTaskType type;
            public IScreenView view;
            public bool done
            {
                get;
                private set;
            }
            Action<Exception> callback;
        }


        Dictionary<string, IScreenView> unuseViews = new Dictionary<string, IScreenView>();


        List<IScreenView> views = new List<IScreenView>();//显示栈，当前在显示栈中的界面

        public IScreenView Peek()
        {
            if (views.Count == 0) return null;
            return views[views.Count - 1];
        }

        public void Update(float delta)
        {
            for (int i = views.Count - 1; i >= 0; i--)
            {
                var v = views[i];


                //当前的帧循环

                //float start = Time.realtimeSinceStartup;
                v.Update(delta);
                //float end = Time.realtimeSinceStartup;

                //MyDebug.Log( "[" + v.name + "] - " + "View Update execution time:" + (end - start));


                if (!v.isTransparent)
                {
                    break;
                }
                if (!v.isLoad)
                {
                    break;
                }
            }
            foreach (var t in tasks)
            {
               // float start = Time.time;
                t.view.UpdateTask(delta);
               // float end = Time.time;

                //Debug.LogError("UpdateTask execution time:" + (end - start));
            }
            if (taskCurrect == null && tasks.Count > 0)
            {
               // float start = Time.time;
                taskCurrect = tasks.Dequeue();
                taskCurrect.Begin();
               // float end = Time.time;

                //Debug.LogError("task execution time:" + (end - start));
            }
            if (taskCurrect != null && taskCurrect.done)
            {
                taskCurrect = null;
//                MyDebug.Log("TaskFinish");
            }

        }


      

        //public void SendMassage(string svName, int msg ,Action callback = null)
        //{
        //    var sv = GetScreenView(svName);
        //    if (sv == null)
        //    {
        //        Debug.LogError("找不到view - name:" + svName);
        //        return;
        //    }
        //    if (sv is IReceiver)
        //    {
        //        (sv as IReceiver).ReceiveMassage(msg,callback);
        //    }
        //    else
        //    {
        //        Debug.LogError("接收对象并没实现IReceiver接口!");
        //    }
         
        //}
    }




}