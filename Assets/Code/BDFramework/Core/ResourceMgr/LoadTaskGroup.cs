using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 单个任务的数据存储
    /// </summary>
    public class LoadTask
    {
        public int Id;
        public string ResourcePath;
    }


  
    /// <summary>
    /// 这是加载任务的组,每组任务可能有1个以上taskid组成
    /// </summary>
    public class LoadTaskGroup
    {
        public bool IsComplete { get; private set; }
       
        private Action<int, int> onOneTaskComplete = null;
        private Action<IDictionary<string, UnityEngine.Object>> onAllTaskComplete = null;
        
        /// <summary>
        /// 资源集合
        /// </summary>
        Dictionary<string ,UnityEngine.Object> objectsMap =new Dictionary<string, UnityEngine.Object>();
        /// <summary>
        /// 任务总量
        /// </summary>
        private int taskNum;
        /// <summary>
        /// 任务组
        /// </summary>
        /// <param name="taskIdList">所有任务组</param>
        /// <param name="onOneTaskComplete">加载进度</param>
        public LoadTaskGroup(List<LoadTask> taskIdList,  Action<int, int> onOneTaskComplete =null , Action<IDictionary<string, UnityEngine.Object>> onAllTaskComplete = null)
        {
            this.TaskIdList = taskIdList;
            this.onOneTaskComplete = onOneTaskComplete;
            this.onAllTaskComplete = onAllTaskComplete;
            taskNum = taskIdList.Count;
        }
        /// <summary>
        /// 任务列表
        /// </summary>
        public List<LoadTask> TaskIdList = null;
        
        /// <summary>
        /// 获取一个task
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public LoadTask GetTask()
        {
            if (this.TaskIdList.Count > 0)
            {
                var t = TaskIdList[0];
                return t;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// 移除任务
        /// </summary>
        /// <param name="taskId"></param>
        public void RemoveTask(int taskId)
        {
            var task = TaskIdList.Find((t) => t.Id == taskId);
            if (task != null)
            {
                TaskIdList.Remove(task);
            }
        }

        /// <summary>
        /// 当一个任务完成触发
        /// </summary>
        public void OnOneTaskComplete(int taskId, string resName,UnityEngine.Object obj)
        {
            var task = TaskIdList.Find((t) => t.Id == taskId);
            if (task!=null)
            {
                TaskIdList.Remove(task);
                this.objectsMap[resName] = obj;
                //单进度通知
                if (onOneTaskComplete != null)
                {
                    onOneTaskComplete(taskNum - TaskIdList.Count, taskNum);
                }

                //完成所有的
                if (TaskIdList.Count == 0)
                {
                    IsComplete = true;
                    //总进度通知
                    if (onAllTaskComplete != null)
                    {
                        onAllTaskComplete(this.objectsMap);
                    }                 
                }
            }
        }
    }
}