using System;
using System.Reflection;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 初始化SubWindows节点，并注册到当前窗口.
    /// </summary>
    public class SubWindowAttribute : AutoInitComponentAttribute
    {
        public string Path;

        public SubWindowAttribute(string path)
        {
            this.Path = path;
        }

        /// <summary>
        /// 设置字段
        /// </summary>
        /// <param name="com"></param>
        /// <param name="fieldInfo"></param>
        public override void AutoSetField(IComponent com, FieldInfo fieldInfo)
        {
            Type uiType = fieldInfo.FieldType;
            var transform = com.Transform.Find(this.Path);
            // if (uiType.IsSubclassOf(typeof(AWindow)))
            // {
                if (!transform)
                {
                    BDebug.LogError($"窗口:{com} 不存在节点:{this.Path}");
                    return;
                }

                var subWindow = Activator.CreateInstance(uiType, new object[] { transform }) as IWindow;
                fieldInfo.SetValue(com, subWindow);
                (com as IWindow).RegisterSubWindow(subWindow);
               
            // }
            // else
            // {
            //     BDebug.LogError($"窗口:{com} 属性：{fieldInfo.Name} 不是子窗口!");
            // }
        }


        /// <summary>
        /// 设置property
        /// </summary>
        /// <param name="com"></param>
        /// <param name="propertyInfo"></param>
        public override void AutoSetProperty(IComponent com, PropertyInfo propertyInfo)
        {
            Type uiType = propertyInfo.PropertyType;
            var transform = com.Transform.Find(this.Path);
            // if (uiType.IsSubclassOf(typeof(AWindow)))
            // {
                if (!transform)
                {
                    BDebug.LogError($"窗口:{com} 不存在节点:{this.Path}");
                    return;
                }

                var subWindow = Activator.CreateInstance(uiType, new object[] { transform }) as IWindow;
                propertyInfo.SetValue(com, subWindow);
                (com as IWindow).RegisterSubWindow(subWindow);
            // }
            // else
            // {
            //     BDebug.LogError($"窗口:{com} 属性：{propertyInfo.Name} 不是子窗口!");
            // }
        }
    }
}