using System;
using System.Reflection;
using UnityEngine;

namespace BDFramework.Editor.Unity3dEx
{
    /// <summary>
    /// 反射UnityShader相关辅助接口
    /// </summary>
    public class ShaderUtilImpl
    {
        delegate string[] GetShaderGlobalKeywords_type(Shader shader);

        static GetShaderGlobalKeywords_type GetShaderGlobalKeywords_impl;

        public static string[] GetShaderGlobalKeywords(Shader shader)
        {
            if (GetShaderGlobalKeywords_impl == null) GetShaderGlobalKeywords_impl = Delegate.CreateDelegate(typeof(GetShaderGlobalKeywords_type), typeof(UnityEditor.ShaderUtil).GetMethod("GetShaderGlobalKeywords", BindingFlags.Static | BindingFlags.NonPublic)) as GetShaderGlobalKeywords_type;
            return GetShaderGlobalKeywords_impl(shader) as string[];
        }

        delegate string[] GetAllGlobalKeywords_type();

        static GetAllGlobalKeywords_type GetAllGlobalKeywords_impl;

        public static string[] GetAllGlobalKeywords()
        {
            if (GetAllGlobalKeywords_impl == null) GetAllGlobalKeywords_impl = Delegate.CreateDelegate(typeof(GetAllGlobalKeywords_type), typeof(UnityEditor.ShaderUtil).GetMethod("GetAllGlobalKeywords", BindingFlags.Static | BindingFlags.NonPublic)) as GetAllGlobalKeywords_type;
            return GetAllGlobalKeywords_impl() as string[];
        }

        delegate string[] GetShaderLocalKeywords_type(Shader shader);

        static GetShaderLocalKeywords_type GetShaderLocalKeywords_impl;

        public static string[] GetShaderLocalKeywords(Shader shader)
        {
            if (GetShaderLocalKeywords_impl == null) GetShaderLocalKeywords_impl = Delegate.CreateDelegate(typeof(GetShaderLocalKeywords_type), typeof(UnityEditor.ShaderUtil).GetMethod("GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.NonPublic)) as GetShaderLocalKeywords_type;
            return GetShaderLocalKeywords_impl(shader) as string[];
        }

        delegate void GetShaderVariantEntriesFiltered_type(Shader shader, int maxEntries, string[] filterKeywords, ShaderVariantCollection excludeCollection, out int[] passTypes, out string[] keywordLists, out string[] remainingKeywords);

        static GetShaderVariantEntriesFiltered_type GetShaderVariantEntriesFiltered_impl;

        public static void GetShaderVariantEntriesFiltered(Shader shader, int maxEntries, string[] filterKeywords, ShaderVariantCollection excludeCollection, out int[] passTypes, out string[] keywordLists, out string[] remainingKeywords)
        {
            if (GetShaderVariantEntriesFiltered_impl == null) GetShaderVariantEntriesFiltered_impl = Delegate.CreateDelegate(typeof(GetShaderVariantEntriesFiltered_type), typeof(UnityEditor.ShaderUtil).GetMethod("GetShaderVariantEntriesFiltered", BindingFlags.Static | BindingFlags.NonPublic)) as GetShaderVariantEntriesFiltered_type;
            GetShaderVariantEntriesFiltered_impl(shader, maxEntries, filterKeywords, excludeCollection, out passTypes, out keywordLists, out remainingKeywords);
        }

        public struct ShaderVariantEntriesData
        {
            public int[]    passTypes;
            public string[] keywordLists;
            public string[] remainingKeywords;
        }

        delegate ShaderVariantEntriesData GetShaderVariantEntriesFilteredInternal_type(Shader shader, int maxEntries, string[] filterKeywords, ShaderVariantCollection excludeCollection);

        static GetShaderVariantEntriesFilteredInternal_type GetShaderVariantEntriesFilteredInternal_impl;

        public static ShaderVariantEntriesData GetShaderVariantEntriesFilteredInternal(Shader shader, int maxEntries, string[] filterKeywords, ShaderVariantCollection excludeCollection)
        {
            if (GetShaderVariantEntriesFilteredInternal_impl == null) GetShaderVariantEntriesFilteredInternal_impl = Delegate.CreateDelegate(typeof(GetShaderVariantEntriesFilteredInternal_type), typeof(UnityEditor.ShaderUtil).GetMethod("GetShaderVariantEntriesFilteredInternal", BindingFlags.Static | BindingFlags.NonPublic)) as GetShaderVariantEntriesFilteredInternal_type;
            return GetShaderVariantEntriesFilteredInternal_impl(shader, maxEntries, filterKeywords, excludeCollection);
        }
    }
}