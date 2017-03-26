using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ScreenView_Test
{
    public bool isBusy
    {
        get;
        set;
    }

    public bool isLoad
    {
        get;
        set;
    }

    public bool isTransparent
    {
        get; set;
    }

    public string name
    {
       get
        {
            return "test1";
        }
    }

public void BeginExit(Action<Exception> onExit)
    {
        Debug.LogFormat("{0}:on BeginExit", this.name);
    }

    public void BeginInit(Action<Exception> onInit)
    {
        Debug.LogFormat("{0}:on Init", this.name);
    }

    public void Destory()
    {
        Debug.LogFormat("{0}:on destory", this.name);
    }

    public void Update(float delta)
    {
        Debug.LogFormat("{0}:on update", this.name);
    }

    public void UpdateTask(float delta)
    {
       
    }
}
