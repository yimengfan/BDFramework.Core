using System.Collections.Generic;

public interface IVersionData
{
    Dictionary<string,string> FileInfoMap { get; }
    List<string> CompareWithOther(IVersionData data);
}
/// <summary>
/// 
/// </summary>
public class FileHashData
{
    public string fileName;
    public string hash;
}

public class VersionDataBase : IVersionData
{
    public VersionDataBase()
    {
        this.FileInfoMap = new Dictionary<string, string>();
    }
    public Dictionary<string,string> FileInfoMap
    {
        get;
        private set;
    }

    public List<string> CompareWithOther(IVersionData data)
    {
        List<string> list = new List<string>();

        foreach (var v in data.FileInfoMap)
        {
            string fhd = null;
            this.FileInfoMap.TryGetValue(v.Key, out fhd);
            //本地存在,比较hash
            if (fhd != null)
            {
                if(fhd   !=  v.Value)
                {
                    list.Add(v.Key);
                }
            }
            //本地不存在
            else
            {
                list.Add(v.Key);
            }
        }
        BDeBug.I.Log("需要更新个数:" + list.Count);
        return list;
    }
}
/// <summary>
/// 
/// </summary>
public class VersionData_Table : VersionDataBase
{

}

public class VersionData_Art : VersionDataBase
{
}

public class VersionData_Code : VersionDataBase
{
}
