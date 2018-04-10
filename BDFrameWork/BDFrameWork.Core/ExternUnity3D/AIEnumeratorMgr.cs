using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDFramework
{
    /// <summary>
    /// 协程任务管理器
    /// </summary>
   abstract public class AIEnumeratorTaskMgr
   {
        public AIEnumeratorTaskMgr()
        {
            curExecTaskIds = new List<int>();
        }
        private List<int> curExecTaskIds;


        public int StartCroutine(IEnumerator ie)
        {
            return IEnumeratorTool.StartCoroutine(ie);
        }

        public void StopCoroutine(int id)
        {
            IEnumeratorTool.StopCoroutine(id);
        }

        public void StopAllCroutine()
        {
            foreach (var id in this.curExecTaskIds)
            {
                IEnumeratorTool.StopCoroutine(id);
            }
        }
   }
}
