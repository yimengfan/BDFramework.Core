using System;
using System.Collections;

public class TabFile
{
    private string FileName;
    private string[] Title;
    private ArrayList Body;
    public int CurrentLine { get; private set; }

    public TabFile(string strFileName, string strContent)
    {
        FileName = strFileName;
        //Debugger.Log(FileName);

        string[] content = strContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        Title = content[0].Split(new char[] { '\t' });

        Body = new ArrayList();
        for (int i = 1; i < content.Length; i++)
        {
            string[] line = content[i].Split(new char[] { '\t' });
            Body.Add(line);
        }
        Body.TrimToSize();
        Begin();
    }

    public void Begin()
    {
        CurrentLine = -1;
    }

    public bool Next()
    {
        CurrentLine++;
        if (CurrentLine >= Body.Count)
            return false;
        return true;
    }

    public T Get<T>(string strColName)
    {
        int nColIndex = Array.IndexOf(Title, strColName);
        if (nColIndex < 0)
        {
            //Log.Write(LogLevel.ERROR, "TabFile: read data error, file:{0}, colName:{1}", FileName, strColName);
            return default(T);
        }
        return this.Get<T>(nColIndex);
    }

    public T Get<T>(int nColIndex)
    {
        string strValue = this.getValueString(nColIndex);
        Type t = typeof(T);
        try
        {
            return (T)Convert.ChangeType(strValue, t);
        }
        catch (Exception)
        {
            //Log.Write(LogLevel.ERROR, "TabFile: {0} Wrong Format: {1}:{2}", FileName, nColIndex, CurrentLine);
        }
        return default(T);
    }

    private string getValueString(int nColIndex)
    {
        string[] line = (string[])Body[CurrentLine];
        if (nColIndex < 0 || nColIndex >= line.Length)
        {
            //Log.Write(LogLevel.ERROR, "TabFile:" + FileName + " Wrong ColIndex: " + nColIndex + " At Line: " + CurrentLine);
            return null;
        }
        return line[nColIndex];
    }

    public string GetString(string strColName)
    {
        int nColIndex = Array.IndexOf(Title, strColName);
        return getValueString(nColIndex);
    }

    public int GetInt32(string strColName)
    {
        return GetInt32(Array.IndexOf(Title, strColName));
    }

    public int GetInt32(int nColIndex)
    {
        try
        {
            return Convert.ToInt32(getValueString(nColIndex));
        }
        catch (Exception)
        {
            //Log.Write(LogLevel.ERROR, "TabFile: " + FileName + "Wrong Format: " + nColIndex + " : " + CurrentLine);
            return 0;
        }
    }

    public uint GetUInt32(string strColName)
    {
        return GetUInt32(Array.IndexOf(Title, strColName));
    }

    public uint GetUInt32(int nColIndex)
    {
        try
        {
            return Convert.ToUInt32(getValueString(nColIndex));
        }
        catch (Exception)
        {
            //Log.Write(LogLevel.ERROR, "TabFile: " + FileName + "Wrong Format: " + nColIndex + " : " + CurrentLine);
            return 0;
        }
    }

    public double GetDouble(string strColName)
    {
        return GetDouble(Array.IndexOf(Title, strColName));
    }

    public double GetDouble(int nColIndex)
    {
        try
        {
            return Convert.ToDouble(getValueString(nColIndex));
        }
        catch (Exception)
        {
            //Log.Write(LogLevel.ERROR, "TabFile: " + FileName + "Wrong Format: " + nColIndex + " : " + CurrentLine);
            return 0;
        }
    }

    public float GetFloat(string strColName)
    {
        return (float)GetDouble(strColName);
    }

    public float GetFloat(int nColIndex)
    {
        return (float)GetDouble(nColIndex);
    }

    public int GetCount()
    {
        return Body.Count;
    }
}