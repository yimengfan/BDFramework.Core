using System.Collections;
using System.IO;
using BDFramework;
using BDFramework.VersionContrller;
using BDFramework.Core.Tools;
using Game.ILRuntime;
using UnityEngine;
using UnityEngine.UI;

public class WindowPreconfig : MonoBehaviour
{
    private InputField inputField;
    private Text text_DownloadProcess;
    private Button btn_Download;
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
        btn_Pass = this.transform.Find("btn_Pass").GetComponent<Button>();
        //
        this.btn_Pass.onClick.AddListener(Onclick_PassAndLaunch);
        this.btn_Download.onClick.AddListener(Onclick_DownLoadAndLaunch);
        inputField.text = "127.0.0.1";
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
        //删除本地的文件
        var cachedir = IPath.Combine(Application.persistentDataPath, BDApplication.GetPlatformPath(Application.platform));
        if (Directory.Exists(cachedir))
        {
            Directory.Delete(cachedir, true);
        }

        var url = "http://" + this.inputField.text;
        AssetsVersionContorller.Start(UpdateMode.Repair, url, null,
            (i, j) =>
            {
                this.text_DownloadProcess.text = string.Format("{0}/{1}", i, j);
                //下载完毕
                if (i == j)
                {
                    this.text_DownloadProcess.text = "下载完毕";
                    //启动
                    BDLauncher.Inst.Launch(this.GetType().Assembly.GetTypes(), GameLogicCLRBinding.Bind);
                }
            }, //进度通知
            (e) => { this.text_DownloadProcess.text = e; }); //错误
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
