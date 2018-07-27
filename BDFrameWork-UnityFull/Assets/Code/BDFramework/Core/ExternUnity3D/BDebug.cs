using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Rendering;

public class BDebug : MonoBehaviour
{
    public bool IsLog = false;

   static private bool isLog = false;

    private void Awake()
    {
        isLog = this.IsLog;
    }

    static public void Log(object s)
    {
        if(isLog)
            UnityEngine.Debug.Log(s);
    }
    
    //b
    static public void Log(object s, string  color)
    {
        if (isLog)
        {
            s = string.Format("<color={0}>{1}</color>", color, s);
            UnityEngine.Debug.Log(s);
        }

    }

   static public void LogFormat(string format, params object[] args)
    {
        if (isLog)
          UnityEngine.Debug.LogFormat(format, args);
    }


   static public void LogError(object s)
    {
        if (isLog)
            UnityEngine.Debug.LogError(s);
    }

    void Update()
    {
        isLog = IsLog;
    }
}
