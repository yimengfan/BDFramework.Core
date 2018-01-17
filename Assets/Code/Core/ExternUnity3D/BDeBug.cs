using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    //
    public void Log(object s)
    {
       if(IsLog)
            UnityEngine.Debug.Log(s);
    }

    public void LogFormat(string format, params object[] args)
    {
        if (IsLog)
          UnityEngine.Debug.LogFormat(format, args);
    }
}
