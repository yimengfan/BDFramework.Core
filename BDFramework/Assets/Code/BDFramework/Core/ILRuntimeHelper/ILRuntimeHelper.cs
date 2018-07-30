using System.IO;
using ILRuntime.Reflection;
using ILRuntime.Runtime.Enviorment;
using LitJson;
//;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using UnityEngine;


namespace BDFramework
{
  static  public class ILRuntimeHelper
  {
      public static AppDomain AppDomain { get; private set; }
      public static bool IsRunning { get; private set; }
      public static void LoadHotfix(bool isLoadPdb)
      {
          string dllPath = "hotfix/hotfix.dll";
          string pdbPath = "hotfix/hotfix.pdb";

          IsRunning = true;
          #if UNITY_EDITOR
          dllPath =  Path.Combine(Application.streamingAssetsPath, dllPath);
          pdbPath =  Path.Combine(Application.streamingAssetsPath, pdbPath);
          #elif UNITY_IPHONE || UNITY_ANDROID
          dllPath =  Path.Combine(Application.persistentDataPath, dllPath);
          pdbPath =  Path.Combine(Application.persistentDataPath, pdbPath);           
          #endif

        
      
          
          AppDomain = new AppDomain();
          if (isLoadPdb)
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
          //ILRuntime.Runtime.Generated.CLRBindings.Initialize(AppDomain);
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