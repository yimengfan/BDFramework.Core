using BDFramework.UFlux;
using UnityEngine;
//这里的命名空间必须为：BDFramework.Uflux
namespace  BDFramework.UFlux
{
    public class TransformChild
    {
        /// <summary>
        /// 控制几个子节点隐藏
        /// </summary>
        public  void ShowHideChildByNumber(){}
    }
    
    /// <summary>
    /// 自定义逻辑的适配器
    /// </summary>
    [ComponentBindAdaptor(typeof(TransformChild))]
    public class CustomLogicBindAdaptorTransformChild : AComponentBindAdaptor
    {
        
        public override void Init()
        {
            base.Init();
            setPropCustomLogicMap[nameof(TransformChild.ShowHideChildByNumber)] = ShowHideChildByNumber;
          
        }
        /// <summary>
        /// 设置几个隐藏或者不隐藏
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