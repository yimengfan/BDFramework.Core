using System;
using System.Collections.Generic;
using BDFramework.ResourceMgr.V2;
using UnityEngine;
using UnityEngine.U2D;

namespace BDFramework.ResourceMgr
{
  
    static public class AssetConfig
    {
        /// <summary>
        /// 资源类型配置
        /// </summary>
        static public Dictionary<Type, List<string>> AssetTypeConfigMap = new Dictionary<Type, List<string>>()
        {
            
            {typeof(GameObject), new List<string>() {".prefab"}},                                                 //Prefab
            {typeof(SpriteAtlas), new List<string>() {".spriteatlas"}},                                           //Atlas
            {typeof(Texture), new List<string>() {".jpg", ".jpeg", ".png", ".tga",".psd",".bmp",".iff",".pict"}}, //Tex
            {typeof(Material), new List<string>() {".mat"}},                                                      //mat
            {typeof(Shader), new List<string>() {".shader"}},                                                     //mat
            {typeof(TextAsset), new List<string>() {".json", ".xml", ".info", ".txt"}},                           //TextAsset
            {typeof(AudioClip), new List<string>() {".mp3", ".ogg", ".wav"}},                                     //sound
            {typeof(Mesh), new List<string>() {".mesh"}},                                                         //mesh
            {typeof(Font), new List<string>() {".otf",".fnt", ".fon", ".font", ".ttf", ".ttc",  ".eot",}},        //sound
        };
        
        
        /// <summary>
        /// 资源类型配置
        /// </summary>
        static public Dictionary<AssetBundleItem.AssetTypeEnum, List<string>> AssetEnumTypeConfigMap = new Dictionary<AssetBundleItem.AssetTypeEnum, List<string>>()
        {
            {AssetBundleItem.AssetTypeEnum.Prefab, new List<string>() {".prefab"}},                                                     //Prefab
            {AssetBundleItem.AssetTypeEnum.SpriteAtlas, new List<string>() {".spriteatlas"}},                                           //Atlas
            {AssetBundleItem.AssetTypeEnum.Texture, new List<string>() {".jpg", ".jpeg", ".png", ".tga",".psd",".bmp",".iff",".pict"}}, //Tex
            {AssetBundleItem.AssetTypeEnum.Mat, new List<string>() {".mat"}},                                                           //mat
            {AssetBundleItem.AssetTypeEnum.Shader, new List<string>() {".shader"}},                                                     //mat
            {AssetBundleItem.AssetTypeEnum.TextAsset, new List<string>() {".json", ".xml", ".info", ".txt"}},                           //TextAsset
            {AssetBundleItem.AssetTypeEnum.AudioClip, new List<string>() {".mp3", ".ogg", ".wav"}},                                     //sound
            {AssetBundleItem.AssetTypeEnum.Mesh, new List<string>() {".mesh"}},                                                         //mesh
            {AssetBundleItem.AssetTypeEnum.Font, new List<string>() {".otf",".fnt", ".fon", ".font", ".ttf", ".ttc",  ".eot",}},        //sound
        };
    }
}