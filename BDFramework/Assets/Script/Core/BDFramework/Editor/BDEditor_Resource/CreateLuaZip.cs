using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
//using Ionic.Zip;
//using UGameClient;
using System.Diagnostics;
using System;
using System.Text;

public class CreateLuaZip
{
    public static void Execute()
	{
        FileUtil.DeleteFileOrDirectory(Application.dataPath + "/temp");
        string gameTempPath = Application.dataPath+"/temp/Luas";
        string tempPath = Application.dataPath + "/temp/Lua";
        string inPath = Application.dataPath + "/uLua/Lua";
        string gameInPath = Application.dataPath + "/Luas";
		string outPath = Application.dataPath + "/StreamingAssets/Lua.bytes";
        //ZipFile zip = new ZipFile();
        //UGUtil.MakeSureDir( tempPath);
        FileUtil.CopyFileOrDirectory(inPath, tempPath);
        FileUtil.CopyFileOrDirectory(gameInPath, gameTempPath);

        RemoveMetaFiles(tempPath);
        RemoveMetaFiles(gameTempPath);

        if (AssetBundleController.encodeLuaFile) {
            EncodeLuaFiles(Application.dataPath + "/temp");
            if (AssetBundleController.buildTarget == BuildTarget.iOS)
            {
                EncodeLuacFiles(Application.dataPath + "/temp");
            }
        }

        //zip.AddDirectory(tempPath, "uLua/Lua");
        //zip.AddDirectory(gameTempPath,"Luas");
        //zip.Save(outPath);
        FileUtil.DeleteFileOrDirectory( Application.dataPath+"/temp");
        AssetDatabase.Refresh();
	}
    static void EncodeLuaFile(string srcFile, string outFile)
    {
     
        string AppDataPath = Application.dataPath;
        string luaexe = string.Empty;
        string args = string.Empty;
        string exedir = string.Empty;
        string currDir = Directory.GetCurrentDirectory();
        bool isWin = false;
        if (AssetBundleController.buildTarget == BuildTarget.StandaloneWindows || AssetBundleController.buildTarget == BuildTarget.Android)
        {
            luaexe = "luajit.exe";
            args = "-b " + srcFile + " " + outFile;
            exedir = AppDataPath.Replace("Assets", "") + "LuaEncoder/luajit/";
            isWin = true;
        }
        else if (AssetBundleController.buildTarget == BuildTarget.iOS)
        {
            luaexe = "./luac";
            args = "-o " + outFile + " " + srcFile;
            exedir = AppDataPath.Replace("Assets", "") + "LuaEncoder/luavm/";
            isWin = false;
        }
        Directory.SetCurrentDirectory(exedir);
        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = luaexe;
        info.Arguments = args;
        info.WindowStyle = ProcessWindowStyle.Hidden;
        info.UseShellExecute = isWin;
        info.ErrorDialog = true;
        //Util.Log(info.FileName + " " + info.Arguments);

        Process pro = Process.Start(info);
        pro.WaitForExit();
        Directory.SetCurrentDirectory(currDir);
    }

    public static void EncodeLuaFiles(string luaDir)
    {
        string[] fileNames = Directory.GetFiles(luaDir, "*.lua", SearchOption.AllDirectories);
        for (int i = 0; i < fileNames.Length; i++)
        {
            EncodeLuaFile(fileNames[i], fileNames[i]);
        }
    }
    public static byte[] encodeKey = Encoding.UTF8.GetBytes("youzudaxiastudio");

    private static void EncodeLuacFiles(string luaDir)
    {
        string[] fileNames = Directory.GetFiles(luaDir, "*.lua", SearchOption.AllDirectories);
        for (int i = 0; i < fileNames.Length; i++)
        {
            EncodeLuacFile(fileNames[i], fileNames[i]);
        }
    }
    private static void EncodeLuacFile(string sourceFile,string outFile) {
        //byte[] str = null;
        //if (File.Exists(sourceFile)) {
        //    str = File.ReadAllBytes(sourceFile);
        //    byte[] encodedBytes = Xxtea.XXTEA.Encrypt(str, encodeKey);
        //    File.WriteAllBytes(outFile, encodedBytes);
        //}
            
    }



    private static void RemoveMetaFiles(string dirPath) {
        //delete .svn directory.  just for mac version.
        string[] dirNames = Directory.GetDirectories(dirPath,"*.svn",SearchOption.AllDirectories);
        int filesLen = dirNames.Length;
        for (int i = 0; i < filesLen; i++)
        {
            Directory.Delete(dirNames[i],true);
        }
        string[] fileNames = Directory.GetFiles(dirPath,"*.*",SearchOption.AllDirectories);
        filesLen = fileNames.Length;
        for (int i = 0; i < filesLen; i++)
        {
            if (!(fileNames[i].ToLower().EndsWith(".lua")) && !(fileNames[i].ToLower().EndsWith(".pb")))
            {
                File.Delete(fileNames[i]);
            }
        }
    }
	public static void ExecuteUnZip()
	{
  //      string destUnzipFolder = Application.dataPath + "/StreamingAssets/";
		//string zipFile = Application.dataPath + "/StreamingAssets/Lua.bytes";

  //      ZipFile zip = new ZipFile(zipFile);
  //      zip.ExtractAll(destUnzipFolder, ExtractExistingFileAction.OverwriteSilently);
  //      AssetDatabase.Refresh();
	}

}
