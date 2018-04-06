using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RegistViewItem : MonoBehaviour
{

    public bool isBindPath = true;
    public string bindDataName;
    private string _root;
    public string Root { set { this._root = value; } get { return this._root; } }
    public Type GetUIType()
    {
        UIBehaviour[] uiBehaviours = gameObject.GetComponents<UIBehaviour>();
        for (int i = 0; i < uiBehaviours.Length; i++)
        {
            if (uiBehaviours[i].GetType().Equals(typeof(UnityEngine.UI.Image))) continue;
            return uiBehaviours[i].GetType();
        }
        return typeof(UnityEngine.UI.Image);
    }

    public string GetBindPath()
    {
        string path = gameObject.name;
        Transform tsTp = gameObject.transform;
        if (isBindPath)
        {
            while (tsTp.parent && !tsTp.parent.name.Equals(_root))
            {
                path = path.Insert(0, tsTp.parent.name + "/");
                tsTp = tsTp.parent;
            }
            return "BSetTransform(\"" + path + "\")";
        }
        return "";
    }

    public string GetPath(bool isGet)
    {
        string path = gameObject.name;
        Transform tsTp = gameObject.transform;
        if (isGet)
        {
            while (tsTp.parent && !tsTp.parent.name.Equals(_root))
            {
                path = path.Insert(0, tsTp.parent.name + "/");
                tsTp = tsTp.parent;
            }
            return "BSetTransform(\"" + path + "\")";
        }
        return "";
    }

    public string GetBindDataName()
    {
        if (!string.IsNullOrEmpty(bindDataName))
        {
            return "BBindData(\"" + bindDataName.Trim() + "\")";
        }
        return null;
    }
}
