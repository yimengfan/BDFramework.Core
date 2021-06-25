using System;
using Google.Protobuf;
using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;

public class IMessageAdapter : CrossBindingAdaptor
{
    public override Type BaseCLRType => typeof(IMessage);
    public override Type AdaptorType => typeof(Adaptor);
    
    static CrossBindingMethodInfo<CodedInputStream> mMergeFrom = new CrossBindingMethodInfo<CodedInputStream>("MergeFrom");
    static CrossBindingMethodInfo<CodedOutputStream> mWriteTo = new CrossBindingMethodInfo<CodedOutputStream>("WriteTo");
    static CrossBindingFunctionInfo<int> mCalculateSize = new CrossBindingFunctionInfo<int>("CalculateSize");
    
    public override object CreateCLRInstance(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
    {
        return new Adaptor(appdomain, instance);
    }

    public class Adaptor : CrossBindingAdaptorType, IMessage
    {
        ILTypeInstance instance;
        ILRuntime.Runtime.Enviorment.AppDomain appdomain;
        public ILTypeInstance ILInstance => instance;

        //private IMethod mMergeFrom;
        //private IMethod mWriteTo;
        //private IMethod mCalculateSize;
        //缓存这个数组来避免调用时的GC Alloc
        object[] param1 = new object[1];
        
        public Adaptor()
        {
        }
        
        public Adaptor(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
        {
            this.appdomain = appdomain;
            this.instance = instance;
        }


        public void MergeFrom(CodedInputStream input)
        {
            //mMergeFrom = instance.Type.GetMethod("MergeFrom", 1);
            //appdomain.Invoke(mMergeFrom, instance, new object[]{input});
            mMergeFrom.Invoke(this.instance, input);
        }

        public void WriteTo(CodedOutputStream output)
        {
            //mWriteTo = instance.Type.GetMethod("WriteTo", 1);
            //appdomain.Invoke(mWriteTo, instance, new object[]{output});
            mWriteTo.Invoke(this.instance, output);
        }

        public int CalculateSize()
        {
            //mCalculateSize = instance.Type.GetMethod("CalculateSize", 0);
            //return (int) appdomain.Invoke(mCalculateSize, instance);
            return mCalculateSize.Invoke(this.instance);
        }
    }
}
