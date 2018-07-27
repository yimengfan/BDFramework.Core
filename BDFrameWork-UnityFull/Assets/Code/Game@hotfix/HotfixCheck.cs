using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotfixCheck
{
   static public void Log()
   {
      var t = typeof(Game.Data.Buff);
      Debug.Log("fullname:" + t.FullName);
      Debug.Log("hotfix 检查成功!");
   }
}
