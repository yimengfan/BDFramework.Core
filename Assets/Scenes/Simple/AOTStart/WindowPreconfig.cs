using System.Collections;
using System.IO;
using System.Linq;
using BDFramework;
using BDFramework.Configure;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using Cysharp.Threading.Tasks;
using Game.Config;
using LitJson;
using Talos.E2E;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AOT 预配置界面。
/// AOT preconfiguration screen.
/// 该界面在资源下载与进入主流程前保留宿主侧可见入口，并在 Talos E2E 强制模式下由宿主显式桥接测试启动。
/// This screen keeps the host-owned visible entry before resource download and main-flow launch, and explicitly bridges Talos E2E startup from host code in forced mode.
/// </summary>
public class WindowPreconfig : MonoBehaviour
{
     private InputField inputField;
    private Text text_DownloadProcess;
    private Button btn_Download;
    private Button btn_DownloadRepair;
    private Button btn_GetSubPackage;
    private Button btn_Pass;
    private Button btn_ClearPersistent;
    ServerConfigProcessor.Config serverConfig;
    /// <summary>
    /// 预配置界面启动入口。
    /// Startup entry for the preconfiguration screen.
    /// 负责绑定 UI、加载服务器配置，并保持宿主预配置界面对业务启动前链路可见。
    /// It binds the UI, loads the server configuration, and keeps the host preconfiguration screen visible before the business launch chain proceeds.
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

        this.serverConfig = GameConfigManager.Inst.GetConfig<ServerConfigProcessor.Config>();
        if (Application.isEditor)
        {
            inputField.text = this.serverConfig .FileServerUrl;
        }
        else
        {
            inputField.text = this.serverConfig .FileServerUrl;
        }
        Debug.Log("FileServer:" + this.serverConfig.FileServerUrl);

        // Talos E2E 自动检测与启动。
        // 在宿主预配置界面完成基础初始化后，检测当前是否为 Debug 构建，
        // 若是则自动拉起 E2E TCP 监听服务，供外部 Playwright 测试驱动连接。
        // Auto-detect and launch Talos E2E. After the host preconfiguration screen
        // finishes its basic setup, check whether the current build is a debug build;
        // if so, automatically start the E2E TCP listener for external Playwright drivers.
        E2EAutoInit.CheckAndLaunch();
    }


    /// <summary>
    /// 点击按钮事件
    /// </summary>
    void Onclick_PassAndLaunch()
    {
        //启动
        BDLauncherHotfix.Launch();
        //
        this.StartCoroutine(IE_Destroy());
    }


    /// <summary>
    /// 下载且启动
    /// </summary>
    private void OnClick_DownLoadAndLaunch()
    {
        
        
        Debug.Log(BApplication.persistentDataPath);
        var url = serverConfig.FileServerUrl;
        float totalSize = -1;
        float curDoanloadSize = -1;

        BResources.StartAssetsVersionControl(UpdateMode.CompareWithRepairCoreAssets, url, null,
            (curDownload, allDownloadList) =>
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
                    case AssetsVersionController.RetStatus.SuccessNeedRestart:
                    {
                        this.text_DownloadProcess.text = "下载完毕，有代码更新，请重启游戏";
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
        Debug.Log(BApplication.persistentDataPath);
        var url = serverConfig.FileServerUrl;
        float totalSize = 0;
        float curDoanloadSize = 0;
        BResources.StartAssetsVersionControl(UpdateMode.RepairFull, url, null, (curDownload, allDownloadList) =>
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
                    case AssetsVersionController.RetStatus.SuccessNeedRestart:
                    {
                        this.text_DownloadProcess.text = "下载完毕，请重启游戏";
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
        var url = serverConfig.FileServerUrl;
        BResources.GetServerSubPacks(url, (map) =>
        {
            //获取到子包
            Debug.Log("获取到子包信息:\n" + JsonMapper.ToJson(map, true));

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

        var url = serverConfig.FileServerUrl;
        float totalSize = -1;
        float curDoanloadSize = -1;
        BResources.StartAssetsVersionControl(UpdateMode.CompareSimple, url, subPackageName,
            (curDownload, allDownloadList) =>
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
                    case AssetsVersionController.RetStatus.SuccessNeedRestart:
                    {
                        this.text_DownloadProcess.text = "下载完毕，请重启游戏";
                        Debug.Log("分包下载完毕，且包含 DLL 更新，请重启游戏后再进入。");
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
            var path = IPath.Combine(BApplication.persistentDataPath, BApplication.GetPlatformLoadPath(runtime));
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        //清理完毕
        var dirs = Directory.GetDirectories(Application.persistentDataPath, "*");
        Debug.Log(Application.persistentDataPath);
        Debug.Log("清理完毕~,剩余目录:" + dirs.Length);
    }
}
