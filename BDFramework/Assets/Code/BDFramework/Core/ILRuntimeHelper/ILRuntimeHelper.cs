using System;
using System.IO;
using BDFramework.Helper;
using ILRuntime.Reflection;
using LitJson;
//;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using UnityEngine;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;


namespace BDFramework
{
  static  public class ILRuntimeHelper
  {
      public static AppDomain AppDomain { get; private set; }
      public static bool IsRunning { get; private set; }
      public static void LoadHotfix(bool isLoadPdb)
      {
          IsRunning = true;
          string dllPath = Utils.ResourcePlatformPath  + "/hotfix/hotfix.dll";
          string pdbPath =  Utils.ResourcePlatformPath + "/hotfix/hotfix.pdb";
          var _dllPath =  Path.Combine(Application.persistentDataPath, dllPath);
          
          //加载路径
          dllPath = File.Exists(_dllPath)? _dllPath:  Path.Combine(Application.streamingAssetsPath, dllPath);
          pdbPath = File.Exists(_dllPath)?   Path.Combine(Application.streamingAssetsPath,pdbPath)
                                         :   Path.Combine(Application.streamingAssetsPath, pdbPath);
          //
          AppDomain = new AppDomain();
          if (isLoadPdb && File.Exists(pdbPath))
          {
              var dllfs = File.ReadAllBytes(dllPath);
              var pdbfs = File.ReadAllBytes(pdbPath);
              using (MemoryStream dll = new MemoryStream(dllfs))
              {
                  using (MemoryStream pdb = new MemoryStream(pdbfs))
                  {
                      AppDomain.LoadAssembly(dll, pdb, new PdbReaderProvider());
                  }
              }
          }
          else
          {
              using (System.IO.FileStream fs = new System.IO.FileStream(dllPath, FileMode.Open,FileAccess.Read))
              {
                  AppDomain.LoadAssembly(fs);
              }
          }


          //绑定的初始化
          AdapterRegister.RegisterCrossBindingAdaptor(AppDomain);
          ILRuntime.Runtime.Generated.CLRBindings.Initialize(AppDomain);
          ILRuntime.Runtime.Generated.CLRManualBindings.Initialize(AppDomain);
          //
          ILRuntimeDelegateHelper.Register(AppDomain);
          JsonMapper.RegisterILRuntimeCLRRedirection(AppDomain);
          if (Application.isEditor)
          {
             AppDomain.DebugService.StartDebugService(56000);
             Debug.Log("热更调试器 准备待命~");
          }
          //
          AppDomain.Invoke("HotfixCheck", "Log",null,null);
      }
  }
}