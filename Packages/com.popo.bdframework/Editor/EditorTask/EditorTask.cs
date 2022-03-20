using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BDFramework.Editor.Task
{
    /// <summary>
    /// 编辑器任务
    /// 打上对应标签，则按规则执行
    /// </summary>
    public class EditorTask
    {
        public class EditorTaskAttribute : Attribute
        {
            /// <summary>
            /// 是否能执行
            /// </summary>
            /// <returns></returns>
            virtual public bool IsCanDoExcute()
            {
                return true;
            }
        }

        /// <summary>
        /// 当代码构建完
        /// </summary>
        public class EditorTaskOnUnityLoadOrCodeRecompiledAttribute : EditorTaskAttribute
        {
            // /// <summary>
            // /// 执行次数
            // /// 以每次重新打开编辑器计算
            // /// </summary>
            // public int ExcuteCount { get; set; }

            /// <summary>
            /// 描述
            /// </summary>
            public string Des { get; set; }

            public EditorTaskOnUnityLoadOrCodeRecompiledAttribute(string des)
            {
                this.Des = des;
            }
        }

        /// <summary>
        /// 当进入playmode
        /// </summary>
        public class EditorTaskOnWillEnterPlaymodeAttribute : EditorTaskAttribute
        {
            /// <summary>
            /// 描述
            /// </summary>
            public string Des { get; set; }

            public EditorTaskOnWillEnterPlaymodeAttribute(string des)
            {
                this.Des = des;
            }
        }
        /// <summary>
        /// 每天都执行，定时
        /// </summary>
        public class EditorTaskEveryDay : EditorTaskAttribute
        {
            /// <summary>
            /// 描述
            /// </summary>
            public string Des { get; private set; }

            /// <summary>
            /// 事件
            /// </summary>
            public DateTime ExcuteTime { get; private set; } //= new DateTime()

            public EditorTaskEveryDay(string des)
            {
                this.Des = des;
            }
        }


        /// <summary>
        /// editortask方法数据
        /// </summary>
        public class EditorTaskMethodData
        {
            public EditorTaskAttribute Attr { get; set; }
            public MethodInfo MethodInfo { get; set; }
        }

        /// <summary>
        /// editortask集合
        /// </summary>
        private Dictionary<Type, List<EditorTaskMethodData>> editortaskMethodMap = new Dictionary<Type, List<EditorTaskMethodData>>();

        /// <summary>
        /// 搜集所有EditorTask方法
        /// </summary>
        public void CollectEditorTaskMedthod()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where((assembly) => assembly.FullName.ToLower().Contains("editor")).ToList();
            foreach (var assembly in assemblies)
            {
                var editortypes = assembly.GetTypes();
                //只搜集editor
                foreach (var type in  editortypes)
                {
                    //
                    if (type != null && type.IsClass)
                    {
                        foreach (var mi in type.GetMethods())
                        {
                            //搜集editortask的method
                            if (mi.IsStatic)
                            {
                                var taskAttr = mi.GetCustomAttribute<EditorTaskAttribute>(false);
                                if (taskAttr != null)
                                {
                                    editortaskMethodMap.TryGetValue(taskAttr.GetType(), out var list);
                                    if (list == null)
                                    {
                                        list = new List<EditorTaskMethodData>();
                                        editortaskMethodMap[taskAttr.GetType()] = list;
                                    }

                                    //添加任务
                                    list.Add(new EditorTaskMethodData() {Attr = taskAttr, MethodInfo = mi});
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 执行对应的EditorTask方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void DoEditorTasks<T>() where T : EditorTaskAttribute
        {
            this.editortaskMethodMap.TryGetValue(typeof(T), out var list);
            if (list != null)
            {
                foreach (var editorTask in list)
                {
                    //执行
                    editorTask.MethodInfo.Invoke(null, new object[] { });
                }
            }
        }


        /// <summary>
        /// 当unity加载或者重新编译
        /// </summary>
        public void OnUnityLoadOrCodeRecompiled()
        {
            DoEditorTasks<EditorTaskOnUnityLoadOrCodeRecompiledAttribute>();
        }

        /// <summary>
        /// 编辑器更新
        /// </summary>
        public void OnEditorUpdate()
        {
        }

        /// <summary>
        /// 当进入playmode
        /// </summary>
        public void OnEnterWillPlayMode()
        {
            DoEditorTasks<EditorTaskOnWillEnterPlaymodeAttribute>();
        }
    }
}