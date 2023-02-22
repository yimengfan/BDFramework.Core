using System.Collections;
using System.IO;
using BDFramework;
using BDFramework.VersionController;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.VersionController;
using Game.ILRuntime;
using LitJson;
using UnityEngine;
using UnityEngine.UI;

public class WindowPreconfig : MonoBehaviour
{
    private InputField inputField;
    private Text text_DownloadProcess;
    private Button btn_Download;
    private Button btn_DownloadRepair;
    private Button btn_GetSubPackage;
    private Button btn_Pass;
    private Button btn_ClearPersistent;

    /// <summary>
    /// 开始
    /// </summary>
    void Start()
    {
        //节点发现
        inputField = this.transform.Find("InputField").GetComponent<InputField>();
        text_DownloadProcess = this.transform.Find("text_DownloadProcess").GetComponent<Text>();
        btn_Download = this.transform.Find("btn_Download").GetComponent<Button>();
        btn_DownloadRepair = this.transform.Find("btn_DownloadRepair").GetComponent<Button>();
        btn_GetSubPackage = this.transform.Find("btn_GetSubPackage").GetComponent<Button>();
        btn_Pass = this.transform.Find("btn_Pass").GetComponent<Button>();
        btn_ClearPersistent = this.transform.Find("btn_ClearPersistent").GetComponent<Button>();
        //
        this.btn_Pass.onClick.AddListener(Onclick_PassAndLaunch);
        this.btn_Download.onClick.AddListener(OnClick_DownLoadAndLaunch);
        this.btn_DownloadRepair.onClick.AddListener(Onclick_Download_RepairMode);
        this.btn_GetSubPackage.onClick.AddListener(OnClick_GetSubPackage);
        this.btn_ClearPersistent.onClick.AddListener(OnClick_ClearPersistent);
        //
        if (Application.isEditor)
        {
            inputField.text = "127.0.0.1:10086/AssetBundle";
        }
        else
        {
            inputField.text = "192.168.0.1:10086/AssetBundle";
        }
    }


    /// <summary>
    /// 点击按钮事件
    /// </summary>
    void Onclick_PassAndLaunch()
    {
        //直接启动
        BDLauncher.Inst.Launch( GameLogicCLRBinding.Bind);
        //
        this.StartCoroutine(IE_Destroy());
    }


    /// <summary>
    /// 下载且启动
    /// </summary>
    private void OnClick_DownLoadAndLaunch()
    {
        Debug.Log(Application.persistentDataPath);
        var url = "http://" + this.inputField.text;
        float totalSize = -1;
        float curDoanloadSize = -1;
        BResources.StartAssetsVersionControl(UpdateMode.CompareWithRepair, url, null, (curDownload, allDownloadList) =>
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
                    case AssetsVersionController.RetStatus.Success:
                    {
                        this.text_DownloadProcess.text = "下载完毕";
                        //启动
                       // this.Onclick_PassAndLaunch();
                    }
                        break;
                    case AssetsVersionController.RetStatus.Error:
                    {
                        //错误
                        this.text_DownloadProcess.text = msg;
                    }
                        break;
                }
            });
    }

    /// <summary>
    /// 下载资源-修复模式
    /// </summary>
    private void Onclick_Download_RepairMode()
    {
        Debug.Log(Application.persistentDataPath);
        var url = "http://" + this.inputField.text;
        float totalSize =0;
        float curDoanloadSize = 0;
        BResources.StartAssetsVersionControl(UpdateMode.Repair, url, null, (curDownload, allDownloadList) =>
        {
            if (totalSize == 0)
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
                case AssetsVersionController.RetStatus.Success:
                {
                    this.text_DownloadProcess.text = "下载完毕";
                    //启动
                    // this.Onclick_PassAndLaunch();
                }
                    break;
                case AssetsVersionController.RetStatus.Error:
                {
                    //错误
                    this.text_DownloadProcess.text = msg;
                }
                    break;
            }
        });
    }
    
    
    /// <summary>
    /// 获取子包
    /// </summary>
    private void OnClick_GetSubPackage()
    {
        var url = "http://" + inputField.text;
        BResources.GetServerSubPacks(url, (map) =>
        {
            //获取到子包
            Debug.Log("获取到子包信息:\n" + JsonMapper.ToJson(map,true));

            //全隐藏
            var grid = this.transform.Find("grid_SubPack");
            foreach (Transform child in grid)
            {
                child.gameObject.SetActive(false);
            }
            //显示
            var idx = 0;
            foreach (var kv in map)
            {
                var btn = grid.GetChild(idx)?.GetComponent<Button>();
                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();
                btn.transform.GetChild(0).GetComponent<Text>().text = "下载子包:" + kv.Key;
                //添加监听
                btn.onClick.AddListener(() =>
                {
                    //下载
                    this.Onclick_DownloadSubPackageLoadAndLaunch((kv.Key));
                });
                   
                idx++;
            }
            
        });
        
    }
    
    
    /// <summary>
    /// 下载分包且启动
    /// </summary>
    private void Onclick_DownloadSubPackageLoadAndLaunch(string subPackageName)
    {
        Debug.Log(Application.persistentDataPath);

        var url = "http://" + this.inputField.text;
        float totalSize = -1;
        float curDoanloadSize = -1;
        BResources.StartAssetsVersionControl(UpdateMode.Compare, url, subPackageName, (curDownload, allDownloadList) =>
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
                //进度通知`
                this.text_DownloadProcess.text = string.Format("{0}KB / {1}KB", curDoanloadSize, totalSize);
            },
            (status, msg) =>
            {
                switch (status)
                {
                    case AssetsVersionController.RetStatus.Success:
                    {
                        this.text_DownloadProcess.text = "下载完毕";
                        
                        Debug.Log("分包下载完毕,此时资源不全,进入游戏可能会有bug!");
                    }
                        break;
                    case AssetsVersionController.RetStatus.Error:
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

    /// <summary>
    /// 清理persistent
    /// </summary>
    private void OnClick_ClearPersistent()
    {
        foreach (var runtime in BApplication.SupportPlatform)
        {
            var path = IPath.Combine(Application.persistentDataPath, BApplication.GetPlatformPath(runtime));
            if (Directory.Exists(path))
            {
                Directory.Delete(path,true);
            }
        }
        //清理完毕
        var dirs = Directory.GetDirectories(Application.persistentDataPath, "*");
        Debug.Log(Application.persistentDataPath);
        Debug.Log("清理完毕~,剩余目录:" + dirs.Length);
    }
}
