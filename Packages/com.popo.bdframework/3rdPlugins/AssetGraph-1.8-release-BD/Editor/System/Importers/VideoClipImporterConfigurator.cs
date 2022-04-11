using UnityEditor;
using System;

using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    [Serializable] 
    [CustomAssetImporterConfigurator(typeof(VideoClipImporter), "Video", "setting.m4v")]
    public class VideoClipImportSettingsConfigurator : IAssetImporterConfigurator
    {
        public void Initialize (ConfigurationOption option)
        {
        }

        public bool IsModified (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as VideoClipImporter;
            var t = importer as VideoClipImporter;
            if (r == null || t == null) {
                throw new AssetGraphException ($"Invalid AssetImporter assigned for {importer.assetPath}");
            }
            return !IsEqual (t, r);
        }

        public void Configure (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as VideoClipImporter;
            var t = importer as VideoClipImporter;
            if (r == null || t == null) {
                throw new AssetGraphException ($"Invalid AssetImporter assigned for {importer.assetPath}");
            }
            OverwriteImportSettings (t, r);
        }

        public void OnInspectorGUI (AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged)
        {
        }

        private bool IsEqual (VideoClipImporter target, VideoClipImporter reference)
        {
            if (!CompareVideoImporterTargetSettings (target.defaultTargetSettings, reference.defaultTargetSettings))
                return false;

            /* read only properties. ImportSettingConfigurator will not use these properties for diff. */
            /* 
            importer.frameCount             
            importer.frameRate              
            importer.isPlayingPreview       
            importer.outputFileSize         
            importer.sourceAudioTrackCount  
            importer.sourceFileSize         
            importer.sourceHasAlpha         
            */

            if (target.deinterlaceMode != reference.deinterlaceMode)
                return false;
            if (target.flipHorizontal != reference.flipHorizontal)
                return false;
            if (target.flipVertical != reference.flipVertical)
                return false;
            if (target.importAudio != reference.importAudio)
                return false;
            if (target.keepAlpha != reference.keepAlpha)
                return false;
            #if !UNITY_2019_3_OR_NEWER
            if (target.linearColor != reference.linearColor)
                return false;
            if (target.quality != reference.quality)
                return false;
            if (target.useLegacyImporter != reference.useLegacyImporter)
                return false;
            #endif

            if (target.pixelAspectRatioDenominator != reference.pixelAspectRatioDenominator) return false;
            if (target.pixelAspectRatioNumerator != reference.pixelAspectRatioNumerator) return false;

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups) {
                var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName (g, 
                                       BuildTargetUtility.PlatformNameType.VideoClipImporter);

                try {
                    var r = reference.GetTargetSettings (platformName);
                    var t = target.GetTargetSettings (platformName);

                    // if both targets are null - keep going
                    if (r == null && t == null) {
                        continue;
                    }

                    if (r == null || t == null) {
                        return false;
                    }

                    if (!CompareVideoImporterTargetSettings (r, t)) {
                        return false;
                    }

                } catch (Exception e) {
                    LogUtility.Logger.LogError ("VideoClipImporter", 
                        string.Format ("Failed to test equality setting for {0}: file :{1} type:{3} reason:{2}", 
                            platformName, target.assetPath, e.Message, e.GetType ()));
                }
            }

            return true;
        }

        private static void OverwriteImportSettings (VideoClipImporter target, VideoClipImporter reference)
        {
            target.defaultTargetSettings = reference.defaultTargetSettings;
            target.deinterlaceMode = reference.deinterlaceMode;
            target.flipHorizontal = reference.flipHorizontal;
            target.flipVertical = reference.flipVertical;
            target.importAudio = reference.importAudio;
            target.keepAlpha = reference.keepAlpha;
            
            #if !UNITY_2019_3_OR_NEWER
            target.linearColor = reference.linearColor;
            target.quality = reference.quality;
            target.useLegacyImporter = reference.useLegacyImporter;
            #endif

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups) {
                var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName (g, 
                                       BuildTargetUtility.PlatformNameType.VideoClipImporter);

                try {
                    var setting = reference.GetTargetSettings (platformName);
                    if (setting != null) {
                        target.SetTargetSettings (platformName, setting);
                    } else {
                        target.ClearTargetSettings (platformName);
                    }
                } catch (Exception e) {
                    LogUtility.Logger.LogWarning ("VideoClipImporter",
                        $"Failed to set override setting for platform {platformName}: file :{target.assetPath} \\nreason:{e.Message}");
                }
            }

            /* read only */
            /* 
            importer.frameCount             
            importer.frameRate              
            importer.isPlayingPreview       
            importer.outputFileSize         
            importer.sourceAudioTrackCount  
            importer.sourceFileSize         
            importer.sourceHasAlpha         
            importer.pixelAspectRatioDenominator
            importer.pixelAspectRatioNumerator
            */
        }

        private static bool CompareVideoImporterTargetSettings (VideoImporterTargetSettings t, VideoImporterTargetSettings r)
        {

            if (r == null) {
                if (t != null) {
                    return false;
                }
            }

            if (r.aspectRatio != t.aspectRatio)
                return false;
            if (r.bitrateMode != t.bitrateMode)
                return false;
            if (r.codec != t.codec)
                return false;
            if (r.customHeight != t.customHeight)
                return false;
            if (r.customWidth != t.customWidth)
                return false;
            if (r.enableTranscoding != t.enableTranscoding)
                return false;
            if (r.resizeMode != t.resizeMode)
                return false;
            if (r.spatialQuality != t.spatialQuality)
                return false;
            return true;
        }
    }
}
