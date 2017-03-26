using System;
using System.IO;

public class ResConfigFile
{
	public static void GenFile(string fileFullName,string content)
	{
		if(File.Exists(fileFullName))
		{
			File.Delete(fileFullName);
		}

		FileStream fs = new FileStream(fileFullName,FileMode.Create);
		StreamWriter mySw = new StreamWriter(fs);
		mySw.Write(content);
		mySw.Close();
		fs.Close();
	}

	public static string ContentHelpWrite(int id,string content)
	{
		string final = string.Format("{0}\t{1}\r\n",id,content);
		return final;
	}
}
