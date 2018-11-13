using System;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

public class HotfixCheck
{
   static public void Log()
   {

   }
   
   public class  testclass
   {
      public int i = 1;
   }
   static async public void TestAction(string s,string s2,Action<int,int>callback2,Action<string> callback)
   {
      int i = 222111;
      var test =  new testclass();
      test.i = 2222;
      var json = JsonMapper.ToJson(test);
      var o = JsonMapper.ToObject<testclass>(json);
      //
      callback(json.ToString());
      int m = 1;
      int n = 2;
      callback2(m, n);
   }
}
