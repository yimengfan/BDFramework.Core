using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Rendering;

public class BDeBug : MonoBehaviour
{
    static public BDeBug  I
    {
        get;
        private set;
    }
    private void Awake()
    {
        I = this;
    }
    public bool IsLog = false;
    
    public void Log(object s)
    {
        if(IsLog)
            UnityEngine.Debug.Log(s);
    }
    
    //
    public void Log(object s, string  color)
    {
        if (IsLog)
        {
            s = string.Format("<color={0}>{1}</color>", color, s);
            UnityEngine.Debug.Log(s);
        }

    }

    public void LogFormat(string format, params object[] args)
    {
        if (IsLog)
          UnityEngine.Debug.LogFormat(format, args);
    }
}
