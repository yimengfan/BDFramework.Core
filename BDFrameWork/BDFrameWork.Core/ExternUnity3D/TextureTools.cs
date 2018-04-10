using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

namespace BDFramework.Tools
{
    static public class TextureTools
    {
        // Use this for initialization
        static public Texture2D Merge(Image mainImage, Image[] subImages)
        {
            var mainTex = mainImage.sprite.texture;
            var mainTexSize = mainImage.sprite.textureRect;
            var mainColorArray = mainTex.GetPixels();
            //
            foreach (var image in subImages)
            {
                var subTex = image.sprite.texture;
                var subSize = image.sprite.textureRect;
                var subColorArray = subTex.GetPixels();
                //bg.rectTransform.pivot = Vector2.zero;
                int startX = (int)image.rectTransform.anchoredPosition.x;
                int startY = (int)image.rectTransform.anchoredPosition.y;
                int width = (int)subSize.width; 
                int height = (int)subSize.height;
                //
                for (int i = 0; i <= height; i++) //纵
                {
                    for (int j = 0; j <= width; j++) //横
                    {
                        var subIndex = width * i + j;
                        //
                        if (subIndex >= subColorArray.Length)
                            break;
                        var mainIndex = ((int)mainTexSize.width) * (startY + i) + (startX + j);
                        mainColorArray[mainIndex] = subColorArray[subIndex];
                    }
                }
            }

            Texture2D tex = new Texture2D((int)mainTexSize.width, (int)mainTexSize.height);
            tex.SetPixels(mainColorArray);
            tex.Apply();

            return tex;
        }

    }
}