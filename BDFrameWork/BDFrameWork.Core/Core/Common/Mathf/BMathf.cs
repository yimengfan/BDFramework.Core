using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class BMathf
{
    #region 数学精度操作
   
    static public double Accuracy2(double d)
    {
        return Math.Round(d, 2);
    }
    static public double Accuracy3(double d)
    {
        return Math.Round(d, 3);
    }
    static public double Accuracy4(double d)
    {
        return Math.Round(d, 4);
    }
    #endregion  
}

