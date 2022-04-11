using UnityEditor;
using System;
using System.Runtime.ConstrainedExecution;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    [Serializable]
    [CustomAssetImporterConfigurator(typeof(ModelImporter), "Model", "setting.fbx")]
    public class ModelImportSettingsConfigurator : IAssetImporterConfigurator
    {
        private const float kTOLERANCE = 0.0001f;
        [SerializeField] private bool m_overwriteAnimationClipSettings;
        [SerializeField] private bool m_overwriteHumanDescriptions;

        private static class Styles
        {
            public static readonly GUIContent kOptionsTopLabel = new GUIContent("Animation Import Overwrite Options");

            public static readonly GUIContent kOverwriteAnimationClipSettingsText = new GUIContent(
                "Overwrite AnimationClip Settings",
                "Overwrite Animation Clip configurations if set to true. Animation Clip configurations will not be overwritten if not importing animations.");

            public static readonly GUIContent kOverwriteHumanDescriptionsText = new GUIContent(
                "Overwrite Human Descriptions",
                "Overwrite Human Descriptions if animation rig type is set to Humanoid. Human Descriptions will not be overwritten if rig is not set to Humanoid.");
        }

        public void Initialize(ConfigurationOption option)
        {
            m_overwriteAnimationClipSettings = option.overwriteAnimationClipSettings;
            m_overwriteHumanDescriptions = option.overwriteHumanDescriptions;
        }

        public bool IsModified(AssetImporter referenceImporter, AssetImporter importer, BuildTarget target,
            string group)
        {
            var r = referenceImporter as ModelImporter;
            var t = importer as ModelImporter;
            if (r == null || t == null)
            {
                throw new AssetGraphException($"Invalid AssetImporter assigned for {importer.assetPath}");
            }

            return !IsEqual(t, r);
        }

        public void Configure(AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as ModelImporter;
            var t = importer as ModelImporter;
            if (r == null || t == null)
            {
                throw new AssetGraphException($"Invalid AssetImporter assigned for {importer.assetPath}");
            }

            OverwriteImportSettings(t, r);
        }

        public void OnInspectorGUI(AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged)
        {
            var importer = referenceImporter as ModelImporter;
            if (importer == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(Styles.kOptionsTopLabel);
                GUILayout.Space(4f);
                using (new EditorGUI.DisabledScope(!importer.importAnimation))
                {
                    var bAnimationClipSettings = EditorGUILayout.ToggleLeft(Styles.kOverwriteAnimationClipSettingsText,
                        m_overwriteAnimationClipSettings);

                    if (bAnimationClipSettings != m_overwriteAnimationClipSettings)
                    {
                        m_overwriteAnimationClipSettings = bAnimationClipSettings;
                        onValueChanged();
                    }
                }

                using (new EditorGUI.DisabledScope(importer.animationType != ModelImporterAnimationType.Human))
                {
                    var bDescriptions =
                        EditorGUILayout.ToggleLeft(Styles.kOverwriteHumanDescriptionsText,
                            m_overwriteHumanDescriptions);

                    if (bDescriptions != m_overwriteHumanDescriptions)
                    {
                        m_overwriteHumanDescriptions = bDescriptions;
                        onValueChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Test if reference importer setting has the equal setting as given target.
        /// ImportSettingsConfigurator will not test read only properties.
        /// 
        /// </summary>
        /// <returns><c>true</c>, if both settings are the equal, <c>false</c> otherwise.</returns>
        /// <param name="target">Target importer to test equality.</param>
        /// <param name="reference">Reference importer to test equality.</param>
        private bool IsEqual(ModelImporter target, ModelImporter reference)
        {
#if UNITY_2019_3_OR_NEWER
            if (target.materialImportMode != reference.materialImportMode)
                return false;
#else
            if (target.importMaterials != reference.importMaterials)
                return false;
#endif
            if (target.importAnimation != reference.importAnimation)
                return false;
            if (target.meshCompression != reference.meshCompression)
                return false;
            if (target.importNormals != reference.importNormals)
                return false;
            if (target.optimizeGameObjects != reference.optimizeGameObjects)
                return false;
            if (target.motionNodeName != reference.motionNodeName)
                return false;
            if (target.useFileUnits != reference.useFileUnits)
                return false;

            if (target.addCollider != reference.addCollider)
                return false;
            if (target.animationCompression != reference.animationCompression)
                return false;
            if (Math.Abs(target.animationPositionError - reference.animationPositionError) > kTOLERANCE)
                return false;
            if (Math.Abs(target.animationRotationError - reference.animationRotationError) > kTOLERANCE)
                return false;
            if (Math.Abs(target.animationScaleError - reference.animationScaleError) > kTOLERANCE)
                return false;
            if (target.animationType != reference.animationType)
                return false;
            if (target.animationWrapMode != reference.animationWrapMode)
                return false;
#if UNITY_2019_3_OR_NEWER
            if (target.avatarSetup != reference.avatarSetup)
                return false;
#endif

            if (target.bakeIK != reference.bakeIK)
                return false;

            // clipAnimations
            if (reference.importAnimation && m_overwriteAnimationClipSettings)
            {
                if (target.clipAnimations.Length != reference.clipAnimations.Length)
                    return false;
                for (int i = 0; i < target.clipAnimations.Length; i++)
                {
                    if (Math.Abs(target.clipAnimations[i].additiveReferencePoseFrame -
                                 reference.clipAnimations[i].additiveReferencePoseFrame) > kTOLERANCE)
                        return false;
                    if (target.clipAnimations[i].curves != reference.clipAnimations[i].curves)
                        return false;
                    if (Math.Abs(target.clipAnimations[i].cycleOffset - reference.clipAnimations[i].cycleOffset) >
                        kTOLERANCE)
                        return false;
                    if (target.clipAnimations[i].events != reference.clipAnimations[i].events)
                        return false;
                    if (Math.Abs(target.clipAnimations[i].firstFrame - reference.clipAnimations[i].firstFrame) >
                        kTOLERANCE)
                        return false;
                    if (target.clipAnimations[i].hasAdditiveReferencePose !=
                        reference.clipAnimations[i].hasAdditiveReferencePose)
                        return false;
                    if (target.clipAnimations[i].heightFromFeet != reference.clipAnimations[i].heightFromFeet)
                        return false;
                    if (Math.Abs(target.clipAnimations[i].heightOffset - reference.clipAnimations[i].heightOffset) >
                        kTOLERANCE)
                        return false;
                    if (target.clipAnimations[i].keepOriginalOrientation !=
                        reference.clipAnimations[i].keepOriginalOrientation)
                        return false;
                    if (target.clipAnimations[i].keepOriginalPositionXZ !=
                        reference.clipAnimations[i].keepOriginalPositionXZ)
                        return false;
                    if (target.clipAnimations[i].keepOriginalPositionY !=
                        reference.clipAnimations[i].keepOriginalPositionY)
                        return false;
                    if (Math.Abs(target.clipAnimations[i].lastFrame - reference.clipAnimations[i].lastFrame) >
                        kTOLERANCE)
                        return false;
                    if (target.clipAnimations[i].lockRootHeightY != reference.clipAnimations[i].lockRootHeightY)
                        return false;
                    if (target.clipAnimations[i].lockRootPositionXZ != reference.clipAnimations[i].lockRootPositionXZ)
                        return false;
                    if (target.clipAnimations[i].lockRootRotation != reference.clipAnimations[i].lockRootRotation)
                        return false;
                    if (target.clipAnimations[i].loop != reference.clipAnimations[i].loop)
                        return false;
                    if (target.clipAnimations[i].loopPose != reference.clipAnimations[i].loopPose)
                        return false;
                    if (target.clipAnimations[i].loopTime != reference.clipAnimations[i].loopTime)
                        return false;
                    if (target.clipAnimations[i].maskNeedsUpdating != reference.clipAnimations[i].maskNeedsUpdating)
                        return false;
                    if (target.clipAnimations[i].maskSource != reference.clipAnimations[i].maskSource)
                        return false;
                    if (target.clipAnimations[i].maskType != reference.clipAnimations[i].maskType)
                        return false;
                    if (target.clipAnimations[i].mirror != reference.clipAnimations[i].mirror)
                        return false;
                    if (target.clipAnimations[i].name != reference.clipAnimations[i].name)
                        return false;
                    if (Math.Abs(target.clipAnimations[i].rotationOffset - reference.clipAnimations[i].rotationOffset) >
                        kTOLERANCE)
                        return false;
                    if (target.clipAnimations[i].takeName != reference.clipAnimations[i].takeName)
                        return false;
                    if (target.clipAnimations[i].wrapMode != reference.clipAnimations[i].wrapMode)
                        return false;
                }
            }

            // extraExposedTransformPaths
            {
                if (target.extraExposedTransformPaths.Length != reference.extraExposedTransformPaths.Length)
                    return false;
                for (int i = 0; i < target.extraExposedTransformPaths.Length; i++)
                {
                    if (target.extraExposedTransformPaths[i] != reference.extraExposedTransformPaths[i])
                        return false;
                }
            }

            if (target.generateAnimations != reference.generateAnimations)
                return false;
            if (target.generateSecondaryUV != reference.generateSecondaryUV)
                return false;
            if (Math.Abs(target.globalScale - reference.globalScale) > kTOLERANCE)
                return false;

            // humanDescription
            if (reference.animationType == ModelImporterAnimationType.Human && m_overwriteHumanDescriptions)
            {
                if (Math.Abs(target.humanDescription.armStretch - reference.humanDescription.armStretch) > kTOLERANCE)
                    return false;
                if (Math.Abs(target.humanDescription.feetSpacing - reference.humanDescription.feetSpacing) > kTOLERANCE)
                    return false;
                if (target.humanDescription.hasTranslationDoF != reference.humanDescription.hasTranslationDoF)
                    return false;

                // human
                {
                    if (target.humanDescription.human.Length != reference.humanDescription.human.Length)
                        return false;
                    for (int i = 0; i < target.humanDescription.human.Length; i++)
                    {
                        if (target.humanDescription.human[i].boneName != reference.humanDescription.human[i].boneName)
                            return false;
                        if (target.humanDescription.human[i].humanName != reference.humanDescription.human[i].humanName)
                            return false;

                        // limit
                        if (Math.Abs(target.humanDescription.human[i].limit.axisLength -
                                     reference.humanDescription.human[i].limit.axisLength) > kTOLERANCE)
                            return false;
                        if (target.humanDescription.human[i].limit.center !=
                            reference.humanDescription.human[i].limit.center)
                            return false;
                        if (target.humanDescription.human[i].limit.max != reference.humanDescription.human[i].limit.max)
                            return false;
                        if (target.humanDescription.human[i].limit.min != reference.humanDescription.human[i].limit.min)
                            return false;
                        if (target.humanDescription.human[i].limit.useDefaultValues !=
                            reference.humanDescription.human[i].limit.useDefaultValues)
                            return false;
                    }
                }

                if (Math.Abs(target.humanDescription.legStretch - reference.humanDescription.legStretch) > kTOLERANCE)
                    return false;
                if (Math.Abs(target.humanDescription.lowerArmTwist - reference.humanDescription.lowerArmTwist) >
                    kTOLERANCE)
                    return false;
                if (Math.Abs(target.humanDescription.lowerLegTwist - reference.humanDescription.lowerLegTwist) >
                    kTOLERANCE)
                    return false;

                // skeleton
                {
                    if (target.humanDescription.skeleton.Length != reference.humanDescription.skeleton.Length)
                        return false;
                    for (int i = 0; i < target.humanDescription.skeleton.Length; i++)
                    {
                        if (target.humanDescription.skeleton[i].name != reference.humanDescription.skeleton[i].name)
                            return false;
                        if (target.humanDescription.skeleton[i].position !=
                            reference.humanDescription.skeleton[i].position)
                            return false;
                        if (target.humanDescription.skeleton[i].rotation !=
                            reference.humanDescription.skeleton[i].rotation)
                            return false;
                        if (target.humanDescription.skeleton[i].scale != reference.humanDescription.skeleton[i].scale)
                            return false;
                    }
                }

                if (Math.Abs(target.humanDescription.upperArmTwist - reference.humanDescription.upperArmTwist) >
                    kTOLERANCE)
                    return false;
                if (Math.Abs(target.humanDescription.upperLegTwist - reference.humanDescription.upperLegTwist) >
                    kTOLERANCE)
                    return false;
            }

            if (target.humanoidOversampling != reference.humanoidOversampling)
                return false;
            if (target.importBlendShapeNormals != reference.importBlendShapeNormals)
                return false;
            if (target.importBlendShapes != reference.importBlendShapes)
                return false;
            if (target.isReadable != reference.isReadable)
                return false;
            if (target.materialName != reference.materialName)
                return false;
            if (target.materialSearch != reference.materialSearch)
                return false;

#if UNITY_2019_2_OR_NEWER
            if (target.maxBonesPerVertex != reference.maxBonesPerVertex)
                return false;
            if (target.meshOptimizationFlags != reference.meshOptimizationFlags)
                return false;
            if (Math.Abs(target.minBoneWeight - reference.minBoneWeight) > kTOLERANCE)
                return false;
            if (target.skinWeights != reference.skinWeights)
                return false;
            if (target.sortHierarchyByName != reference.sortHierarchyByName)
                return false;
#endif

            if (Math.Abs(target.normalSmoothingAngle - reference.normalSmoothingAngle) > kTOLERANCE)
                return false;

#if UNITY_2019_1_OR_NEWER
            if (target.optimizeMeshPolygons != reference.optimizeMeshPolygons)
                return false;
            if (target.optimizeMeshVertices != reference.optimizeMeshVertices)
                return false;
#else
            if (target.optimizeMesh != reference.optimizeMesh)
                return false;
#endif

            if (target.indexFormat != reference.indexFormat)
                return false;
            if (target.materialLocation != reference.materialLocation)
                return false;
            if (target.normalSmoothingSource != reference.normalSmoothingSource)
                return false;
            if (target.preserveHierarchy != reference.preserveHierarchy)
                return false;
            if (target.resampleCurves != reference.resampleCurves)
                return false;
            if (target.useSRGBMaterialColor != reference.useSRGBMaterialColor)
                return false;


            if (Math.Abs(target.secondaryUVAngleDistortion - reference.secondaryUVAngleDistortion) > kTOLERANCE)
                return false;
            if (Math.Abs(target.secondaryUVAreaDistortion - reference.secondaryUVAreaDistortion) > kTOLERANCE)
                return false;
            if (Math.Abs(target.secondaryUVHardAngle - reference.secondaryUVHardAngle) > kTOLERANCE)
                return false;
            if (Math.Abs(target.secondaryUVPackMargin - reference.secondaryUVPackMargin) > kTOLERANCE)
                return false;
            if (target.sourceAvatar != reference.sourceAvatar)
                return false;
            if (target.swapUVChannels != reference.swapUVChannels)
                return false;
            if (target.importTangents != reference.importTangents)
                return false;

            if (target.keepQuads != reference.keepQuads)
                return false;
            if (target.weldVertices != reference.weldVertices)
                return false;

            if (target.importCameras != reference.importCameras)
                return false;
            if (target.importLights != reference.importLights)
                return false;
            if (target.normalCalculationMode != reference.normalCalculationMode)
                return false;
            if (target.importVisibility != reference.importVisibility)
                return false;
            if (target.useFileScale != reference.useFileScale)
                return false;

            if (target.extraUserProperties.Length != reference.extraUserProperties.Length)
                return false;
            for (int i = 0; i < target.extraUserProperties.Length; ++i)
            {
                if (target.extraUserProperties[i] != reference.extraUserProperties[i])
                    return false;
            }

            if (target.importAnimatedCustomProperties != reference.importAnimatedCustomProperties)
            {
                return false;
            }

            if (target.importConstraints != reference.importConstraints)
            {
                return false;
            }

            return true;
        }

        private void OverwriteImportSettings(ModelImporter target, ModelImporter reference)
        {
            target.addCollider = reference.addCollider;
            target.animationCompression = reference.animationCompression;
            target.animationPositionError = reference.animationPositionError;
            target.animationRotationError = reference.animationRotationError;
            target.animationScaleError = reference.animationScaleError;

            target.animationType = reference.animationType;
            target.animationWrapMode = reference.animationWrapMode;
#if UNITY_2019_3_OR_NEWER
            target.avatarSetup = reference.avatarSetup;
#endif

            target.bakeIK = reference.bakeIK;
            if (reference.importAnimation && m_overwriteAnimationClipSettings)
            {
                target.clipAnimations = reference.clipAnimations;
            }

            target.extraExposedTransformPaths = reference.extraExposedTransformPaths;
            target.extraUserProperties = reference.extraUserProperties;

            target.generateAnimations = reference.generateAnimations;
            target.generateSecondaryUV = reference.generateSecondaryUV;
            target.globalScale = reference.globalScale;
            if (reference.animationType == ModelImporterAnimationType.Human && m_overwriteHumanDescriptions)
            {
                target.humanDescription = reference.humanDescription;
            }

            target.humanoidOversampling = reference.humanoidOversampling;

            target.importAnimatedCustomProperties = reference.importAnimatedCustomProperties;
            target.importAnimation = reference.importAnimation;
            target.importBlendShapeNormals = reference.importBlendShapeNormals;
            target.importBlendShapes = reference.importBlendShapes;
            target.importCameras = reference.importCameras;
            target.importConstraints = reference.importConstraints;

            target.importLights = reference.importLights;
#if UNITY_2019_3_OR_NEWER
            target.materialImportMode = reference.materialImportMode;
#else
            target.importMaterials = reference.importMaterials;
#endif
            target.importNormals = reference.importNormals;
            target.importTangents = reference.importTangents;
            target.importVisibility = reference.importVisibility;
            target.indexFormat = reference.indexFormat;

            target.isReadable = reference.isReadable;
            target.keepQuads = reference.keepQuads;
            target.materialLocation = reference.materialLocation;
            target.materialName = reference.materialName;
            target.materialSearch = reference.materialSearch;

#if UNITY_2019_2_OR_NEWER
            target.maxBonesPerVertex = reference.maxBonesPerVertex;
#endif

            target.meshCompression = reference.meshCompression;

#if UNITY_2019_2_OR_NEWER
            target.meshOptimizationFlags = reference.meshOptimizationFlags;
            target.minBoneWeight = reference.minBoneWeight;
#endif
            target.motionNodeName = reference.motionNodeName;

            target.normalCalculationMode = reference.normalCalculationMode;
            target.normalSmoothingAngle = reference.normalSmoothingAngle;
            target.normalSmoothingSource = reference.normalSmoothingSource;
            target.optimizeGameObjects = reference.optimizeGameObjects;
#if UNITY_2019_1_OR_NEWER
            target.optimizeMeshPolygons = reference.optimizeMeshPolygons;
            target.optimizeMeshVertices = reference.optimizeMeshVertices;
#else
            target.optimizeMesh = reference.optimizeMesh;
#endif


            target.preserveHierarchy = reference.preserveHierarchy;
            target.resampleCurves = reference.resampleCurves;

            target.secondaryUVAngleDistortion = reference.secondaryUVAngleDistortion;
            target.secondaryUVAreaDistortion = reference.secondaryUVAreaDistortion;
            target.secondaryUVHardAngle = reference.secondaryUVHardAngle;
            target.secondaryUVPackMargin = reference.secondaryUVPackMargin;

#if UNITY_2019_2_OR_NEWER
            target.skinWeights = reference.skinWeights;
            target.sortHierarchyByName = reference.sortHierarchyByName;
#endif

            target.sourceAvatar = reference.sourceAvatar;
            target.swapUVChannels = reference.swapUVChannels;
            target.useFileScale = reference.useFileScale;
            target.useFileUnits = reference.useFileUnits;
            target.useSRGBMaterialColor = reference.useSRGBMaterialColor;
            target.weldVertices = reference.weldVertices;
        }
    }
}