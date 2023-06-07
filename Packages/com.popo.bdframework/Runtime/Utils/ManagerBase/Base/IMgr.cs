using System;

namespace BDFramework.Mgr
{
    public class ClassData
    {
        public ManagerAttribute Attribute;
        public Type Type;
    }

    public interface IMgr
    {
        /// <summary>
        /// 是否启动
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        void Init();

        /// <summary>
        /// 开始
        /// </summary>
        void Start();

        /// <summary>
        /// 实例类型注册
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attributes"></param>
        bool RegisterTypes(Type type, ManagerAttribute[] attributes);

        /// <summary>
        /// 实例类型注册
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attributes"></param>
        bool RegisterHotfixTypes(Type type, ManagerAttribute[] attributes);
        
        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="args"></param>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        T2 CreateInstance<T2>(object tag, params object[] args) where T2 : class;

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="args"></param>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        T2 CreateInstance<T2>(ClassData tag, params object[] args) where T2 : class;
        /// <summary>
        /// 获取class data
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        ClassData GetClassData(object typeName);
    }
}
