using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PtrReflection
{
    public delegate TResult RefFunc<T, out TResult>(ref T arg);
    public delegate void RefAction<T1, T2>(ref T1 arg1, T2 arg2);

    public unsafe delegate void ActionVoidPtr<T2>(void* arg1, T2 arg2);
    public unsafe delegate void ActionVoidPtrVoidPtr(void* arg1, void* arg2);

    public unsafe delegate T2 FuncVoidPtr<T2>(void* arg1);
    public unsafe delegate  float FuncVoidPtr2(void* arg1);
    
    public unsafe delegate void* FuncVoidPtrVoidPtr(void* arg1);
    


    [StructLayout(LayoutKind.Explicit)]
    public  unsafe partial class PropertyDelegateItem
    { 
        [FieldOffset(0)]
        public object _set;
        [FieldOffset(0)]
        public Delegate _setDelegate;
        [FieldOffset(0)]
        public ActionVoidPtr<object> setObject;
        [FieldOffset(0)]
        public ActionVoidPtrVoidPtr setVoidPtr;

        [FieldOffset(0)]
        public ActionVoidPtr<bool> setBoolean;
        [FieldOffset(0)]
        public ActionVoidPtr<char> setChar;
        [FieldOffset(0)]
        public ActionVoidPtr<sbyte> setSByte;
        [FieldOffset(0)]
        public ActionVoidPtr<byte> setByte;
        [FieldOffset(0)]
        public ActionVoidPtr<short> setInt16;
        [FieldOffset(0)]
        public ActionVoidPtr<ushort> setUInt16;
        [FieldOffset(0)]
        public ActionVoidPtr<int> setInt32;
        [FieldOffset(0)]
        public ActionVoidPtr<uint> setUInt32;
        [FieldOffset(0)]
        public ActionVoidPtr<long> setInt64;
        [FieldOffset(0)]
        public ActionVoidPtr<ulong> setUInt64;
        [FieldOffset(0)]
        public ActionVoidPtr<float> setSingle;
        [FieldOffset(0)]
        public ActionVoidPtr<double> setDouble;
        [FieldOffset(0)]
        public ActionVoidPtr<decimal> setDecimal;
        [FieldOffset(0)]
        public ActionVoidPtr<DateTime> setDateTime;
        [FieldOffset(0)]
        public ActionVoidPtr<string> setString;

        [FieldOffset(16)]
        public object _get;
        [FieldOffset(16)]
        public Delegate _getDelegate;
        [FieldOffset(16)]
        public FuncVoidPtr<object> getObject;
        [FieldOffset(16)]
        public FuncVoidPtrVoidPtr getVoidPtr;

        [FieldOffset(16)]
        public FuncVoidPtr<bool> getBoolean;
        [FieldOffset(16)]
        public FuncVoidPtr<char> getChar;
        [FieldOffset(16)]
        public FuncVoidPtr<sbyte> getSByte;
        [FieldOffset(16)]
        public FuncVoidPtr<byte> getByte;
        [FieldOffset(16)]
        public FuncVoidPtr<short> getInt16;
        [FieldOffset(16)]
        public FuncVoidPtr<ushort> getUInt16;
        [FieldOffset(16)]
        public FuncVoidPtr<int> getInt32;
        [FieldOffset(16)]
        public FuncVoidPtr<uint> getUInt32;
        [FieldOffset(16)]
        public FuncVoidPtr<long> getInt64;
        [FieldOffset(16)]
        public FuncVoidPtr<ulong> getUInt64;
        [FieldOffset(16)]
        public FuncVoidPtr<float> getSingle;
        [FieldOffset(16)]
        public FuncVoidPtr<double> getDouble;
        [FieldOffset(16)]
        public FuncVoidPtr<decimal> getDecimal;
        [FieldOffset(16)]
        public FuncVoidPtr<DateTime> getDateTime;
        [FieldOffset(16)]
        public FuncVoidPtr<string> getString;

        //[FieldOffset(16)]
        //public Delegate get2;
        //[FieldOffset(16)]
        //public Action<object, object> SetObject2;
        //[FieldOffset(16)]
        //public Action<object, double> get_double2;
        //[FieldOffset(16)]
        //public Action<object, float> get_float2;
    }

    public interface IPropertyWrapper
    {
        void Set(object target, object value);
        object Get(object target);
        void SetGet(Delegate d);
        void SetSet(Delegate d);
    }


    public class PropertyWrapper
    {
        public static IPropertyWrapper Create(Type type, PropertyInfo propertyInfo)
        {
            Type propertyType = propertyInfo.PropertyType;
            if (type.IsValueType)
            {
                var setValue = Delegate.CreateDelegate(typeof(RefAction<,>).MakeGenericType(type, propertyType),
                   propertyInfo.GetSetMethod());
                var getValue = Delegate.CreateDelegate(typeof(RefFunc<,>).MakeGenericType(type, propertyType),
                   propertyInfo.GetGetMethod());
                IPropertyWrapper propertyWrapper = (IPropertyWrapper)Activator.CreateInstance(typeof(PropertyStructWrapper<,>).MakeGenericType(type, propertyType));
                propertyWrapper.SetGet(getValue);
                propertyWrapper.SetSet(setValue);
                return propertyWrapper;
            }
            else
            {
                var setValue = Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(type, propertyType),
                   propertyInfo.GetSetMethod());
                var getValue = Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(type, propertyType),
                   propertyInfo.GetGetMethod());
                IPropertyWrapper propertyWrapper = (IPropertyWrapper)Activator.CreateInstance(typeof(PropertyClassWrapper<,>).MakeGenericType(type, propertyType));
                propertyWrapper.SetGet(getValue);
                propertyWrapper.SetSet(setValue);
                return propertyWrapper;
            }
        }



        public static unsafe IPropertyWrapperTarget CreateStructIPropertyWrapperTarget(PropertyInfo propertyInfo)
        {
            var setValue = propertyInfo.GetSetMethod().
                CreateDelegate(typeof(Action<>).MakeGenericType(propertyInfo.PropertyType), null);

            IPropertyWrapperTarget propertyWrapper = (IPropertyWrapperTarget)Activator.CreateInstance(
                typeof(PropertyWrapperTarget<>).MakeGenericType(propertyInfo.PropertyType));
            propertyWrapper.set = setValue;

            return propertyWrapper;
        }

        delegate object GetStructDelegate<Target, Value>(Func<Target, Value> _get, Target target);
        public static object GetStruct<Target, Value>(object t1, object t2)
        {
            Func<Target, Value> _get = (Func<Target, Value>)t1;
            Target target = (Target)t2;
            object obj = _get(target);
            return obj;
        }
        
        public static unsafe Delegate CreateStructGet(Type parntType, PropertyInfo propertyInfo)
        {
            if (parntType.IsValueType)
            {
                var getValue = Delegate.CreateDelegate(typeof(RefFunc<,>).MakeGenericType(parntType, propertyInfo.PropertyType),
                propertyInfo.GetGetMethod());

#if ENABLE_MONO && !Test_Il2cpp
                IGetStruct propertyWrapper = (IGetStruct)Activator.CreateInstance(
                    typeof(RefPropertyGetWrapper<,>).MakeGenericType(parntType, propertyInfo.PropertyType));
                propertyWrapper.get = getValue;
                return propertyWrapper.GetDelegate();
#else
                return getValue;
#endif

            }
            else
            {
                var getValue = Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(parntType, propertyInfo.PropertyType),
                propertyInfo.GetGetMethod());


#if ENABLE_MONO && !Test_Il2cpp
                IGetStruct propertyWrapper = (IGetStruct)Activator.CreateInstance(
                    typeof(PropertyGetWrapper<,>).MakeGenericType(parntType, propertyInfo.PropertyType));
                propertyWrapper.get = getValue;
                return propertyWrapper.GetDelegate();
#else
                return getValue;
#endif
                //MethodInfo methodInfo = typeof(PropertyWrapper).GetMethod(nameof(GetStruct), BindingFlags.Static | BindingFlags.Public);
                //var methodInfo2 = methodInfo.MakeGenericMethod(parntType, propertyInfo.PropertyType);
                //getOutStruct = (Func<object, object, object>)methodInfo2.CreateDelegate(typeof(Func<object, object, object>));
            }
        }

        public static unsafe Delegate CreateStructSet(Type parntType, PropertyInfo propertyInfo)
        {
            if (parntType.IsValueType)
            {
                var setValue = Delegate.CreateDelegate(typeof(RefAction<,>).MakeGenericType(parntType, propertyInfo.PropertyType),
               propertyInfo.GetSetMethod());

#if ENABLE_MONO && !Test_Il2cpp
                ISetStruct propertyWrapper = (ISetStruct)Activator.CreateInstance(
                    typeof(RefPropertySetWrapper<,>).MakeGenericType(parntType, propertyInfo.PropertyType));
                propertyWrapper.set = setValue;
                return propertyWrapper.GetDelegate();
#else
                return setValue;
#endif

            }
            else
            {
                var setValue = Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(parntType, propertyInfo.PropertyType),
                propertyInfo.GetSetMethod());

#if ENABLE_MONO && !Test_Il2cpp
                ISetStruct propertyWrapper = (ISetStruct)Activator.CreateInstance(
                    typeof(PropertySetWrapper<,>).MakeGenericType(parntType, propertyInfo.PropertyType));
                propertyWrapper.set = setValue;
                return propertyWrapper.GetDelegate();
#else
                return setValue;
#endif

            }
        }

        public static Delegate CreateClassGet(Type parntType, PropertyInfo propertyInfo)
        {
            if (parntType.IsValueType)
            {
                var setValue = Delegate.CreateDelegate(typeof(RefFunc<,>).MakeGenericType(parntType, propertyInfo.PropertyType),
                   propertyInfo.GetGetMethod());
                return setValue;
            }
            else
            {
                var setValue = Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(parntType, propertyInfo.PropertyType),
                   propertyInfo.GetGetMethod());
                return setValue;
            }
        }

        public static Delegate CreateClassSet(Type parntType, PropertyInfo propertyInfo)
        {
            if (parntType.IsValueType)
            {
                var setValue = Delegate.CreateDelegate(typeof(RefAction<,>).MakeGenericType(parntType, propertyInfo.PropertyType),
                   propertyInfo.GetSetMethod());
                return setValue;
            }
            else
            {
                var setValue = Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(parntType, propertyInfo.PropertyType),
                   propertyInfo.GetSetMethod());
                return setValue;
            }
        }



        public static Delegate CreateGetTargetDelegate(PropertyInfo propertyInfo)
        {
            var getValue = propertyInfo.GetGetMethod().
                CreateDelegate(typeof(Func<>).MakeGenericType(propertyInfo.PropertyType), null);
            return getValue;
        }

        public static Delegate CreateSetTargetDelegate(PropertyInfo propertyInfo)
        {
            var setValue = propertyInfo.SetMethod.
                CreateDelegate(typeof(Action<>).MakeGenericType(propertyInfo.PropertyType), null);
            return setValue;
        }

    }

    public class PropertyClassWrapper<Target, Value> : IPropertyWrapper
    {
        Action<Target, Value> set;
        Func<Target, Value> get;
        public void SetGet(Delegate d)
        {
            get = (Func<Target, Value>)d;
        }

        public void SetSet(Delegate d)
        {
            set = (Action<Target, Value>)d;
        }

        public object Get(object target)
        {
            return get((Target)target);
        }

        public void Set(object target, object value)
        {
            set((Target)target, (Value)value);
        }
    }


    public interface IPropertyWrapperTarget
    {
        Delegate set { get; set; }
        void Set(object value);
    }

    public class PropertyWrapperTarget<Value> : IPropertyWrapperTarget
    {
        Action<Value> _set;
        public Delegate set
        {
            get { return _set; }
            set { _set = (Action<Value>)value; }
        }

        public void Set(object value)
        {
            _set((Value)value);
        }
    }

    public interface IGetStruct
    {
        Delegate get { get; set; }
        Delegate GetDelegate();
    }

    public class PropertyGetWrapper<Target, Value> : IGetStruct
    {
        public Func<Target, Value> _get;
        public Delegate get
        {
            get { return _get; }
            set { _get = (Func<Target, Value>)value; }
        }

        public object GetStruct(Target t)
        {
            object obj = _get(t);
            return obj;
        }
        public Delegate GetDelegate()
        {
            Func<Target, object> v = GetStruct;
            return v;
        }
    }

    public class RefPropertyGetWrapper<Target, Value> : IGetStruct
    {
        public RefFunc<Target, Value> _get;
        public Delegate get
        {
            get { return _get; }
            set { _get = (RefFunc<Target, Value>)value; }
        }

        public object GetStruct(ref Target t)
        {
            object obj = _get(ref t);
            return obj;
        }
        public Delegate GetDelegate()
        {
            RefFunc<Target, object> v = GetStruct;
            return v;
        }
    }

    public interface ISetStruct
    {
        Delegate set { get; set; }
        Delegate GetDelegate();
    }
    public class PropertySetWrapper<Target, Value> : ISetStruct
    {
        public Action<Target, Value> _set;
        public Delegate set
        {
            get { return _set; }
            set { _set = (Action<Target, Value>)value; }
        }

        public void SetStruct(Target t, object value)
        {
            _set(t, (Value)value);
        }
        public Delegate GetDelegate()
        {
            Action<Target, object> v = SetStruct;
            return v;
        }
    }

    public class RefPropertySetWrapper<Target, Value> : ISetStruct
    {
        public RefAction<Target, Value> _set;
        public Delegate set
        {
            get { return _set; }
            set { _set = (RefAction<Target, Value>)value; }
        }

        public void SetStruct(ref Target t, object value)
        {
            _set(ref t, (Value)value);
        }
        public Delegate GetDelegate()
        {
            RefAction<Target, object> v = SetStruct;
            return v;
        }
    }

    public class PropertyStructWrapper<Target, Value> : IPropertyWrapper
    {
        RefAction<Target, Value> set;
        RefFunc<Target, Value> get;
        public void SetGet(Delegate d)
        {
            get = (RefFunc<Target, Value>)d;
        }

        public void SetSet(Delegate d)
        {
            set = (RefAction<Target, Value>)d;
        }

        public object Get(object target)
        {
            var d = (Box<Target>)target;
            return get(ref d.value);
        }

        public void Set(object target, object value)
        {
            var d = (Box<Target>)target;
            set(ref d.value, (Value)value);
        }
    }


}
