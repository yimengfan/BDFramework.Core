using System;
using System.Collections.Generic;
using System.Reflection;

namespace ILRuntime.Runtime.Generated
{
    class ManualCLRBindings
    {
        /// <summary>
        /// Initialize the CLR binding, please invoke this AFTER CLR Redirection registration
        /// </summary>
        public static void Initialize(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            // System_Activator_Binding.Register(app);
            // System_Reflection_FieldInfo_Binding.Register(app);
            // System_Reflection_MemberInfo_Binding.Register(app);
            // System_Reflection_MethodInfo_Binding.Register(app);
            // System_Reflection_PropertyInfo_Binding.Register(app);
            // System_Reflection_MethodBase_Binding.Register(app);
            // System_Type_Binding.Register(app);
            // UnityEngine_Debug_Binding.Register(app);
        }

        /// <summary>
        /// Release the CLR binding, please invoke this BEFORE ILRuntime Appdomain destroy
        /// </summary>
        public static void Shutdown(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            
        }
    }
}
