using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IEnumeratorLaunch : MonoBehaviour 
{
    public static IEnumeratorLaunch Instance
    {
        get;
        private set;
    }
    public void Enqueue(IEnumerator ie)
    {
       StartCoroutine(ie);
    }
	void Awake () 
    {
        Instance = this;
	}
	
}
