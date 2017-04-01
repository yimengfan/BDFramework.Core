using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PathUtility
{
	//获取目录下特定名字的的所有目录
	static public void	GetAllDirInDir(string dirPath,string inclusiveName,List<string> result)
	{
		if (string.IsNullOrEmpty(inclusiveName))
			return ;
        var allfiles = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);

        foreach (var file in allfiles)
        {
            if (string.IsNullOrEmpty(file))
                continue;

            if (file.IndexOf(inclusiveName) != -1 //包含
                && file.IndexOf(".js")   == -1  &&  file.IndexOf(".cs") == -1//不包含
                && file.IndexOf(".meta") == -1
                )
            {
                result.Add(file);
            }
        }
	}

	//路径规范化
	public static string PathSeparatorNormalize(string path)
	{
		char[] bufStr = path.ToCharArray();
		
		for (int n = 0; n < path.Length; n++)
		{
			if (path[n] == '/' || path[n] == '\\')
				bufStr[n] = '/';
		}

		path = new string(bufStr);
		return path;
	}

	//组合路径
	public static string CombinePath(string path1, string path2)
	{
        UnityEngine.Debug.Log(path1+"   "+path2);
        UnityEngine.Debug.Log(Path.Combine(path1, path2));
		return PathSeparatorNormalize(Path.Combine(path1, path2));
	}

	//递归创建路径下的目录
	public static void CreateDirInPath(string path)
	{
		//如果是文件路径，去除文件名
		path = PathSeparatorNormalize(path);
		if(File.Exists(path))
		{
			path	= path.Substring(0,path.LastIndexOf('/'));
		}

		//开始创建路径
		string[] pathes = path.Split('/');
		if(pathes.Length > 1)
		{
			string temp = pathes[0];
			for(int i = 1; i < pathes.Length;i++)
			{
				temp += "/" + pathes[i];
				if(!Directory.Exists(temp))
					Directory.CreateDirectory(temp);
			}
		}
	}

	//获取目录下特定名字的的所有目录
	static public void	GetAllDirInDir(string dirPath,ref List<string> result)
	{
		if (dirPath[dirPath.Length-1] != '/' && dirPath[dirPath.Length-1] != '\\')
			dirPath = dirPath + "/";
		
		DirectoryInfo	dir				= new DirectoryInfo(dirPath);		
		// CheckDir
		if (dir.Exists == false)
			Debug.LogError("Directory not found - " + dirPath);
		
		foreach(DirectoryInfo childdir in dir.GetDirectories())
		{
			//if (childdir.Name != inclusiveName)
			if (!string.IsNullOrEmpty(childdir.Name))			
			{
				GetAllDirInDir(CombinePath(dirPath,childdir.Name),ref result);
				result.Add(childdir.FullName);
			}
			else
			{
				
			}
		}
		
		return ;
	}
}
