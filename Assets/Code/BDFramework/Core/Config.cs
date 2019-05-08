using System.Collections;
using System.Collections.Generic;
using BDFramework;
using UnityEngine;
using UnityEngine.Serialization;

public enum AssetLoadPath
{
    Editor = 0,
    Persistent,
    StreamingAsset
}

//
public enum HotfixCodeRunMode
{
    ByILRuntime = 0,
    ByReflection,
}
public class Config : MonoBehaviour
{
    public AssetLoadPath CodeRoot = AssetLoadPath.Editor;
    public AssetLoadPath SQLRoot = AssetLoadPath.Editor;
    public AssetLoadPath ArtRoot = AssetLoadPath.Editor;

    public string CustomArtRoot = "";
    //只在非Editor模式下生效
    public HotfixCodeRunMode CodeRunMode = HotfixCodeRunMode.ByILRuntime;
    public string FileServerUrl = "192.168.8.68";

    //
    public bool IsHotfix = false;
    public string GateServerIp = "";
    public int Port;
    public bool IsNeedNet = false;

    #region    FPS计算
    
    float fps;
    float deltaTime = 0.0f;
    float msec;
    Rect rect;
    GUIStyle style = new GUIStyle();
    void Start()
    {
        int w = Screen.width, h = Screen.height;
        rect = new Rect(w - 350, 0, 300, h * 4 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 4 / 100;
        style.normal.textColor = new Color(1.0f, 0.0f, 0f, 1.0f);
    }

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        msec = deltaTime * 1000.0f;
        fps = 1.0f / deltaTime;

        GUI.Label(rect, string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps), style);
    }

    #endregion

}