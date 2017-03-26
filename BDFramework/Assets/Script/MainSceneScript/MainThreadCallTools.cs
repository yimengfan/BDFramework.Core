using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MainThreadCallTools : MonoBehaviour
{
    /// <summary>
    /// 压入的action任务
    /// </summary>
    public class ActionTask
    {
        public Action m_willDoAction;
        public Action m_callBackAction;
    }
    /// <summary>
    /// 任务队列
    /// </summary>
    static Queue<ActionTask> m_actionTaskQueue = new Queue<ActionTask>();
    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="action"></param>
    /// <param name="callBack"></param>
    static public void ExecAction(Action action,Action callBack =null)
    {
        var task = new ActionTask()
        {
            m_willDoAction = action,
            m_callBackAction = callBack

        };

        m_actionTaskQueue.Enqueue(task);
    }
   /// <summary>
   /// 携程队列
   /// </summary>
    static Queue<IEnumerator> m_coroutineQueue = new Queue<IEnumerator>();
    /// <summary>
    /// 开始协程
    /// </summary>
    /// <param name="ie"></param>
    static public new void StartCoroutine(IEnumerator ie)
    {
        m_coroutineQueue.Enqueue(ie);
    }

    static Queue<string> m_stopCoroutineQueue = new Queue<string>();
    /// <summary>
    /// 关闭协程
    /// </summary>
    static public void StopCoroutine(string ie)
    {
        m_stopCoroutineQueue.Enqueue(ie);
    }
    /// <summary>
    /// 主循环
    /// </summary>
    void Update()
    {
        //携程循环
        if (m_coroutineQueue.Count > 0)
        {
            var ie = m_coroutineQueue.Dequeue();
            base.StartCoroutine(ie);
        }
        //停止协程
        while (m_stopCoroutineQueue.Count > 0)
        {
            var cr = m_stopCoroutineQueue.Dequeue();
            base.StopCoroutine(cr);
        }
        //主线程循环
        if (m_actionTaskQueue.Count > 0)
        {
            var task = m_actionTaskQueue.Dequeue();
            task.m_willDoAction();
            if (task.m_callBackAction != null)
            {
                task.m_callBackAction();
            }
        }
    }

}
