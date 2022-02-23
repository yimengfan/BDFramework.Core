using System.Collections;
using System.IO;
using BDFramework;
using BDFramework.VersionController;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.VersionController;
using Game.ILRuntime;
using UnityEngine;
using UnityEngine.UI;

public class WindowPreconfig : MonoBehaviour
{
    private InputField inputField;
    private Text text_DownloadProcess;
    private Button btn_Download;
    private Button btn_DownloadSubPackage;
    private Button btn_Pass;


    /// <summary>
    /// 开始
    /// </summary>
    void Start()
    {
        //节点发现
        inputField = this.transform.Find("InputField").GetComponent<InputField>();
        text_DownloadProcess = this.transform.Find("text_DownloadProcess").GetComponent<Text>();
        btn_Download = this.transform.Find("btn_Download").GetComponent<Button>();
        btn_DownloadSubPackage = this.transform.Find("btn_DownloadSubPackage").GetComponent<Button>();
        btn_Pass = this.transform.Find("btn_Pass").GetComponent<Button>();
        //
        this.btn_Pass.onClick.AddListener(Onclick_PassAndLaunch);
        this.btn_Download.onClick.AddListener(Onclick_DownLoadAndLaunch);
        this.btn_DownloadSubPackage.onClick.AddListener(Onclick_DownloadSubPackageLoadAndLaunch);
        inputField.text = "127.0.0.1:8081";
    }


    /// <summary>
    /// 点击按钮事件
    /// </summary>
    void Onclick_PassAndLaunch()
    {
        //直接启动
        BDLauncher.Inst.Launch(this.GetType().Assembly.GetTypes(), GameLogicCLRBinding.Bind);
        //
        this.StartCoroutine(IE_Destroy());
    }


    private void Onclick_DownLoadAndLaunch()
    {
        //删除本地的文件，这不是正式环境逻辑，请勿参考
        // var cachedir = IPath.Combine(Application.persistentDataPath, BDApplication.GetPlatformPath(Application.platform));
        // if (Directory.Exists(cachedir))
        // {
        //     Directory.Delete(cachedir, true);
        // }

        Debug.Log(Application.persistentDataPath);
        var url = "http://" + this.inputField.text;
        float totalSize = -1;
        float curDoanloadSize = -1;
        BResources.StartAssetsVersionControl(UpdateMode.CompareVersionConfig, url, null, (curDownload, allDownloadList) =>
            {
                if (totalSize == -1)
                {
                    foreach (var item in allDownloadList)
                    {
                        totalSize += item.FileSize;
                    }

                    curDoanloadSize = 0;
                }

                curDoanloadSize += curDownload.FileSize;
                //进度通知,显示下载的
                this.text_DownloadProcess.text = string.Format("{0}KB / {1}KB", curDoanloadSize, totalSize);
            },
            (status, msg) =>
            {
                //下载状态
                switch (status)
                {
                    case AssetsVersionController.VersionControllerStatus.Success:
                    {
                        this.text_DownloadProcess.text = "下载完毕";
                        //启动
                        this.Onclick_PassAndLaunch();
                    }
                        break;
                    case AssetsVersionController.VersionControllerStatus.Error:
                    {
                        //错误
                        this.text_DownloadProcess.text = msg;
                    }
                        break;
                }
            });
    }

    /// <summary>
    /// 下载子包
    /// </summary>
    private void Onclick_DownloadSubPackageLoadAndLaunch()
    {
        Debug.Log(Application.persistentDataPath);

        var url = "http://" + this.inputField.text;
        var subPackageName = "TestChar";
        float totalSize = -1;
        float curDoanloadSize = -1;
        AssetsVersionController.Start(UpdateMode.CompareVersionConfig, url, subPackageName, (curDownload, allDownloadList) =>
            {
                if (totalSize == -1)
                {
                    foreach (var item in allDownloadList)
                    {
                        totalSize += item.FileSize;
                    }

                    curDoanloadSize = 0;
                }

                curDoanloadSize += curDownload.FileSize;
                //进度通知
                this.text_DownloadProcess.text = string.Format("{0}KB / {1}KB", curDoanloadSize, totalSize);
            },
            (status, msg) =>
            {
                switch (status)
                {
                    case AssetsVersionController.VersionControllerStatus.Success:
                    {
                        this.text_DownloadProcess.text = "下载完毕";
                        //启动
                        this.Onclick_PassAndLaunch();
                    }
                        break;
                    case AssetsVersionController.VersionControllerStatus.Error:
                    {
                        //错误
                        this.text_DownloadProcess.text = msg;
                    }
                        break;
                }
            });
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <returns></returns>
    IEnumerator IE_Destroy()
    {
        yield return new WaitForSeconds(3);
        Destroy(this.gameObject);
    }
}
