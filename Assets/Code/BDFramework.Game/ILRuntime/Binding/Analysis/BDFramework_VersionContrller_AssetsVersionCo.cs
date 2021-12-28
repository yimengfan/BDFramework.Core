using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using ILRuntime.CLR.TypeSystem;
using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;
using ILRuntime.Reflection;
using ILRuntime.CLR.Utils;

namespace ILRuntime.Runtime.Generated
{
    unsafe class BDFramework_VersionContrller_AssetsVersionContrller_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(BDFramework.VersionContrller.AssetsVersionContrller);
            args = new Type[]{typeof(BDFramework.VersionContrller.UpdateMode), typeof(System.String), typeof(System.String), typeof(System.Action<BDFramework.VersionContrller.ServerAssetItem, System.Collections.Generic.List<BDFramework.VersionContrller.ServerAssetItem>>), typeof(System.Action<BDFramework.VersionContrller.AssetsVersionContrller.VersionControllerStatus, System.String>)};
            method = type.GetMethod("Start", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Start_0);


        }


        static StackObject* Start_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 5);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Action<BDFramework.VersionContrller.AssetsVersionContrller.VersionControllerStatus, System.String> @onTaskEndCallback = (System.Action<BDFramework.VersionContrller.AssetsVersionContrller.VersionControllerStatus, System.String>)typeof(System.Action<BDFramework.VersionContrller.AssetsVersionContrller.VersionControllerStatus, System.String>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Action<BDFramework.VersionContrller.ServerAssetItem, System.Collections.Generic.List<BDFramework.VersionContrller.ServerAssetItem>> @onDownloadProccess = (System.Action<BDFramework.VersionContrller.ServerAssetItem, System.Collections.Generic.List<BDFramework.VersionContrller.ServerAssetItem>>)typeof(System.Action<BDFramework.VersionContrller.ServerAssetItem, System.Collections.Generic.List<BDFramework.VersionContrller.ServerAssetItem>>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.String @assetsPackageName = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            System.String @serverConfigPath = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            BDFramework.VersionContrller.UpdateMode @updateMode = (BDFramework.VersionContrller.UpdateMode)typeof(BDFramework.VersionContrller.UpdateMode).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)20);
            __intp.Free(ptr_of_this_method);


            BDFramework.VersionContrller.AssetsVersionContrller.Start(@updateMode, @serverConfigPath, @assetsPackageName, @onDownloadProccess, @onTaskEndCallback);

            return __ret;
        }



    }
}
