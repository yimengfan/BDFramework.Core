using ILRuntime.Runtime.Generated;
using UnityEngine;

namespace Game.ILRuntime
{
    static public class GameLogicCLRBinding
    {
        /// <summary>
        /// 主工程的绑定
        /// </summary>
        /// <param name="isRegisterBindings"></param>
        static public void Bind(bool isRegisterBindings)
        {
            var AppDomain = BDFramework.ILRuntimeHelper.AppDomain;
            //绑定的初始化
            //ada绑定
            AdapterRegister.RegisterCrossBindingAdaptor(AppDomain);
            //delegate绑定
            ILRuntimeDelegateHelper.Register(AppDomain);
            //值类型绑定
            AppDomain.RegisterValueTypeBinder(typeof(Vector2), new Vector2Binder());
            AppDomain.RegisterValueTypeBinder(typeof(Vector3), new Vector3Binder());
            AppDomain.RegisterValueTypeBinder(typeof(Vector4), new Vector4Binder());
            AppDomain.RegisterValueTypeBinder(typeof(Quaternion), new QuaternionBinder());

            //是否注册各种binding
            if (isRegisterBindings)
            {
                //手动绑定放前
                ManualCLRBindings.Initialize(AppDomain);
                //自动绑定最后
                CLRBindings.Initialize(AppDomain);
                //PreCLRBinding.Initialize(AppDomain);
                BDebug.Log("[ILRuntime] CLR Binding Success!!!");
            }
        }
    }
}