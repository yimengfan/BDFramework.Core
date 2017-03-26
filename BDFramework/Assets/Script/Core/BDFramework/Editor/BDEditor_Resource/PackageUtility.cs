using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PackageUtility
{
	//获取目录下特定名字的的所有目录
	static public void	GetAllDirInDir(string dirPath,string inclusiveName,string exclusiveName,ref List<string> result)
	{
		if (string.IsNullOrEmpty(inclusiveName))
			return ;

		if (dirPath[dirPath.Length-1] != '/' && dirPath[dirPath.Length-1] != '\\')
			dirPath = dirPath + "/";

		DirectoryInfo	dir				= new DirectoryInfo(dirPath);		
		// CheckDir
		if (dir.Exists == false)
			Debug.LogError("Directory not found - " + dirPath);

		foreach(DirectoryInfo childdir in dir.GetDirectories())
		{
			if(!string.IsNullOrEmpty(exclusiveName) && childdir.Name.Contains(exclusiveName))
				continue;

			if (childdir.Name != inclusiveName)
			{
				GetAllDirInDir(CombinePath(dirPath,childdir.Name),inclusiveName,exclusiveName,ref result);
			}
			else
			{
				result.Add(childdir.Parent.FullName);
			}
		}

		return ;
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
