using System;
using System.IO;
using BDFramework.Helper;
using ILRuntime.Reflection;
using LitJson;
//;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using UnityEngine;
using UnityEngine.Networking;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;


namespace BDFramework
{
  static  public class ILRuntimeHelper
  {
      public static AppDomain AppDomain { get; private set; }
      public static bool IsRunning { get; private set; }
      public static void LoadHotfix(string root)
      {
          if (AppDomain != null)
          {
              //AppDomain.FreeILIntepreter(AppDomain.);
          }
       
          //
          IsRunning = true;
          string dllPath = root +"/" + Utils.GetPlatformPath(Application.platform)  + "/hotfix/hotfix.dll";
          string pdbPath = root +"/" + Utils.GetPlatformPath(Application.platform)  + "/hotfix/hotfix.pdb";
          
          BDebug.Log("DLL加载路径:" + dllPath,"red");
          //
          AppDomain = new AppDomain();
          if ( File.Exists(pdbPath))
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
             
              UnityWebRequest request;
              using (System.IO.FileStream fs = new System.IO.FileStream(dllPath, FileMode.Open,FileAccess.Read))
              {
                  AppDomain.LoadAssembly(fs);
              }
          }


          //绑定的初始化
          AdapterRegister.RegisterCrossBindingAdaptor(AppDomain);
          ILRuntime.Runtime.Generated.CLRBindings.Initialize(AppDomain);
          ILRuntime.Runtime.Generated.CLRManualBindings.Initialize(AppDomain);
//          ILRuntime.Runtime.Generated.PreCLRBuilding.Initialize(AppDomain);
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