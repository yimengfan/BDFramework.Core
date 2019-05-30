using System.IO;
using BDFramework;
using BDFramework.Helper;
using BDFramework.VersionContrller;
using Game.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主工程部分ui直接放在Resources下
/// 主工程所有代码 都是以M开头，区分热更工程中，避免引用错误
/// </summary>
[M_UI(1, "UI/MainLoading")]
public class Windows_PreConfig : M_AWindow
{
    [M_TransformPath("InputField")] private InputField inputField;

    [M_TransformPath("text_DownloadProcess")]
    private Text text_DownloadProcess;

    [M_TransformPath("btn_Download")] private Button btn_Download;

    [M_TransformPath("btn_Pass")] private Button btn_Pass;

    //
    public Windows_PreConfig(string path) : base(path)
    {
    }

    public Windows_PreConfig(Transform transform) : base(transform)
    {
    }


    public override void Init()
    {
        base.Init();

        this.btn_Pass.onClick.AddListener(Onclick_Pass);
        this.btn_Download.onClick.AddListener(Onclick_DownLoad);
        inputField.text = "127.0.0.1";
    }


    void Onclick_Pass()
    {
        //直接启动
        GameObject.Find("BDFrame").GetComponent<BDLauncher>().Launch();
        this.Close();
    }


    private void Onclick_DownLoad()
    {
        //删除本地的文件
        var cachedir = IPath.Combine(Application.persistentDataPath, Utils.GetPlatformPath(Application.platform));
        if (Directory.Exists(cachedir))
        {
            Directory.Delete(cachedir, true);
        }

        var url = "http://" + this.inputField.text;
        VersionContorller.Start(UpdateMode.Repair, url, Application.persistentDataPath,
            (i, j) =>
            {
                this.text_DownloadProcess.text = string.Format("{0}/{1}", i, j);
                //下载完毕
                if (i == j)
                {
                    this.text_DownloadProcess.text = "下载完毕";
                    //启动
                    GameObject.Find("BDFrame").GetComponent<BDLauncher>().Launch();
                }
            },
            (e) =>
            {
                this.text_DownloadProcess.text = e;
            });
    }
}