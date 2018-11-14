using System;
using System.Collections.Generic;
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
    unsafe class UnityEngine_UI_GraphicRebuildTracker_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.GraphicRebuildTracker);
            args = new Type[]{typeof(UnityEngine.UI.Graphic)};
            method = type.GetMethod("TrackGraphic", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, TrackGraphic_0);
            args = new Type[]{typeof(UnityEngine.UI.Graphic)};
            method = type.GetMethod("UnTrackGraphic", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, UnTrackGraphic_1);





        }


        static StackObject* TrackGraphic_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.Graphic @g = (UnityEngine.UI.Graphic)typeof(UnityEngine.UI.Graphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            UnityEngine.UI.GraphicRebuildTracker.TrackGraphic(@g);

            return __ret;
        }

        static StackObject* UnTrackGraphic_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.Graphic @g = (UnityEngine.UI.Graphic)typeof(UnityEngine.UI.Graphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            UnityEngine.UI.GraphicRebuildTracker.UnTrackGraphic(@g);

            return __ret;
        }





    }
}
