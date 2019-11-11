using System.Collections;
using System.Collections.Generic;
using BDFramework.UFlux;
using NUnit.Framework;


namespace Tests
{
    public class UFluxTest
    {
        public class T1AStateBase : StateBase
        {

            public int ia = 0;
            public int ib = 0;
            public string sa = "";
            public bool bb = false;
            //
            public T2AStateBase AStateBase =new T2AStateBase();
            public List<T2AStateBase> stateList =new List<T2AStateBase>(){new T2AStateBase(), new T2AStateBase()};
            public Dictionary<string,T2AStateBase> stateMap = new Dictionary<string, T2AStateBase>()
            {
                {"1",new T2AStateBase()},
                {"2",new T2AStateBase()}
            };
            
        }
        
        public class T2AStateBase : StateBase
        {
            public int a =0;
            public string b = "";
            public bool c =false;
        }
        
        public class T3AStateBase: StateBase
        {
            public int i =0;
        }
        
        [Test]
        public void Test_UFluxStateCompare()
        {
            
            
        }
    }
}
