using UnityEngine;
using UnityEditor;
using System.IO;
using BDFramework.Editor.Asset;
using BDFramework.Editor.BuildPackage;
using BDFramework.Helper;
using BDFramework.Editor.UI;
using BDFramework.Editor.TableData;
namespace BDFramework.Editor
{


    public enum BDEditorMenuEnum
    {
        BDSetting = 1,
        UIMVCTools=51,
        BuildPackage_DLL=52,
        BuildPackage_Assetbundle=53,
        BuildPackage_Table_Table2Class=54,
        BuildPackage_Table_GenSqlite=55,
        BuildPackage_Table_Json2Sqlite=56,
        //
        OnekeyBuildAsset =101,
        
    }
    
}