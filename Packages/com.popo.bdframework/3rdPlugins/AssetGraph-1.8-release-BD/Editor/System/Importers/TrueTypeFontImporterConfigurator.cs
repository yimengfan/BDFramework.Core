using UnityEditor;

using System;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    [Serializable] 
    [CustomAssetImporterConfigurator(typeof(TrueTypeFontImporter), "Font", "setting.ttf")]
    public class TrueTypeFontImportSettingsConfigurator : IAssetImporterConfigurator
    {
        public void Initialize (ConfigurationOption option)
        {
        }

        public bool IsModified (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as TrueTypeFontImporter;
            var t = importer as TrueTypeFontImporter;
            if (r == null || t == null) {
                throw new AssetGraphException ($"Invalid AssetImporter assigned for {importer.assetPath}");
            }
            return !IsEqual (t, r);
        }

        public void Configure (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as TrueTypeFontImporter;
            var t = importer as TrueTypeFontImporter;
            if (r == null || t == null) {
                throw new AssetGraphException ($"Invalid AssetImporter assigned for {importer.assetPath}");
            }
            OverwriteImportSettings (t, r);
        }

        public void OnInspectorGUI (AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged)
        {
        }

        private void OverwriteImportSettings (TrueTypeFontImporter target, TrueTypeFontImporter reference)
        {
            target.ascentCalculationMode = reference.ascentCalculationMode;
            target.characterPadding = reference.characterPadding;
            target.characterSpacing = reference.characterSpacing;
            target.customCharacters = reference.customCharacters;
            target.fontNames = reference.fontNames;
            target.fontReferences = reference.fontReferences;
            target.fontRenderingMode = reference.fontRenderingMode;
            target.fontSize = reference.fontSize;
            target.fontTextureCase = reference.fontTextureCase;
//            target.fontTTFName = reference.fontTTFName;
            target.includeFontData = reference.includeFontData;
            target.shouldRoundAdvanceValue = reference.shouldRoundAdvanceValue;
        }

        private bool IsEqual (TrueTypeFontImporter target, TrueTypeFontImporter reference)
        {
            if (target.ascentCalculationMode != reference.ascentCalculationMode) {
                return false;
            }

            if (target.characterPadding != reference.characterPadding) {
                return false;
            }

            if (target.characterSpacing != reference.characterSpacing) {
                return false;
            }

            if (target.customCharacters != reference.customCharacters) {
                return false;
            }

            if (target.fontNames.Length != reference.fontNames.Length) {
                return false;
            }

            for (int i = 0; i < target.fontNames.Length; ++i) {
                if (target.fontNames [i] != reference.fontNames [i]) {
                    return false;
                }
            }

            if (target.fontReferences.Length != reference.fontReferences.Length) {
                return false;
            }

            for (int i = 0; i < target.fontNames.Length; ++i) {
                if (!target.fontReferences [i].Equals(reference.fontReferences [i])) {
                    return false;
                }
            }

            if (target.fontRenderingMode != reference.fontRenderingMode) {
                return false;
            }

            if (target.fontSize != reference.fontSize) {
                return false;
            }

            if (target.fontTextureCase != reference.fontTextureCase) {
                return false;
            }

            if (target.shouldRoundAdvanceValue != reference.shouldRoundAdvanceValue) {
                return false;
            }

            return true;
        }
    }
}
