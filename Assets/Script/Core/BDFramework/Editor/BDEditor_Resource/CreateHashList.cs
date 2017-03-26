using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;
using System.Xml.Serialization;

[Serializable]
[XmlRoot("Files")]
public class AssetInfo
{
    [XmlAttribute("FileName")]
	public string fileName;
    [XmlAttribute("Hash128")]
	public string hash128;
}

public class CreateHashList
{
    private static AssetBundleManifest m_abmanifest;
    private static IEnumerator m_en = null;
    private static bool m_run = false;

    static Action<bool> successCallback;
    static IEnumerator StartLoadManifest(UnityEditor.BuildTarget target)
    {
      
        string platform = AssetBundleController.GetPlatformName(target);
        string dir = "file:///" + System.IO.Path.Combine(Application.dataPath, "AssetBundle/" + platform) + "AllResources";

        WWW www = new WWW(dir);
        yield return www;

        if (!string.IsNullOrEmpty(www.error))
            yield break;

        UnityEngine.Object[] obj = www.assetBundle.LoadAllAssets();

        m_abmanifest = obj[0] as AssetBundleManifest;

        if (m_abmanifest != null) {
            Execute(platform);
        }
        www.assetBundle.Unload(true);
        if (successCallback != null)
        {
            successCallback(true);
        }
        //Debug.Log("Create Hash File Success");
       // EditorUtility.DisplayDialog("", "生成资源Hash文件完成", "OK");
    }

    public static void StartLoad(Action<bool> action)
    {
        if (m_run) return;
        m_run = true;
        successCallback = action;
    }
    public static void Update(UnityEditor.BuildTarget target)
    {

        if (!m_run)
        {
            if (m_en != null)
                m_en = null;
            return;
        }
        if (m_en == null)
        {
            m_en = StartLoadManifest(target);
        }
        if (!m_en.MoveNext())
            m_run = false;

    }

	public static void Execute(string platform)
	{

       
        SortedList<string, AssetInfo> fileHaskList = new SortedList<string, AssetInfo>();

        string[] allABNames = m_abmanifest.GetAllAssetBundles();
        for (int i = 0; i < allABNames.Length; i++)
        {
            string curABName = allABNames[i];
            if (string.IsNullOrEmpty(curABName))
                continue;

            Hash128 hash = m_abmanifest.GetAssetBundleHash(curABName);

            if (!fileHaskList.ContainsKey(curABName))
            {
                AssetInfo ai = new AssetInfo();
                ai.fileName = curABName;
                ai.hash128 = hash.ToString();
                fileHaskList.Add(curABName, ai);
            }
            else
                Debug.Log("<Two File has the same name> name = " + curABName);
        }

        string savePath = System.IO.Path.Combine(Application.dataPath, "AssetBundle/") + platform + "/VersionNum";
        if (Directory.Exists(savePath) == false)
            Directory.CreateDirectory(savePath);

        if (File.Exists(savePath + "/VersionHash-old.xml"))
        {
            System.IO.File.Delete(savePath + "/VersionHash-old.xml");
        }

        if (File.Exists(savePath + "/VersionHash.xml"))
        {
            System.IO.File.Move(savePath + "/VersionHash.xml", savePath + "/VersionHash-old.xml");
        }

        XmlDocument xmlDoc = new XmlDocument();
        XmlElement xmlRoot = xmlDoc.CreateElement("Files");
        xmlDoc.AppendChild(xmlRoot);

        XmlElement xmlElemTemp = xmlDoc.CreateElement("File");
        xmlRoot.AppendChild(xmlElemTemp);
        xmlElemTemp.SetAttribute("FileName", "AllResources");
        xmlElemTemp.SetAttribute("Hash128", "");

        foreach (KeyValuePair<string, AssetInfo> pair in fileHaskList)
        {
            XmlElement xmlElem = xmlDoc.CreateElement("File");
            xmlRoot.AppendChild(xmlElem);

            AssetInfo curAI = pair.Value;
            xmlElem.SetAttribute("FileName", pair.Key);
            xmlElem.SetAttribute("Hash128", curAI.hash128);
        }
        xmlDoc.Save(savePath + "/VersionHash.xml");
        xmlDoc = null;
        AssetDatabase.Refresh();
	}

    public static SortedList<string, AssetInfo> ReadHashFile(string fileName)
	{
		SortedList<string, AssetInfo> DicHash = new SortedList<string, AssetInfo>();

		if (System.IO.File.Exists(fileName) == false)
			return DicHash;
		
		XmlDocument XmlDoc = new XmlDocument();
		XmlDoc.Load(fileName);
		XmlElement XmlRoot = XmlDoc.DocumentElement;
		
		foreach (XmlNode node in XmlRoot.ChildNodes)
		{
			if ((node is XmlElement) == false)
				continue;

			AssetInfo info = new AssetInfo();
			info.fileName = (node as XmlElement).GetAttribute("FileName");
            info.hash128 = (node as XmlElement).GetAttribute("Hash128");
			
			if (DicHash.ContainsKey(info.fileName) == false)
			{
				DicHash.Add(info.fileName, info);
			}
		}
		
		XmlRoot = null;
		XmlDoc = null;
		
		return DicHash;
	}
	
}