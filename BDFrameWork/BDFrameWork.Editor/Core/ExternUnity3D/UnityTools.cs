using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
public static class UnityTools
{
    public static Vector2 DesignSize = new Vector2(1920f, 1080f);
   public static Vector2 TouchToScreenPos(Vector2 pos)
   {
        var x = (pos.x / Screen.width  - 0.5f) * DesignSize.x;
        var y = (pos.y / Screen.height - 0.5f) * DesignSize.y;

        return new Vector2(x,y);
   }

    public static Vector2 TouchToScreenPos(Vector2 pos ,Vector2 DesignSize)
    {
        var x = (pos.x / Screen.width - 0.5f) * DesignSize.x;
        var y = (pos.y / Screen.height - 0.5f) * DesignSize.y;

        return new Vector2(x, y);
    }
}

