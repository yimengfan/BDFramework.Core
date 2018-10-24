using System;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 异步任务
    /// </summary>
    public class LoadTask
    {
        public enum state
        {
            Waiting,
            Loading,
            End,
        }

        public LoadTask()
        {
            CurState = state.Waiting;
        }

        //任务id
        public int id;

        //当前状态
        public state CurState;

        //任务
        Action dotask;

        //注册task
        public void RegisterTask(Action task)
        {
            dotask = task;
        }

        //dotask
        public void DoTask()
        {
            if (dotask != null)
            {
                CurState = state.Loading;
                dotask();
            }
        }

        //任务结束
        public void EndTask()
        {
            CurState = state.End;
        }
    }
}
