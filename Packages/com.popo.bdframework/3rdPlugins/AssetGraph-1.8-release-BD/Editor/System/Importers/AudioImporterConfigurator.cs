using UnityEditor;

using System;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    [Serializable] 
    [CustomAssetImporterConfigurator(typeof(AudioImporter), "Audio", "setting.wav")]
    public class AudioImportSettingsConfigurator : IAssetImporterConfigurator
    {
        public void Initialize (ConfigurationOption option)
        {
        }

        public bool IsModified (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as AudioImporter;
            var t = importer as AudioImporter;
            if (r == null || t == null) {
                throw new AssetGraphException ($"Invalid AssetImporter assigned for {importer.assetPath}");
            }
            return !IsEqual (t, r);
        }

        public void Configure (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as AudioImporter;
            var t = importer as AudioImporter;
            if (r == null || t == null) {
                throw new AssetGraphException ($"Invalid AssetImporter assigned for {importer.assetPath}");
            }
            OverwriteImportSettings (t, r);
        }

        public void OnInspectorGUI (AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged)
        {
        }

        private void OverwriteImportSettings (AudioImporter target, AudioImporter reference)
        {
            target.defaultSampleSettings = reference.defaultSampleSettings;
            target.forceToMono = reference.forceToMono;
            target.preloadAudioData = reference.preloadAudioData;

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups) {
                var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName (g,
                                       BuildTargetUtility.PlatformNameType.AudioImporter);

                if (reference.ContainsSampleSettingsOverride (platformName)) {
                    var setting = reference.GetOverrideSampleSettings (platformName);
                    if (!target.SetOverrideSampleSettings (platformName, setting)) {
                        LogUtility.Logger.LogError ("AudioImporter",
                            $"Failed to set override setting for {platformName}: {target.assetPath}");
                    }
                } else {
                    target.ClearSampleSettingOverride (platformName);
                }
            }

            target.loadInBackground = reference.loadInBackground;
            target.ambisonic = reference.ambisonic;
        }

        private bool IsEqual (AudioImporter target, AudioImporter reference)
        {
            UnityEngine.Assertions.Assert.IsNotNull (reference);

            if (!IsEqualAudioSampleSetting (target.defaultSampleSettings, reference.defaultSampleSettings)) {
                return false;
            }

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups) {
                var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName (g,
                                       BuildTargetUtility.PlatformNameType.AudioImporter);

                if (target.ContainsSampleSettingsOverride (platformName) !=
                    reference.ContainsSampleSettingsOverride (platformName)) {
                    return false;
                }
                if (target.ContainsSampleSettingsOverride (platformName)) {
                    var t = target.GetOverrideSampleSettings (platformName);
                    var r = reference.GetOverrideSampleSettings (platformName);
                    if (!IsEqualAudioSampleSetting (t, r)) {
                        return false;
                    }
                }
            }

            if (target.forceToMono != reference.forceToMono)
                return false;
            if (target.loadInBackground != reference.loadInBackground)
                return false;
            if (target.ambisonic != reference.ambisonic)
                return false;
            if (target.preloadAudioData != reference.preloadAudioData)
                return false;

            return true;
        }

        private bool IsEqualAudioSampleSetting (AudioImporterSampleSettings target, AudioImporterSampleSettings reference)
        {
            // defaultSampleSettings
            if (target.compressionFormat != reference.compressionFormat)
                return false;
            if (target.loadType != reference.loadType)
                return false;
            if (target.quality != reference.quality)
                return false;
            if (target.sampleRateOverride != reference.sampleRateOverride)
                return false;
            if (target.sampleRateSetting != reference.sampleRateSetting)
                return false;

            return true;
        }
    }
}
