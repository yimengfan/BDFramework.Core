using UnityEditor;

using System;
using System.Collections.Generic;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

    public partial class PrefabBuilderUtility {

        private static void PreserveUserAddedComponentsAndObjects (string prefabSavePath, GameObject generatedPrefab, Action<GameObject, Component> onError) 
        {

            GameObject previousPrefab = (GameObject)AssetDatabase.LoadMainAssetAtPath (prefabSavePath);
            if (previousPrefab == null) {
                return;
            }

            CompareAndRestore (previousPrefab, generatedPrefab, onError);
        }

        /// <summary>
        /// Preserves changes made with source Object and copy to destination.
        /// </summary>
        /// <param name="src">Original game object.</param>
        /// <param name="dst">Destination game object.</param>
        private static void CompareAndRestore(GameObject src, GameObject dst, Action<GameObject, Component> onError) {

//            var src_components = src.GetComponents<Component> ();
//            var dst_components = dst.GetComponents<Component> ();
//
//            if (src_components.Length > dst_components) {
//                for (int i = dst_components.Length; i < src_components.Length; ++i) {
//                    if (ComponentUtility.ReplaceComponentsIfDifferent()) {
//                    }
//                }
//
//
//                for (int i = dst_components.Length; i < src_components.Length; ++i) {
//                    if (ComponentUtility.CopyComponent ()) {
//                    }
//                }
//            }
        }
    }

    public class ComponentUtility
    {
        static private bool CompareComponentOrderAndTypes(List<Component> srcComponents, List<Component> dstComponents)
        {
            if (srcComponents.Count != dstComponents.Count)
                return false;

            for (int i = 0; i != srcComponents.Count; i++)
            {
                if (srcComponents[i].GetType() != dstComponents[i].GetType())
                    return false;
            }

            return true;
        }

        private static void DestroyComponents(List<Component> components)
        {
            // Delete in reverse order (to avoid errors when RequireComponent is used)
            for (int i = components.Count - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(components[i]);
        }

        public delegate bool IsDesiredComponent(Component c);

        public static void DestroyComponentsMatching(GameObject dst, IsDesiredComponent componentFilter)
        {
            var dstComponents = new List<Component>();
            dst.GetComponents(dstComponents);
            dstComponents.RemoveAll(x => !componentFilter(x));
            DestroyComponents(dstComponents);
        }

        public static void ReplaceComponentsIfDifferent(GameObject src, GameObject dst, IsDesiredComponent componentFilter)
        {
            var srcComponents = new List<Component>();
            src.GetComponents(srcComponents);
            srcComponents.RemoveAll(x => !componentFilter(x));

            var dstComponents = new List<Component>();
            dst.GetComponents(dstComponents);
            dstComponents.RemoveAll(x => !componentFilter(x));

            // Generate components
            if (!CompareComponentOrderAndTypes(srcComponents, dstComponents))
            {
                DestroyComponents(dstComponents);

                // Add src components to dst
                dstComponents.Clear();
                for (int i = 0; i != srcComponents.Count; i++)
                {
                    Component com = dst.AddComponent(srcComponents[i].GetType());
                    dstComponents.Add(com);
                }
            }

            // Copy Data to components
            for (int i = 0; i != srcComponents.Count; i++)
                UnityEditor.EditorUtility.CopySerializedIfDifferent(srcComponents[i], dstComponents[i]);
        }
    }
}
