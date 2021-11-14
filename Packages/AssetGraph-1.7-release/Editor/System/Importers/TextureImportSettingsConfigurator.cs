using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.AssetGraph
{
    [Serializable]
    [CustomAssetImporterConfigurator(typeof(TextureImporter), "Texture", "setting.png")]
    public class TextureImportSettingsConfigurator : IAssetImporterConfigurator
    {
        [SerializeField]
        private bool m_overwritePackingTag;

        [SerializeField]
        private bool m_overwriteSpriteSheet;

        [SerializeField]
        private SerializableMultiTargetString m_customPackingTagTemplate;

        public void Initialize(ConfigurationOption option)
        {
            m_overwritePackingTag      = option.overwritePackingTag;
            m_overwriteSpriteSheet     = option.overwriteSpriteSheet;
            m_customPackingTagTemplate = option.customPackingTagTemplate;
        }

        public bool IsModified(AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as TextureImporter;
            var t = importer as TextureImporter;
            if (t == null || r == null)
            {
                //Debug.LogWarning($"Invalid AssetImporter assigned for {referenceImporter.}");
            }

            return !IsEqual(t, r, GetTagName(target, group));
        }

        public void Configure(AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as TextureImporter;
            var t = importer as TextureImporter;
            if (t == null || r == null)
            {
                throw new AssetGraphException($"Invalid AssetImporter assigned for {importer.assetPath}");
            }

            OverwriteImportSettings(t, r, GetTagName(target, group));
        }

        public void OnInspectorGUI(AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged)
        {
            var importer = referenceImporter as TextureImporter;
            if (importer == null)
            {
                return;
            }

            string content = @"目标图片的 
TextureType
PixelsPerUnit 
GeneratePhyiscsShape 
Read/Write Enable 
GenerateMipMaps 
多平台图片质量设置 
等属性将被锁定";
            GUILayout.Label(content);

            if (importer.textureType == TextureImporterType.Sprite)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Sprite Settings");
                    GUILayout.Space(4f);
                    var bSpriteSheet = EditorGUILayout.ToggleLeft("Configure Sprite Mode", m_overwriteSpriteSheet);
                    var bPackingTag  = EditorGUILayout.ToggleLeft("Configure Sprite Packing Tag", m_overwritePackingTag);

                    if (bSpriteSheet != m_overwriteSpriteSheet || bPackingTag != m_overwritePackingTag)
                    {
                        m_overwriteSpriteSheet = bSpriteSheet;
                        m_overwritePackingTag  = bPackingTag;
                        onValueChanged();
                    }

                    if (m_overwritePackingTag)
                    {
                        if (m_customPackingTagTemplate == null)
                        {
                            m_customPackingTagTemplate = new SerializableMultiTargetString();
                        }

                        var val = m_customPackingTagTemplate.DefaultValue;

                        var newValue = EditorGUILayout.TextField("Packing Tag", val);
                        if (newValue != val)
                        {
                            m_customPackingTagTemplate.DefaultValue = newValue;
                            onValueChanged();
                        }
                    }

                    EditorGUILayout.HelpBox("You can configure packing tag name with \"*\" to include group name in your sprite tag.", MessageType.Info);
                }
            }
        }

        private string GetTagName(BuildTarget target, string groupName)
        {
            return m_customPackingTagTemplate[target].Replace("*", groupName);
        }

        private void ApplySpriteTag(BuildTarget target, IEnumerable<PerformGraph.AssetGroups> incoming)
        {
            foreach (var ag in incoming)
            {
                foreach (var groupKey in ag.assetGroups.Keys)
                {
                    var assets = ag.assetGroups[groupKey];
                    foreach (var asset in assets)
                    {
                        if (asset.importerType == typeof(UnityEditor.TextureImporter))
                        {
                            var importer = AssetImporter.GetAtPath(asset.importFrom) as TextureImporter;

                            importer.spritePackingTag = GetTagName(target, groupKey);
                            importer.SaveAndReimport();
                            asset.TouchImportAsset();
                        }
                    }
                }
            }
        }

        private bool IsEqual(TextureImporter target, TextureImporter reference, string tagName)
        {
            // UnityEditor.TextureImporter.textureFormat' is obsolete: 
            // `textureFormat is not longer accessible at the TextureImporter level
            if (target == null)
            {
                //空资源跳过不设置
                return false;
            }

            if (reference == null || target.textureType != reference.textureType) return false;

            TextureImporterSettings targetSetting    = new TextureImporterSettings();
            TextureImporterSettings referenceSetting = new TextureImporterSettings();

            target.ReadTextureSettings(targetSetting);
            reference.ReadTextureSettings(referenceSetting);


            if (targetSetting.aniso                              != referenceSetting.aniso) return false;
            if (targetSetting.compressionQuality                 != referenceSetting.compressionQuality) return false;
            if (targetSetting.spriteGenerateFallbackPhysicsShape != referenceSetting.spriteGenerateFallbackPhysicsShape) return false;
            if (targetSetting.spritePixelsPerUnit                != referenceSetting.spritePixelsPerUnit) return false;
            if (targetSetting.npotScale                          != referenceSetting.npotScale) return false;
            if (targetSetting.readable                           != referenceSetting.readable) return false;
            if (targetSetting.streamingMipmapsPriority           != referenceSetting.streamingMipmapsPriority) return false;
            if (targetSetting.mipmapEnabled                      != referenceSetting.mipmapEnabled) return false;
            if (m_overwritePackingTag)
            {
                if (target.spritePackingTag != reference.spritePackingTag) return false;
            }


            if (target.androidETC2FallbackOverride != reference.androidETC2FallbackOverride) return false;
            if (target.compressionQuality          != reference.compressionQuality) return false;
            if (target.crunchedCompression         != reference.crunchedCompression) return false;
            if (target.isReadable                  != reference.isReadable) return false;
            if (target.npotScale                   != reference.npotScale) return false;
            if (target.streamingMipmaps            != reference.streamingMipmaps) return false;
            if (target.streamingMipmapsPriority    != reference.streamingMipmapsPriority) return false;
            if (target.textureCompression          != reference.textureCompression) return false;
            if (target.mipmapEnabled               != reference.mipmapEnabled) return false;


            var refDefault = reference.GetDefaultPlatformTextureSettings();
            var impDefault = target.GetDefaultPlatformTextureSettings();
            if (!CompareImporterPlatformSettings(refDefault, impDefault)) return false;

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups)
            {
                var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName(g, BuildTargetUtility.PlatformNameType.TextureImporter);

                var impSet       = reference.GetPlatformTextureSettings(platformName);
                var targetImpSet = target.GetPlatformTextureSettings(platformName);
                if (!CompareImporterPlatformSettings(impSet, targetImpSet)) return false;
            }


            return true;
        }

        private void OverwriteImportSettings(TextureImporter target, TextureImporter reference, string tagName)
        {
            target.textureType = reference.textureType;

            var targetSetting    = new TextureImporterSettings();
            var referenceSetting = new TextureImporterSettings();

            target.ReadTextureSettings(targetSetting);
            reference.ReadTextureSettings(referenceSetting);

            //以下都是被锁定的图片设置
            targetSetting.aniso                              = referenceSetting.aniso;
            targetSetting.compressionQuality                 = referenceSetting.compressionQuality;
            targetSetting.spriteGenerateFallbackPhysicsShape = referenceSetting.spriteGenerateFallbackPhysicsShape;
            targetSetting.spritePixelsPerUnit                = referenceSetting.spritePixelsPerUnit;
            targetSetting.npotScale                          = referenceSetting.npotScale;
            targetSetting.readable                           = referenceSetting.readable;
            targetSetting.streamingMipmapsPriority           = referenceSetting.streamingMipmapsPriority;
            targetSetting.mipmapEnabled                      = referenceSetting.mipmapEnabled;

            target.SetTextureSettings(targetSetting);

            target.androidETC2FallbackOverride = reference.androidETC2FallbackOverride;
            target.anisoLevel                  = reference.anisoLevel;
            target.compressionQuality          = reference.compressionQuality;
            target.crunchedCompression         = reference.crunchedCompression;
            target.isReadable                  = reference.isReadable;
            target.npotScale                   = reference.npotScale;
            target.streamingMipmapsPriority    = reference.streamingMipmapsPriority;
            target.streamingMipmaps            = reference.streamingMipmaps;
            target.textureCompression          = reference.textureCompression;
            target.mipmapEnabled               = reference.mipmapEnabled;

            if (m_overwritePackingTag)
            {
                if (!string.IsNullOrEmpty(tagName))
                {
                    target.spritePackingTag = tagName;
                }
                else
                {
                    target.spritePackingTag = reference.spritePackingTag;
                }
            }

            var defaultPlatformSetting = reference.GetDefaultPlatformTextureSettings();
            target.SetPlatformTextureSettings(defaultPlatformSetting);

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups)
            {
                var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName(g, BuildTargetUtility.PlatformNameType.TextureImporter);
                var impSet       = reference.GetPlatformTextureSettings(platformName);
                target.SetPlatformTextureSettings(impSet);
            }
        }

        bool CompareImporterPlatformSettings(TextureImporterPlatformSettings c1, TextureImporterPlatformSettings c2)
        {
            if (c1.allowsAlphaSplitting        != c2.allowsAlphaSplitting) return false;
            if (c1.androidETC2FallbackOverride != c2.androidETC2FallbackOverride) return false;
            if (c1.compressionQuality          != c2.compressionQuality) return false;
            if (c1.crunchedCompression         != c2.crunchedCompression) return false;
            if (c1.format                      != c2.format) return false;
            if (c1.maxTextureSize              != c2.maxTextureSize) return false;
            if (c1.name                        != c2.name) return false;
            if (c1.overridden                  != c2.overridden) return false;
            if (c1.resizeAlgorithm             != c2.resizeAlgorithm) return false;
            if (c1.textureCompression          != c2.textureCompression) return false;

            return true;
        }
    }
}