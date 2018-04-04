using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;


public class Sha1
{
    public static SHA1 sha1 = null;
    public static string CalcSHA1String(byte[] data)
    {
        if (sha1 == null)
            sha1 = SHA1.Create();

        byte[] hash = sha1.ComputeHash(data);

        string shastr = "";
        foreach (var h in hash)
        {
            shastr += h.ToString("X02");
        }
        return shastr;
    }
}