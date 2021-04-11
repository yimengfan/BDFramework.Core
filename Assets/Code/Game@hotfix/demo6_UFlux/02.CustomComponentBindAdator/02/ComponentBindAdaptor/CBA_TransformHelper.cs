using BDFramework.UFlux;
using UnityEngine;
//这里的命名空间必须为：BDFramework.Uflux
namespace  BDFramework.UFlux
{
    public class TransformHelper
    {
        /// <summary>
        /// 控制几个子节点隐藏
        /// </summary>
        public  void ShowHideChildByNumber(){}
    }
    
    
    /// <summary>
    /// 自定义逻辑的适配器
    /// </summary>
    [ComponentBindAdaptor(typeof(TransformHelper))]
    public class CBA_TransformHelper : AComponentBindAdaptor
    {
        
        public override void Init()
        {
            base.Init();
            setPropCustomLogicMap[nameof(TransformHelper.ShowHideChildByNumber)] = ShowHideChildByNumber;
          
        }
        /// <summary>
        /// 根据值隐藏显示子节点
        /// </summary>
        /// <param name="value"></param>
        private void ShowHideChildByNumber(Transform transform,object value)
        {
            var count = (int) value;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(i<count);
            }
        }


        
    }
}